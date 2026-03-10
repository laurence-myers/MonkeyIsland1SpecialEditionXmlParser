using System.IO;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenLPAKFormCommand( LPAKFile lpakFile, string fileName ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			if( string.IsNullOrWhiteSpace( fileName ) )
			{
				return CommandResult.Fail( "Invalid file name" );
			}
			if( !File.Exists( fileName ) )
			{
				return CommandResult.Fail( "File does not exist" );
			}

			var form = new LPAKForm(
				fileName: fileName,
				mdiParent: MainForm.Instance,
				lpakFile: lpakFile,
				windowState: FormWindowState.Normal
			)
			{
				Text = fileName,
			};
			form.Show();

			return CommandResult.Success(string.Empty);
		}
	}
}
