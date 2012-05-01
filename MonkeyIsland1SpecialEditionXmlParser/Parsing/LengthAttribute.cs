using System;

namespace MonkeyIsland1SpecialEditionXmlParser.Parsing
{
	public class LengthAttribute : Attribute
	{
		public int Length
		{
			get;
			private set;
		}

		public LengthAttribute( int length )
		{
			this.Length = length;
		}
	}
}
