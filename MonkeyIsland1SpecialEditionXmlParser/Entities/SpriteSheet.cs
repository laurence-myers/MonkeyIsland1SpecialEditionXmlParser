
namespace MonkeyIsland1SpecialEditionXmlParser.Entities
{
	public class SpriteSheet
	{
		/// <summary>
		/// Gets or sets the index of the sprite sheet.
		/// </summary>
		public int Index
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the relative path to the sprite sheet file.
		/// </summary>
		public string Path
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat( this.Index, " (", this.Path, ")" );
		}
	}
}
