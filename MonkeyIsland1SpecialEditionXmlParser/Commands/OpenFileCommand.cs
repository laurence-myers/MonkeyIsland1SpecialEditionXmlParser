using System;
using System.Windows.Forms;
using lpak = MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenFileCommand( string openFileName ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			if( !openFileName.EndsWith( ".pak", StringComparison.OrdinalIgnoreCase ) )
			{
				MessageBox.Show( @"File must end with "".pak""." );
				return CommandResult.Fail( "Invalid file extension" );
			}

			var file = lpak.Parser.Parse( openFileName );
			if( file == null )
			{
				MessageBox.Show( "Unable to parse room file." );
				return CommandResult.Fail( "Failed to parse room file" );
			}

			new AddRecentLPAKFileNameCommand( openFileName ).Execute();

			new OpenLPAKFormCommand( file, openFileName ).Execute();

			return CommandResult.Success($"Opened LPAK file: {openFileName}");
		}
	}
}
