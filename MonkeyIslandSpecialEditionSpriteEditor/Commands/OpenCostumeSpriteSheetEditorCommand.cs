using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;
using MonkeyIslandSpecialEditionSpriteEditor.UI;

namespace MonkeyIslandSpecialEditionSpriteEditor.Commands
{
	public class OpenCostumeSpriteSheetEditorCommand( LPAKFile lpakFile, string? fileName, int fileIndex, int? backdropRoomNumber = null, int backdropPlacementIndex = 0 ) : BaseCommand
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

			// when opened to place a specific actor, show that room behind the costume
			if( backdropRoomNumber != null )
			{
				form.SelectRoomBackdrop( backdropRoomNumber.Value, backdropPlacementIndex );
			}

			form.Show();

			return CommandResult.Success( string.Empty );
		}
	}
}
