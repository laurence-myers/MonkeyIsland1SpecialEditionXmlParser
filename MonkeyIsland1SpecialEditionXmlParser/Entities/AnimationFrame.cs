
namespace MonkeyIsland1SpecialEditionXmlParser.Entities
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

		public int UnknownInteger2
		{
			get;
			set;
		}

		public int FrameAddress
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat(
				this.SpriteGroupIdentifier, "; ",
				this.UnknownInteger1, "; ",
				this.UnknownInteger2, "; ",
				this.FrameAddress
				);
		}
	}
}
