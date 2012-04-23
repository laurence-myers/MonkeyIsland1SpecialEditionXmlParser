using System.IO;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenRoomFormCommand : BaseCommand
	{
		public Room Room
		{
			get;
			set;
		}

		public string FileName
		{
			get;
			set;
		}

		protected override bool InnerExecute()
		{
			if( this.Room == null )
			{
				return false;
			}
			if( string.IsNullOrWhiteSpace( this.FileName ) )
			{
				return false;
			}
			if( !File.Exists( this.FileName ) )
			{
				return false;
			}

			var roomForm = new RoomForm()
			{
				MdiParent = MainForm.Instance,
				Room = this.Room,
				Text = this.FileName,
				WindowState = FormWindowState.Maximized,
			};
			roomForm.Show();

			return true;
		}
	}
}
