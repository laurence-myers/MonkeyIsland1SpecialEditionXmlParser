using System.Collections.Generic;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities
{
	/// <summary>
	/// The list of <see cref="RoomObject"/> instances belonging to one
	/// <see cref="RoomObjectHeader"/>.
	/// </summary>
	public class RoomObjectGroup(
		List<RoomObject> roomObjectList
	)
	{
		private RoomObjectGroup() : this(
			roomObjectList: null!
		) {}

		public int Index
		{
			get;
			set;
		}

		public List<RoomObject> RoomObjectList
		{
			get;
			set;
		} = roomObjectList;
	}
}
