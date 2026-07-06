
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities
{
	/// <summary>
	/// An attachment point referenced by a <see cref="Sprite"/> via
	/// <see cref="Sprite.PathPointIndex"/>, used by e.g. sword fighting costumes to anchor
	/// props to a frame.
	/// </summary>
	public class PathPoint
	{
		/// <summary>
		/// Gets or sets the path point type (attachment slot). Observed values 0 to 4; the
		/// header's <see cref="Header.PathPointTypeCount"/> is the highest type plus one.
		/// </summary>
		public byte Type
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a flag whose exact meaning is unknown. Observed values 0 and 1.
		/// </summary>
		public byte Flag
		{
			get;
			set;
		}

		/// <summary>
		/// Always 0 in the retail data; most likely padding.
		/// </summary>
		public byte UnknownByte3
		{
			get;
			set;
		}

		/// <summary>
		/// Always 0 in the retail data; most likely padding.
		/// </summary>
		public byte UnknownByte4
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the horizontal position relative to the actor origin, in HD pixels.
		/// </summary>
		public float X
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the vertical position relative to the actor origin, in HD pixels.
		/// </summary>
		public float Y
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat(
				this.Type, "; ",
				this.Flag, "; ",
				this.X, "; ",
				this.Y
				);
		}
	}
}
