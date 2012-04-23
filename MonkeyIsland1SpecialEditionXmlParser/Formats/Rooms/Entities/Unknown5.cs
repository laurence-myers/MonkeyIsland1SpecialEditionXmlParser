using System.Collections.Generic;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class Unknown5
	{
		public int Index
		{
			get;
			set;
		}

		public List<int> Int32List
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Join( "; ", this.Int32List );
		}
	}
}
