
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities
{
	public class SpriteGroupHeader
	{
		/// <summary>
		/// Gets or sets the sprite group identifier (the classic SCUMM limb number).
		/// </summary>
		public int Identifier
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the identifier of the first sprite in the group.
		/// <see cref="Frame.SpriteIdentifier"/> values are resolved against this base:
		/// the sprite drawn is SpriteList[Frame.SpriteIdentifier - FirstSpriteIdentifier].
		/// </summary>
		public int FirstSpriteIdentifier
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
				this.FirstSpriteIdentifier, "; ",
				this.SpriteCount, "; ",
				this.SpriteAddress
				);
		}
	}
}
