using System;
using System.Collections.Generic;
using System.IO;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;

namespace MonkeyIslandSpecialEditionSpriteEditor.Commands
{
	/// <summary>
	/// Imports every PNG under a folder as a texture override, mapping each file back to its
	/// resource path by the mirrored folder structure the batch export writes, e.g.
	/// "&lt;folder&gt;\art\rooms\x.png" becomes "art/rooms/x.dxt". A non-null resource list limits
	/// the import to those textures (used by the sprite editors); other PNGs are ignored.
	/// </summary>
	public class BatchImportTexturesPngCommand( LPAKFile? lpakFile, string? directoryPath, IReadOnlyCollection<string>? resourcePaths ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			if( lpakFile == null )
			{
				return CommandResult.Fail( "Invalid LPAK file" );
			}
			if( string.IsNullOrWhiteSpace( directoryPath ) || !Directory.Exists( directoryPath ) )
			{
				return CommandResult.Fail( "Import directory does not exist" );
			}

			var pngFiles = Directory.GetFiles( directoryPath!, "*.png", SearchOption.AllDirectories );
			if( pngFiles.Length == 0 )
			{
				return CommandResult.Fail( "No PNG files found under " + directoryPath );
			}

			var scope = resourcePaths == null
				? null
				: new HashSet<string>( resourcePaths, StringComparer.OrdinalIgnoreCase );

			var importedCount = 0;
			var unmatchedCount = 0;
			var failures = new List<string>();
			foreach( var pngFile in pngFiles )
			{
				var relativePath = pngFile
					.Substring( directoryPath!.Length )
					.TrimStart( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar );
				var resourcePath = Path
					.ChangeExtension( relativePath, ".dxt" )
					.Replace( Path.DirectorySeparatorChar, '/' );

				if( scope != null && !scope.Contains( resourcePath ) )
				{
					continue;
				}

				var entryIndex = lpakFile.FindEntryIndex( name => string.Equals( name, resourcePath, StringComparison.OrdinalIgnoreCase ) );
				if( entryIndex < 0 )
				{
					unmatchedCount++;
					continue;
				}

				// use the entry's exact name so the override file matches the pak casing
				var result = ImportTexturePngCommand.Import( lpakFile, lpakFile.PakFileNames[entryIndex].FileName, pngFile );
				if( result.IsSuccess )
				{
					importedCount++;
				}
				else
				{
					failures.Add( resourcePath + ": " + result.Error );
				}
			}

			if( importedCount == 0 && failures.Count == 0 )
			{
				return CommandResult.Fail( string.Concat(
					"No texture PNGs matched. The folder structure must mirror the resource paths (e.g. ",
					directoryPath, Path.DirectorySeparatorChar, "art", Path.DirectorySeparatorChar, "...)."
				) );
			}

			var summary = string.Concat(
				"Imported ", importedCount, " texture override(s) from ", directoryPath,
				unmatchedCount > 0 ? string.Concat( " (", unmatchedCount, " PNG(s) had no matching texture)" ) : ""
			);
			return failures.Count == 0
				? CommandResult.Success( summary )
				: CommandResult.Fail( string.Concat( summary, "\n\nFailed:\n", BatchExportTexturesPngCommand.DescribeFailures( failures ) ) );
		}
	}
}
