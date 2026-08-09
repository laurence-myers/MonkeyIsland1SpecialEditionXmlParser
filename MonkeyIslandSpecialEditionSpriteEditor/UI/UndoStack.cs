using System;
using System.Collections.Generic;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	/// <summary>
	/// A per-editor undo/redo stack of <see cref="UndoableEdit"/>s. Consecutive edits on the
	/// same owner and key coalesce (a rapid burst of spinner ticks, or a whole drag, becomes one
	/// undo step), and a saved marker lets the editor tell whether the document differs from the
	/// last save. Recording a new edit clears the redo stack.
	/// </summary>
	public sealed class UndoStack
	{
		private const int MaxDepth = 200;
		private static readonly TimeSpan MergeWindow = TimeSpan.FromMilliseconds( 750 );

		private readonly List<UndoableEdit> undoList = new List<UndoableEdit>();
		private readonly List<UndoableEdit> redoList = new List<UndoableEdit>();
		private readonly Func<DateTime> clock;

		// the top-of-undo edit when the document was last saved (null = saved with an empty stack);
		// identity-compared, so undo/redo back to it reads as "saved" again
		private UndoableEdit? savedTop;
		private bool savedValid = true;

		// whether the next same-owner/key edit may merge into the current top; cleared by
		// undo/redo/save and by an explicit selection change, so gestures do not merge across them
		private bool allowMergeIntoTop;

		public UndoStack( Func<DateTime>? clock = null )
		{
			this.clock = clock ?? ( () => DateTime.UtcNow );
		}

		/// <summary>
		/// Raised after any change to the stack (record, undo, redo, save, clear).
		/// </summary>
		public event EventHandler? StateChanged;

		public bool CanUndo => this.undoList.Count > 0;

		public bool CanRedo => this.redoList.Count > 0;

		public string? UndoDescription => this.CanUndo ? this.undoList[this.undoList.Count - 1].Description : null;

		public string? RedoDescription => this.CanRedo ? this.redoList[this.redoList.Count - 1].Description : null;

		/// <summary>
		/// Gets whether the document matches the last <see cref="MarkSaved"/> position (or the
		/// initial empty state). Drives the editor's modified flag.
		/// </summary>
		public bool IsAtSavedPosition => this.savedValid && this.Top() == this.savedTop;

		private UndoableEdit? Top()
		{
			return this.undoList.Count > 0 ? this.undoList[this.undoList.Count - 1] : null;
		}

		/// <summary>
		/// Records an edit. It coalesces into the current top when that has the same owner and key
		/// and either <paramref name="forceCoalesce"/> is set (used for the moves of one drag) or
		/// it arrived within the merge window; otherwise it is pushed as a new step and the redo
		/// stack is cleared. The edit's actions are not run - the caller has already applied the
		/// change.
		/// </summary>
		public void Record( UndoableEdit edit, bool forceCoalesce = false )
		{
			edit.Timestamp = this.clock();

			var top = this.Top();
			var merge = this.allowMergeIntoTop
				&& top != null
				&& top.Owner != null
				&& ReferenceEquals( top.Owner, edit.Owner )
				&& top.Key == edit.Key
				&& ( forceCoalesce || edit.Timestamp - top.Timestamp <= MergeWindow );

			if( merge )
			{
				// keep the oldest Undo (the value before the gesture began) and adopt the newest
				// Redo, description and refresh, so the merged step spans the whole gesture
				top!.Redo = edit.Redo;
				top.Description = edit.Description;
				top.Select = edit.Select;
				top.ExtraRefresh = edit.ExtraRefresh;
				top.Timestamp = edit.Timestamp;
			}
			else
			{
				this.redoList.Clear();
				this.undoList.Add( edit );
				this.allowMergeIntoTop = true;
				this.TrimToDepth();
			}

			this.StateChanged?.Invoke( this, EventArgs.Empty );
		}

		private void TrimToDepth()
		{
			while( this.undoList.Count > MaxDepth )
			{
				var removed = this.undoList[0];
				this.undoList.RemoveAt( 0 );
				if( ReferenceEquals( removed, this.savedTop ) )
				{
					// the saved position scrolled off the bottom, so it can never be reached again
					this.savedValid = false;
				}
			}
		}

		public void Undo()
		{
			if( !this.CanUndo )
			{
				return;
			}
			var edit = this.undoList[this.undoList.Count - 1];
			this.undoList.RemoveAt( this.undoList.Count - 1 );
			edit.Undo();
			edit.Select?.Invoke();
			edit.ExtraRefresh?.Invoke();
			this.redoList.Add( edit );
			this.allowMergeIntoTop = false;
			this.StateChanged?.Invoke( this, EventArgs.Empty );
		}

		public void Redo()
		{
			if( !this.CanRedo )
			{
				return;
			}
			var edit = this.redoList[this.redoList.Count - 1];
			this.redoList.RemoveAt( this.redoList.Count - 1 );
			edit.Redo();
			edit.Select?.Invoke();
			edit.ExtraRefresh?.Invoke();
			this.undoList.Add( edit );
			this.allowMergeIntoTop = false;
			this.StateChanged?.Invoke( this, EventArgs.Empty );
		}

		/// <summary>
		/// Marks the current position as saved. A gesture continued after this starts a fresh
		/// step, so the saved position stays detectable.
		/// </summary>
		public void MarkSaved()
		{
			this.savedTop = this.Top();
			this.savedValid = true;
			this.allowMergeIntoTop = false;
			this.StateChanged?.Invoke( this, EventArgs.Empty );
		}

		/// <summary>
		/// Ends the current coalescing run so the next edit starts a new undo step even if it has
		/// the same owner and key (called on selection changes).
		/// </summary>
		public void BreakCoalescing()
		{
			this.allowMergeIntoTop = false;
		}

		public void Clear()
		{
			this.undoList.Clear();
			this.redoList.Clear();
			this.savedTop = null;
			this.savedValid = true;
			this.allowMergeIntoTop = false;
			this.StateChanged?.Invoke( this, EventArgs.Empty );
		}
	}
}
