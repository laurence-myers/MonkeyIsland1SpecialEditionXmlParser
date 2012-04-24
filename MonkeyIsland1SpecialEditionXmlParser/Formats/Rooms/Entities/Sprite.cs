
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class Sprite
	{
		/// <summary>
		/// Gets or sets the index of the sprite.
		/// </summary>
		public int Index
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the byte address for the texture file name.
		/// </summary>
		public int TextureFileNameAddress
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the texture file name.
		/// </summary>
		public string TextureFileName
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
		}

		/// <summary>
		/// Gets or sets the Y component of the sprite's rectangle.
		/// </summary>
		public int TextureY
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the width of the sprite's rectangle.
		/// </summary>
		public int TextureWidth
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the height of the sprite's rectangle.
		/// </summary>
		public int TextureHeight
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets X component of the offset.
		/// </summary>
		public float OffsetX
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets Y component of the offset.
		/// </summary>
		public float OffsetY
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets layer index. NB: this might be a sort order.
		/// </summary>
		public int Layer
		{
			get;
			set;
		}

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
