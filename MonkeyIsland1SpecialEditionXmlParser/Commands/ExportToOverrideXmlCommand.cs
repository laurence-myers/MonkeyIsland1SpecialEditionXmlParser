using System.IO;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportToOverrideXmlCommand( string? lpakFilePath, string? resourcePath, object? obj ) : BaseCommand
	{
		protected override bool InnerExecute()
		{
			if( string.IsNullOrWhiteSpace( lpakFilePath ) )
			{
				return false;
			}
			if( string.IsNullOrWhiteSpace( resourcePath ) )
			{
				return false;
			}
			if( obj == null )
			{
				return false;
			}

			// Get the directory containing the LPAK file
			var lpakDirectory = Path.GetDirectoryName( lpakFilePath );
			if( lpakDirectory == null || resourcePath == null )
			{
				return false;
			}

			// Combine with the resource path to create the full export path
			var exportPath = Path.Combine( lpakDirectory, "overrides", resourcePath );

			// Create directory structure if it doesn't exist
			var exportDirectory = Path.GetDirectoryName( exportPath );
			if( !string.IsNullOrWhiteSpace( exportDirectory ) && !Directory.Exists( exportDirectory ) )
			{
				Directory.CreateDirectory( exportDirectory );
			}

			Helper.WriteObjectToFile( exportPath, obj );

			return true;
		}
	}
}
