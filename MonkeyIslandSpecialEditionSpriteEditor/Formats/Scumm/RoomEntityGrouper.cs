using System.Collections.Generic;
using System.Linq;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm
{
	/// <summary>
	/// Reconstructs a room's entities - things the classic game draws through several objects, one
	/// at a time - from the object data, since SCUMM stores no entity record of its own.
	///
	/// The frames of one thing are recognisable: a run of consecutive SE sprite groups whose
	/// classic objects sit at the exact same position and size (room 28's fire is objects 317,
	/// 318, 319, all 16x16 at 520,80; each swinging pirate is three objects at one spot). The
	/// scripts confirm the reading - they draw such runs as <c>base + random(n)</c> or step
	/// through them in a loop - but they never name them. The only names in the classic data are
	/// the per-object OBNA strings, and for an animation usually only the first frame (or none)
	/// carries one. Surveyed over the retail rooms: of 72 same-rect runs, 16 have a frame name,
	/// none is ever named by a script's setObjectName, 7 sit inside a larger named object (the
	/// fire inside "fireplace", the store lever inside "handle"), and 49 have no name at all.
	/// So the name comes from a frame when one has it, else from the smallest named object that
	/// encloses the frames, else a placeholder.
	///
	/// A run whose frames carry two or more different names is not one entity: room 58's map
	/// hotspots ("campsite", "bones", "stump"...) all share one rectangle, and room 20's "banana"
	/// and "bananas" are two objects, so such runs are left ungrouped.
	/// </summary>
	public static class RoomEntityGrouper
	{
		/// <summary>
		/// Groups the SE sprite groups of a room into entities.
		/// </summary>
		/// <param name="groupObjectIds">The classic object number of each SE sprite group, in group order.</param>
		/// <param name="objectsById">The room's classic objects by number.</param>
		/// <returns>The entities found, in group order; groups outside a run are not in any entity.</returns>
		public static List<RoomEntity> Group( IReadOnlyList<int> groupObjectIds, IReadOnlyDictionary<int, ClassicObject> objectsById )
		{
			var entities = new List<RoomEntity>();
			var index = 0;
			while( index < groupObjectIds.Count )
			{
				ClassicObject first;
				if( !objectsById.TryGetValue( groupObjectIds[index], out first ) || first.Width <= 0 || first.Height <= 0 )
				{
					index++;
					continue;
				}

				var end = index + 1;
				while( end < groupObjectIds.Count
					&& objectsById.TryGetValue( groupObjectIds[end], out var next )
					&& next.X == first.X && next.Y == first.Y && next.Width == first.Width && next.Height == first.Height )
				{
					end++;
				}

				if( end - index >= 2 )
				{
					var entity = BuildEntity( groupObjectIds, objectsById, index, end );
					if( entity != null )
					{
						entities.Add( entity );
					}
				}
				index = end;
			}
			return entities;
		}

		private static RoomEntity? BuildEntity( IReadOnlyList<int> groupObjectIds, IReadOnlyDictionary<int, ClassicObject> objectsById, int start, int end )
		{
			var frames = new List<ClassicObject>();
			for( var groupIndex = start; groupIndex < end; groupIndex++ )
			{
				frames.Add( objectsById[groupObjectIds[groupIndex]] );
			}

			// two or more different names means two or more different things sharing a rectangle
			var names = frames.Select( f => ClassicObjectNames.CleanName( f.Name ) ).Where( n => n.Length > 0 ).Distinct().ToList();
			if( names.Count > 1 )
			{
				return null;
			}

			var first = frames[0];
			var entity = new RoomEntity
			{
				X = first.X,
				Y = first.Y,
				Width = first.Width,
				Height = first.Height,
			};
			for( var groupIndex = start; groupIndex < end; groupIndex++ )
			{
				entity.GroupIndices.Add( groupIndex );
				entity.ObjectIds.Add( groupObjectIds[groupIndex] );
			}

			if( names.Count == 1 )
			{
				entity.Name = ClassicObjectNames.Capitalize( names[0] );
				entity.NameSource = RoomEntityNameSource.FrameName;
				return entity;
			}

			// borrow the name of the smallest named object at this spot (the fire is drawn inside the
			// "fireplace"; the store's lever positions inside its "handle"). The test is non-strict, so
			// a named object with the same rectangle as the frames (room 30's "handle" is exactly the
			// lever's rect) qualifies too - that is the intended name, not an accident.
			var frameIds = new HashSet<int>( entity.ObjectIds );
			var enclosing = objectsById.Values
				.Where( o => !frameIds.Contains( o.ObjectId ) && ClassicObjectNames.CleanName( o.Name ).Length > 0
					&& o.X <= first.X && o.Y <= first.Y
					&& o.X + o.Width >= first.X + first.Width && o.Y + o.Height >= first.Y + first.Height )
				.OrderBy( o => o.Width * o.Height )
				.ThenBy( o => o.ObjectId )
				.FirstOrDefault();
			if( enclosing != null )
			{
				entity.Name = ClassicObjectNames.Capitalize( ClassicObjectNames.CleanName( enclosing.Name ) );
				entity.NameSource = RoomEntityNameSource.EnclosingObject;
				entity.NamedByObjectId = enclosing.ObjectId;
				return entity;
			}

			entity.Name = "Objects " + entity.DescribeObjectIds();
			entity.NameSource = RoomEntityNameSource.None;
			return entity;
		}
	}
}
