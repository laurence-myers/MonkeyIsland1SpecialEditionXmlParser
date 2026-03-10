using System.Collections.Generic;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class SpriteGroup(
		List<Sprite> spriteList
	)
	{
		public List<Sprite> SpriteList
		{
			get;
			set;
		} = spriteList;
	}
}
