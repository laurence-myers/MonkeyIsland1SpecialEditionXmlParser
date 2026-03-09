using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.UI;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenImageFormCommand( Bitmap? image, string title ) : BaseCommand
	{
		protected override bool InnerExecute()
		{
			if( image == null )
			{
				return false;
			}

			var form = new ImageViewerForm()
			{
				Text = title,
				MdiParent = MainForm.Instance,
				WindowState = FormWindowState.Normal,
			};

			var spriteSetPreviewControl = form.Controls.OfType<SpriteSetPreviewControl>().FirstOrDefault();
			if( spriteSetPreviewControl == null )
			{
				return false;
			}
			spriteSetPreviewControl.Sprites.Add( new SpriteSetPreviewControlSprite() { Image = image } );

			form.Show();
			return true;
		}
	}
}
