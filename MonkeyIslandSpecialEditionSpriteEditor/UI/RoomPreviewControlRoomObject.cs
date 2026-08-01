using System.Collections.Generic;
using System.Drawing;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	/// <summary>
	/// A room object overlay shown by the <see cref="RoomPreviewControl"/>: the entity that
	/// carries the screen offset, plus the resolved draws (one for a sprite-variant object,
	/// one per chunk for an image-variant object). Draw positions are relative to the
	/// entity's offset, so moving the object only needs a repaint, not a rebuild.
	/// </summary>
	public class RoomPreviewControlRoomObject( RoomObject roomObject, string name )
	{
		/// <summary>
		/// Gets the room object entity; its OffsetX/OffsetY place the overlay on screen.
		/// </summary>
		public RoomObject RoomObject
		{
			get;
		} = roomObject;

		/// <summary>
		/// Gets the object's name from its header (e.g. "Water"), for labels.
		/// </summary>
		public string Name
		{
			get;
		} = name;

		/// <summary>
		/// Gets the resolved draws, in chunk order for image-variant objects.
		/// </summary>
		public List<RoomPreviewControlRoomObjectDraw> Draws
		{
			get;
		} = new List<RoomPreviewControlRoomObjectDraw>();

		/// <summary>
		/// Gets or sets a value indicating whether the overlay is drawn. Room objects
		/// default to hidden: the game composes them with per-name logic (animation,
		/// scripted states) the preview can only approximate.
		/// </summary>
		public bool Visible
		{
			get;
			set;
		} = true;

		/// <summary>
		/// Gets the overlay's screen rectangle: the union of the draws, placed at the
		/// entity's current offset.
		/// </summary>
		public RectangleF ScreenRect
		{
			get
			{
				if( this.Draws.Count == 0 )
				{
					return new RectangleF( this.RoomObject.OffsetX, this.RoomObject.OffsetY, 0, 0 );
				}

				var union = this.Draws[0].RelativeRect;
				for( var index = 1; index < this.Draws.Count; index++ )
				{
					union = RectangleF.Union( union, this.Draws[index].RelativeRect );
				}
				union.Offset( this.RoomObject.OffsetX, this.RoomObject.OffsetY );
				return union;
			}
		}

		public override string ToString()
		{
			return string.Concat( this.Name, "/", this.RoomObject.Index, " @ ", this.ScreenRect );
		}
	}

	/// <summary>
	/// One draw of a <see cref="RoomPreviewControlRoomObject"/>: a texture region placed
	/// relative to the owning object's screen offset.
	/// </summary>
	public class RoomPreviewControlRoomObjectDraw( Image? texture, RectangleF sourceRect, RectangleF relativeRect )
	{
		/// <summary>
		/// Gets or sets the texture the draw samples from; null when missing.
		/// </summary>
		public Image? Texture
		{
			get;
			set;
		} = texture;

		/// <summary>
		/// Gets or sets the source rectangle within the texture.
		/// </summary>
		public RectangleF SourceRect
		{
			get;
			set;
		} = sourceRect;

		/// <summary>
		/// Gets or sets the destination rectangle, relative to the owning object's offset.
		/// </summary>
		public RectangleF RelativeRect
		{
			get;
			set;
		} = relativeRect;

		/// <summary>
		/// Gets or sets a value indicating whether this draw is shown (image chunks can be
		/// toggled individually from the tree).
		/// </summary>
		public bool Visible
		{
			get;
			set;
		} = true;
	}
}
