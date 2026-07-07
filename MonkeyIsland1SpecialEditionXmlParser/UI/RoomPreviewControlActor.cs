using System.Drawing;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Scumm.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	/// <summary>
	/// An actor shown by the <see cref="RoomPreviewControl"/>: a costume rendered in its
	/// standing pose, positioned where the classic scripts place the actor. Actors are a
	/// display-only overlay - they are not part of the SE room file.
	/// </summary>
	public class RoomPreviewControlActor( ClassicActorPlacement placement, Bitmap? image, PointF position, string label )
	{
		/// <summary>
		/// Gets or sets the placement the overlay was built from.
		/// </summary>
		public ClassicActorPlacement Placement
		{
			get;
			set;
		} = placement;

		/// <summary>
		/// Gets or sets the composited costume image; null when the costume could not be
		/// resolved or rendered.
		/// </summary>
		public Bitmap? Image
		{
			get;
			set;
		} = image;

		/// <summary>
		/// Gets or sets the image's top left corner in HD room pixels.
		/// </summary>
		public PointF Position
		{
			get;
			set;
		} = position;

		/// <summary>
		/// Gets or sets the text shown for the actor in the actor list.
		/// </summary>
		public string Label
		{
			get;
			set;
		} = label;

		/// <summary>
		/// Gets or sets a value indicating whether the actor is drawn.
		/// </summary>
		public bool Visible
		{
			get;
			set;
		} = true;

		/// <summary>
		/// Gets or sets whether the actor draws in front of the static foreground props,
		/// like the game does for actors on walkboxes with mask 0 (e.g. the pirate leaders,
		/// whose hands rest on their table); actors on masked boxes draw behind the
		/// foreground (e.g. the storekeeper behind his counter).
		/// </summary>
		public bool DrawAboveForeground
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the draw rectangle in HD room pixels.
		/// </summary>
		public RectangleF ScreenRect
		{
			get
			{
				return new RectangleF(
					this.Position,
					this.Image != null ? this.Image.Size : Size.Empty
				);
			}
		}

		public override string ToString()
		{
			return this.Label;
		}
	}
}
