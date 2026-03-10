using System.IO;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ApplyOverrideCommand( string? lpakFilePath, string? resourcePath ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			if( string.IsNullOrWhiteSpace( lpakFilePath ) )
			{
				return CommandResult.Fail( "Invalid LPAK file path" );
			}
			if( string.IsNullOrWhiteSpace( resourcePath ) )
			{
				return CommandResult.Fail( "Invalid resource path" );
			}

			// Get the directory containing the LPAK file
			var lpakDirectory = Path.GetDirectoryName( lpakFilePath );
			if( lpakDirectory == null || resourcePath == null )
			{
				return CommandResult.Fail( "Invalid LPAK file path or resource path" );
			}

			// Construct the override XML file path
			var overrideXmlPath = Path.Combine( lpakDirectory, "overrides", resourcePath );

			// Check if the override file exists
			if( !File.Exists( overrideXmlPath ) )
			{
				return CommandResult.Fail( "Override XML file does not exist" );
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

			return CommandResult.Success( $"Override written to {binaryResourcePath}" );
		}
	}
}
