
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class Unknown4Header(
		int unknown4NameAddress,
		int unknown4Count,
		int unknown4Address
	)
	{
		private Unknown4Header() : this(
			unknown4NameAddress: 0,
			unknown4Count: 0,
			unknown4Address: 0
		) {}

		public int Unknown4NameAddress
		{
			get;
			set;
		} = unknown4NameAddress;

		public int Unknown4Count
		{
			get;
			set;
		} = unknown4Count;

		public int Unknown4Address
		{
			get;
			set;
		} = unknown4Address;

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
