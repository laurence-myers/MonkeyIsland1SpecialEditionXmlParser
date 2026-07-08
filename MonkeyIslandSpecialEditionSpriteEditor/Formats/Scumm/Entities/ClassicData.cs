using System.Collections.Generic;
using System.Linq;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities
{
	/// <summary>
	/// The fully loaded classic SCUMM data set: all rooms with their objects,
	/// plus the room name table when the index file was available.
	/// </summary>
	public class ClassicData(
		List<ClassicRoom> roomList,
		Dictionary<int, string> roomNames,
		List<ClassicCostume> costumeList,
		string source
	)
	{
		private ClassicData() : this(
			roomList: new List<ClassicRoom>(),
			roomNames: new Dictionary<int, string>(),
			costumeList: new List<ClassicCostume>(),
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
		/// Gets or sets the costumes, in costume number order. Empty when the index file
		/// (with the DCOS costume directory) was not available.
		/// </summary>
		public List<ClassicCostume> CostumeList
		{
			get;
			set;
		} = costumeList;

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

		/// <summary>
		/// Finds a costume by its classic costume number (matches the SE costume's
		/// Header.Identifier).
		/// </summary>
		public ClassicCostume? FindCostume( int costumeId )
		{
			return this.CostumeList.FirstOrDefault( c => c.CostumeId == costumeId );
		}

		public override string ToString()
		{
			return string.Concat( this.RoomList.Count, " rooms from ", this.Source );
		}
	}
}
