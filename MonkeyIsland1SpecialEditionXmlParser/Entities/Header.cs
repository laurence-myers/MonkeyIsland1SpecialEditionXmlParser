
namespace MonkeyIsland1SpecialEditionXmlParser.Entities
{
	public class Header
	{
		/// <summary>
		/// Gets or sets the numerical identification of the file.
		/// </summary>
		public int Identifier
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
		/// Gets or sets the number of texture file names in the file.
		/// </summary>
		public int TextureFileNameCount
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the byte address of the section after the Name of the file.
		/// </summary>
		public int AfterNameAddress
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
		/// Gets or sets the byte address to the first animation header.
		/// </summary>
		public int AnimationHeaderAddress
		{
			get;
			set;
		}

		public int UnknownInteger1
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the number of points on the path.
		/// </summary>
		public int PointsOnPathCount
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the byte address of the first path.
		/// </summary>
		public int PathAddress
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
		/// Gets or sets the byte address to the first texture file name.
		/// </summary>
		public int TextureFileNameAddress
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
