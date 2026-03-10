namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public abstract class BaseCommand
	{
		public bool Execute()
		{
			var success = this.InnerExecute();

			return success;
		}

		protected abstract bool InnerExecute();
	}
}
