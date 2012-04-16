using System.Drawing;

namespace MonkeyIsland1SpecialEditionXmlParser.Entities
{
	public class PathPoint
	{
		public byte UnknownByte1
		{
			get;
			set;
		}

		public byte UnknownByte2
		{
			get;
			set;
		}

		public byte UnknownByte3
		{
			get;
			set;
		}

		public byte UnknownByte4
		{
			get;
			set;
		}
		
		public float UnknownFloat1
		{
			get;
			set;
		}

		public float UnknownFloat2
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat(
				this.UnknownByte1, "; ",
				this.UnknownByte2, "; ",
				this.UnknownByte3, "; ",
				this.UnknownByte4, "; ",
				this.UnknownFloat1, "; ",
				this.UnknownFloat2
				);
		}
	}
}
