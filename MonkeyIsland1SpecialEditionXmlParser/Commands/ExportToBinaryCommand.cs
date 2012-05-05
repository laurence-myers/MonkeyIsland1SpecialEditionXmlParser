using System.IO;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportToBinaryCommand : BaseCommand
	{
		public string ExportFileName
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
			if( string.IsNullOrWhiteSpace( this.ExportFileName ) )
			{
				return false;
			}
			if( this.Bytes == null || this.Bytes.Length == 0 )
			{
				return false;
			}

			File.WriteAllBytes( this.ExportFileName, this.Bytes );
			return true;
		}
	}
}
