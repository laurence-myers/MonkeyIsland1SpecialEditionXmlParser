using System.IO;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportToBinaryCommand( string exportFileName, byte[] bytes ) : BaseCommand
	{
		protected override bool InnerExecute()
		{
			if( string.IsNullOrWhiteSpace( exportFileName ) )
			{
				return false;
			}
			if( bytes == null || bytes.Length == 0 )
			{
				return false;
			}

			File.WriteAllBytes( exportFileName, bytes );
			return true;
		}
	}
}
