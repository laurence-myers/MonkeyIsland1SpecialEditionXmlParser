using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenSpriteSheetEditorCommand( LPAKFile lpakFile, string? fileName, int fileIndex ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			if( string.IsNullOrWhiteSpace( fileName ) )
			{
				return CommandResult.Fail( "Invalid file name" );
			}

			var form = new SpriteSheetEditorForm(
				fileIndex: fileIndex,
				lpakFile: lpakFile,
				mdiParent: MainForm.Instance,
				windowState: FormWindowState.Normal
			)
			{
				Text = string.Concat( "Spritesheet Editor - ", fileName ),
			};
			form.Show();

			return CommandResult.Success( string.Empty );
		}
	}
}
