using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportRoomToMergedPngWithDialogCommand : BaseCommand
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

		public ExportRoomToMergedPngWithDialogCommand()
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

			Command.ExportRoomToMergedPng.LPAKFile = this.LPAKFile;
			Command.ExportRoomToMergedPng.Room = this.Room;
			Command.ExportRoomToMergedPng.ExportFileName = this.imageExportDialog.FileName;

			var succes = Command.ExportRoomToMergedPng.Execute();
			return succes;
		}
	}
}
