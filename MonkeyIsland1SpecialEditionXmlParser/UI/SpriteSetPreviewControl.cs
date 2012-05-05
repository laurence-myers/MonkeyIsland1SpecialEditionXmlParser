using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	public class SpriteSetPreviewControl : Control
	{
		public List<SpriteSetPreviewControlSprite> Sprites
		{
			get;
			private set;
		}

		public SpriteSetPreviewControl()
		{
			this.SetStyle( ControlStyles.AllPaintingInWmPaint, true );
			this.SetStyle( ControlStyles.UserPaint, true );

			this.Sprites = new List<SpriteSetPreviewControlSprite>();
		}

		protected override void OnPaintBackground( PaintEventArgs args )
		{
			args.Graphics.ClearWithTransparancyGrid();

			var sprites = this.Sprites;
			if( sprites == null || sprites.Count == 0 )
			{
				args.Graphics.DrawString( "NO SPRITES", this.Font, Brushes.Red, 0, 0 );
				return;
			}

			var sortedSprites = sprites.OrderBy( s => s.Layer );
			foreach( var sprite in sortedSprites )
			{
				args.Graphics.DrawImage( sprite.Image, 0, 0 );
			}
		}
	}
}
