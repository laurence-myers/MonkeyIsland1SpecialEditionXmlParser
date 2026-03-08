using System.IO;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ApplyOverrideCommand : BaseCommand
	{
		public string? LpakFilePath
		{
			get;
			set;
		}

		public string? ResourcePath
		{
			get;
			set;
		}

		protected override bool InnerExecute()
		{
			if( string.IsNullOrWhiteSpace( this.LpakFilePath ) )
			{
				return false;
			}
			if( string.IsNullOrWhiteSpace( this.ResourcePath ) )
			{
				return false;
			}

			// Get the directory containing the LPAK file
			var lpakDirectory = Path.GetDirectoryName( this.LpakFilePath );
			if( lpakDirectory == null || this.ResourcePath == null )
			{
				return false;
			}

			// Construct the override XML file path
			var overrideXmlPath = Path.Combine( lpakDirectory, "overrides", this.ResourcePath );

			// Check if the override file exists
			if( !File.Exists( overrideXmlPath ) )
			{
				return false;
			}

			// Load the Costume from the XML file
			var costume = MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Parser.ReadCostumeFromXmlFile( overrideXmlPath );

			// Construct the binary resource path (without the "overrides/" prefix)
			var binaryResourcePath = Path.Combine( lpakDirectory, this.ResourcePath );

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
