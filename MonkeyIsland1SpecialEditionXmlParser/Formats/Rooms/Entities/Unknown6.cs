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

		public List<Int16> Int16List
		{
			get;
			set;
		}

		public List<Int32> Int32List
		{
			get;
			set;
		}

		public List<float> FloatList
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
