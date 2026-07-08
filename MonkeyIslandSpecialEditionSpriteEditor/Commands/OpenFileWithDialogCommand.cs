using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.UI;

namespace MonkeyIslandSpecialEditionSpriteEditor.Commands
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

		protected override CommandResult InnerExecute()
		{
			if( this.openFileDialog.ShowDialog( MainForm.Instance ) != DialogResult.OK )
			{
				return CommandResult.Fail( string.Empty );
			}

			this.FileName = openFileDialog.FileName;

			return new OpenFileCommand( this.FileName ).Execute();
		}
	}
}
