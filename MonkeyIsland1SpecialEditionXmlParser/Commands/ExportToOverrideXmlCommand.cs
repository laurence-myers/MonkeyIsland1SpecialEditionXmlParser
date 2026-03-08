using System.IO;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportToOverrideXmlCommand : BaseCommand
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

		public object? Object
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
			if( this.Object == null )
			{
				return false;
			}

			// Get the directory containing the LPAK file
			var lpakDirectory = Path.GetDirectoryName( this.LpakFilePath );
			if( lpakDirectory == null || this.ResourcePath == null )
			{
				return false;
			}

			// Combine with the resource path to create the full export path
			var exportPath = Path.Combine( lpakDirectory, "overrides", this.ResourcePath );

			// Create directory structure if it doesn't exist
			var exportDirectory = Path.GetDirectoryName( exportPath );
			if( !string.IsNullOrWhiteSpace( exportDirectory ) && !Directory.Exists( exportDirectory ) )
			{
				Directory.CreateDirectory( exportDirectory );
			}

			Helper.WriteObjectToFile( exportPath, this.Object );

			return true;
		}
	}
}
