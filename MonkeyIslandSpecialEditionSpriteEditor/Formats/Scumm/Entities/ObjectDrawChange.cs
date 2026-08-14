namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities
{
	/// <summary>
	/// The kind of object visibility change a script performs.
	/// </summary>
	public enum ObjectDrawKind
	{
		/// <summary>setState(object, state): state 0 hides the object, non-zero draws its image.</summary>
		SetState,

		/// <summary>drawObject(object): makes the object visible (optionally with a state).</summary>
		Draw,

		/// <summary>pickupObject(object): removes the object from the room (into inventory).</summary>
		Pickup,

		/// <summary>setOwnerOf(object, owner): owner 0 keeps it in the room, non-zero removes it.</summary>
		SetOwner,
	}

	/// <summary>
	/// Which script a change came from. Entry scripts run whenever the room is entered, so they
	/// are the strongest signal for a room's initial appearance; the rest are gameplay.
	/// </summary>
	public enum ScriptSourceKind
	{
		Entry,
		Local,
		Exit,
		Global,
	}

	/// <summary>
	/// One object visibility change recovered from a classic SCUMM script by a static, control
	/// flow blind scan (see <see cref="ScriptScanner"/>): a single setState/drawObject/pickup/
	/// setOwner call with literal operands. The scan cannot evaluate the branch conditions that
	/// guard these calls, so a room can carry several conflicting changes for one object (the
	/// different plot states it can be in); this record keeps the source so a later pass can try
	/// to separate those into scenarios.
	/// </summary>
	public class ObjectDrawChange(
		int objectId,
		ObjectDrawKind kind,
		int value,
		ScriptSourceKind sourceKind,
		int? sourceRoom,
		int sourceScriptId,
		int order
	)
	{
		/// <summary>Gets or sets the classic object number the change targets.</summary>
		public int ObjectId
		{
			get;
			set;
		} = objectId;

		/// <summary>Gets or sets the kind of change.</summary>
		public ObjectDrawKind Kind
		{
			get;
			set;
		} = kind;

		/// <summary>
		/// Gets or sets the change's literal value: the state for <see cref="ObjectDrawKind.SetState"/>
		/// and <see cref="ObjectDrawKind.Draw"/> (1 when drawObject carried no explicit state), the
		/// owner for <see cref="ObjectDrawKind.SetOwner"/>, and 0 for <see cref="ObjectDrawKind.Pickup"/>.
		/// </summary>
		public int Value
		{
			get;
			set;
		} = value;

		/// <summary>Gets or sets the kind of script the change came from.</summary>
		public ScriptSourceKind SourceKind
		{
			get;
			set;
		} = sourceKind;

		/// <summary>
		/// Gets or sets the room whose LFLF the script lives in (entry/exit/local scripts); null
		/// for global scripts, whose object changes are attributed to the object's owning room.
		/// </summary>
		public int? SourceRoom
		{
			get;
			set;
		} = sourceRoom;

		/// <summary>Gets or sets the local script number (200+), or 0 for entry/exit/global scripts.</summary>
		public int SourceScriptId
		{
			get;
			set;
		} = sourceScriptId;

		/// <summary>Gets or sets the scan order, so the changes can be replayed deterministically.</summary>
		public int Order
		{
			get;
			set;
		} = order;

		/// <summary>
		/// Whether this change makes the object visible (a non-zero state or a drawObject), as
		/// opposed to hiding or removing it.
		/// </summary>
		public bool MakesVisible
		{
			get
			{
				switch( this.Kind )
				{
					case ObjectDrawKind.SetState:
						return this.Value != 0;
					case ObjectDrawKind.Draw:
						return this.Value != 0;
					default:
						return false;
				}
			}
		}

		public override string ToString()
		{
			return string.Concat( "obj ", this.ObjectId, " ", this.Kind, "=", this.Value, " from ", this.SourceKind,
				this.SourceScriptId > 0 ? " " + this.SourceScriptId : "", this.SourceRoom != null ? " (room " + this.SourceRoom + ")" : "" );
		}
	}
}
