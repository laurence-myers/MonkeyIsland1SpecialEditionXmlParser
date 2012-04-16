
namespace MonkeyIsland1SpecialEditionXmlParser.Entities
{
	public class AnimationFrame
	{
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

		public int UnknownInteger3
		{
			get;
			set;
		}

		public int UnknownInteger4
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat(
				this.UnknownInteger1, "; ",
				this.UnknownInteger2, "; ",
				this.UnknownInteger3, "; ",
				this.UnknownInteger4
				);
		}
	}
}
