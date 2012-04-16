
namespace MonkeyIsland1SpecialEditionXmlParser.Entities
{
	public class AnimationHeader
	{
		/// <summary>
		/// Gets or sets the address of the animation name.
		/// </summary>
		public int NameAddress
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the animation identifier.
		/// </summary>
		public int Identifier
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the number of animation frames.
		/// </summary>
		public int AnimationFrameCount
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the address for the first animation frame.
		/// </summary>
		public int AnimationFrameAddress
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat(
				this.NameAddress, "; ",
				this.Identifier, "; ",
				this.AnimationFrameCount, "; ",
				this.AnimationFrameAddress
				);
		}
	}
}
