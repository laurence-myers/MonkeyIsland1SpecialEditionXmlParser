using System.Collections.Generic;
using System.Linq;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities
{
	/// <summary>
	/// A classic SCUMM v5 costume (COST resource). The Special Edition costume files mirror
	/// its structure: an SE sprite group is a classic limb (SpriteGroup.Identifier = limb
	/// number), an SE sprite's raw identifier (its group index plus the group's
	/// FirstSpriteIdentifier) is the classic cel index, and the SE animation list matches
	/// this costume's defined animations in order (the classic numbering is
	/// chore * 4 + direction, with undefined numbers skipped).
	/// </summary>
	public class ClassicCostume(
		int costumeId,
		int roomNumber,
		int maximumAnimationNumber,
		int format,
		bool mirror,
		List<ClassicLimb> limbList,
		List<ClassicAnimation> animationList
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
		/// Gets or sets the animation definitions the costume carries, in animation number order
		/// (matching the SE animation list's index order; gaps are skipped).
		/// </summary>
		public List<ClassicAnimation> AnimationList
		{
			get;
			set;
		} = animationList;

		/// <summary>
		/// Finds a limb by its limb number (matches the SE sprite group's Identifier), or null.
		/// </summary>
		public ClassicLimb? FindLimb( int limbNumber )
		{
			return this.LimbList.FirstOrDefault( l => l.LimbNumber == limbNumber );
		}

		/// <summary>
		/// Finds an animation definition by its animation number (matches the SE animation's
		/// index in the costume's animation list), or null.
		/// </summary>
		public ClassicAnimation? FindAnimation( int animationNumber )
		{
			return this.AnimationList.FirstOrDefault( a => a.AnimationNumber == animationNumber );
		}

		public override string ToString()
		{
			return string.Concat( "ClassicCostume [Id=", this.CostumeId, "; Room=", this.RoomNumber, "; Limbs=", this.LimbList.Count, "]" );
		}
	}

	/// <summary>
	/// One animation of a classic costume: the per-limb command sequences the engine steps
	/// through while the animation plays. The animation number matches the index of the SE
	/// costume's animation with the same role (chore * 4 + direction).
	/// </summary>
	public class ClassicAnimation(
		int animationNumber,
		List<ClassicAnimationLimb> limbList
	)
	{
		/// <summary>
		/// Gets or sets the animation number (the index into the costume's animation offset
		/// table).
		/// </summary>
		public int AnimationNumber
		{
			get;
			set;
		} = animationNumber;

		/// <summary>
		/// Gets or sets the limbs the animation drives, in limb number order.
		/// </summary>
		public List<ClassicAnimationLimb> LimbList
		{
			get;
			set;
		} = limbList;

		/// <summary>
		/// Finds a limb sequence by its limb number, or null when the animation leaves the
		/// limb alone.
		/// </summary>
		public ClassicAnimationLimb? FindLimb( int limbNumber )
		{
			return this.LimbList.FirstOrDefault( l => l.LimbNumber == limbNumber );
		}

		public override string ToString()
		{
			return string.Concat( "ClassicAnimation [Number=", this.AnimationNumber, "; Limbs=", this.LimbList.Count, "]" );
		}
	}

	/// <summary>
	/// One limb's command sequence within a classic animation: the cel indices (values below
	/// 0x71; higher values are engine commands like sound triggers) the limb steps through.
	/// A non-looping sequence holds its last command once played through.
	/// </summary>
	public class ClassicAnimationLimb(
		int limbNumber,
		bool loop,
		List<int> commandList
	)
	{
		/// <summary>
		/// A command at or above this value is an engine action (sound trigger, hide/show),
		/// not a cel index.
		/// </summary>
		public const int FirstNonCelCommand = 0x71;

		/// <summary>
		/// Gets or sets the limb number (0 to 15).
		/// </summary>
		public int LimbNumber
		{
			get;
			set;
		} = limbNumber;

		/// <summary>
		/// Gets or sets whether the sequence loops; a non-looping sequence holds its last
		/// command.
		/// </summary>
		public bool Loop
		{
			get;
			set;
		} = loop;

		/// <summary>
		/// Gets or sets the raw command bytes of the sequence.
		/// </summary>
		public List<int> CommandList
		{
			get;
			set;
		} = commandList;

		public override string ToString()
		{
			return string.Concat( "ClassicAnimationLimb [Number=", this.LimbNumber, "; Loop=", this.Loop, "; Commands=", this.CommandList.Count, "]" );
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
		/// Gets or sets the cels, indexed by the raw SE sprite identifier (the SE group's
		/// sprite list starts at its FirstSpriteIdentifier); entries can be null for
		/// undefined cels.
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
	/// whose raw identifier equals this cel's index corresponds to it: SE ScreenX/ScreenY
	/// are roughly this cel's RelX/RelY scaled to HD (registered against the cel's top or
	/// bottom edge depending on the limb), SE MoveX/MoveY are MoveX/MoveY scaled to HD.
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
