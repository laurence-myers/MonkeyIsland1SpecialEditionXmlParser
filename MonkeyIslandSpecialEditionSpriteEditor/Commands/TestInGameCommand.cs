using System;
using System.Collections.Generic;
using System.IO;
using MonkeyIslandSpecialEditionSpriteEditor.PlayTest;
using MonkeyIslandSpecialEditionSpriteEditor.UI;

namespace MonkeyIslandSpecialEditionSpriteEditor.Commands
{
	/// <summary>
	/// The play-test loop in one action: writes every unsaved room and costume edit in the open
	/// editors as loose overrides (texture imports are already on disk), then launches the real
	/// Special Edition game or brings the running one forward. The game re-reads a room's overrides
	/// each time the room is entered, so the modder just walks out and back in to see the change.
	/// </summary>
	public class TestInGameCommand( string? pakDirectory ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			if( string.IsNullOrWhiteSpace( pakDirectory ) || !Directory.Exists( pakDirectory ) )
			{
				return CommandResult.Fail( "Open a pak first - the game folder is where the overrides go." );
			}

			var written = new List<string>();
			var failed = new List<string>();

			foreach( var editor in SpriteSheetEditorForm.Instances )
			{
				Collect( editor.IsDirty, editor.SaveOverrideIfDirty( silent: true ), editor.ResourcePath, written, failed );
			}

			foreach( var editor in CostumeSpriteSheetEditorForm.Instances )
			{
				Collect( editor.IsDirty, editor.SaveOverrideIfDirty( silent: true ), editor.ResourcePath, written, failed );
			}

			if( failed.Count > 0 )
			{
				return CommandResult.Fail( "Could not write: " + string.Join( ", ", failed ) + " - fix that before testing." );
			}

			var summary = written.Count == 0
				? "No unsaved edits (textures are written on import). "
				: string.Concat( "Wrote ", written.Count, " override", written.Count == 1 ? "" : "s", ": ", string.Join( ", ", written ), ". " );

			// If the game is running, auto-inject mise-mreload.dll (once per session) and fire the in-place
			// hot-reload: evict [handle+4] + synchronous re-parse on the render thread, then rebuild +
			// LockRect the textures. Reloads room/costume metadata (.room.xml/.costume.xml) and textures
			// (.dxt) with no room change or interpreter disruption.
			if( GameLauncher.FocusIfRunning() )
			{
				HotReloadInjector.TryEnsureInjected( out var injectMessage );
				if( HotReloadClient.TrySignal() )
				{
					return CommandResult.Success( summary + "Hot-reloaded the running game." );
				}

				// injected just now (or could not) but nothing reloaded yet — usually not in a room
				return CommandResult.Success( summary + injectMessage + " — enter the room (scroll it once), then Test in game." );
			}

			// Not running — launch it; the DLL gets injected on the next Test in game once the game is up.
			string outcome;
			try
			{
				outcome = GameLauncher.LaunchOrFocus( pakDirectory );
			}
			catch( Exception exception )
			{
				return CommandResult.Fail( "Overrides written, but the game could not be started: " + exception.Message );
			}
			return CommandResult.Success( summary + outcome + " Test in game again once you are in the room." );
		}

		private static void Collect( bool wasDirty, bool ok, string? resourcePath, List<string> written, List<string> failed )
		{
			if( !wasDirty )
			{
				return;
			}

			var name = string.IsNullOrEmpty( resourcePath ) ? "?" : Path.GetFileName( resourcePath );
			( ok ? written : failed ).Add( name );
		}
	}
}
