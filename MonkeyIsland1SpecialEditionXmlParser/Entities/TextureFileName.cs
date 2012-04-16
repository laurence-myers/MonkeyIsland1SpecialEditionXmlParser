
namespace MonkeyIsland1SpecialEditionXmlParser.Entities
{
	public class TextureFileName
	{
		/// <summary>
		/// Gets or sets the index of the texture file name.
		/// </summary>
		public int Index
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the relative texture file name.
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
