using System.Collections.Generic;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class Room(
		Header header,
		List<StaticSpriteHeader> staticSpriteHeaderList,
		List<SpriteHeader> spriteHeaderList,
		List<Unknown6Header> unknown6HeaderList,
		List<RoomObjectHeader> roomObjectHeaderList,
		List<Unknown5Header> unknown5HeaderList,
		List<List<StaticSprite>> staticSpriteList,
		List<SpriteGroup> spriteGroupList,
		List<Unknown6> unknown6List,
		List<RoomObjectGroup> roomObjectGroupList,
		List<Unknown5> unknown5List
	)
	{
		private Room() : this(
			header: new Header(),
			staticSpriteHeaderList: null!,
			spriteHeaderList: null!,
			unknown6HeaderList: null!,
			roomObjectHeaderList: null!,
			unknown5HeaderList: null!,
			staticSpriteList: null!,
			spriteGroupList: null!,
			unknown6List: null!,
			roomObjectGroupList: null!,
			unknown5List: null!
		) {}

		public Header Header
		{
			get;
			set;
		} = header;

		public List<StaticSpriteHeader> StaticSpriteHeaderList
		{
			get;
			set;
		} = staticSpriteHeaderList;

		public List<SpriteHeader> SpriteHeaderList
		{
			get;
			set;
		} = spriteHeaderList;

		public List<Unknown6Header> Unknown6HeaderList
		{
			get;
			set;
		} = unknown6HeaderList;

		public List<RoomObjectHeader> RoomObjectHeaderList
		{
			get;
			set;
		} = roomObjectHeaderList;

		public List<Unknown5Header> Unknown5HeaderList
		{
			get;
			set;
		} = unknown5HeaderList;

		public List<List<StaticSprite>> StaticSpriteList
		{
			get;
			set;
		} = staticSpriteList;

		public List<SpriteGroup> SpriteGroupList
		{
			get;
			set;
		} = spriteGroupList;

		public List<Unknown6> Unknown6List
		{
			get;
			set;
		} = unknown6List;

		public List<RoomObjectGroup> RoomObjectGroupList
		{
			get;
			set;
		} = roomObjectGroupList;

		public List<Unknown5> Unknown5List
		{
			get;
			set;
		} = unknown5List;

		public override string ToString()
		{
			return string.Concat( this.Header.Identifier, "-", this.Header.Name );
		}
	}
}
