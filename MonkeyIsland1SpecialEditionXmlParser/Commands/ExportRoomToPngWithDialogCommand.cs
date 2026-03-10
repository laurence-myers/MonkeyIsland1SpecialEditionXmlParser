using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportRoomToPngWithDialogCommand( LPAKFile lpakFile, Room room ) : BaseCommand
	{
		private readonly FolderBrowserDialog imageExportDialog = new FolderBrowserDialog()
		{
			ShowNewFolderButton = true,
		};

		protected override CommandResult InnerExecute()
		{
			if( this.imageExportDialog.ShowDialog( MainForm.Instance ) != DialogResult.OK )
			{
				return CommandResult.Fail( "" );
			}

			return new ExportRoomToPngCommand( this.imageExportDialog.SelectedPath, lpakFile, room ).Execute();
		}
	}
}
