using System;
using System.Linq;
using System.Windows.Forms;

namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	public partial class ImageViewerForm : Form
	{
		public ImageViewerForm()
		{
			this.InitializeComponent();
		}

		private void ExportAsPng( object sender, EventArgs args )
		{
			if( this.spriteSetPreviewControl == null )
			{
				return;
			}
			if( this.spriteSetPreviewControl.Sprites == null )
			{
				return;
			}

			var sprite = this.spriteSetPreviewControl.Sprites.FirstOrDefault();
			if( sprite == null )
			{
				return;
			}

			Command.ExportToPngWithDialog.ExportImage = this.spriteSetPreviewControl.Sprites[0].Image;
			Command.ExportToPngWithDialog.ExportFileName = this.spriteSetPreviewControl.Sprites[0].Name + ".png";
			Command.ExportToPngWithDialog.Execute();
		}
	}
}
