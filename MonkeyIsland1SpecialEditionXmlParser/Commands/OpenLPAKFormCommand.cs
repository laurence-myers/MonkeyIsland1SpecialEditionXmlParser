using System.IO;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenLPAKFormCommand : BaseCommand
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
			if( !File.Exists( this.FileName ) )
			{
				return false;
			}

			var form = new LPAKForm()
			{
				FileName = this.FileName,
				MdiParent = MainForm.Instance,
				LPAKFile = this.LPAKFile,
				Text = this.FileName,
				WindowState = FormWindowState.Normal,
			};
			form.Show();

			return true;
		}
	}
}
