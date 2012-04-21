
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class StaticTexture
	{
		/// <summary>
		/// Gets or sets the static texture index.
		/// </summary>
		public int Index
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the X component of the position.
		/// </summary>
		public int X
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the Y component of the position.
		/// </summary>
		public int Y
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the width.
		/// </summary>
		public int Width
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the height.
		/// </summary>
		public int Height
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the byte address of the texture file name.
		/// </summary>
		public int TextureFileNameAddress
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
