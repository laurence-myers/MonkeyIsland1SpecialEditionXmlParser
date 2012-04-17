
namespace MonkeyIsland1SpecialEditionXmlParser.Entities
{
	public class TextureHeader
	{
		/// <summary>
		/// Gets or sets the number of sprites on the texture.
		/// </summary>
		public int TextureSpriteCount1
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the byte address for the texture file name.
		/// </summary>
		public int TextureFileNameAddress1
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the number of sprites on the texture.
		/// </summary>
		public int TextureSpriteCount2
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the byte address for the texture file name.
		/// </summary>
		public int TextureFileNameAddress2
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat(
				this.TextureSpriteCount1, "; ",
				this.TextureFileNameAddress1, "; ",
				this.TextureSpriteCount2, "; ",
				this.TextureFileNameAddress2
				);
		}
	}
}
