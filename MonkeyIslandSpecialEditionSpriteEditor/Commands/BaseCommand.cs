global using CommandResult = MonkeyIslandSpecialEditionSpriteEditor.Lib.Result<string, string>;
using MonkeyIslandSpecialEditionSpriteEditor.UI;

namespace MonkeyIslandSpecialEditionSpriteEditor.Commands
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
