using System.Collections.Generic;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class Room(
		Header header,
		List<StaticSpriteHeader> staticSpriteHeaderList,
		List<SpriteHeader> spriteHeaderList,
		List<Unknown6Header> unknown6HeaderList,
		List<Unknown4Header> unknown4HeaderList,
		List<Unknown5Header> unknown5HeaderList,
		List<List<StaticSprite>> staticSpriteList,
		List<SpriteGroup> spriteGroupList,
		List<Unknown6> unknown6List,
		List<Unknown4Group> unknown4GroupList,
		List<Unknown5> unknown5List
	)
	{
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

		public List<Unknown4Header> Unknown4HeaderList
		{
			get;
			set;
		} = unknown4HeaderList;

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

		public List<Unknown4Group> Unknown4GroupList
		{
			get;
			set;
		} = unknown4GroupList;

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
