using System.Collections.Generic;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities
{
	public class SpriteGroup(
		List<Sprite> spriteList
	)
	{
		private SpriteGroup() : this(
			spriteList: null!
		) {}

		public List<Sprite> SpriteList
		{
			get;
			set;
		} = spriteList;
	}
}
