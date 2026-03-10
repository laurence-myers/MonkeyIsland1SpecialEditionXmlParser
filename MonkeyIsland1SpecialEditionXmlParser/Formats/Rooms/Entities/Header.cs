using System;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public struct Header(
		int identifier,
		int nameAddress,
		int unkn03,
		int unkn04,
		int staticSpriteHeaderCount,
		int staticSpriteHeaderAddress,
		int spriteHeaderCount,
		int spriteHeaderAddress,
		int unkn09,
		int unkn10,
		int unknown6HeaderAddress1,
		int unknown6HeaderCount,
		int unknown6HeaderAddress2,
		int unknown4HeaderCount,
		int unknown4HeaderAddress,
		int unknown5HeaderCount,
		int unknown5HeaderAddress,
		int alwaysZero1,
		int alwaysZero2,
		int alwaysZero3,
		string name
	)
	{
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

		public Int32 Unkn03
		{
			get;
			set;
		} = unkn03;

		public Int32 Unkn04
		{
			get;
			set;
		} = unkn04;

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

		public Int32 Unkn09
		{
			get;
			set;
		} = unkn09;

		public Int32 Unkn10
		{
			get;
			set;
		} = unkn10;

		public Int32 Unknown6HeaderAddress1
		{
			get;
			set;
		} = unknown6HeaderAddress1;

		public Int32 Unknown6HeaderCount
		{
			get;
			set;
		} = unknown6HeaderCount;

		public Int32 Unknown6HeaderAddress2
		{
			get;
			set;
		} = unknown6HeaderAddress2;

		public Int32 Unknown4HeaderCount
		{
			get;
			set;
		} = unknown4HeaderCount;

		public Int32 Unknown4HeaderAddress
		{
			get;
			set;
		} = unknown4HeaderAddress;

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
