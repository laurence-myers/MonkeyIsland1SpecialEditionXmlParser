
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class Unknown5Header(
		int unknown5Count,
		int unknown5Address
	)
	{
		private Unknown5Header() : this(
			unknown5Count: 0,
			unknown5Address: 0
		) {}

		public int Unknown5Count
		{
			get;
			set;
		} = unknown5Count;

		public int Unknown5Address
		{
			get;
			set;
		} = unknown5Address;

		public override string ToString()
		{
			return string.Concat(
				this.Unknown5Count, "; ",
				this.Unknown5Address
				);
		}
	}
}
