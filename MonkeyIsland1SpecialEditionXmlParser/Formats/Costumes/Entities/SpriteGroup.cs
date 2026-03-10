using System.Collections.Generic;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities
{
	public class SpriteGroup( List<Sprite> spriteList, int identifier, int index )
	{
		private SpriteGroup() : this(
			spriteList: null!,
			identifier: 0,
			index: 0
		) {}

		/// <summary>
		/// Gets or sets the index of the sprite group.
		/// </summary>
		public int Index
		{
			get;
			set;
		} = index;

		/// <summary>
		/// Gets or sets the sprite group identifier.
		/// </summary>
		public int Identifier
		{
			get;
			set;
		} = identifier;

		/// <summary>
		/// Gets or sets the list of sprites.
		/// </summary>
		public List<Sprite> SpriteList
		{
			get;
			set;
		} = spriteList;

		public override string ToString()
		{
			return string.Concat( "SpriteGroup [Identifier=", this.Identifier, "; Sprites=", this.SpriteList.Count, "]" );
		}
	}
}
