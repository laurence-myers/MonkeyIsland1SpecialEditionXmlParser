using System;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	/// <summary>
	/// One reversible edit: a pair of closures that write the old and new values back into the
	/// same entity object, so undo and redo preserve object identity (the tree nodes, selection
	/// and preview view-models all key off the entity, so restoring a snapshot would break them).
	/// </summary>
	public sealed class UndoableEdit
	{
		/// <summary>
		/// Gets or sets the entity the edit mutates. Consecutive edits with the same owner and
		/// <see cref="Key"/> coalesce into one undo step (see <see cref="UndoStack"/>), so a
		/// burst of spinner ticks or a drag collapses to a single undo.
		/// </summary>
		public object? Owner
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the coalescing key: which field or gesture this edit represents (e.g.
		/// "ScreenXY", "TextureX", "AtlasRect"). Only edits with the same owner and key merge.
		/// </summary>
		public string Key
		{
			get;
			set;
		} = "";

		/// <summary>
		/// Gets or sets a short human description shown after "Undo"/"Redo" in the menu.
		/// </summary>
		public string Description
		{
			get;
			set;
		} = "";

		/// <summary>
		/// Gets or sets the action that writes the captured old values back into <see cref="Owner"/>.
		/// </summary>
		public Action Undo
		{
			get;
			set;
		} = delegate { };

		/// <summary>
		/// Gets or sets the action that writes the captured new values back into <see cref="Owner"/>.
		/// </summary>
		public Action Redo
		{
			get;
			set;
		} = delegate { };

		/// <summary>
		/// Gets or sets an optional action that re-selects <see cref="Owner"/> in the editor, run
		/// after undo/redo so the property fields and preview show the affected entity.
		/// </summary>
		public Action? Select
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets an optional extra refresh run after undo/redo, for edits that change more
		/// than a field value (e.g. re-rendering a baked background layer or repopulating a combo).
		/// </summary>
		public Action? ExtraRefresh
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets when the edit was recorded; used for time-window coalescing.
		/// </summary>
		public DateTime Timestamp
		{
			get;
			set;
		}
	}
}
