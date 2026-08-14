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
		/// Gets or sets the state and owner every object has when a new game starts (DOBJ),
		/// keyed by object number. Empty when the index file was not available.
		/// </summary>
		public Dictionary<int, ClassicObjectStartState> ObjectStartStates
		{
			get;
			set;
		} = new Dictionary<int, ClassicObjectStartState>();

		/// <summary>
		/// Gets or sets a human readable description of where the data was loaded from.
		/// </summary>
		public string Source
		{
			get;
			set;
		} = source;

		/// <summary>
		/// Gets or sets the full path of the loose resource file (.001) the data was read from,
		/// or null when it came from inside the pak. Writing walkboxes patches this file (or, for
		/// pak data, a loose override created beside the pak).
		/// </summary>
		public string? LooseDataFilePath
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the full path of the loose index file (.000) beside the resource file, or
		/// null when it came from inside the pak or has no index sibling.
		/// </summary>
		public string? LooseIndexFilePath
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the pak's resource (.001) entry name when the data was read from inside
		/// the pak, or null when it was loose. Used to place a loose override at the same path.
		/// </summary>
		public string? PakDataEntryName
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the pak's index (.000) entry name when the data was read from inside the
		/// pak, or null. The index sibling is extracted next to a resource override so room names
		/// and costumes survive a reload.
		/// </summary>
		public string? PakIndexEntryName
		{
			get;
			set;
		}

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
