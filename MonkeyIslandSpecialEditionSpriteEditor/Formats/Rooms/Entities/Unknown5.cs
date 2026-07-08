using System.Collections.Generic;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities
{
	public class Unknown5( int index, List<int> int32List )
	{
		private Unknown5() : this(
			index: 0,
			int32List: null!
		) {}

		public int Index
		{
			get;
			set;
		} = index;

		public List<int> Int32List
		{
			get;
			set;
		} = int32List;

		public override string ToString()
		{
			return string.Join( "; ", this.Int32List );
		}
	}
}
