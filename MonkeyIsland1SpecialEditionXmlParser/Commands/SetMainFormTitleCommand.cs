using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class SetMainFormTitleCommand : BaseCommand
	{
		public string OpenFileName
		{
			get;
			set;
		}

		protected override bool InnerExecute()
		{
			MainForm.Instance.Text = this.OpenFileName;
			return true;
		}
	}
}
