using System.Collections.Generic;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities
{
	/// <summary>
	/// Where a room entity's display name came from, so the UI can say how sure it is.
	/// </summary>
	public enum RoomEntityNameSource
	{
		/// <summary>No name anywhere in the classic data; the label is a generated placeholder.</summary>
		None,

		/// <summary>One of the entity's own frame objects carries the name (OBNA).</summary>
		FrameName,

		/// <summary>
		/// A different, named classic object encloses the frames (room 28's "fireplace" around the
		/// unnamed fire cels), so the entity borrows its name.
		/// </summary>
		EnclosingObject,
	}

	/// <summary>
	/// A room entity: a thing in the room that the classic game draws through several objects,
	/// one at a time - the frames of an animation (the SCUMM Bar's fire is objects 317, 318, 319)
	/// or the positions of a lever. SCUMM has no entity record; this is reconstructed from the
	/// data (see <see cref="RoomEntityGrouper"/>). Each frame is one SE sprite group.
	/// </summary>
	public class RoomEntity
	{
		/// <summary>Gets or sets the display name (cleaned of OBNA padding), or a placeholder when none was found.</summary>
		public string Name
		{
			get;
			set;
		} = "";

		/// <summary>
		/// The label to show for the entity: its name, or "Unnamed entity" when the classic data gave
		/// none. This is the single place the placeholder is decided, so the tree and the frame strip
		/// stay in step.
		/// </summary>
		public string DisplayName
		{
			get
			{
				return this.NameSource == RoomEntityNameSource.None ? "Unnamed entity" : this.Name;
			}
		}

		/// <summary>Gets or sets where <see cref="Name"/> came from.</summary>
		public RoomEntityNameSource NameSource
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the object number of the enclosing object the name was borrowed from, or
		/// 0 when the name source is not <see cref="RoomEntityNameSource.EnclosingObject"/>.
		/// </summary>
		public int NamedByObjectId
		{
			get;
			set;
		}

		/// <summary>Gets the SE sprite group indices of the frames, in frame order.</summary>
		public List<int> GroupIndices
		{
			get;
		} = new List<int>();

		/// <summary>Gets the classic object numbers of the frames, in frame order (parallel to <see cref="GroupIndices"/>).</summary>
		public List<int> ObjectIds
		{
			get;
		} = new List<int>();

		/// <summary>Gets or sets the shared classic X of the frames, in classic pixels.</summary>
		public int X
		{
			get;
			set;
		}

		/// <summary>Gets or sets the shared classic Y of the frames, in classic pixels.</summary>
		public int Y
		{
			get;
			set;
		}

		/// <summary>Gets or sets the shared classic width of the frames, in classic pixels.</summary>
		public int Width
		{
			get;
			set;
		}

		/// <summary>Gets or sets the shared classic height of the frames, in classic pixels.</summary>
		public int Height
		{
			get;
			set;
		}

		/// <summary>Gets the number of frames.</summary>
		public int FrameCount
		{
			get
			{
				return this.GroupIndices.Count;
			}
		}

		/// <summary>
		/// The 1-based frame number of a group in this entity, or 0 when the group is not a frame of it.
		/// </summary>
		public int FrameNumberOf( int groupIndex )
		{
			return this.GroupIndices.IndexOf( groupIndex ) + 1;
		}

		/// <summary>
		/// The object numbers as a compact range ("317-319") when they are consecutive, else a list.
		/// </summary>
		public string DescribeObjectIds()
		{
			return ClassicObjectNames.FormatRange( this.ObjectIds );
		}

		public override string ToString()
		{
			return string.Concat( this.DisplayName, "  [", this.FrameCount, " frames: objects ", this.DescribeObjectIds(), "]" );
		}
	}
}
