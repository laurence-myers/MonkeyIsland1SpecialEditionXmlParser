using System;
using MonkeyIslandSpecialEditionSpriteEditor.UI;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class UndoStackTests
	{
		private DateTime now;
		private UndoStack stack = null!;
		private int value;

		[SetUp]
		public void SetUp()
		{
			this.now = new DateTime( 2026, 1, 1, 0, 0, 0, DateTimeKind.Utc );
			this.stack = new UndoStack( () => this.now );
			this.value = 0;
		}

		private void Advance( int milliseconds )
		{
			this.now = this.now.AddMilliseconds( milliseconds );
		}

		// records value := newValue, capturing oldValue for undo
		private UndoableEdit Set( object owner, string key, int oldValue, int newValue )
		{
			this.value = newValue;
			return new UndoableEdit
			{
				Owner = owner,
				Key = key,
				Description = key,
				Undo = () => this.value = oldValue,
				Redo = () => this.value = newValue,
			};
		}

		[Test]
		public void Record_ThenUndoRedo_RestoresValues()
		{
			var owner = new object();
			this.stack.Record( this.Set( owner, "v", 0, 5 ) );
			Assert.That( this.value, Is.EqualTo( 5 ) );
			Assert.That( this.stack.CanUndo, Is.True );

			this.stack.Undo();
			Assert.That( this.value, Is.EqualTo( 0 ) );
			Assert.That( this.stack.CanRedo, Is.True );

			this.stack.Redo();
			Assert.That( this.value, Is.EqualTo( 5 ) );
		}

		[Test]
		public void SameOwnerAndKey_WithinWindow_CoalesceToOneStep()
		{
			var owner = new object();
			this.stack.Record( this.Set( owner, "v", 0, 1 ) );
			this.Advance( 100 );
			this.stack.Record( this.Set( owner, "v", 1, 2 ) );
			this.Advance( 100 );
			this.stack.Record( this.Set( owner, "v", 2, 3 ) );

			// one undo returns to the value before the whole burst
			this.stack.Undo();
			Assert.That( this.value, Is.EqualTo( 0 ) );
			Assert.That( this.stack.CanUndo, Is.False );
		}

		[Test]
		public void SameOwnerAndKey_AfterWindow_AreSeparateSteps()
		{
			var owner = new object();
			this.stack.Record( this.Set( owner, "v", 0, 1 ) );
			this.Advance( 1000 );
			this.stack.Record( this.Set( owner, "v", 1, 2 ) );

			this.stack.Undo();
			Assert.That( this.value, Is.EqualTo( 1 ) );
			this.stack.Undo();
			Assert.That( this.value, Is.EqualTo( 0 ) );
		}

		[Test]
		public void ForceCoalesce_MergesEvenAcrossTheWindow()
		{
			var owner = new object();
			this.stack.Record( this.Set( owner, "drag", 0, 1 ) );
			this.Advance( 5000 );
			this.stack.Record( this.Set( owner, "drag", 1, 2 ), forceCoalesce: true );

			this.stack.Undo();
			Assert.That( this.value, Is.EqualTo( 0 ) );
			Assert.That( this.stack.CanUndo, Is.False );
		}

		[Test]
		public void DifferentKey_DoesNotCoalesce()
		{
			var owner = new object();
			this.stack.Record( this.Set( owner, "a", 0, 1 ) );
			this.stack.Record( this.Set( owner, "b", 1, 2 ) );
			Assert.That( this.stack.UndoDescription, Is.EqualTo( "b" ) );

			this.stack.Undo();
			this.stack.Undo();
			Assert.That( this.stack.CanUndo, Is.False );
		}

		[Test]
		public void DifferentOwner_DoesNotCoalesce()
		{
			this.stack.Record( this.Set( new object(), "v", 0, 1 ) );
			this.stack.Record( this.Set( new object(), "v", 1, 2 ) );

			this.stack.Undo();
			this.stack.Undo();
			Assert.That( this.stack.CanUndo, Is.False );
		}

		[Test]
		public void BreakCoalescing_StartsANewStep()
		{
			var owner = new object();
			this.stack.Record( this.Set( owner, "v", 0, 1 ) );
			this.stack.BreakCoalescing();
			this.stack.Record( this.Set( owner, "v", 1, 2 ) );

			this.stack.Undo();
			Assert.That( this.value, Is.EqualTo( 1 ) );
		}

		[Test]
		public void Undo_ThenNewEdit_ClearsRedo()
		{
			var owner = new object();
			this.stack.Record( this.Set( owner, "a", 0, 1 ) );
			this.stack.Undo();
			Assert.That( this.stack.CanRedo, Is.True );

			this.stack.Record( this.Set( owner, "b", 0, 9 ) );
			Assert.That( this.stack.CanRedo, Is.False );
		}

		[Test]
		public void EditAfterUndo_DoesNotMergeIntoRestoredStep()
		{
			var owner = new object();
			this.stack.Record( this.Set( owner, "v", 0, 1 ) );
			this.stack.Undo();
			this.stack.Record( this.Set( owner, "v", 0, 2 ) );

			// the post-undo edit is its own step, not merged into the (undone) one
			this.stack.Undo();
			Assert.That( this.value, Is.EqualTo( 0 ) );
			Assert.That( this.stack.CanUndo, Is.False );
		}

		[Test]
		public void SavedPosition_TrueInitiallyAndAfterSave()
		{
			Assert.That( this.stack.IsAtSavedPosition, Is.True );

			var owner = new object();
			this.stack.Record( this.Set( owner, "v", 0, 1 ) );
			Assert.That( this.stack.IsAtSavedPosition, Is.False );

			this.stack.MarkSaved();
			Assert.That( this.stack.IsAtSavedPosition, Is.True );
		}

		[Test]
		public void SavedPosition_ReturnsWhenUndoingRedoingBackToIt()
		{
			var owner = new object();
			this.stack.Record( this.Set( owner, "a", 0, 1 ) );
			this.stack.MarkSaved();
			this.Advance( 1000 );
			this.stack.Record( this.Set( owner, "b", 1, 2 ) );
			Assert.That( this.stack.IsAtSavedPosition, Is.False );

			this.stack.Undo();
			Assert.That( this.stack.IsAtSavedPosition, Is.True );

			this.stack.Redo();
			Assert.That( this.stack.IsAtSavedPosition, Is.False );
		}

		[Test]
		public void SavedPosition_ContinuingAGestureAfterSave_IsModified()
		{
			var owner = new object();
			this.stack.Record( this.Set( owner, "v", 0, 1 ) );
			this.stack.MarkSaved();
			this.Advance( 100 ); // within the merge window
			this.stack.Record( this.Set( owner, "v", 1, 2 ) );

			// the continued edit must not hide inside the saved step
			Assert.That( this.stack.IsAtSavedPosition, Is.False );
		}

		[Test]
		public void SavedPosition_DivergingFromSavedBranch_StaysModified()
		{
			var owner = new object();
			this.stack.Record( this.Set( owner, "a", 0, 1 ) );
			this.Advance( 1000 );
			this.stack.Record( this.Set( owner, "b", 1, 2 ) );
			this.stack.MarkSaved(); // saved at "b"
			this.stack.Undo(); // back to "a"
			this.Advance( 1000 );
			this.stack.Record( this.Set( owner, "c", 1, 3 ) ); // new branch; "b" now unreachable

			Assert.That( this.stack.IsAtSavedPosition, Is.False );
			this.stack.Undo();
			Assert.That( this.stack.IsAtSavedPosition, Is.False );
		}

		[Test]
		public void Clear_EmptiesEverythingAndReadsAsSaved()
		{
			var owner = new object();
			this.stack.Record( this.Set( owner, "v", 0, 1 ) );
			this.stack.Clear();

			Assert.That( this.stack.CanUndo, Is.False );
			Assert.That( this.stack.CanRedo, Is.False );
			Assert.That( this.stack.IsAtSavedPosition, Is.True );
		}

		[Test]
		public void Cap_TrimsOldestBeyond200AndInvalidatesASavedPositionThatScrolledOff()
		{
			var owner = new object();
			this.stack.Record( this.Set( owner, "k0", 0, 0 ) );
			this.stack.MarkSaved(); // saved at the very first edit
			Assert.That( this.stack.IsAtSavedPosition, Is.True );

			// push well past the cap; each is its own step (distinct keys defeat coalescing)
			for( var i = 1; i <= 250; i++ )
			{
				this.Advance( 1000 );
				this.stack.Record( this.Set( owner, "k" + i, i - 1, i ) );
			}

			// the saved edit scrolled off the bottom, so we are modified and can never be "saved"
			// again without a new MarkSaved
			Assert.That( this.stack.IsAtSavedPosition, Is.False );
		}
	}
}
