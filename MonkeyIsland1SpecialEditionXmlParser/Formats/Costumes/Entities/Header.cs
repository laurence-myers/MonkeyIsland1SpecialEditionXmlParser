
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
		int pathPointTypeCount,
		int pathPointCount,
		int pathPointAddress,
		int unknownInteger5,
		int unknownInteger6,
		float unknownFloat7,
		float unknownFloat8,
		float unknownFloat9,
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
			pathPointTypeCount: 0,
			pathPointCount: 0,
			pathPointAddress: 0,
			unknownInteger5: 0,
			unknownInteger6: 0,
			unknownFloat7: 0.25f,
			unknownFloat8: 1f,
			unknownFloat9: 1f,
			unknownInteger10: 0,
			unknownInteger11: 0,
			unknownInteger12: 0,
			name: null!
		) {}

		/// <summary>
		/// Gets or sets the numerical identification of the file. Matches the classic SCUMM
		/// costume number.
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

		/// <summary>
		/// Unknown. Observed values 0 to 4 in the retail data; does not correlate with the
		/// sound, path point, texture or animation counts.
		/// </summary>
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

		/// <summary>
		/// Gets or sets the number of distinct path point types used by the costume
		/// (the highest <see cref="PathPoint.Type"/> plus one); 0 when there are no
		/// path points.
		/// </summary>
		public int PathPointTypeCount
		{
			get;
			set;
		} = pathPointTypeCount;

		/// <summary>
		/// Gets or sets the number of path points.
		/// </summary>
		public int PathPointCount
		{
			get;
			set;
		} = pathPointCount;

		/// <summary>
		/// Gets or sets the byte address to the first path point. Points at the position
		/// where path points would be written even when <see cref="PathPointCount"/> is 0.
		/// </summary>
		public int PathPointAddress
		{
			get;
			set;
		} = pathPointAddress;

		/// <summary>
		/// Unknown. 0 or 1 in the retail data; 1 mostly on main speaking characters
		/// (guybrush, stan, lechuck, ...), 0 on props and background costumes.
		/// </summary>
		public int UnknownInteger5
		{
			get;
			set;
		} = unknownInteger5;

		/// <summary>
		/// Unknown. Always 0 in the retail data.
		/// </summary>
		public int UnknownInteger6
		{
			get;
			set;
		} = unknownInteger6;

		/// <summary>
		/// Unknown. Always 0.25 in the retail data.
		/// </summary>
		public float UnknownFloat7
		{
			get;
			set;
		} = unknownFloat7;

		/// <summary>
		/// Unknown. Always 1.0 in the retail data.
		/// </summary>
		public float UnknownFloat8
		{
			get;
			set;
		} = unknownFloat8;

		/// <summary>
		/// Unknown. Always 1.0 in the retail data.
		/// </summary>
		public float UnknownFloat9
		{
			get;
			set;
		} = unknownFloat9;

		/// <summary>
		/// Unknown. Always 0 in the retail data.
		/// </summary>
		public int UnknownInteger10
		{
			get;
			set;
		} = unknownInteger10;

		/// <summary>
		/// Unknown. Always 0 in the retail data.
		/// </summary>
		public int UnknownInteger11
		{
			get;
			set;
		} = unknownInteger11;

		/// <summary>
		/// Unknown. Always 0 in the retail data.
		/// </summary>
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
