using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenXmlExportDialogCommand : BaseCommand
	{
		private XmlExportDialog xmlExportDialog;

		public string FileName
		{
			get
			{
				return this.xmlExportDialog.ExportFileDialog.FileName;
			}
			set
			{
				this.xmlExportDialog.ExportFileDialog.FileName = value;
			}
		}

		public string ExportFileName
		{
			get
			{
				return this.xmlExportDialog.ExportFileName;
			}
		}

		public string XsltFileName
		{
			get
			{
				return this.xmlExportDialog.XsltFileName;
			}
		}

		public OpenXmlExportDialogCommand()
		{
			this.xmlExportDialog = new XmlExportDialog()
			{
				Text = "XML Export",
			};
		}

		protected override bool InnerExecute()
		{
			if( this.xmlExportDialog.ShowDialog( MainForm.Instance ) != DialogResult.OK )
			{
				return false;
			}
			return true;
		}
	}
}
