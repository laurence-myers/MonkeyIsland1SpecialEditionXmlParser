using System.Drawing;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	/// <summary>
	/// An actor shown by the <see cref="RoomPreviewControl"/>: a costume rendered in its
	/// standing pose, positioned where the classic scripts place the actor. Actors are a
	/// display-only overlay - they are not part of the SE room file.
	/// </summary>
	public class RoomPreviewControlActor( ClassicActorPlacement placement, Bitmap? image, PointF origin, string label )
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
		/// Gets or sets the actor origin - its feet - within <see cref="Image"/>. The room's
		/// classic-to-HD transform places the origin, which in turn gives <see cref="Position"/>;
		/// keeping the two apart lets the actor be re-placed when the transform is edited.
		/// </summary>
		public PointF Origin
		{
			get;
			set;
		} = origin;

		/// <summary>
		/// Gets or sets the image's top left corner in HD room pixels (already accounting for
		/// <see cref="Scale"/>, so the feet stay on the placement).
		/// </summary>
		public PointF Position
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a uniform scale applied to the drawn image. The costume image is
		/// composited at the costume scale (which matches the usual 144-line rooms); in a
		/// fullscreen room, whose art is smaller, this drops below 1 so the actor sits at the
		/// room's own size rather than oversized. The feet stay anchored because
		/// <see cref="Position"/> is set from the scaled origin.
		/// </summary>
		public float Scale
		{
			get;
			set;
		} = 1.0f;

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
				var size = this.Image != null ? this.Image.Size : Size.Empty;
				return new RectangleF(
					this.Position,
					new SizeF( size.Width * this.Scale, size.Height * this.Scale )
				);
			}
		}

		public override string ToString()
		{
			return this.Label;
		}
	}
}
