
namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities
{
	/// <summary>
	/// A room object drawn as a cut-out from an objects texture (e.g. "objects_a2.dxt").
	/// The rectangle is in screen space (e.g. the "Sky" object of a room spans the full
	/// room width/height).
	/// </summary>
	public class RoomObjectSprite(
		int textureFileNameAddress,
		int x,
		int y,
		int width,
		int height
	)
	{
		private RoomObjectSprite() : this(
			textureFileNameAddress: 0,
			x: 0,
			y: 0,
			width: 0,
			height: 0
		) {}

		/// <summary>
		/// Gets or sets the byte address of the texture file name. NB: in the original game
		/// data this is stored as a signed relative offset which is frequently negative
		/// (pointing back into the sprite texture name pool).
		/// </summary>
		public int TextureFileNameAddress
		{
			get;
			set;
		} = textureFileNameAddress;

		/// <summary>
		/// Gets or sets the texture file name.
		/// </summary>
		public string? TextureFileName
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the X component of the position.
		/// </summary>
		public int X
		{
			get;
			set;
		} = x;

		/// <summary>
		/// Gets or sets the Y component of the position.
		/// </summary>
		public int Y
		{
			get;
			set;
		} = y;

		/// <summary>
		/// Gets or sets the width.
		/// </summary>
		public int Width
		{
			get;
			set;
		} = width;

		/// <summary>
		/// Gets or sets the height.
		/// </summary>
		public int Height
		{
			get;
			set;
		} = height;

		public override string ToString()
		{
			return string.Concat(
				this.TextureFileName, "; ",
				this.X, "; ",
				this.Y, "; ",
				this.Width, "; ",
				this.Height
				);
		}
	}
}
