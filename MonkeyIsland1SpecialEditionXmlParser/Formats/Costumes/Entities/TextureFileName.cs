
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities
{
	public class TextureFileName( string path, int index )
	{
		/// <summary>
		/// Gets or sets the index of the texture file name.
		/// </summary>
		public int Index
		{
			get;
			set;
		} = index;

		/// <summary>
		/// Gets or sets the relative texture file name.
		/// </summary>
		public string Path
		{
			get;
			set;
		} = path;

		public override string ToString()
		{
			return string.Concat( this.Index, " (", this.Path, ")" );
		}
	}
}
