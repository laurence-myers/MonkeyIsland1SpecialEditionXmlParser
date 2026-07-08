using System.Drawing;
using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.UI;

namespace MonkeyIslandSpecialEditionSpriteEditor.Commands
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

		protected override CommandResult InnerExecute()
		{
			if( exportImage == null )
			{
				return CommandResult.Fail( "Invalid export image" );
			}
			if( string.IsNullOrWhiteSpace( exportFileName ) )
			{
				return CommandResult.Fail( "Invalid export file name" );
			}

			this.saveFileDialog.FileName = exportFileName;
			if( this.saveFileDialog.ShowDialog( MainForm.Instance ) != DialogResult.OK )
			{
				return CommandResult.Fail( "Export cancelled by user" );
			}

			return new ExportToPngCommand( exportImage, this.saveFileDialog.FileName ).Execute();
		}
	}
}
