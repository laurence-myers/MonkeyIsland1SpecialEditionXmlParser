using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenImageExportDialogCommand : BaseCommand
	{
		private readonly ImageExportDialog imageExportDialog;

		public string FilePrefix
		{
			get
			{
				return this.imageExportDialog.FilePrefix;
			}
			set
			{
				this.imageExportDialog.FilePrefix = value;
			}
		}

		public string Directory
		{
			get
			{
				return this.imageExportDialog.Directory;
			}
		}

		public Padding SpritePadding
		{
			get
			{
				return this.imageExportDialog.SpritePadding;
			}
		}

		public OpenImageExportDialogCommand(string filePrefix)
		{
			this.imageExportDialog = new ImageExportDialog()
			{
				Text = "Image Export",
			};

			this.FilePrefix = filePrefix;
		}

		protected override CommandResult InnerExecute()
		{
			if( this.imageExportDialog.ShowDialog( MainForm.Instance ) != DialogResult.OK )
			{
				return CommandResult.Fail( string.Empty );
			}
			return CommandResult.Success( string.Empty );
		}
	}
}
