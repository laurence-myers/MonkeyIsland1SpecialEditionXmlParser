
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	/// <summary>
	/// One instance of a named room object. Every instance in the original game data carries
	/// exactly one of <see cref="Sprite"/> (a cut-out drawn from an objects texture) or
	/// <see cref="Image"/> (a chunked, room-sized image such as an animated "extra_*" overlay).
	/// </summary>
	public class RoomObject(
		int index,
		int spriteAddress,
		int imageAddress,
		float offsetX,
		float offsetY
	)
	{
		private RoomObject() : this(
			index: 0,
			spriteAddress: 0,
			imageAddress: 0,
			offsetX: 0,
			offsetY: 0
		) {}

		public int Index
		{
			get;
			set;
		} = index;

		/// <summary>
		/// Gets or sets the byte address of the sprite record, or 0 if this instance has none.
		/// </summary>
		public int SpriteAddress
		{
			get;
			set;
		} = spriteAddress;

		/// <summary>
		/// Gets or sets the byte address of the image record, or 0 if this instance has none.
		/// </summary>
		public int ImageAddress
		{
			get;
			set;
		} = imageAddress;

		/// <summary>
		/// Gets or sets the X component of the offset. NB: meaning inferred from the
		/// analogous float pair on <see cref="Sprite"/>; not verified in-game.
		/// </summary>
		public float OffsetX
		{
			get;
			set;
		} = offsetX;

		/// <summary>
		/// Gets or sets the Y component of the offset. NB: meaning inferred from the
		/// analogous float pair on <see cref="Sprite"/>; not verified in-game.
		/// </summary>
		public float OffsetY
		{
			get;
			set;
		} = offsetY;

		public RoomObjectSprite? Sprite
		{
			get;
			set;
		}

		public RoomObjectImage? Image
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat(
				this.SpriteAddress, "; ",
				this.ImageAddress, "; ",
				this.OffsetX, "; ",
				this.OffsetY
				);
		}
	}
}
