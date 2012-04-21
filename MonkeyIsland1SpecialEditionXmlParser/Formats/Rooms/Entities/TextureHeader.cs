
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class TextureHeader
	{
		/// <summary>
		/// Gets or sets the byte address of the file name.
		/// </summary>
		public int TextureFileNameAddress
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the in-game X component of the position.
		/// </summary>
		public int PositionX
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the in-game Y component of the position.
		/// </summary>
		public int PositionZ
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the rectangle width of the texture.
		/// </summary>
		public int Width
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the rectangle height of the texture.
		/// </summary>
		public int Height
		{
			get;
			set;
		}
		
		public override string ToString()
		{
			return string.Concat(
				this.TextureFileNameAddress, "; ",
				this.PositionX, "; ",
				this.PositionZ, "; ",
				this.Width, "; ",
				this.Height
				);
		}
	}
}
