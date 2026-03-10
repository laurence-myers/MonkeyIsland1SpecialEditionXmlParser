global using CommandResult = MonkeyIsland1SpecialEditionXmlParser.Lib.Result<string, string>;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{

	public abstract class BaseCommand
	{
		public CommandResult Execute()
		{
			var result = this.InnerExecute();

			var statusText = result.IsSuccess switch
			{
				true => result.Value,
				false => result.Error,
			};
			if( !string.IsNullOrEmpty( statusText ) )
			{
				MainForm.Instance.SetStatusText( statusText );
			}
			
			return result;
		}

		protected abstract CommandResult InnerExecute();
	}
}
