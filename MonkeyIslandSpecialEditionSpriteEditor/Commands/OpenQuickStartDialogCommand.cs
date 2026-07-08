using MonkeyIslandSpecialEditionSpriteEditor.UI;

namespace MonkeyIslandSpecialEditionSpriteEditor.Commands
{
	public class OpenQuickStartDialogCommand : BaseCommand
	{
		private readonly QuickStartForm quickStartForm = new QuickStartForm()
		{
		};

		protected override CommandResult InnerExecute()
		{
			this.quickStartForm.ShowDialog( MainForm.Instance );
			return CommandResult.Success(string.Empty);
		}
	}
}
