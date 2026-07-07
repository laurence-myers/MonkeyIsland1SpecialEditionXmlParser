using System.Collections.Generic;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Scumm.Entities
{
	public class ClassicRoom(
		int roomNumber,
		int width,
		int height,
		List<ClassicObject> objectList
	)
	{
		private ClassicRoom() : this(
			roomNumber: 0,
			width: 0,
			height: 0,
			objectList: new List<ClassicObject>()
		) {}

		/// <summary>
		/// Gets or sets the classic room number (LOFF). Matches the SE room's Header.Identifier.
		/// </summary>
		public int RoomNumber
		{
			get;
			set;
		} = roomNumber;

		/// <summary>
		/// Gets or sets the room name (RNAM, from the index file), if known.
		/// </summary>
		public string? Name
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the room width in classic pixels (RMHD).
		/// </summary>
		public int Width
		{
			get;
			set;
		} = width;

		/// <summary>
		/// Gets or sets the room height in classic pixels (RMHD).
		/// </summary>
		public int Height
		{
			get;
			set;
		} = height;

		/// <summary>
		/// Gets or sets the objects defined in the room (OBCD).
		/// </summary>
		public List<ClassicObject> ObjectList
		{
			get;
			set;
		} = objectList;

		/// <summary>
		/// Gets or sets the actor placements extracted from the room's scripts (see
		/// <see cref="ScriptScanner"/>), in setup order. Empty when the scripts were not
		/// scanned or place no actors here.
		/// </summary>
		public List<ClassicActorPlacement> ActorPlacementList
		{
			get;
			set;
		} = new List<ClassicActorPlacement>();

		/// <summary>
		/// Returns the room's objects keyed by object number. The first occurrence wins on duplicates.
		/// </summary>
		public Dictionary<int, ClassicObject> GetObjectsById()
		{
			var objectsById = new Dictionary<int, ClassicObject>();
			foreach( var classicObject in this.ObjectList )
			{
				if( !objectsById.ContainsKey( classicObject.ObjectId ) )
				{
					objectsById.Add( classicObject.ObjectId, classicObject );
				}
			}
			return objectsById;
		}

		public override string ToString()
		{
			return string.Concat( this.RoomNumber, "-", this.Name, " (", this.ObjectList.Count, " objects)" );
		}
	}
}
