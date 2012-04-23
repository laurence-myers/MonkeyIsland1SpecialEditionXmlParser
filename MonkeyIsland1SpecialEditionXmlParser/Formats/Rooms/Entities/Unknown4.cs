
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class Unknown4
	{
		public int Index
		{
			get;
			set;
		}

		public int Unknown4_1Address
		{
			get;
			set;
		}

		public int Unknown4_2Address
		{
			get;
			set;
		}

		public float Unkn3
		{
			get;
			set;
		}

		public float Unkn4
		{
			get;
			set;
		}

		public Unknown4_1 Unknown4_1
		{
			get;
			set;
		}

		public Unknown4_2 Unknown4_2
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat(
				this.Unknown4_1Address, "; ",
				this.Unknown4_2Address, "; ",
				this.Unkn3, "; ",
				this.Unkn4
				);
		}
	}
}
