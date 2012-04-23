using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenFileWithDialogCommand : BaseCommand
	{
		private OpenFileDialog openFileDialog;

		public string FileName
		{
			get;
			private set;
		}

		public OpenFileWithDialogCommand()
		{
			this.openFileDialog = new OpenFileDialog()
			{
				AddExtension = false,
				AutoUpgradeEnabled = true,
				CheckFileExists = true,
				CheckPathExists = true,
				DefaultExt = "xml",
				DereferenceLinks = true,
				Filter = "XML files|*.xml|All files|*.*",
				FilterIndex = 0,
				InitialDirectory = string.Empty, // TODO
				Multiselect = false,
				ReadOnlyChecked = false,
				RestoreDirectory = false,
				ShowHelp = false,
				ShowReadOnly = false,
				SupportMultiDottedExtensions = true,
				Title = "Open File",
			};
		}

		protected override bool InnerExecute()
		{
			if( this.openFileDialog.ShowDialog( MainForm.Instance ) != DialogResult.OK )
			{
				return false;
			}

			Command.OpenFile.OpenFileName
				= this.FileName
				= openFileDialog.FileName
				;

			var success = Command.OpenFile.Execute();
			return success;
		}
	}
}
