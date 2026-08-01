
namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities
{
	/// <summary>
	/// One chunk of a <see cref="RoomObjectImage"/>: a rectangle relative to the owning
	/// <see cref="RoomObject"/>'s screen offset, plus the texture file that fills it
	/// (e.g. "extra_lava_f0_chunk_1024_0.dxt"). Same clip semantics as
	/// <see cref="StaticSprite"/>: the rectangle is a 1:1 clip of a power-of-two padded
	/// texture, not a scale target (207 of the 266 retail image chunks are padded).
	/// </summary>
	public class RoomObjectImageChunk(
		int index,
		int x,
		int y,
		int width,
		int height,
		int textureFileNameAddress
	) : ITextureReference
	{
		private RoomObjectImageChunk() : this(
			index: 0,
			x: 0,
			y: 0,
			width: 0,
			height: 0,
			textureFileNameAddress: 0
		) {}

		public int Index
		{
			get;
			set;
		} = index;

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

		/// <summary>
		/// Gets or sets the byte address of the texture file name.
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

		public override string ToString()
		{
			return string.Concat(
				this.X, "; ",
				this.Y, "; ",
				this.Width, "; ",
				this.Height, "; ",
				this.TextureFileName
				);
		}
	}
}
