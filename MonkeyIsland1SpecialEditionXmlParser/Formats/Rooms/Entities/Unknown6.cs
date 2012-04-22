using System;
using System.Collections.Generic;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class Unknown6
	{
		public int Index
		{
			get;
			set;
		}

		public List<byte> ByteList
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Join( "; ", this.ByteList.ToArray() );
		}
	}
}
