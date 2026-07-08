
namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Entities
{
	/// <summary>
	/// A pair of texture entries; the file stores one of these records per two texture file
	/// names (the second half is zero when the texture count is odd).
	/// </summary>
	public class TextureHeader
	{
		/// <summary>
		/// Gets or sets the number of sprites that reference the texture (sprites with
		/// TextureNumber -1 are not counted).
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
		/// Gets or sets the number of sprites that reference the texture (sprites with
		/// TextureNumber -1 are not counted).
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
