using System;
using System.IO;
using System.Linq;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm
{
	/// <summary>
	/// UI-free orchestration for applying and removing a walkbox patch against an opened pak: it
	/// reads the pristine (pak) and current (loose override or pak) classic data, runs the
	/// <see cref="ClassicPatcher"/>, and writes the result as a loose override with the .000
	/// sibling. Shared by the editor commands and the standalone CLI, so neither the console tool
	/// nor a headless run touches WinForms.
	/// </summary>
	public static class ClassicPatchService
	{
		public static CommandResult Apply( LPAKFile? lpakFile, string? patchPath )
		{
			return Run( lpakFile, patchPath, apply: true );
		}

		public static CommandResult Remove( LPAKFile? lpakFile, string? patchPath )
		{
			return Run( lpakFile, patchPath, apply: false );
		}

		private static CommandResult Run( LPAKFile? lpakFile, string? patchPath, bool apply )
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

			var result = apply
				? ClassicPatcher.Apply( pristine, current, patch )
				: ClassicPatcher.Remove( pristine, current, patch );
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
				if( !apply && result.EncodedBytes!.SequenceEqual( pristine ) )
				{
					// removing left the data identical to the pristine pak: drop the loose files
					DeleteOverrideFiles( lpakFile, overridePath );
				}
				else
				{
					if( apply )
					{
						var pakDirectory = Path.GetDirectoryName( lpakFile.FileNameOnDisk );
						if( pakDirectory != null )
						{
							var extractError = ClassicOverride.ExtractIndexSibling( lpakFile, pakDirectory );
							if( extractError != null )
							{
								return CommandResult.Fail( extractError );
							}
						}
					}
					ClassicOverride.AtomicWrite( overridePath, result.EncodedBytes! );
				}
			}
			catch( Exception exception )
			{
				return CommandResult.Fail( "Failed to write the classic data: " + exception.Message );
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
