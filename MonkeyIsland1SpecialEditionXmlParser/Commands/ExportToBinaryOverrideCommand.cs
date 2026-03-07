using System.IO;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportToBinaryOverrideCommand : BaseCommand
	{
		public string LpakFilePath
		{
			get;
			set;
		}

		public string ResourcePath
		{
			get;
			set;
		}

		public byte[] Bytes
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
			if( this.Bytes == null || this.Bytes.Length == 0 )
			{
				return false;
			}

			// Get the directory containing the LPAK file
			var lpakDirectory = Path.GetDirectoryName( this.LpakFilePath );

			// Combine with the resource path to create the full export path
			var exportPath = Path.Combine( lpakDirectory, this.ResourcePath );

			// Create directory structure if it doesn't exist
			var exportDirectory = Path.GetDirectoryName( exportPath );
			if( !string.IsNullOrWhiteSpace( exportDirectory ) && !Directory.Exists( exportDirectory ) )
			{
				Directory.CreateDirectory( exportDirectory );
			}

			File.WriteAllBytes( exportPath, this.Bytes );
			return true;
		}
	}
}
