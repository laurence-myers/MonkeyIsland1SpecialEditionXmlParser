using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportRoomToPngWithDialogCommand : BaseCommand
	{
		private FolderBrowserDialog imageExportDialog;

		public LPAKFile LPAKFile
		{
			get;
			set;
		}

		public Room Room
		{
			get;
			set;
		}

		public ExportRoomToPngWithDialogCommand()
		{
			this.imageExportDialog = new FolderBrowserDialog()
			{
				ShowNewFolderButton = true,
			};
		}

		protected override bool InnerExecute()
		{
			if( this.imageExportDialog.ShowDialog( MainForm.Instance ) != DialogResult.OK )
			{
				return false;
			}

			Command.ExportRoomToPng.LPAKFile = this.LPAKFile;
			Command.ExportRoomToPng.Room = this.Room;
			Command.ExportRoomToPng.ExportPath = this.imageExportDialog.SelectedPath;

			var succes = Command.ExportRoomToPng.Execute();
			return succes;
		}
	}
}
