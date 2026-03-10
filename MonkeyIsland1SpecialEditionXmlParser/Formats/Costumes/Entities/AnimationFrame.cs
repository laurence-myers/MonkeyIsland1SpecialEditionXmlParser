using System.Collections.Generic;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities
{
	public class AnimationFrame(
		int index,
		int spriteGroupIdentifier,
		int unknownInteger1,
		int frameCount,
		int frameAddress
	)
	{
		/// <summary>
		/// Gets or sets the index of the animation frame.
		/// </summary>
		public int Index
		{
			get;
			set;
		} = index;

		/// <summary>
		/// Gets or sets the identifier of the sprite group.
		/// </summary>
		public int SpriteGroupIdentifier
		{
			get;
			set;
		} = spriteGroupIdentifier;

		public int UnknownInteger1
		{
			get;
			set;
		} = unknownInteger1;

		/// <summary>
		/// Gets or sets the number of frames.
		/// </summary>
		public int FrameCount
		{
			get;
			set;
		} = frameCount;

		/// <summary>
		/// Gets or sets the byte address of the first frame.
		/// </summary>
		public int FrameAddress
		{
			get;
			set;
		} = frameAddress;

		/// <summary>
		/// Gets or sets the list of frame.
		/// </summary>
		public List<Frame>? FrameList
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat(
				this.SpriteGroupIdentifier, "; ",
				this.UnknownInteger1, "; ",
				this.FrameCount, "; ",
				this.FrameAddress
				);
		}
	}
}
