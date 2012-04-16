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
		/// Gets or sets the list of animation headers.
		/// </summary>
		public List<AnimationHeader> AnimationHeaderList
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the list of unknown entries before the texture file names.
		/// </summary>
		public List<PathPoint> PathPointList
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the list of sprite group headers.
		/// </summary>
		public List<SpriteGroupHeader> SpriteGroupHeaderList
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the list of texture file names.
		/// </summary>
		public List<TextureFileName> TextureFileNameList
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

		/// <summary>
		/// Gets or sets the list of sprite groups.
		/// </summary>
		public List<SpriteGroup> SpriteGroupList
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
