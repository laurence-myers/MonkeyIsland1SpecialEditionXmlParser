using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenCostumeSpriteSheetEditorCommand( LPAKFile lpakFile, string? fileName, int fileIndex ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			if( string.IsNullOrWhiteSpace( fileName ) )
			{
				return CommandResult.Fail( "File name cannot be empty" );
			}

			var form = new CostumeSpriteSheetEditorForm(
				fileIndex: fileIndex,
				lpakFile: lpakFile,
				mdiParent: MainForm.Instance,
				windowState: FormWindowState.Normal
			)
			{
				Text = fileName,
			};
			form.Show();

			return CommandResult.Success( string.Empty );
		}
	}
}
