using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenRoomFormCommand( LPAKFile lpakFile, string? fileName, int fileIndex ) : BaseCommand
	{
		protected override bool InnerExecute()
		{
			if( lpakFile == null )
			{
				return false;
			}
			if( string.IsNullOrWhiteSpace( fileName ) )
			{
				return false;
			}

			var form = new RoomForm(
				fileIndex: fileIndex,
				lpakFile: lpakFile,
				mdiParent: MainForm.Instance,
				windowState: FormWindowState.Normal
			)
			{
				Text = fileName,
			};
			form.Show();

			return true;
		}
	}
}
