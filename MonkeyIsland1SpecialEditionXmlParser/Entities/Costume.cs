using System.Collections.Generic;

namespace MonkeyIsland1SpecialEditionXmlParser.Entities
{
	public class Costume
	{
		/// <summary>
		/// Gets or sets the header.
		/// </summary>
		public Header Header
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the list of unknown entries after the costume name.
		/// </summary>
		public List<UnknownAfterName> UnknownAfterNameList
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the list of unknown entries before the sprite sheets.
		/// </summary>
		public List<UnknownBeforeSpriteSheets> UnknownBeforeSpriteSheetsList
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the list of sprite sheets.
		/// </summary>
		public List<SpriteSheet> SpriteSheetList
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the list of animations.
		/// </summary>
		public List<Animation> AnimationList
		{
			get;
			set;
		}

		public override string ToString()
		{
			return this.Header.Name;
		}
	}
}
