using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenRoomFormCommand : BaseCommand
	{
		public LPAKFile LPAKFile
		{
			get;
			set;
		}

		public string FileName
		{
			get;
			set;
		}

		public int FileIndex
		{
			get;
			set;
		}

		protected override bool InnerExecute()
		{
			if( this.LPAKFile == null )
			{
				return false;
			}
			if( string.IsNullOrWhiteSpace( this.FileName ) )
			{
				return false;
			}

			var form = new RoomForm()
			{
				FileIndex = this.FileIndex,
				LPAKFile = this.LPAKFile,
				MdiParent = MainForm.Instance,
				Text = this.FileName,
				WindowState = FormWindowState.Normal,
			};
			form.Show();

			return true;
		}
	}
}
