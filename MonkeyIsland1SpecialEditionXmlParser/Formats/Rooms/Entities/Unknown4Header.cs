
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class Unknown4Header
	{
		public int Unknown4NameAddress
		{
			get;
			set;
		}

		public int Unknown4Count
		{
			get;
			set;
		}

		public int Unknown4Address
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat(
				this.Unknown4NameAddress, "; ",
				this.Unknown4Count, "; ",
				this.Unknown4Address
				);
		}
	}
}
