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

			// If the game is running, make sure the hot-reload DLL is injected and fire the in-place reload
			// (evict [handle+4] + synchronous re-parse on the render thread, then rebuild + LockRect the
			// textures) — room/costume metadata (.room.xml/.costume.xml) and .dxt with no room change.
			if( GameLauncher.FocusIfRunning() )
			{
				// The hook must be active BEFORE a room loads to capture that room's resource handles.
				// If it was already injected (at the menu, on a prior launch), the current room's handles
				// were captured on entry and everything reloads. If we inject only now, mid-room, those
				// handles were loaded before the hook, so .room.xml still reloads (read from a fixed
				// address) but costumes/textures need one room re-entry to stream through the hook.
				var wasActive = HotReloadClient.IsAvailable();
				HotReloadInjector.TryEnsureInjected( out var injectMessage );
				HotReloadClient.TrySignal();

				return wasActive
					? CommandResult.Success( summary + "Hot-reloaded the running game." )
					: CommandResult.Success( summary + injectMessage
						+ " — .room.xml reloaded; re-enter the room once so costumes/textures are captured, then Test in game again." );
			}

			// Not running — launch it and inject at the MENU (before any room loads), in the background,
			// so every resource including costumes is captured as the room streams in.
			string outcome;
			try
			{
				outcome = GameLauncher.LaunchOrFocus( pakDirectory );
			}
			catch( Exception exception )
			{
				return CommandResult.Fail( "Overrides written, but the game could not be started: " + exception.Message );
			}

			HotReloadInjector.InjectWhenReady();
			return CommandResult.Success( summary + outcome + " Hot-reload arms at the menu — enter the room, then Test in game to reload your edits." );
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
