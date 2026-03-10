
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities
{
	public class Header(
		int identifier,
		int nameAddress,
		int textureFileNameCount,
		int textureHeaderAddress,
		int animationCount,
		int animationHeaderAddress,
		int unknownInteger1,
		int spriteGroupHeaderCount,
		int spriteGroupHeaderAddress,
		int unknownInteger4,
		int pathPointCount,
		int pathPointAddress,
		int unknownInteger5,
		int unknownInteger6,
		int unknownInteger7,
		int unknownInteger8,
		int unknownInteger9,
		int unknownInteger10,
		int unknownInteger11,
		int unknownInteger12,
		string name )
	{
		private Header() : this(
			identifier: 0,
			nameAddress: 0,
			textureFileNameCount: 0,
			textureHeaderAddress: 0,
			animationCount: 0,
			animationHeaderAddress: 0,
			unknownInteger1: 0,
			spriteGroupHeaderCount: 0,
			spriteGroupHeaderAddress: 0,
			unknownInteger4: 0,
			pathPointCount: 0,
			pathPointAddress: 0,
			unknownInteger5: 0,
			unknownInteger6: 0,
			unknownInteger7: 0,
			unknownInteger8: 0,
			unknownInteger9: 0,
			unknownInteger10: 0,
			unknownInteger11: 0,
			unknownInteger12: 0,
			name: null!
		) {}

		/// <summary>
		/// Gets or sets the numerical identification of the file.
		/// </summary>
		public int Identifier
		{
			get;
			set;
		} = identifier;

		/// <summary>
		/// Gets or sets the byte address of the Name of the file.
		/// </summary>
		public int NameAddress
		{
			get;
			set;
		} = nameAddress;

		/// <summary>
		/// Gets or sets the number of texture file names in the file.
		/// </summary>
		public int TextureFileNameCount
		{
			get;
			set;
		} = textureFileNameCount;

		/// <summary>
		/// Gets or sets the byte address of the first texture header.
		/// </summary>
		public int TextureHeaderAddress
		{
			get;
			set;
		} = textureHeaderAddress;

		/// <summary>
		/// Gets or sets the number of animations in the file.
		/// </summary>
		public int AnimationCount
		{
			get;
			set;
		} = animationCount;

		/// <summary>
		/// Gets or sets the byte address to the first animation header.
		/// </summary>
		public int AnimationHeaderAddress
		{
			get;
			set;
		} = animationHeaderAddress;

		public int UnknownInteger1
		{
			get;
			set;
		} = unknownInteger1;

		/// <summary>
		/// Gets or sets the number of sprite group headers.
		/// </summary>
		public int SpriteGroupHeaderCount
		{
			get;
			set;
		} = spriteGroupHeaderCount;

		/// <summary>
		/// Gets or sets the byte address of the first sprite group header.
		/// </summary>
		public int SpriteGroupHeaderAddress
		{
			get;
			set;
		} = spriteGroupHeaderAddress;

		public int UnknownInteger4
		{
			get;
			set;
		} = unknownInteger4;


		/// <summary>
		/// Gets or sets the number of 12 byte records before the texture file names.
		/// </summary>
		public int PathPointCount
		{
			get;
			set;
		} = pathPointCount;

		/// <summary>
		/// Gets or sets the byte address to the first path point.
		/// </summary>
		public int PathPointAddress
		{
			get;
			set;
		} = pathPointAddress;

		public int UnknownInteger5
		{
			get;
			set;
		} = unknownInteger5;

		public int UnknownInteger6
		{
			get;
			set;
		} = unknownInteger6;

		public int UnknownInteger7
		{
			get;
			set;
		} = unknownInteger7;

		public int UnknownInteger8
		{
			get;
			set;
		} = unknownInteger8;

		public int UnknownInteger9
		{
			get;
			set;
		} = unknownInteger9;

		public int UnknownInteger10
		{
			get;
			set;
		} = unknownInteger10;

		public int UnknownInteger11
		{
			get;
			set;
		} = unknownInteger11;

		public int UnknownInteger12
		{
			get;
			set;
		} = unknownInteger12;

		/// <summary>
		/// Gets or sets the name of the file.
		/// </summary>
		public string Name
		{
			get;
			set;
		} = name;

		public override string ToString()
		{
			return this.Name;
		}
	}
}
