using System.Collections.Generic;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class StaticSpriteHeader
	{
		/// <summary>
		/// Gets or sets the index of the static sprite header.
		/// </summary>
		public int Index
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the identifier.
		/// </summary>
		public int Identifier
		{
			get;
			set;
		}

		public int Unkn1
		{
			get;
			set;
		}

		public int Unkn2
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the number of static sprites.
		/// </summary>
		public int StaticSpriteCount
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the byte address for the first static sprite.
		/// </summary>
		public int StaticSpriteAddress
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat(
				this.Identifier, "; ",
				this.Unkn1, "; ",
				this.Unkn2, "; ",
				this.StaticSpriteCount, "; ",
				this.StaticSpriteAddress
				);
		}
	}
}
