
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities
{
	public class Frame(
		int spriteIdentifier,
		int unknownInteger2,
		int unknownInteger3
	)
	{
		private Frame() : this(
			spriteIdentifier: 0,
			unknownInteger2: 0,
			unknownInteger3: 0
		) {}

		public int SpriteIdentifier
		{
			get;
			set;
		} = spriteIdentifier;

		public int UnknownInteger2
		{
			get;
			set;
		} = unknownInteger2;

		public int UnknownInteger3
		{
			get;
			set;
		} = unknownInteger3;

		public override string ToString()
		{
			return string.Concat(
				this.SpriteIdentifier, "; ",
				this.UnknownInteger2, "; ",
				this.UnknownInteger3
				);
		}
	}
}
