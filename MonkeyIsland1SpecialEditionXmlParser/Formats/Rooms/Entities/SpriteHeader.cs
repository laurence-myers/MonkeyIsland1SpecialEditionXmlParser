
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
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
		/// Gets or sets the identifier.
		/// </summary>
		public int Identifier
		{
			get;
		} = identifier;

		/// <summary>
		/// Gets or sets the number of sprites.
		/// </summary>
		public int SpriteCount
		{
			get;
		} = spriteCount;

		/// <summary>
		/// Gets or sets the byte address for the first sprite.
		/// </summary>
		public int SpriteAddress
		{
			get;
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
