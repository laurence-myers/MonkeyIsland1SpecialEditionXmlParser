using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenFileWithDialogCommand : BaseCommand
	{
		private readonly OpenFileDialog openFileDialog = new OpenFileDialog()
		{
			AddExtension = false,
			AutoUpgradeEnabled = true,
			CheckFileExists = true,
			CheckPathExists = true,
			DefaultExt = "pak",
			DereferenceLinks = true,
			Filter = "PAK files|*.pak|XML files|*.xml|All files|*.*",
			FilterIndex = 0,
			InitialDirectory = string.Empty,
			Multiselect = false,
			ReadOnlyChecked = false,
			RestoreDirectory = false,
			ShowHelp = false,
			ShowReadOnly = false,
			SupportMultiDottedExtensions = true,
			Title = "Open File",
		};

		private string? FileName;

		protected override bool InnerExecute()
		{
			if( this.openFileDialog.ShowDialog( MainForm.Instance ) != DialogResult.OK )
			{
				return false;
			}

			this.FileName = openFileDialog.FileName;

			var success = new OpenFileCommand( this.FileName ).Execute();
			return success;
		}
	}
}
