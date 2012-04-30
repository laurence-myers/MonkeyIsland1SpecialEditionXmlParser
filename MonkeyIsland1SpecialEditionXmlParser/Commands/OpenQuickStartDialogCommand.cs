using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenQuickStartDialogCommand : BaseCommand
	{
		private QuickStartForm quickStartForm;

		public OpenQuickStartDialogCommand()
		{
			this.quickStartForm = new QuickStartForm()
			{
			};
		}

		protected override bool InnerExecute()
		{
			this.quickStartForm.ShowDialog( MainForm.Instance );
			return true;
		}
	}
}
