namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class StaticSpriteHeader(
		int index,
		int identifier,
		int sourceWidth,
		int sourceHeight,
		int staticSpriteCount,
		int staticSpriteAddress
	)
	{
		private StaticSpriteHeader() : this(
			index: 0,
			identifier: 0,
			sourceWidth: 0,
			sourceHeight: 0,
			staticSpriteCount: 0,
			staticSpriteAddress: 0
		) {}

		/// <summary>
		/// Gets or sets the index of the static sprite header.
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
			set;
		} = identifier;

		/// <summary>
		/// Gets or sets the width of the layer in source art pixels (the hi-res canvas the
		/// art was authored at, roughly 2.3-2.5x the screen-space room size).
		/// </summary>
		public int SourceWidth
		{
			get;
			set;
		} = sourceWidth;

		/// <summary>
		/// Gets or sets the height of the layer in source art pixels (2400 or 2592 for
		/// every room in the game).
		/// </summary>
		public int SourceHeight
		{
			get;
			set;
		} = sourceHeight;

		/// <summary>
		/// Gets or sets the number of static sprites.
		/// </summary>
		public int StaticSpriteCount
		{
			get;
			set;
		} = staticSpriteCount;

		/// <summary>
		/// Gets or sets the byte address for the first static sprite.
		/// </summary>
		public int StaticSpriteAddress
		{
			get;
			set;
		} = staticSpriteAddress;

		public override string ToString()
		{
			return string.Concat(
				this.Identifier, "; ",
				this.SourceWidth, "; ",
				this.SourceHeight, "; ",
				this.StaticSpriteCount, "; ",
				this.StaticSpriteAddress
				);
		}
	}
}
