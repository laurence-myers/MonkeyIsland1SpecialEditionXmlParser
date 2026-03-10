using System.Collections.Generic;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities
{
	public class Animation(
		string name,
		List<AnimationFrame> animationFrameList
	)
	{
		private Animation() : this(
			name: null!,
			animationFrameList: null!
		) {}
		
		/// <summary>
		/// Gets or sets the name of the animation.
		/// </summary>
		public string Name
		{
			get;
			set;
		} = name;

		/// <summary>
		/// Gets or sets a list of frames for the animation.
		/// </summary>
		public List<AnimationFrame> AnimationFrameList
		{
			get;
			set;
		} = animationFrameList;

		public override string ToString()
		{
			return string.Concat( "Animation [Name=", this.Name, "; Frames=", this.AnimationFrameList.Count, "]" );
		}
	}
}
