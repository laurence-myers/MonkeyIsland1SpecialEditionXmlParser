
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities
{
	public class SpriteGroupHeader
	{
		/// <summary>
		/// Gets or sets the sprite group identifier.
		/// </summary>
		public int Identifier
		{
			get;
			set;
		}

		public int UnkownInteger1
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the count of sprites in the group.
		/// </summary>
		public int SpriteCount
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the byte address of the first sprite.
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
				this.UnkownInteger1, "; ",
				this.SpriteCount, "; ",
				this.SpriteAddress
				);
		}
	}
}
