
namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities
{
	/// <summary>
	/// An actor placement extracted from the classic SCUMM scripts: "actor N stands at (x, y)
	/// in room R wearing costume C". Placements come from putActorInRoom/putActor opcodes with
	/// literal arguments; positions are in classic 320x200-era room pixels with the origin at
	/// the actor's feet.
	/// </summary>
	public class ClassicActorPlacement(
		int actorNumber,
		int x,
		int y,
		int? costumeId,
		bool costumeInferred,
		int? direction,
		int? elevation,
		string source
	)
	{
		private ClassicActorPlacement() : this(
			actorNumber: 0,
			x: 0,
			y: 0,
			costumeId: null,
			costumeInferred: false,
			direction: null,
			elevation: null,
			source: ""
		) {}

		/// <summary>
		/// Gets or sets the classic actor number (an engine slot, not a costume).
		/// </summary>
		public int ActorNumber
		{
			get;
			set;
		} = actorNumber;

		/// <summary>
		/// Gets or sets the x position of the actor's feet in classic room pixels.
		/// </summary>
		public int X
		{
			get;
			set;
		} = x;

		/// <summary>
		/// Gets or sets the y position of the actor's feet in classic room pixels.
		/// </summary>
		public int Y
		{
			get;
			set;
		} = y;

		/// <summary>
		/// Gets or sets the classic costume number the actor wears (matches the SE costume's
		/// Header.Identifier and the number prefix of the SE costume file name), or null when
		/// no literal costume assignment was found.
		/// </summary>
		public int? CostumeId
		{
			get;
			set;
		} = costumeId;

		/// <summary>
		/// Gets or sets a value indicating whether the costume was inferred from an assignment
		/// in another script (rather than the script that placed the actor).
		/// </summary>
		public bool CostumeInferred
		{
			get;
			set;
		} = costumeInferred;

		/// <summary>
		/// Gets or sets the direction the actor faces: 0 = left/west, 1 = right/east,
		/// 2 = front/south, 3 = back/north; null when unknown (front is a sensible default).
		/// </summary>
		public int? Direction
		{
			get;
			set;
		} = direction;

		/// <summary>
		/// Gets or sets the actor's elevation in classic pixels (drawn that much higher than
		/// its y position), or null when never set.
		/// </summary>
		public int? Elevation
		{
			get;
			set;
		} = elevation;

		/// <summary>
		/// Gets or sets a human readable description of the script the placement came from,
		/// e.g. "entry script" or "local script 204".
		/// </summary>
		public string Source
		{
			get;
			set;
		} = source;

		/// <summary>
		/// Returns the direction as the animation name suffix used by the SE costumes
		/// ("Left", "Right", "Front" or "Back"); front when the direction is unknown.
		/// </summary>
		public string DirectionName
		{
			get
			{
				switch( this.Direction )
				{
					case 0:
						return "Left";
					case 1:
						return "Right";
					case 3:
						return "Back";
					default:
						return "Front";
				}
			}
		}

		public override string ToString()
		{
			return string.Concat(
				"actor ", this.ActorNumber,
				" costume ", this.CostumeId?.ToString() ?? "?",
				" @ (", this.X, ",", this.Y, ") ",
				this.Source
			);
		}
	}
}
