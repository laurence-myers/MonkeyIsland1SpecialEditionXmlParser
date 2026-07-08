using System.Collections.Generic;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Entities
{
	/// <summary>
	/// One limb track of an animation: the sequence of frames shown by a single sprite group
	/// while the animation plays.
	/// </summary>
	public class AnimationFrame(
		int index,
		int spriteGroupIdentifier,
		int playbackFlags,
		int frameCount,
		int frameAddress
	)
	{
		private AnimationFrame() : this(
			index: 0,
			spriteGroupIdentifier: 0,
			playbackFlags: 0,
			frameCount: 0,
			frameAddress: 0
		) {}

		/// <summary>
		/// Gets or sets the index of the animation frame.
		/// </summary>
		public int Index
		{
			get;
			set;
		} = index;

		/// <summary>
		/// Gets or sets the identifier of the sprite group (the classic SCUMM limb number).
		/// </summary>
		public int SpriteGroupIdentifier
		{
			get;
			set;
		} = spriteGroupIdentifier;

		/// <summary>
		/// Gets or sets the playback flags for this track. Observed values: 3 on looping
		/// animations (Init/Walk/Stand/Talk), 1 on one-shot animations (chores), 0 on
		/// empty tracks, 2 rare.
		/// </summary>
		public int PlaybackFlags
		{
			get;
			set;
		} = playbackFlags;

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
				this.PlaybackFlags, "; ",
				this.FrameCount, "; ",
				this.FrameAddress
				);
		}
	}
}
