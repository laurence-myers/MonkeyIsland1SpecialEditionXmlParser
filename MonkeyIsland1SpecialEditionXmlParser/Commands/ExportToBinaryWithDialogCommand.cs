using System;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportToBinaryWithDialogCommand : BaseCommand
	{
		private SaveFileDialog saveFileDialog;

		public Byte[] Bytes
		{
			get;
			set;
		}

		public ExportToBinaryWithDialogCommand()
		{
			this.saveFileDialog = new SaveFileDialog()
			{
				AddExtension = false,
				AutoUpgradeEnabled = true,
				CheckFileExists = false,
				CheckPathExists = true,
				CreatePrompt = false,
				DefaultExt = "dat",
				DereferenceLinks = true,
				Filter = "Data files|*.dat|Binary files|*.bin|All files|*.*",
				FilterIndex = 0,
				OverwritePrompt = true,
				RestoreDirectory = false,
				ShowHelp = false,
				SupportMultiDottedExtensions = true,
				Title = "Binary Export",
				ValidateNames = true,
			};
		}

		protected override bool InnerExecute()
		{
			if( this.saveFileDialog.ShowDialog( MainForm.Instance ) != DialogResult.OK )
			{
				return false;
			}

			Command.ExportToBinary.ExportFileName = this.saveFileDialog.FileName;
			Command.ExportToBinary.Bytes = this.Bytes;

			var success = Command.ExportToBinary.Execute();
			return success;
		}
	}
}
