using System.Collections.Generic;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities
{
	public class Unknown6(
		int index,
		List<byte> byteList
	)
	{
		private Unknown6() : this(
			index: 0,
			byteList: null!
		) {}

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
