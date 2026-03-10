
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class StaticSprite(
		int index,
		int x,
		int y,
		int width,
		int height,
		int textureFileNameAddress
	)
	{
		private StaticSprite() : this(
			index: 0,
			x: 0,
			y: 0,
			width: 0,
			height: 0,
			textureFileNameAddress: 0
		) {}

		/// <summary>
		/// Gets or sets the static sprite index.
		/// </summary>
		public int Index
		{
			get;
			set;
		} = index;

		/// <summary>
		/// Gets or sets the X component of the position.
		/// </summary>
		public int X
		{
			get;
			set;
		} = x;

		/// <summary>
		/// Gets or sets the Y component of the position.
		/// </summary>
		public int Y
		{
			get;
			set;
		} = y;

		/// <summary>
		/// Gets or sets the width.
		/// </summary>
		public int Width
		{
			get;
			set;
		} = width;

		/// <summary>
		/// Gets or sets the height.
		/// </summary>
		public int Height
		{
			get;
			set;
		} = height;

		/// <summary>
		/// Gets or sets the byte address of the texture file name.
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

		public override string ToString()
		{
			return string.Concat(
				this.X, "; ",
				this.Y, "; ",
				this.Width, "; ",
				this.Height, "; ",
				this.TextureFileNameAddress
				);
		}
	}
}
