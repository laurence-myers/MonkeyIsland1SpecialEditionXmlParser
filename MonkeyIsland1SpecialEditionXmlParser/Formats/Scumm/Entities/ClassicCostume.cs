using System.Collections.Generic;
using System.Linq;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Scumm.Entities
{
	/// <summary>
	/// A classic SCUMM v5 costume (COST resource). The Special Edition costume files mirror
	/// its structure: an SE sprite group is a classic limb (SpriteGroup.Identifier = limb
	/// number), an SE sprite is a classic cel, and an SE animation's Identifier is the
	/// classic animation number (chore * 4 + direction).
	/// </summary>
	public class ClassicCostume(
		int costumeId,
		int roomNumber,
		int maximumAnimationNumber,
		int format,
		bool mirror,
		List<ClassicLimb> limbList
	)
	{
		/// <summary>
		/// Gets or sets the costume number (matches the SE costume's Header.Identifier).
		/// </summary>
		public int CostumeId
		{
			get;
			set;
		} = costumeId;

		/// <summary>
		/// Gets or sets the number of the room whose LFLF block contains the costume.
		/// </summary>
		public int RoomNumber
		{
			get;
			set;
		} = roomNumber;

		/// <summary>
		/// Gets or sets the highest animation number defined by the costume.
		/// </summary>
		public int MaximumAnimationNumber
		{
			get;
			set;
		} = maximumAnimationNumber;

		/// <summary>
		/// Gets or sets the costume format byte (without the mirror flag); 0x58 = 16 colour
		/// palette, 0x59 = 32 colour palette.
		/// </summary>
		public int Format
		{
			get;
			set;
		} = format;

		/// <summary>
		/// Gets or sets whether western facing frames are mirrored eastern frames.
		/// </summary>
		public bool Mirror
		{
			get;
			set;
		} = mirror;

		/// <summary>
		/// Gets or sets the limbs that have cels, in limb number order.
		/// </summary>
		public List<ClassicLimb> LimbList
		{
			get;
			set;
		} = limbList;

		/// <summary>
		/// Finds a limb by its limb number (matches the SE sprite group's Identifier), or null.
		/// </summary>
		public ClassicLimb? FindLimb( int limbNumber )
		{
			return this.LimbList.FirstOrDefault( l => l.LimbNumber == limbNumber );
		}

		public override string ToString()
		{
			return string.Concat( "ClassicCostume [Id=", this.CostumeId, "; Room=", this.RoomNumber, "; Limbs=", this.LimbList.Count, "]" );
		}
	}

	/// <summary>
	/// One limb of a classic costume: its table of cels (pictures). Matches the SE sprite
	/// group with the same Identifier.
	/// </summary>
	public class ClassicLimb(
		int limbNumber,
		List<ClassicCel?> celList
	)
	{
		/// <summary>
		/// Gets or sets the limb number (0 to 15).
		/// </summary>
		public int LimbNumber
		{
			get;
			set;
		} = limbNumber;

		/// <summary>
		/// Gets or sets the cels, indexed like the SE sprite group's sprite list; entries can
		/// be null for undefined cels.
		/// </summary>
		public List<ClassicCel?> CelList
		{
			get;
			set;
		} = celList;

		public override string ToString()
		{
			return string.Concat( "ClassicLimb [Number=", this.LimbNumber, "; Cels=", this.CelList.Count, "]" );
		}
	}

	/// <summary>
	/// One cel (picture) of a classic costume limb, in classic 320x200 pixels. The SE sprite
	/// with the same index in the matching sprite group corresponds to it: SE ScreenX/ScreenY
	/// are this cel's RelX/RelY scaled to HD, SE MoveX/MoveY are MoveX/MoveY scaled to HD.
	/// </summary>
	public class ClassicCel(
		int index,
		int width,
		int height,
		int relX,
		int relY,
		int moveX,
		int moveY
	)
	{
		public int Index
		{
			get;
			set;
		} = index;

		public int Width
		{
			get;
			set;
		} = width;

		public int Height
		{
			get;
			set;
		} = height;

		/// <summary>
		/// Gets or sets the horizontal draw offset relative to the actor position.
		/// </summary>
		public int RelX
		{
			get;
			set;
		} = relX;

		/// <summary>
		/// Gets or sets the vertical draw offset relative to the actor position.
		/// </summary>
		public int RelY
		{
			get;
			set;
		} = relY;

		/// <summary>
		/// Gets or sets how far the actor moves horizontally when this cel is shown.
		/// </summary>
		public int MoveX
		{
			get;
			set;
		} = moveX;

		/// <summary>
		/// Gets or sets how far the actor moves vertically when this cel is shown.
		/// </summary>
		public int MoveY
		{
			get;
			set;
		} = moveY;

		public override string ToString()
		{
			return string.Concat( this.Width, "x", this.Height, " @ ", this.RelX, ",", this.RelY );
		}
	}
}
