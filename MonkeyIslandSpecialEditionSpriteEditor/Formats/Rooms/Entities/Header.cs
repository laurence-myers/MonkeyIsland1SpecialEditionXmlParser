using System;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities
{
	/// <summary>
	/// The 80-byte room header: twenty <see cref="Int32"/> fields followed by the room name.
	/// Every section is described by a (count, relative-address) pair, in that order. All
	/// addresses are stored on disk as offsets relative to the field's own position; the
	/// parser resolves them to absolute byte positions.
	/// </summary>
	public struct Header(
		int identifier,
		int nameAddress,
		int width,
		int height,
		int staticSpriteHeaderCount,
		int staticSpriteHeaderAddress,
		int spriteHeaderCount,
		int spriteHeaderAddress,
		int unknown9,
		int unknown6HeaderCountA,
		int unknown6HeaderAddressA,
		int unknown6HeaderCount,
		int unknown6HeaderAddress,
		int roomObjectHeaderCount,
		int roomObjectHeaderAddress,
		int unknown5HeaderCount,
		int unknown5HeaderAddress,
		int alwaysZero1,
		int alwaysZero2,
		int alwaysZero3,
		string name
	)
	{
		// Don't use this, it's just for XML serialization
		public Header() : this(
			identifier: 0,
			nameAddress: 0,
			width: 0,
			height: 0,
			staticSpriteHeaderCount: 0,
			staticSpriteHeaderAddress: 0,
			spriteHeaderCount: 0,
			spriteHeaderAddress: 0,
			unknown9: 0,
			unknown6HeaderCountA: 0,
			unknown6HeaderAddressA: 0,
			unknown6HeaderCount: 0,
			unknown6HeaderAddress: 0,
			roomObjectHeaderCount: 0,
			roomObjectHeaderAddress: 0,
			unknown5HeaderCount: 0,
			unknown5HeaderAddress: 0,
			alwaysZero1: 0,
			alwaysZero2: 0,
			alwaysZero3: 0,
			name: null!
		) {}

		/// <summary>
		/// Gets or sets the identifier.
		/// </summary>
		public Int32 Identifier
		{
			get;
			set;
		} = identifier;

		/// <summary>
		/// Gets or sets the byte address of the name.
		/// </summary>
		public Int32 NameAddress
		{
			get;
			set;
		} = nameAddress;

		/// <summary>
		/// Gets or sets the room's width in screen pixels (matches the extent covered by the
		/// static sprite layer chunks).
		/// </summary>
		public Int32 Width
		{
			get;
			set;
		} = width;

		/// <summary>
		/// Gets or sets the room's height in screen pixels (1037 for every room in the game).
		/// </summary>
		public Int32 Height
		{
			get;
			set;
		} = height;

		public Int32 StaticSpriteHeaderCount
		{
			get;
			set;
		} = staticSpriteHeaderCount;

		public Int32 StaticSpriteHeaderAddress
		{
			get;
			set;
		} = staticSpriteHeaderAddress;

		public Int32 SpriteHeaderCount
		{
			get;
			set;
		} = spriteHeaderCount;

		public Int32 SpriteHeaderAddress
		{
			get;
			set;
		} = spriteHeaderAddress;

		/// <summary>
		/// Gets or sets a per-room value read and stored by the engine; purpose unknown.
		/// Always 1 or 2 in the game data.
		/// </summary>
		public Int32 Unknown9
		{
			get;
			set;
		} = unknown9;

		/// <summary>
		/// Gets or sets the count for the "A" variant of the <see cref="Unknown6Header"/>
		/// section. This section is empty (count 0) in every room in the game; its address
		/// still points at the same table as <see cref="Unknown6HeaderAddress"/>.
		/// </summary>
		public Int32 Unknown6HeaderCountA
		{
			get;
			set;
		} = unknown6HeaderCountA;

		/// <summary>
		/// Gets or sets the address for the empty "A" variant of the
		/// <see cref="Unknown6Header"/> section (see <see cref="Unknown6HeaderCountA"/>).
		/// </summary>
		public Int32 Unknown6HeaderAddressA
		{
			get;
			set;
		} = unknown6HeaderAddressA;

		public Int32 Unknown6HeaderCount
		{
			get;
			set;
		} = unknown6HeaderCount;

		public Int32 Unknown6HeaderAddress
		{
			get;
			set;
		} = unknown6HeaderAddress;

		public Int32 RoomObjectHeaderCount
		{
			get;
			set;
		} = roomObjectHeaderCount;

		public Int32 RoomObjectHeaderAddress
		{
			get;
			set;
		} = roomObjectHeaderAddress;

		public Int32 Unknown5HeaderCount
		{
			get;
			set;
		} = unknown5HeaderCount;

		public Int32 Unknown5HeaderAddress
		{
			get;
			set;
		} = unknown5HeaderAddress;

		public Int32 AlwaysZero1
		{
			get;
			set;
		} = alwaysZero1;

		public Int32 AlwaysZero2
		{
			get;
			set;
		} = alwaysZero2;

		public Int32 AlwaysZero3
		{
			get;
			set;
		} = alwaysZero3;

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		public string Name
		{
			get;
			set;
		} = name;
	}
}
