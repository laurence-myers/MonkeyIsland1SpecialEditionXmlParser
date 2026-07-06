
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class Sprite(
		int index,
		int textureFileNameAddress,
		int textureX,
		int textureY,
		int textureWidth,
		int textureHeight,
		float offsetX,
		float offsetY,
		int layer
	) : IAtlasSprite
	{
		private Sprite() : this(
			index: 0,
			textureFileNameAddress: 0,
			textureX: 0,
			textureY: 0,
			textureWidth: 0,
			textureHeight: 0,
			offsetX: 0,
			offsetY: 0,
			layer: 0
		) {}

		/// <summary>
		/// Gets or sets the index of the sprite.
		/// </summary>
		public int Index
		{
			get;
			set;
		} = index;

		/// <summary>
		/// Gets or sets the byte address for the texture file name.
		/// </summary>
		public int TextureFileNameAddress
		{
			get;
			set;
		} = textureFileNameAddress;

		/// <summary>
		/// Gets or sets the texture file name.
		/// </summary>
		public string? TextureFileName
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the X component of the sprite's rectangle.
		/// </summary>
		public int TextureX
		{
			get;
			set;
		} = textureX;

		/// <summary>
		/// Gets or sets the Y component of the sprite's rectangle.
		/// </summary>
		public int TextureY
		{
			get;
			set;
		} = textureY;

		/// <summary>
		/// Gets or sets the width of the sprite's rectangle.
		/// </summary>
		public int TextureWidth
		{
			get;
			set;
		} = textureWidth;

		/// <summary>
		/// Gets or sets the height of the sprite's rectangle.
		/// </summary>
		public int TextureHeight
		{
			get;
			set;
		} = textureHeight;

		/// <summary>
		/// Gets or sets X component of the offset.
		/// </summary>
		public float OffsetX
		{
			get;
			set;
		} = offsetX;

		/// <summary>
		/// Gets or sets Y component of the offset.
		/// </summary>
		public float OffsetY
		{
			get;
			set;
		} = offsetY;

		/// <summary>
		/// Gets or sets layer index. NB: this might be a sort order.
		/// </summary>
		public int Layer
		{
			get;
			set;
		} = layer;

		public override string ToString()
		{
			return string.Concat(
				this.TextureFileNameAddress, "; ",
				this.TextureX, "; ",
				this.TextureY, "; ",
				this.TextureWidth, "; ",
				this.TextureHeight, "; ",
				this.OffsetX, "; ",
				this.OffsetY, "; ",
				this.Layer
				);
		}
	}
}
