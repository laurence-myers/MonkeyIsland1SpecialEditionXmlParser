using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
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
