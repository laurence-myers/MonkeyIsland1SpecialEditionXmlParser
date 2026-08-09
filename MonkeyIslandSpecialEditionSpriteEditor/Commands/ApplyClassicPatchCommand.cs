using System;
using System.IO;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm;

namespace MonkeyIslandSpecialEditionSpriteEditor.Commands
{
	/// <summary>
	/// Applies a redistributable walkbox patch to the user's own game data, writing the result as
	/// a loose classic-data override beside the pak (the pak stays pristine). Refuses to run when
	/// the patch was built for different game data, or when a target room has been changed by
	/// something else.
	/// </summary>
	public class ApplyClassicPatchCommand( LPAKFile? lpakFile, string? patchPath ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			if( lpakFile == null || string.IsNullOrWhiteSpace( lpakFile.FileNameOnDisk ) )
			{
				return CommandResult.Fail( "Invalid LPAK file" );
			}
			if( string.IsNullOrWhiteSpace( patchPath ) || !File.Exists( patchPath ) )
			{
				return CommandResult.Fail( "Patch file not found." );
			}

			ClassicPatch patch;
			try
			{
				patch = Helper.ReadObjectFromFile<ClassicPatch>( patchPath! );
			}
			catch( Exception exception )
			{
				return CommandResult.Fail( "Could not read the patch file: " + exception.Message );
			}

			var validationError = patch.Validate();
			if( validationError != null )
			{
				return CommandResult.Fail( validationError );
			}

			var dataIndex = ClassicOverride.ResourceEntryIndex( lpakFile );
			var overridePath = ClassicOverride.ResourceOverridePath( lpakFile );
			if( dataIndex < 0 || overridePath == null )
			{
				return CommandResult.Fail( "This pak has no classic data (.001) to patch." );
			}
			if( ClassicOverride.ResourceEntryIsCompressed( lpakFile, dataIndex ) )
			{
				return CommandResult.Fail( "The classic data is compressed in this pak, which the walk box patch tools do not support." );
			}

			byte[] pristine;
			byte[] current;
			try
			{
				pristine = lpakFile.ReadEntryBytes( dataIndex );
				current = File.Exists( overridePath ) ? File.ReadAllBytes( overridePath ) : pristine;
			}
			catch( Exception exception )
			{
				return CommandResult.Fail( "Could not read the classic data: " + exception.Message );
			}

			var result = ClassicPatcher.Apply( pristine, current, patch );
			if( !result.Ok )
			{
				return CommandResult.Fail( result.Message );
			}
			if( !result.Changed )
			{
				return CommandResult.Success( result.Message );
			}

			var pakDirectory = Path.GetDirectoryName( lpakFile.FileNameOnDisk );
			if( pakDirectory != null )
			{
				var extractError = ClassicOverride.ExtractIndexSibling( lpakFile, pakDirectory );
				if( extractError != null )
				{
					return CommandResult.Fail( extractError );
				}
			}

			try
			{
				ClassicOverride.AtomicWrite( overridePath, result.EncodedBytes! );
			}
			catch( Exception exception )
			{
				return CommandResult.Fail( "Failed to write the patched classic data: " + exception.Message );
			}

			ClassicDataLocator.Invalidate( lpakFile.FileNameOnDisk );
			return CommandResult.Success( string.Concat( result.Message, " Reopen the room to see the change." ) );
		}
	}
}
