using System;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	public struct Header
	{
		/// <summary>
		/// Gets or sets the identifier.
		/// </summary>
		public Int32 Identifier
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the byte address of the name.
		/// </summary>
		public Int32 NameAddress
		{
			get;
			set;
		}
		
		public Int32 Unkn03
		{
			get;
			set;
		}
		
		public Int32 Unkn04
		{
			get;
			set;
		}
		
		public Int32 StaticSpriteHeaderCount
		{
			get;
			set;
		}
		
		public Int32 StaticSpriteHeaderAddress
		{
			get;
			set;
		}
		
		public Int32 SpriteHeaderCount
		{
			get;
			set;
		}

		public Int32 SpriteHeaderAddress
		{
			get;
			set;
		}
	
		public Int32 Unkn09
		{
			get;
			set;
		}
		
		public Int32 Unkn10
		{
			get;
			set;
		}

		public Int32 Unknown6HeaderAddress1
		{
			get;
			set;
		}
	
		public Int32 Unknown6HeaderCount
		{
			get;
			set;
		}
		
		public Int32 Unknown6HeaderAddress2
		{
			get;
			set;
		}
		
		public Int32 Unknown4Count
		{
			get;
			set;
		}
		
		public Int32 Unknown4Address
		{
			get;
			set;
		}
		
		public Int32 Unknown5Count
		{
			get;
			set;
		}

		public Int32 Unknown5Address
		{
			get;
			set;
		}

		public Int32 AlwaysZero1
		{
			get;
			set;
		}

		public Int32 AlwaysZero2
		{
			get;
			set;
		}

		public Int32 AlwaysZero3
		{
			get;
			set;
		}
		
		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		public string Name
		{
			get;
			set;
		}
	}
}
