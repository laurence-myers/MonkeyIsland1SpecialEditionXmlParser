using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities;
using MonkeyIslandSpecialEditionSpriteEditor.UI;

namespace MonkeyIslandSpecialEditionSpriteEditor.Commands
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
