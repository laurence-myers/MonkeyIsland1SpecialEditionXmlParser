using System.Collections.Generic;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class Unknown6(
		int index,
		List<byte> byteList
	)
	{
		public int Index
		{
			get;
			set;
		} = index;

		public List<byte> ByteList
		{
			get;
			set;
		} = byteList;

		public override string ToString()
		{
			return string.Join( "; ", this.ByteList.ToArray() );
		}
	}
}
