using System.Drawing;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportToPngWithDialogCommand : BaseCommand
	{
		private SaveFileDialog saveFileDialog;

		public Bitmap ExportImage
		{
			get;
			set;
		}

		public string ExportFileName
		{
			get;
			set;
		}

		public ExportToPngWithDialogCommand()
		{
			this.saveFileDialog = new SaveFileDialog()
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
		}

		protected override bool InnerExecute()
		{
			if( this.ExportImage == null )
			{
				return false;
			}
			if( string.IsNullOrWhiteSpace( this.ExportFileName ) )
			{
				return false;
			}

			if( this.saveFileDialog.ShowDialog( MainForm.Instance ) != DialogResult.OK )
			{
				return false;
			}

			Command.ExportToPng.ExportFileName = this.saveFileDialog.FileName;
			Command.ExportToPng.ExportImage = this.ExportImage;

			var success = Command.ExportToPng.Execute();
			return success;
		}
	}
}
