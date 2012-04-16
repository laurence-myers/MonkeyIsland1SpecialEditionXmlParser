using System.Drawing;

namespace MonkeyIsland1SpecialEditionXmlParser.Entities
{
	public class UnknownBeforeSpriteSheets
	{
		public int UnknownInteger
		{
			get;
			set;
		}

		public Color UnknownColor1
		{
			get;
			set;
		}

		public Color UnknownColor2
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat(
				this.UnknownInteger, "; ",
				this.UnknownColor1, "; ",
				this.UnknownColor2
				);
		}
	}
}
