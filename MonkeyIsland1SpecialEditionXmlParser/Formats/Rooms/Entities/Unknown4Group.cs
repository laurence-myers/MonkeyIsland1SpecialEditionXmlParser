using System.Collections.Generic;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class Unknown4Group(
		List<Unknown4> unknown4List
	)
	{
		private Unknown4Group() : this(
			unknown4List: null!
		) {}

		public int Index
		{
			get;
			set;
		}

		public List<Unknown4> Unknown4List
		{
			get;
			set;
		} = unknown4List;
	}
}
