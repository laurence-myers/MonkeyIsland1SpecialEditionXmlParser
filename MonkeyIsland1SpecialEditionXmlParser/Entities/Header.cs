
namespace MonkeyIsland1SpecialEditionXmlParser.Entities
{
	public class Header
	{
		/// <summary>
		/// Gets or sets the numerical identification of the file.
		/// </summary>
		public int ID
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the byte address of the Name of the file.
		/// </summary>
		public int NameAddress
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the number of sprite sheet file names in the file.
		/// </summary>
		public int SpriteSheetFileNameCount
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the byte address of the section after the Name of the file.
		/// </summary>
		public int AfterNameOffset
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the number of animations in the file.
		/// </summary>
		public int AnimationCount
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the byte address to some section.
		/// </summary>
		public int UnknownAddress1
		{
			get;
			set;
		}

		public int UnknownInteger1
		{
			get;
			set;
		}

		public int UnknownInteger2
		{
			get;
			set;
		}

		public int UnknownInteger3
		{
			get;
			set;
		}

		public int UnknownInteger4
		{
			get;
			set;
		}


		/// <summary>
		/// Gets or sets the number of 12 byte records before the sprite sheet file names.
		/// </summary>
		public int UnknownCount1
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the byte address to the 12 byte records before the sprite sheet file names.
		/// </summary>
		public int UnknownAddress2
		{
			get;
			set;
		}

		public int UnknownInteger5
		{
			get;
			set;
		}

		public int UnknownInteger6
		{
			get;
			set;
		}

		public int UnknownInteger7
		{
			get;
			set;
		}

		public int UnknownInteger8
		{
			get;
			set;
		}

		public int UnknownInteger9
		{
			get;
			set;
		}

		public int UnknownInteger10
		{
			get;
			set;
		}

		public int UnknownInteger11
		{
			get;
			set;
		}

		public int UnknownInteger12
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the name of the file.
		/// </summary>
		public string Name
		{
			get;
			set;
		}

		public override string ToString()
		{
			return this.Name;
		}
	}
}
