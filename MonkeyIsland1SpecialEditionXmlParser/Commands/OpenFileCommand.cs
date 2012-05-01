using System;
using System.Windows.Forms;
using lpak = MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenFileCommand : BaseCommand
	{
		public string OpenFileName
		{
			get;
			set;
		}

		protected override bool InnerExecute()
		{
			if( !this.OpenFileName.EndsWith( ".pak", StringComparison.OrdinalIgnoreCase ) )
			{
				MessageBox.Show( @"File must end with "".pak""." );
				return false;
			}

			var file = lpak.Parser.Parse( this.OpenFileName );
			if( file == null )
			{
				MessageBox.Show( "Unable to parse room file." );
				return false;
			}

			Command.AddRecentLPAKFileName.RecentFileName = this.OpenFileName;
			Command.AddRecentLPAKFileName.Execute();

			Command.OpenLPAKForm.FileName = this.OpenFileName;
			Command.OpenLPAKForm.LPAKFile = file;
			Command.OpenLPAKForm.Execute();

			return true;
		}
	}
}
