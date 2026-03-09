using System.Drawing;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportToPngWithDialogCommand( Bitmap exportImage, string exportFileName ) : BaseCommand
	{
		private readonly SaveFileDialog saveFileDialog = new SaveFileDialog()
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
			Title = "Export to PNG",
			ValidateNames = true,
		};

		protected override bool InnerExecute()
		{
			if( exportImage == null )
			{
				return false;
			}
			if( string.IsNullOrWhiteSpace( exportFileName ) )
			{
				return false;
			}

			if( this.saveFileDialog.ShowDialog( MainForm.Instance ) != DialogResult.OK )
			{
				return false;
			}

			var success = new ExportToPngCommand( exportImage, this.saveFileDialog.FileName ).Execute();
			return success;
		}
	}
}
