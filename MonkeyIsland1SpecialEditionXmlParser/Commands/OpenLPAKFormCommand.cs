using System.IO;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenLPAKFormCommand( LPAKFile lpakFile, string fileName ) : BaseCommand
	{
		protected override bool InnerExecute()
		{
			if( string.IsNullOrWhiteSpace( fileName ) )
			{
				return false;
			}
			if( !File.Exists( fileName ) )
			{
				return false;
			}

			var form = new LPAKForm()
			{
				FileName = fileName,
				MdiParent = MainForm.Instance,
				LPAKFile = lpakFile,
				Text = fileName,
				WindowState = FormWindowState.Normal,
			};
			form.Show();

			return true;
		}
	}
}
