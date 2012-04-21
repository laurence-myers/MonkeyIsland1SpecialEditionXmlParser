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

		public List<StaticTextureHeader> StaticTextureHeaderList
		{
			get;
			set;
		}

		public List<SpriteHeader> SpriteHeaderList
		{
			get;
			set;
		}

		public List<Unknown3> Unknown3List
		{
			get;
			set;
		}

		public List<Unknown4> Unknown4List
		{
			get;
			set;
		}

		public List<Unknown5> Unknown5List
		{
			get;
			set;
		}

		public List<List<StaticTexture>> StaticTextureList
		{
			get;
			set;
		}

		public List<SpriteGroup> SpriteGroupList
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
