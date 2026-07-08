
using System.Xml.Serialization;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities
{
	public class SpriteHeader(
		int index,
		int identifier,
		int spriteCount,
		int spriteAddress
	)
	{
		private SpriteHeader() : this(
			index: 0,
			identifier: 0,
			spriteCount: 0,
			spriteAddress: 0
		) {}

		/// <summary>
		/// Gets or sets the index.
		/// </summary>
		public int Index
		{
			get;
			set;
		} = index;

		/// <summary>
		/// Gets or sets the identifier (the classic-game object id this sprite group
		/// corresponds to).
		/// </summary>
		public int Identifier
		{
			get;
			set;
		} = identifier;

		/// <summary>
		/// Gets or sets the number of sprites. Recomputed from the sprite list on write,
		/// so it is not serialized.
		/// </summary>
		[XmlIgnore]
		public int SpriteCount
		{
			get;
			set;
		} = spriteCount;

		/// <summary>
		/// Gets or sets the byte address for the first sprite.
		/// </summary>
		[XmlIgnore]
		public int SpriteAddress
		{
			get;
			set;
		} = spriteAddress;

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
