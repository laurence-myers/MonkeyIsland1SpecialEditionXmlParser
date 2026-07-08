
namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Entities
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
		/// Gets or sets the animation identifier: the classic SCUMM animation number,
		/// chore * 4 + direction (0 = Left/west, 1 = Right/east, 2 = Front/south,
		/// 3 = Back/north). E.g. InitLeft = 4, WalkLeft = 8, StandLeft = 12,
		/// StartTalkLeft = 16, StopTalkLeft = 20.
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
