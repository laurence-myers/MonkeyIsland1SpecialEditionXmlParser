using System.IO;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportToOverrideXmlCommand( string? lpakFilePath, string? resourcePath, object? obj ) : BaseCommand
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
			if( obj == null )
			{
				return CommandResult.Fail( "Invalid object to export" );
			}

			// Get the directory containing the LPAK file
			var lpakDirectory = Path.GetDirectoryName( lpakFilePath );
			if( lpakDirectory == null || resourcePath == null )
			{
				return CommandResult.Fail( "Invalid LPAK directory or resource path" );
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

			return CommandResult.Success( $"Exported to override XML: {exportPath}" );
		}
	}
}
