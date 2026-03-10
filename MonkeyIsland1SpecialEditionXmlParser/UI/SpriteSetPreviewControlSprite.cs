using System.Drawing;

namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	public class SpriteSetPreviewControlSprite
	{
		public SpriteSetPreviewControlSprite( Bitmap image )
		{
			this.Image = image;
		}
		
		public SpriteSetPreviewControlSprite( Bitmap image, int layer, string? name ) : this(image)
		{
			this.Layer = layer;
			this.Name = name;
		}

		public Bitmap Image
		{
			get;
			set;
		}

		public int Layer
		{
			get;
			set;
		}

		public string? Name
		{
			get;
			set;
		}
	}
}
