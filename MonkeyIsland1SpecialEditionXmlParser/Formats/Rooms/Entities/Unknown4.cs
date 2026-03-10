
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class Unknown4(
		int index,
		int unknown41Address,
		int unknown42Address,
		float unkn3,
		float unkn4
	)
	{
		private Unknown4() : this(
			index: 0,
			unknown41Address: 0,
			unknown42Address: 0,
			unkn3: 0,
			unkn4: 0
		) {}

		public int Index
		{
			get;
			set;
		} = index;

		public int Unknown4_1Address
		{
			get;
			set;
		} = unknown41Address;

		public int Unknown4_2Address
		{
			get;
			set;
		} = unknown42Address;

		public float Unkn3
		{
			get;
			set;
		} = unkn3;

		public float Unkn4
		{
			get;
			set;
		} = unkn4;

		public Unknown4_1? Unknown4_1
		{
			get;
			set;
		}

		public Unknown4_2? Unknown4_2
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
