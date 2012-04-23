using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenImageExportDialogCommand : BaseCommand
	{
		private ImageExportDialog imageExportDialog;

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

		public OpenImageExportDialogCommand()
		{
			this.imageExportDialog = new ImageExportDialog()
			{
				Text = "Image Export",
			};
		}

		protected override bool InnerExecute()
		{
			if( this.imageExportDialog.ShowDialog( MainForm.Instance ) != DialogResult.OK )
			{
				return false;
			}
			return true;
		}
	}
}
