using System.Collections.Generic;
using System.Linq;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Scumm.Entities
{
	/// <summary>
	/// The fully loaded classic SCUMM data set: all rooms with their objects,
	/// plus the room name table when the index file was available.
	/// </summary>
	public class ClassicData(
		List<ClassicRoom> roomList,
		Dictionary<int, string> roomNames,
		string source
	)
	{
		private ClassicData() : this(
			roomList: new List<ClassicRoom>(),
			roomNames: new Dictionary<int, string>(),
			source: ""
		) {}

		/// <summary>
		/// Gets or sets the rooms, in file order.
		/// </summary>
		public List<ClassicRoom> RoomList
		{
			get;
			set;
		} = roomList;

		/// <summary>
		/// Gets or sets the room names keyed by room number (RNAM).
		/// </summary>
		public Dictionary<int, string> RoomNames
		{
			get;
			set;
		} = roomNames;

		/// <summary>
		/// Gets or sets a human readable description of where the data was loaded from.
		/// </summary>
		public string Source
		{
			get;
			set;
		} = source;

		/// <summary>
		/// Finds a room by its classic room number (matches the SE room's Header.Identifier).
		/// </summary>
		public ClassicRoom? FindRoom( int roomNumber )
		{
			return this.RoomList.FirstOrDefault( r => r.RoomNumber == roomNumber );
		}

		public override string ToString()
		{
			return string.Concat( this.RoomList.Count, " rooms from ", this.Source );
		}
	}
}
