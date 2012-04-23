using System;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public abstract class BaseCommand
	{
		public bool Execute()
		{
			var executingHandler = this.Executing;
			if( executingHandler != null )
			{
				executingHandler( this, EventArgs.Empty );
			}

			var success = this.InnerExecute();

			var executedHandler = this.Executing;
			if( executedHandler != null )
			{
				executedHandler( this, EventArgs.Empty );
			}

			return success;
		}

		protected abstract bool InnerExecute();

		public event EventHandler Executing;
		public event EventHandler Executed;
	}
}
