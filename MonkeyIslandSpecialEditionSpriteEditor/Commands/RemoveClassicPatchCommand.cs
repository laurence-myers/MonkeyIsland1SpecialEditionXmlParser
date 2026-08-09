using System;
using System.IO;
using System.Linq;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm;

namespace MonkeyIslandSpecialEditionSpriteEditor.Commands
{
	/// <summary>
	/// Removes a previously applied walkbox patch, restoring the patched rooms to the user's
	/// pristine game data while leaving edits to other rooms intact. When the result is identical
	/// to the pristine pak, the loose override is deleted so the pak loads directly.
	/// </summary>
	public class RemoveClassicPatchCommand( LPAKFile? lpakFile, string? patchPath ) : BaseCommand
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
				return CommandResult.Fail( "This pak has no classic data (.001)." );
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

			var result = ClassicPatcher.Remove( pristine, current, patch );
			if( !result.Ok )
			{
				return CommandResult.Fail( result.Message );
			}
			if( !result.Changed )
			{
				return CommandResult.Success( result.Message );
			}

			try
			{
				if( result.EncodedBytes!.SequenceEqual( pristine ) )
				{
					// the override now equals the pristine pak, so drop the loose files entirely
					DeleteOverrideFiles( lpakFile, overridePath );
				}
				else
				{
					ClassicOverride.AtomicWrite( overridePath, result.EncodedBytes! );
				}
			}
			catch( Exception exception )
			{
				return CommandResult.Fail( "Failed to update the classic data: " + exception.Message );
			}

			ClassicDataLocator.Invalidate( lpakFile.FileNameOnDisk );
			return CommandResult.Success( string.Concat( result.Message, " Reopen the room to see the change." ) );
		}

		private static void DeleteOverrideFiles( LPAKFile lpakFile, string resourceOverridePath )
		{
			if( File.Exists( resourceOverridePath ) )
			{
				File.Delete( resourceOverridePath );
			}

			// the .000 index sibling extracted alongside is now redundant (the pak's is pristine)
			var indexEntryIndex = lpakFile.FindEntryIndex( name => name.EndsWith( ".000", StringComparison.OrdinalIgnoreCase ) );
			var pakDirectory = Path.GetDirectoryName( lpakFile.FileNameOnDisk );
			if( indexEntryIndex >= 0 && pakDirectory != null )
			{
				var indexEntryName = lpakFile.PakFileNames[indexEntryIndex].FileName;
				if( !string.IsNullOrWhiteSpace( indexEntryName ) )
				{
					var indexOverridePath = Path.Combine( pakDirectory, indexEntryName! );
					if( File.Exists( indexOverridePath ) )
					{
						File.Delete( indexOverridePath );
					}
				}
			}
		}
	}
}
