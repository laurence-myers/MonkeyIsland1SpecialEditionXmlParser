
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities
{
	public class Frame(
		int spriteIdentifier,
		int command,
		string? soundName
	)
	{
		/// <summary>
		/// The <see cref="Command"/> value that plays <see cref="SoundName"/>.
		/// </summary>
		public const int PlaySoundCommand = 6;

		private Frame() : this(
			spriteIdentifier: 0,
			command: 0,
			soundName: null
		) {}

		/// <summary>
		/// Gets or sets the sprite shown by this frame, or -1 when the frame is a command
		/// instead of a sprite. The value is resolved against the sprite group's
		/// <see cref="SpriteGroup.FirstSpriteIdentifier"/>: the sprite drawn is
		/// SpriteList[SpriteIdentifier - FirstSpriteIdentifier].
		/// </summary>
		public int SpriteIdentifier
		{
			get;
			set;
		} = spriteIdentifier;

		/// <summary>
		/// Gets or sets the command executed when <see cref="SpriteIdentifier"/> is -1; 0 for
		/// ordinary sprite frames. Observed values: 1 (common; used by many animations),
		/// 2 (mostly Stand* animations), 3 (mostly Walk* animations), 4 (rare) and
		/// 6 (play the sound named by <see cref="SoundName"/>).
		/// </summary>
		public int Command
		{
			get;
			set;
		} = command;

		/// <summary>
		/// Gets or sets the name of the sound played by this frame (command 6), or null.
		/// Stored in the file as a relative pointer to a shared string at the end of the file.
		/// </summary>
		public string? SoundName
		{
			get;
			set;
		} = soundName;

		public override string ToString()
		{
			return string.Concat(
				this.SpriteIdentifier, "; ",
				this.Command, "; ",
				this.SoundName
				);
		}
	}
}
