using System;
using System.Windows.Forms;
using costumes = MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes;
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
			if( this.OpenFileName.EndsWith( ".pak", StringComparison.OrdinalIgnoreCase ) )
			{
				var file = lpak.Parser.Parse( this.OpenFileName );
				if( file == null )
				{
					MessageBox.Show( "Unable to parse room file." );
					return false;
				}

				//Command.AddRecentLPAKFileName.RecentFileName = this.OpenFileName;
				//Command.AddRecentLPAKFileName.Execute();

				Command.OpenLPAKForm.FileName = this.OpenFileName;
				Command.OpenLPAKForm.LPAKFile = file;
				Command.OpenLPAKForm.Execute();
			}
			else if( this.OpenFileName.EndsWith( ".costume.xml", StringComparison.OrdinalIgnoreCase ) )
			{
				var costume = costumes.Parser.Parse( this.OpenFileName );
				if( costume == null )
				{
					MessageBox.Show( "Unable to parse costume file." );
					return false;
				}

				Command.AddRecentCostumeFileName.RecentFileName = this.OpenFileName;
				Command.AddRecentCostumeFileName.Execute();

				Command.OpenCostumeForm.FileName = this.OpenFileName;
				Command.OpenCostumeForm.Costume = costume;
				Command.OpenCostumeForm.Execute();
			}
			else
			{
				MessageBox.Show( @"File must end with "".pak"" or "".costume.xml""." );
				return false;
			}

			return true;
		}
	}
}
