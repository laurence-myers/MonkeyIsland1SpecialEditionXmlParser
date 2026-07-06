
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Scumm.Entities
{
	public class ClassicObject(
		int objectId,
		int x,
		int y,
		int width,
		int height,
		string? name
	)
	{
		private ClassicObject() : this(
			objectId: 0,
			x: 0,
			y: 0,
			width: 0,
			height: 0,
			name: null
		) {}

		/// <summary>
		/// Gets or sets the classic object number (CDHD). Matches the SE room's SpriteHeader.Identifier.
		/// </summary>
		public int ObjectId
		{
			get;
			set;
		} = objectId;

		/// <summary>
		/// Gets or sets the X component of the position in classic pixels (CDHD strips * 8).
		/// </summary>
		public int X
		{
			get;
			set;
		} = x;

		/// <summary>
		/// Gets or sets the Y component of the position in classic pixels (CDHD strips * 8).
		/// </summary>
		public int Y
		{
			get;
			set;
		} = y;

		/// <summary>
		/// Gets or sets the width in classic pixels (CDHD strips * 8).
		/// </summary>
		public int Width
		{
			get;
			set;
		} = width;

		/// <summary>
		/// Gets or sets the height in classic pixels (CDHD strips * 8).
		/// </summary>
		public int Height
		{
			get;
			set;
		} = height;

		/// <summary>
		/// Gets or sets the object name (OBNA), if any.
		/// </summary>
		public string? Name
		{
			get;
			set;
		} = name;

		public override string ToString()
		{
			return string.Concat(
				this.ObjectId, "; ",
				this.X, "; ",
				this.Y, "; ",
				this.Width, "; ",
				this.Height, "; ",
				this.Name
				);
		}
	}
}
