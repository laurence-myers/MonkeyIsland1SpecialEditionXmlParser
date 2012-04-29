using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenShaderFormCommand : BaseCommand
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

		public string LPAKFileName
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

			var form = new ShaderForm()
			{
				FileIndex = this.FileIndex,
				MdiParent = MainForm.Instance,
				LPAKFile = this.LPAKFile,
				LPAKFileName = this.LPAKFileName,
				Text = this.FileName,
				WindowState = FormWindowState.Normal,
			};
			form.Show();

			return true;
		}
	}
}
