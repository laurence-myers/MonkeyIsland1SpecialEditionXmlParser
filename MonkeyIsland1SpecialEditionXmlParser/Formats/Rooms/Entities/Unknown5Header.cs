
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class Unknown5Header
	{
		public int Unkn1
		{
			get;
			set;
		}

		public int Addr2
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat(
				this.Unkn1, "; ",
				this.Addr2
				);
		}
	}
}
