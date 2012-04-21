
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class Unknown5
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

		public int Unkn3
		{
			get;
			set;
		}

		public int Unkn4
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat(
				this.Unkn1, "; ",
				this.Addr2, "; ",
				this.Unkn3, "; ",
				this.Unkn4
				);
		}
	}
}
