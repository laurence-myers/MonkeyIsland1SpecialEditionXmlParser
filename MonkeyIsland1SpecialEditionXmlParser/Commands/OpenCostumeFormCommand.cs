using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenCostumeFormCommand( LPAKFile lpakFile, string fileName, int fileIndex ) : BaseCommand
	{
		protected override bool InnerExecute()
		{
			if( string.IsNullOrWhiteSpace( fileName ) )
			{
				return false;
			}

			var form = new CostumeForm()
			{
				FileIndex = fileIndex,
				LPAKFile = lpakFile,
				MdiParent = MainForm.Instance,
				Text = fileName,
				WindowState = FormWindowState.Normal,
			};
			form.Show();

			return true;
		}
	}
}
