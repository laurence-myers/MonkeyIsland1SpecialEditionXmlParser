using System.Drawing;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes;

namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	/// <summary>
	/// One sprite shown by <see cref="CostumePreviewControl"/>: a resolved placement plus the
	/// texture it samples from.
	/// </summary>
	public class CostumePreviewControlSprite( CostumeSpritePlacement placement, Image? texture )
	{
		public CostumeSpritePlacement Placement
		{
			get;
			set;
		} = placement;

		public Image? Texture
		{
			get;
			set;
		} = texture;

		public bool Visible
		{
			get;
			set;
		} = true;

		/// <summary>
		/// Gets the source rectangle on the texture.
		/// </summary>
		public Rectangle SourceRect
		{
			get
			{
				var sprite = this.Placement.Sprite;
				return new Rectangle( sprite.TextureX, sprite.TextureY, sprite.TextureWidth, sprite.TextureHeight );
			}
		}
	}
}
