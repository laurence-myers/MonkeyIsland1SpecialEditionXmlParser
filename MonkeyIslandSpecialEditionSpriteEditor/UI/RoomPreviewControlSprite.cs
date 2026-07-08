using System.Drawing;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	/// <summary>
	/// A sprite shown by the <see cref="RoomPreviewControl"/>: a resolved placement plus the
	/// texture it samples from.
	/// </summary>
	public class RoomPreviewControlSprite( RoomSpritePlacement placement, Image? texture )
	{
		/// <summary>
		/// Gets or sets the resolved placement (position, sprite, group and classic match).
		/// </summary>
		public RoomSpritePlacement Placement
		{
			get;
			set;
		} = placement;

		/// <summary>
		/// Gets or sets the spritesheet texture the sprite's rectangle samples from; null when missing.
		/// </summary>
		public Image? Texture
		{
			get;
			set;
		} = texture;

		/// <summary>
		/// Gets or sets a value indicating whether the sprite is drawn.
		/// </summary>
		public bool Visible
		{
			get;
			set;
		} = true;

		/// <summary>
		/// Gets the source rectangle within the texture.
		/// </summary>
		public Rectangle SourceRect
		{
			get
			{
				var sprite = this.Placement.Sprite;
				return new Rectangle( sprite.TextureX, sprite.TextureY, sprite.TextureWidth, sprite.TextureHeight );
			}
		}

		public override string ToString()
		{
			return this.Placement.ToString();
		}
	}
}
