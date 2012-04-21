
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities
{
	public class Frame
	{
		public int SpriteIdentifier
		{
			get;
			set;
		}

		public int UnknownInteger2
		{
			get;
			set;
		}

		public int UnknownInteger3
		{
			get;
			set;
		}

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
