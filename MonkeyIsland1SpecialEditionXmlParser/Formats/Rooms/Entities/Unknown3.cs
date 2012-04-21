
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class Unknown3
	{
		public byte Unkn1
		{
			get;
			set;
		}

		public byte Unkn2
		{
			get;
			set;
		}

		public byte Unkn3
		{
			get;
			set;
		}

		public byte Unkn4
		{
			get;
			set;
		}
		
		public int Unkn5
		{
			get;
			set;
		}

		public int Unkn6
		{
			get;
			set;
		}

		public int Addr7
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat(
				this.Unkn1, "; ",
				this.Unkn2, "; ",
				this.Unkn3, "; ",
				this.Unkn4, "; ",
				this.Unkn5, "; ",
				this.Unkn6, "; ",
				this.Addr7
				);
		}
	}
}
