
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities
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
		/// Gets or sets the byte address of the first texture header.
		/// </summary>
		public int TextureHeaderAddress
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
		/// Gets or sets the number of sprite group headers.
		/// </summary>
		public int SpriteGroupHeaderCount
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the byte address of the first sprite group header.
		/// </summary>
		public int SpriteGroupHeaderAddress
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
		/// Gets or sets the number of 12 byte records before the texture file names.
		/// </summary>
		public int PathPointCount
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the byte address to the first path point.
		/// </summary>
		public int PathPointAddress
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
