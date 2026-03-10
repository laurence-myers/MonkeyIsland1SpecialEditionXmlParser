using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportRoomToMergedPngWithDialogCommand( LPAKFile lpakFile, Room room ) : BaseCommand
	{
		private readonly SaveFileDialog imageExportDialog = new SaveFileDialog()
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

		protected override CommandResult InnerExecute()
		{
			if( this.imageExportDialog.ShowDialog( MainForm.Instance ) != DialogResult.OK )
			{
				return CommandResult.Fail( "" );
			}

			return new ExportRoomToMergedPngCommand( this.imageExportDialog.FileName, lpakFile, room ).Execute();
		}
	}
}
