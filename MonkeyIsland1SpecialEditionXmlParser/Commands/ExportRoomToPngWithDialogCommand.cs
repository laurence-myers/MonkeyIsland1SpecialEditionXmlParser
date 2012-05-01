using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportRoomToPngWithDialogCommand : BaseCommand
	{
		private SaveFileDialog imageExportDialog;

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
			this.imageExportDialog = new SaveFileDialog()
			{
				AddExtension = false,
				AutoUpgradeEnabled = true,
				CheckFileExists = false,
				CheckPathExists = true,
				CreatePrompt = false,
				DefaultExt = "png",
				DereferenceLinks = true,
				Filter = "PNG files|*.png|All files|*.*",
				FilterIndex = 0,
				OverwritePrompt = true,
				RestoreDirectory = false,
				ShowHelp = false,
				SupportMultiDottedExtensions = true,
				Title = "Image Export",
				ValidateNames = true,
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
			Command.ExportRoomToPng.ExportFileName = this.imageExportDialog.FileName;

			var succes = Command.ExportRoomToPng.Execute();
			return succes;
		}
	}
}
