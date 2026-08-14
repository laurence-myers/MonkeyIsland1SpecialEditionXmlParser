using System.Collections.Generic;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class ScriptInitialVisibilityTests
	{
		private const int Room = 28;

		[Test]
		public void Untouched_Object_IsVisible()
		{
			// a static-scenery object no script touches stays drawn (the bar's patrons)
			var verdicts = Compute( new[] { 342 } );
			Assert.That( verdicts[342], Is.EqualTo( ScriptInitialVisibility.Verdict.Visible ) );
		}

		[Test]
		public void OnlyHidden_ByRoomScript_IsHidden()
		{
			// the closed safe / un-thrown lever: a room script sets state 0 and never shows it
			var verdicts = Compute( new[] { 389 }, Hide( 389, ScriptSourceKind.Local ) );
			Assert.That( verdicts[389], Is.EqualTo( ScriptInitialVisibility.Verdict.Hidden ) );
		}

		[Test]
		public void OnlyShown_ByRoomScript_IsVisible()
		{
			var verdicts = Compute( new[] { 357 }, Show( 357, ScriptSourceKind.Local ) );
			Assert.That( verdicts[357], Is.EqualTo( ScriptInitialVisibility.Verdict.Visible ) );
		}

		[Test]
		public void DrawObject_CountsAsShown()
		{
			var verdicts = Compute( new[] { 390 }, Draw( 390, ScriptSourceKind.Local ) );
			Assert.That( verdicts[390], Is.EqualTo( ScriptInitialVisibility.Verdict.Visible ) );
		}

		[Test]
		public void ShownAndHidden_ByRoomScripts_IsAmbiguous()
		{
			// a plot-conditional object (the store bell/sign): both branches seen, no entry script
			var verdicts = Compute( new[] { 399 }, Show( 399, ScriptSourceKind.Local ), Hide( 399, ScriptSourceKind.Local ) );
			Assert.That( verdicts[399], Is.EqualTo( ScriptInitialVisibility.Verdict.Ambiguous ) );
		}

		[Test]
		public void EntryScriptHide_WinsOverLocalShow()
		{
			// the entry script asserts the baseline every entry, so it beats a gameplay local script
			var verdicts = Compute( new[] { 389 }, Show( 389, ScriptSourceKind.Local ), Hide( 389, ScriptSourceKind.Entry ) );
			Assert.That( verdicts[389], Is.EqualTo( ScriptInitialVisibility.Verdict.Hidden ) );
		}

		[Test]
		public void EntryScriptShow_WinsOverLocalHide()
		{
			var verdicts = Compute( new[] { 401 }, Hide( 401, ScriptSourceKind.Local ), Show( 401, ScriptSourceKind.Entry ) );
			Assert.That( verdicts[401], Is.EqualTo( ScriptInitialVisibility.Verdict.Visible ) );
		}

		[Test]
		public void LastEntryChange_Wins()
		{
			var verdicts = Compute( new[] { 402 },
				Show( 402, ScriptSourceKind.Entry, order: 0 ),
				Hide( 402, ScriptSourceKind.Entry, order: 1 ) );
			Assert.That( verdicts[402], Is.EqualTo( ScriptInitialVisibility.Verdict.Hidden ) );
		}

		[Test]
		public void GlobalScriptChange_DoesNotDecideRoomDefault()
		{
			// a give/take handler in a global script must not read a takeable mug as hidden - even
			// when the global is physically stored in this room's LFLF, so it carries this room's
			// number (which is how the scanner actually attributes globals)
			var globalHide = new ObjectDrawChange( 362, ObjectDrawKind.SetState, 0, ScriptSourceKind.Global, sourceRoom: Room, sourceScriptId: 0, order: 0 );
			var verdicts = Compute( new[] { 362 }, globalHide );
			Assert.That( verdicts[362], Is.EqualTo( ScriptInitialVisibility.Verdict.Visible ) );
		}

		[Test]
		public void DrawObjectWithLiteralStateZero_Hides()
		{
			// the rare drawObject "set state" form with state 0 hides, like setState 0
			var verdicts = Compute( new[] { 403 },
				new ObjectDrawChange( 403, ObjectDrawKind.Draw, 0, ScriptSourceKind.Local, Room, 200, 0 ) );
			Assert.That( verdicts[403], Is.EqualTo( ScriptInitialVisibility.Verdict.Hidden ) );
		}

		[Test]
		public void OtherRoomScriptChange_IsIgnored()
		{
			var otherRoomHide = new ObjectDrawChange( 307, ObjectDrawKind.SetState, 0, ScriptSourceKind.Local, sourceRoom: 25, sourceScriptId: 205, order: 0 );
			var verdicts = Compute( new[] { 307 }, otherRoomHide );
			Assert.That( verdicts[307], Is.EqualTo( ScriptInitialVisibility.Verdict.Visible ) );
		}

		[Test]
		public void PickupOnly_LeavesObjectVisible()
		{
			// pickup/setOwner are inventory mechanics, not a draw state change
			var verdicts = Compute( new[] { 395 },
				new ObjectDrawChange( 395, ObjectDrawKind.Pickup, 0, ScriptSourceKind.Local, Room, 211, 0 ) );
			Assert.That( verdicts[395], Is.EqualTo( ScriptInitialVisibility.Verdict.Visible ) );
		}

		//-------------------------------------------

		private static IReadOnlyDictionary<int, ScriptInitialVisibility.Verdict> Compute( int[] objectIds, params ObjectDrawChange[] changes )
		{
			return ScriptInitialVisibility.Compute( Room, objectIds, changes );
		}

		private static ObjectDrawChange Hide( int objectId, ScriptSourceKind kind, int order = 0 )
		{
			return new ObjectDrawChange( objectId, ObjectDrawKind.SetState, 0, kind, Room, kind == ScriptSourceKind.Local ? 200 : 0, order );
		}

		private static ObjectDrawChange Show( int objectId, ScriptSourceKind kind, int order = 0 )
		{
			return new ObjectDrawChange( objectId, ObjectDrawKind.SetState, 1, kind, Room, kind == ScriptSourceKind.Local ? 200 : 0, order );
		}

		private static ObjectDrawChange Draw( int objectId, ScriptSourceKind kind, int order = 0 )
		{
			return new ObjectDrawChange( objectId, ObjectDrawKind.Draw, 1, kind, Room, kind == ScriptSourceKind.Local ? 200 : 0, order );
		}
	}
}
