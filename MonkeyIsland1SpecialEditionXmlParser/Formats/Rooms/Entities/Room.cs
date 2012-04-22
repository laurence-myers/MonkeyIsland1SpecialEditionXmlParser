using System.Collections.Generic;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class Room
	{
		public Header Header
		{
			get;
			set;
		}

		public List<StaticSpriteHeader> StaticSpriteHeaderList
		{
			get;
			set;
		}

		public List<SpriteHeader> SpriteHeaderList
		{
			get;
			set;
		}

		public List<Unknown6Header> Unknown6HeaderList
		{
			get;
			set;
		}

		public List<Unknown4Header> Unknown4HeaderList
		{
			get;
			set;
		}

		public List<Unknown5> Unknown5List
		{
			get;
			set;
		}

		public List<List<StaticSprite>> StaticSpriteList
		{
			get;
			set;
		}

		public List<SpriteGroup> SpriteGroupList
		{
			get;
			set;
		}

		public List<Unknown6> Unknown6List
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat( this.Header.Identifier, "-", this.Header.Name );
		}
	}
}
