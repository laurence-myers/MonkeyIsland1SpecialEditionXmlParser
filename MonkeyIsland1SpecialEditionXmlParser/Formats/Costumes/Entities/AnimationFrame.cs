using System.Collections.Generic;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities
{
	public class AnimationFrame
	{
		/// <summary>
		/// Gets or sets the index of the animation frame.
		/// </summary>
		public int Index
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the identifier of the sprite group.
		/// </summary>
		public int SpriteGroupIdentifier
		{
			get;
			set;
		}

		public int UnknownInteger1
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the number of frames.
		/// </summary>
		public int FrameCount
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the byte address of the first frame.
		/// </summary>
		public int FrameAddress
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the list of frame.
		/// </summary>
		public List<Frame> FrameList
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
