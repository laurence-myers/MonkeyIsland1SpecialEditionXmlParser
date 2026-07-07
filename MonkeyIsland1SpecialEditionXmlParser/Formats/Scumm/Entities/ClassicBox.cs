using System;
using System.Drawing;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Scumm.Entities
{
	/// <summary>
	/// A classic SCUMM walkbox (BOXD): a quad the actors walk on, carrying the mask (z-plane)
	/// number that decides whether an actor standing on it is drawn behind the foreground
	/// props. Many boxes are degenerate line segments ("stand here" spots); the placeholder
	/// box 0 sits at (-32000, -32000).
	/// </summary>
	public class ClassicBox(
		Point[] cornerList,
		int mask,
		int flags,
		int scale
	)
	{
		private ClassicBox() : this(
			cornerList: new Point[4],
			mask: 0,
			flags: 0,
			scale: 255
		) {}

		/// <summary>
		/// Gets or sets the four corners (upper left, upper right, lower right, lower left)
		/// in classic room pixels.
		/// </summary>
		public Point[] CornerList
		{
			get;
			set;
		} = cornerList;

		/// <summary>
		/// Gets or sets the mask (z-plane) number: 0 means actors on this box draw in front
		/// of the foreground props, higher values put them behind the corresponding plane.
		/// </summary>
		public int Mask
		{
			get;
			set;
		} = mask;

		/// <summary>
		/// Gets or sets the box flags; 0x80 marks a box invisible to actors.
		/// </summary>
		public int Flags
		{
			get;
			set;
		} = flags;

		/// <summary>
		/// Gets or sets the actor scale on this box (0x8000 selects a scale slot).
		/// </summary>
		public int Scale
		{
			get;
			set;
		} = scale;

		/// <summary>
		/// Gets a value indicating whether actors can be snapped onto this box.
		/// </summary>
		public bool IsWalkable
		{
			get
			{
				return ( this.Flags & 0x80 ) == 0;
			}
		}

		/// <summary>
		/// Returns whether the point lies inside the quad. Degenerate (zero area) boxes
		/// contain nothing; their edge distance decides instead.
		/// </summary>
		public bool Contains( int x, int y )
		{
			var sign = 0;
			for( var index = 0; index < 4; index++ )
			{
				var from = this.CornerList[index];
				var to = this.CornerList[( index + 1 ) % 4];
				var cross = ( to.X - from.X ) * ( y - from.Y ) - ( to.Y - from.Y ) * ( x - from.X );
				if( cross == 0 )
				{
					continue;
				}
				var crossSign = Math.Sign( cross );
				if( sign == 0 )
				{
					sign = crossSign;
				}
				else if( crossSign != sign )
				{
					return false;
				}
			}
			return sign != 0;
		}

		/// <summary>
		/// Returns the distance from the point to the box: 0 inside, otherwise the distance
		/// to the closest edge (works for the degenerate line and point boxes too).
		/// </summary>
		public double DistanceTo( int x, int y )
		{
			if( this.Contains( x, y ) )
			{
				return 0;
			}

			var best = double.MaxValue;
			for( var index = 0; index < 4; index++ )
			{
				var distance = DistanceToSegment( x, y, this.CornerList[index], this.CornerList[( index + 1 ) % 4] );
				if( distance < best )
				{
					best = distance;
				}
			}
			return best;
		}

		private static double DistanceToSegment( int x, int y, Point from, Point to )
		{
			double deltaX = to.X - from.X;
			double deltaY = to.Y - from.Y;
			var lengthSquared = deltaX * deltaX + deltaY * deltaY;

			double t = 0;
			if( lengthSquared > 0 )
			{
				t = ( ( x - from.X ) * deltaX + ( y - from.Y ) * deltaY ) / lengthSquared;
				t = Math.Max( 0, Math.Min( 1, t ) );
			}

			var closestX = from.X + t * deltaX;
			var closestY = from.Y + t * deltaY;
			return Math.Sqrt( ( x - closestX ) * ( x - closestX ) + ( y - closestY ) * ( y - closestY ) );
		}

		public override string ToString()
		{
			return string.Concat( "box mask=", this.Mask, " flags=0x", this.Flags.ToString( "X2" ) );
		}
	}
}
