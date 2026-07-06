using System.IO;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;

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

			// Load the entity from the XML file
			var isCostume = overrideXmlPath.EndsWith( ".costume.xml" );
			var isRoom = overrideXmlPath.EndsWith( ".room.xml" );

			if( !isCostume && !isRoom )
			{
				return CommandResult.Fail( "Unsupported entity type" );
			}
			
			object? entity = null;
			if( isCostume )
			{
				entity = MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Parser.ReadCostumeFromXmlFile( overrideXmlPath );
			} else if( isRoom )
			{
				entity = MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Parser.ReadRoomFromXmlFile( overrideXmlPath );
			}

			if( entity == null )
			{
				return CommandResult.Fail( "Failed to load entity from XML" );
			}

			// never write an entity the parser (or the game) would reject
			try
			{
				if( isCostume )
				{
					MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.SanityChecker.Check( (entity as Costume)! );
				}
				else if( isRoom )
				{
					MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.SanityChecker.Check( (entity as Room)! );
				}
			}
			catch( System.Exception exception )
			{
				return CommandResult.Fail( "Sanity check failed: " + exception.Message );
			}

			// Construct the binary resource path (without the "overrides/" prefix)
			var binaryResourcePath = Path.Combine( lpakDirectory, resourcePath );

			// Create directory structure if it doesn't exist
			var binaryDirectory = Path.GetDirectoryName( binaryResourcePath );
			if( !string.IsNullOrWhiteSpace( binaryDirectory ) && !Directory.Exists( binaryDirectory ) )
			{
				Directory.CreateDirectory( binaryDirectory );
			}

			// Pack the entity to the binary file
			if( isCostume )
			{
				MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Packer.WriteCostumeToBinaryFile( binaryResourcePath, (entity as Costume)! );				
			} else if( isRoom )
			{
				MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Packer.WriteRoomToBinaryFile( binaryResourcePath, (entity as Room)! );
			}
			

			return CommandResult.Success( $"Override written to {binaryResourcePath}" );
		}
	}
}
