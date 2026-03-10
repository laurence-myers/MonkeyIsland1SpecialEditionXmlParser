
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class Unknown4_2(
		int unkn1,
		int unkn2,
		int unkn3,
		int unkn4
	)
	{
		private Unknown4_2() : this(
			unkn1: 0,
			unkn2: 0,
			unkn3: 0,
			unkn4: 0
		) {}

		public int Unkn1
		{
			get;
			set;
		} = unkn1;

		public int Unkn2
		{
			get;
			set;
		} = unkn2;

		public int Unkn3
		{
			get;
			set;
		} = unkn3;

		public int Unkn4
		{
			get;
			set;
		} = unkn4;

		public override string ToString()
		{
			return string.Concat(
				this.Unkn1, "; ",
				this.Unkn2, "; ",
				this.Unkn3, "; ",
				this.Unkn4
				);
		}
	}
}
