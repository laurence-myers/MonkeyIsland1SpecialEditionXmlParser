
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class SpriteHeader
	{
		/// <summary>
		/// Gets or sets the index.
		/// </summary>
		public int Index
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the identifier.
		/// </summary>
		public int Identifier
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the number of sprites.
		/// </summary>
		public int SpriteCount
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the byte address for the first sprite.
		/// </summary>
		public int SpriteAddress
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat(
				this.Identifier, "; ",
				this.SpriteCount, "; ",
				this.SpriteAddress
				);
		}
	}
}
