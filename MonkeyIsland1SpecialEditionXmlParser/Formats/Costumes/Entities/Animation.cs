using System.Collections.Generic;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities
{
	public class Animation
	{
		/// <summary>
		/// Gets or sets the name of the animation.
		/// </summary>
		public string Name
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a list of frames for the animation.
		/// </summary>
		public List<AnimationFrame> AnimationFrameList
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat( "Animation [Name=", this.Name, "; Frames=", this.AnimationFrameList.Count, "]" );
		}
	}
}
