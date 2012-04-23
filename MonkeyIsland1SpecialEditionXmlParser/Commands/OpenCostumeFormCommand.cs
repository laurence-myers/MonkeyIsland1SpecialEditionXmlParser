using System.IO;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenCostumeFormCommand : BaseCommand
	{
		public Costume Costume
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
			if( this.Costume == null )
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

			var costumeForm = new CostumeForm()
			{
				Costume = this.Costume,
				MdiParent = MainForm.Instance,
				Text = this.FileName,
				WindowState = FormWindowState.Maximized,
			};
			costumeForm.Show();

			return true;
		}
	}
}
