namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities
{
	/// <summary>
	/// The state and the owner an object has when a new game starts, from the object directory
	/// (DOBJ) in the classic index file. This is the exact value the engine loads into its object
	/// state table, not a value recovered from the scripts.
	/// </summary>
	public class ClassicObjectStartState(
		int objectId,
		int state,
		int owner,
		uint classFlags
	)
	{
		private ClassicObjectStartState() : this(
			objectId: 0,
			state: 0,
			owner: 0,
			classFlags: 0
		) {}

		/// <summary>Gets or sets the classic object number.</summary>
		public int ObjectId
		{
			get;
			set;
		} = objectId;

		/// <summary>
		/// Gets or sets the start state (0 to 15). The engine draws the object only when the
		/// state is not 0, and it draws the image block IMnn that has the number of the state.
		/// An object whose state has no image block shows nothing.
		/// </summary>
		public int State
		{
			get;
			set;
		} = state;

		/// <summary>
		/// Gets or sets the start owner (0 to 15). Owner 0 means the object is in its room.
		/// A different owner means an actor or the inventory holds it.
		/// </summary>
		public int Owner
		{
			get;
			set;
		} = owner;

		/// <summary>Gets or sets the class flags of the object (setClass bits).</summary>
		public uint ClassFlags
		{
			get;
			set;
		} = classFlags;

		public override string ToString()
		{
			return string.Concat( "obj ", this.ObjectId, " state=", this.State, " owner=", this.Owner );
		}
	}
}
