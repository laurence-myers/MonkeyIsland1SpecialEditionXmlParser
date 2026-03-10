using System.Collections.Generic;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class Unknown4Group(
		List<Unknown4> unknown4List
	)
	{
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
