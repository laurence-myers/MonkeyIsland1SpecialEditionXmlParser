using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	/// <summary>
	/// Decodes textures (preferring loose override files, like the game does) to PNG files
	/// under a target folder. The folder structure mirrors the resource paths so a batch
	/// import can map the files back, e.g. "art/rooms/x.dxt" becomes "&lt;folder&gt;\art\rooms\x.png".
	/// A null resource list means every .dxt texture in the LPAK.
	/// </summary>
	public class BatchExportTexturesPngCommand( LPAKFile? lpakFile, string? directoryPath, IReadOnlyCollection<string>? resourcePaths ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			if( lpakFile == null )
			{
				return CommandResult.Fail( "Invalid LPAK file" );
			}
			if( string.IsNullOrWhiteSpace( directoryPath ) || !Directory.Exists( directoryPath ) )
			{
				return CommandResult.Fail( "Export directory does not exist" );
			}

			var targetResourcePaths = resourcePaths != null
				? resourcePaths.Where( p => !string.IsNullOrWhiteSpace( p ) ).Distinct().ToArray()
				: lpakFile.PakFileNames
					.Select( e => e.FileName )
					.Where( n => n != null && n.EndsWith( ".dxt" ) )
					.Select( n => n! )
					.Distinct()
					.ToArray();
			if( targetResourcePaths.Length == 0 )
			{
				return CommandResult.Fail( "No textures to export" );
			}

			var exportedCount = 0;
			var failures = new List<string>();
			foreach( var resourcePath in targetResourcePaths )
			{
				try
				{
					using( var bitmap = lpakFile.LoadImage( resourcePath ) as Bitmap )
					{
						if( bitmap == null )
						{
							failures.Add( resourcePath + ": texture could not be loaded" );
							continue;
						}

						var pngPath = Path.Combine(
							directoryPath!,
							Path.ChangeExtension( resourcePath, ".png" ).Replace( '/', Path.DirectorySeparatorChar )
						);
						var pngDirectory = Path.GetDirectoryName( pngPath );
						if( !string.IsNullOrWhiteSpace( pngDirectory ) && !Directory.Exists( pngDirectory ) )
						{
							Directory.CreateDirectory( pngDirectory );
						}

						bitmap.Save( pngPath, ImageFormat.Png );
						exportedCount++;
					}
				}
				catch( Exception exception )
				{
					failures.Add( resourcePath + ": " + exception.Message );
				}
			}

			var summary = string.Concat( "Exported ", exportedCount, " of ", targetResourcePaths.Length, " textures to ", directoryPath );
			return failures.Count == 0
				? CommandResult.Success( summary )
				: CommandResult.Fail( string.Concat( summary, "\n\nFailed:\n", DescribeFailures( failures ) ) );
		}

		/// <summary>
		/// Caps a failure list so the message stays readable when many textures fail.
		/// </summary>
		internal static string DescribeFailures( List<string> failures )
		{
			const int maxLines = 20;
			var lines = failures.Take( maxLines ).ToList();
			if( failures.Count > maxLines )
			{
				lines.Add( string.Concat( "... and ", failures.Count - maxLines, " more" ) );
			}
			return string.Join( "\n", lines );
		}
	}
}
