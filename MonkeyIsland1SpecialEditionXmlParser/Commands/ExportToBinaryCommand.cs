using System.IO;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportToBinaryCommand( string exportFileName, byte[] bytes ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			if( string.IsNullOrWhiteSpace( exportFileName ) )
			{
				return CommandResult.Fail( "Invalid export file" );
			}
			if( bytes == null || bytes.Length == 0 )
			{
				return CommandResult.Fail( "Invalid bytes" );
			}

			File.WriteAllBytes( exportFileName, bytes );
			return CommandResult.Success( $"Exported to binary: {exportFileName}" );
		}
	}
}
