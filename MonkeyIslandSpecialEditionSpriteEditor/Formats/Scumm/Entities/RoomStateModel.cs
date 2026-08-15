using System.Collections.Generic;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities
{
	/// <summary>
	/// The interactive story states a room's classic scripts can produce: one control per plot
	/// atom (a plain game global or a bit flag) that changes which objects the room draws. A modder
	/// picks a value for each control and the evaluator re-derives which objects are drawn, so the
	/// room can be toggled between the appearances the scripts create (the SCUMM Bar full of
	/// pirates versus only mugs on the table, a door open versus closed) without running the game.
	/// </summary>
	public class RoomStateModel
	{
		/// <summary>Gets the controls, most-impactful first.</summary>
		public List<RoomStateControl> Controls
		{
			get;
		} = new List<RoomStateControl>();

		/// <summary>
		/// Gets or sets whether discovery stopped early (the enumeration hit its path budget). The
		/// controls are then a lower bound: some plot atoms may be missing.
		/// </summary>
		public bool Incomplete
		{
			get;
			set;
		}
	}

	/// <summary>
	/// One plot atom that changes what a room draws, and the value classes it can take. A plain
	/// game global with two or more value classes is shown as a radio group; a bit flag is shown as
	/// a checkbox.
	/// </summary>
	public class RoomStateControl
	{
		/// <summary>Gets or sets the engine variable number of the plot atom (a bit flag keeps the 0x8000 flag).</summary>
		public int VariableId
		{
			get;
			set;
		}

		/// <summary>Gets or sets the label for the control, derived from the objects it toggles.</summary>
		public string Name
		{
			get;
			set;
		} = "";

		/// <summary>
		/// Gets or sets whether the control is a checkbox (a bit flag with an on/off effect) rather
		/// than a radio group.
		/// </summary>
		public bool IsCheckbox
		{
			get;
			set;
		}

		/// <summary>Gets the value classes the atom can take, each with its object effect.</summary>
		public List<RoomStateOption> Options
		{
			get;
		} = new List<RoomStateOption>();

		/// <summary>
		/// Gets or sets the index in <see cref="Options"/> of the class that contains the game-start
		/// value (0). This is the option the room shows when first entered.
		/// </summary>
		public int DefaultOptionIndex
		{
			get;
			set;
		}
	}

	/// <summary>
	/// One value class of a plot atom: a representative value to pin, a label, and the objects this
	/// class shows and hides compared with the game-start baseline.
	/// </summary>
	public class RoomStateOption
	{
		/// <summary>Gets or sets a value in this class to pin the variable to when the option is picked.</summary>
		public int RepresentativeValue
		{
			get;
			set;
		}

		/// <summary>Gets or sets the label for this option, derived from the objects it draws.</summary>
		public string Label
		{
			get;
			set;
		} = "";

		/// <summary>Gets the object numbers this option draws that the game-start baseline does not.</summary>
		public List<int> ObjectsShown
		{
			get;
		} = new List<int>();

		/// <summary>Gets the object numbers the game-start baseline draws that this option does not.</summary>
		public List<int> ObjectsHidden
		{
			get;
		} = new List<int>();
	}
}
