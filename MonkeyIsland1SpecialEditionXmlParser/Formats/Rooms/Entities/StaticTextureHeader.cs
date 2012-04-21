using System.Collections.Generic;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public class StaticTextureHeader
	{
		/// <summary>
		/// Gets or sets the index of the static texture header.
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
		/// Gets or sets the number of static textures.
		/// </summary>
		public int StaticTextureCount
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the byte address for the first static texture.
		/// </summary>
		public int StaticTextureAddress
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
				this.StaticTextureCount, "; ",
				this.StaticTextureAddress
				);
		}
	}
}
