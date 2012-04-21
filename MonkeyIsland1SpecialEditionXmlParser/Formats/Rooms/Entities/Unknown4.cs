
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class Unknown4
	{
		public int NameAddress
		{
			get;
			set;
		}

		public int DataCount
		{
			get;
			set;
		}

		public int DataAddress
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat(
				this.NameAddress, "; ",
				this.DataCount, "; ",
				this.DataAddress
				);
		}
	}
}
