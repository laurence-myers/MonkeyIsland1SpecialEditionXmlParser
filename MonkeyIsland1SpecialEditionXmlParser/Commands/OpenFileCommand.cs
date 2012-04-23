using System;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.UI;
using costumes = MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes;
using rooms = MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms;

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
			if( this.OpenFileName.EndsWith( ".room.xml", StringComparison.OrdinalIgnoreCase ) )
			{
				var room = rooms.Parser.Parse( this.OpenFileName );
				if( room == null )
				{
					MessageBox.Show( "Unable to parse room file." );
					return false;
				}

				Command.AddRecentRoomFileName.RecentFileName = this.OpenFileName;
				Command.AddRecentRoomFileName.Execute();

				Command.OpenRoomForm.FileName = this.OpenFileName;
				Command.OpenRoomForm.Room = room;
				Command.OpenRoomForm.Execute();
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
				MessageBox.Show( @"File must end with "".costume.xml"" or "".room.xml""." );
				return false;
			}

			MainForm.Instance.EnableExportOptions();

			return true;
		}
	}
}
