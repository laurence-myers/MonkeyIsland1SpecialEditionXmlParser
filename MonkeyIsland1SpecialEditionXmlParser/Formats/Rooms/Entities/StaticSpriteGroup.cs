using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.Parsing;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class StaticSpriteGroup : IEntity
	{
		public int EntityIndex
		{
			get;
			set;
		}

		public Room EntityParent
		{
			get;
			set;
		}

		public StaticSprite[] StaticSpriteList
		{
			get;
			set;
		}

		public override void IterationProcess()
		{
			this.StaticSpriteList = new StaticSprite[this.EntityParent.StaticSpriteHeaderList[this.EntityIndex].StaticSpriteCount];
		}
	}
}
