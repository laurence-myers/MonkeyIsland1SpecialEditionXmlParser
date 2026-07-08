using System;
using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.UI;

namespace MonkeyIslandSpecialEditionSpriteEditor.Commands
{
	public class ExportToBinaryWithDialogCommand( byte[] bytes ) : BaseCommand
	{
		private readonly SaveFileDialog saveFileDialog = new SaveFileDialog()
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

		protected override CommandResult InnerExecute()
		{
			if( this.saveFileDialog.ShowDialog( MainForm.Instance ) != DialogResult.OK )
			{
				return CommandResult.Fail( "" );
			}

			return new ExportToBinaryCommand( this.saveFileDialog.FileName, bytes ).Execute();
		}
	}
}
