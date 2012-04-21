using System.Collections.Generic;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities
{
	public class SpriteGroup
	{
		/// <summary>
		/// Gets or sets the index of the sprite group.
		/// </summary>
		public int Index
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the sprite group identifier.
		/// </summary>
		public int Identifier
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the list of sprites.
		/// </summary>
		public List<Sprite> SpriteList
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat( "SpriteGroup [Identifier=", this.Identifier, "; Sprites=", this.SpriteList.Count, "]" );
		}
	}
}
