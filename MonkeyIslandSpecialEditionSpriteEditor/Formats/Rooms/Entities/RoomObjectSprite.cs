
using System.Xml.Serialization;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities
{
	/// <summary>
	/// A room object drawn as a cut-out from an objects texture (e.g. "objects_a2.dxt").
	/// The rectangle is the source rectangle within that texture; the cut-out is drawn at
	/// the owning <see cref="RoomObject"/>'s screen offset (verified against the retail
	/// art: the bar's Chandelier rect sits at 292;0 in its atlas but hangs at x 939 on
	/// the painted ceiling chain).
	/// </summary>
	public class RoomObjectSprite(
		int textureFileNameAddress,
		int x,
		int y,
		int width,
		int height
	) : IAtlasSprite, ITextureReference
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

		/// <summary>
		/// Gets or sets <see cref="X"/> under its <see cref="IAtlasSprite"/> name, so the
		/// atlas view can edit the rectangle. Not serialized: X is.
		/// </summary>
		[XmlIgnore]
		public int TextureX
		{
			get
			{
				return this.X;
			}
			set
			{
				this.X = value;
			}
		}

		/// <summary>
		/// Gets or sets <see cref="Y"/> under its <see cref="IAtlasSprite"/> name. Not serialized.
		/// </summary>
		[XmlIgnore]
		public int TextureY
		{
			get
			{
				return this.Y;
			}
			set
			{
				this.Y = value;
			}
		}

		/// <summary>
		/// Gets or sets <see cref="Width"/> under its <see cref="IAtlasSprite"/> name. Not serialized.
		/// </summary>
		[XmlIgnore]
		public int TextureWidth
		{
			get
			{
				return this.Width;
			}
			set
			{
				this.Width = value;
			}
		}

		/// <summary>
		/// Gets or sets <see cref="Height"/> under its <see cref="IAtlasSprite"/> name. Not serialized.
		/// </summary>
		[XmlIgnore]
		public int TextureHeight
		{
			get
			{
				return this.Height;
			}
			set
			{
				this.Height = value;
			}
		}

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
