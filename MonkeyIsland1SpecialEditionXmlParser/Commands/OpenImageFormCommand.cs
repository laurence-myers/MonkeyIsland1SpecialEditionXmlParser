using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.UI;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenImageFormCommand( Bitmap? image, string? title ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			if( image == null )
			{
				return CommandResult.Fail( "Invalid image" );
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
				return CommandResult.Fail( "SpriteSetPreviewControl not found" );
			}
			spriteSetPreviewControl.Sprites.Add( new SpriteSetPreviewControlSprite( image: image ) );

			form.Show();
			return CommandResult.Success(string.Empty);
		}
	}
}
