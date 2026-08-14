using System.Collections.Generic;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities
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
		/// Gets or sets the object visibility changes the scripts make to this room's objects
		/// (see <see cref="ScriptScanner"/>), attributed by object number. Includes changes made
		/// by global scripts. Empty when the scripts were not scanned. Feeds the script-derived
		/// "initial state" view via <see cref="ScriptInitialVisibility"/>.
		/// </summary>
		public List<ObjectDrawChange> ObjectDrawChanges
		{
			get;
			set;
		} = new List<ObjectDrawChange>();

		/// <summary>
		/// Gets or sets the walkboxes (BOXD).
		/// </summary>
		public List<ClassicBox> BoxList
		{
			get;
			set;
		} = new List<ClassicBox>();

		/// <summary>
		/// Returns the mask (z-plane) number an actor standing at the point gets: the mask
		/// of the walkable box containing it, or of the nearest walkable box (the engine
		/// snaps placed actors into the closest box the same way). 0 - meaning the actor
		/// draws in front of the foreground props - when the room has no boxes.
		/// </summary>
		public int GetBoxMaskAt( int x, int y )
		{
			ClassicBox? best = null;
			var bestDistance = double.MaxValue;
			foreach( var box in this.BoxList )
			{
				if( !box.IsWalkable )
				{
					continue;
				}
				var distance = box.DistanceTo( x, y );
				if( distance < bestDistance )
				{
					bestDistance = distance;
					best = box;
				}
			}
			return best?.Mask ?? 0;
		}

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
