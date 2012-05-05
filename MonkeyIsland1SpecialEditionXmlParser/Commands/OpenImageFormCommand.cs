using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.UI;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenImageFormCommand : BaseCommand
	{
		public Bitmap Image
		{
			get;
			set;
		}

		public string Title
		{
			get;
			set;
		}

		protected override bool InnerExecute()
		{
			if( this.Image == null )
			{
				return false;
			}

			var form = new ImageViewerForm()
			{
				Text = this.Title,
				MdiParent = MainForm.Instance,
				WindowState = FormWindowState.Normal,
			};

			var spriteSetPreviewControl = form.Controls.OfType<SpriteSetPreviewControl>().FirstOrDefault();
			if( spriteSetPreviewControl == null )
			{
				return false;
			}
			spriteSetPreviewControl.Sprites.Add( new SpriteSetPreviewControlSprite() { Image = this.Image } );

			form.Show();
			return true;
		}
	}
}
