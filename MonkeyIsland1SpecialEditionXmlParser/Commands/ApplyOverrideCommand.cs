using System.IO;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ApplyOverrideCommand( string? lpakFilePath, string? resourcePath ) : BaseCommand
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

			// Get the directory containing the LPAK file
			var lpakDirectory = Path.GetDirectoryName( lpakFilePath );
			if( lpakDirectory == null || resourcePath == null )
			{
				return false;
			}

			// Construct the override XML file path
			var overrideXmlPath = Path.Combine( lpakDirectory, "overrides", resourcePath );

			// Check if the override file exists
			if( !File.Exists( overrideXmlPath ) )
			{
				return false;
			}

			// Load the Costume from the XML file
			var costume = MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Parser.ReadCostumeFromXmlFile( overrideXmlPath );

			// Construct the binary resource path (without the "overrides/" prefix)
			var binaryResourcePath = Path.Combine( lpakDirectory, resourcePath );

			// Create directory structure if it doesn't exist
			var binaryDirectory = Path.GetDirectoryName( binaryResourcePath );
			if( !string.IsNullOrWhiteSpace( binaryDirectory ) && !Directory.Exists( binaryDirectory ) )
			{
				Directory.CreateDirectory( binaryDirectory );
			}

			// Pack the Costume to the binary file
			MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Packer.WriteCostumeToBinaryFile( binaryResourcePath, costume );

			return true;
		}
	}
}
