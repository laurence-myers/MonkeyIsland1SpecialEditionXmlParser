using System.Collections.Generic;
using System.Linq;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class RoomEntityGrouperTests
	{
		[Test]
		public void SameRectRun_NamedFrame_IsOneEntityNamedFromTheFrame()
		{
			// room 28's swinging pirate: 330 "pirate", 331, 332 all at 80,80 40x48
			var objects = Objects(
				Obj( 330, 80, 80, 40, 48, "pirate" ),
				Obj( 331, 80, 80, 40, 48, null ),
				Obj( 332, 80, 80, 40, 48, null ) );

			var entities = RoomEntityGrouper.Group( new[] { 330, 331, 332 }, objects );

			Assert.That( entities.Count, Is.EqualTo( 1 ) );
			var pirate = entities[0];
			Assert.That( pirate.Name, Is.EqualTo( "Pirate" ) );
			Assert.That( pirate.NameSource, Is.EqualTo( RoomEntityNameSource.FrameName ) );
			Assert.That( pirate.GroupIndices, Is.EqualTo( new[] { 0, 1, 2 } ) );
			Assert.That( pirate.ObjectIds, Is.EqualTo( new[] { 330, 331, 332 } ) );
			Assert.That( pirate.FrameNumberOf( 1 ), Is.EqualTo( 2 ) );
			Assert.That( pirate.FrameNumberOf( 7 ), Is.EqualTo( 0 ) );
			Assert.That( pirate.DescribeObjectIds(), Is.EqualTo( "330-332" ) );
		}

		[Test]
		public void UnnamedRun_InsideANamedObject_BorrowsTheSmallestEnclosingName()
		{
			// room 28's fire: 317, 318, 319 (16x16 at 520,80) inside "fireplace" 321 (56x56 at
			// 504,48); a larger named object that also encloses them must lose to the fireplace
			var objects = Objects(
				Obj( 321, 504, 48, 56, 56, "fireplace" ),
				Obj( 322, 480, 40, 120, 100, "wall" ),
				Obj( 317, 520, 80, 16, 16, null ),
				Obj( 318, 520, 80, 16, 16, null ),
				Obj( 319, 520, 80, 16, 16, null ) );

			var entities = RoomEntityGrouper.Group( new[] { 321, 317, 318, 319, 322 }, objects );

			Assert.That( entities.Count, Is.EqualTo( 1 ) );
			var fire = entities[0];
			Assert.That( fire.Name, Is.EqualTo( "Fireplace" ) );
			Assert.That( fire.NameSource, Is.EqualTo( RoomEntityNameSource.EnclosingObject ) );
			Assert.That( fire.NamedByObjectId, Is.EqualTo( 321 ) );
			Assert.That( fire.GroupIndices, Is.EqualTo( new[] { 1, 2, 3 } ) );
		}

		[Test]
		public void UnnamedRun_WithNoEnclosingName_GetsAPlaceholder()
		{
			var objects = Objects(
				Obj( 411, 96, 72, 16, 16, null ),
				Obj( 412, 96, 72, 16, 16, null ),
				Obj( 413, 96, 72, 16, 16, null ),
				// touches but does not enclose
				Obj( 400, 100, 72, 16, 16, "lamp" ) );

			var entities = RoomEntityGrouper.Group( new[] { 411, 412, 413, 400 }, objects );

			Assert.That( entities.Count, Is.EqualTo( 1 ) );
			Assert.That( entities[0].NameSource, Is.EqualTo( RoomEntityNameSource.None ) );
			Assert.That( entities[0].Name, Is.EqualTo( "Objects 411-413" ) );
			// the single place the placeholder is decided for the UI
			Assert.That( entities[0].DisplayName, Is.EqualTo( "Unnamed entity" ) );
		}

		[Test]
		public void DisplayName_IsTheNameForANamedEntity()
		{
			var objects = Objects(
				Obj( 330, 80, 80, 40, 48, "pirate" ),
				Obj( 331, 80, 80, 40, 48, null ) );

			var entities = RoomEntityGrouper.Group( new[] { 330, 331 }, objects );

			Assert.That( entities[0].DisplayName, Is.EqualTo( "Pirate" ) );
		}

		[Test]
		public void FormatRange_IsSharedHyphenFormat_ForRangesAndLists()
		{
			// the tree and the room-states panel format object numbers the same way now
			Assert.That( ClassicObjectNames.FormatRange( new[] { 317, 318, 319 } ), Is.EqualTo( "317-319" ) );
			Assert.That( ClassicObjectNames.FormatRange( new[] { 5, 8, 12 } ), Is.EqualTo( "5, 8, 12" ) );
			Assert.That( ClassicObjectNames.FormatRange( new[] { 42 } ), Is.EqualTo( "42" ) );
			Assert.That( ClassicObjectNames.FormatRange( new int[0] ), Is.EqualTo( "" ) );
		}

		[Test]
		public void RunOfDifferentlyNamedObjects_IsNotAnEntity()
		{
			// room 58's map hotspots share one rectangle but are different things
			var objects = Objects(
				Obj( 675, 0, 0, 48, 32, "campsite" ),
				Obj( 676, 0, 0, 48, 32, "bones" ),
				Obj( 677, 0, 0, 48, 32, "stump" ) );

			var entities = RoomEntityGrouper.Group( new[] { 675, 676, 677 }, objects );

			Assert.That( entities, Is.Empty );
		}

		[Test]
		public void ObjectsAtDifferentPositions_AreNotGrouped()
		{
			// room 12's skull poles: a sequence at distinct spots
			var objects = Objects(
				Obj( 146, 784, 56, 64, 48, null ),
				Obj( 148, 792, 64, 48, 40, null ),
				Obj( 362, 104, 96, 16, 16, "mug@@@@" ),
				Obj( 363, 184, 112, 16, 16, "mug@@@@" ) );

			var entities = RoomEntityGrouper.Group( new[] { 146, 148, 362, 363 }, objects );

			Assert.That( entities, Is.Empty );
		}

		[Test]
		public void PaddedNames_AreCleaned_AndOnlyConsecutiveGroupsFormARun()
		{
			var objects = Objects(
				Obj( 10, 5, 5, 8, 8, "mug@@@@@" ),
				Obj( 11, 5, 5, 8, 8, null ),
				Obj( 20, 50, 5, 8, 8, "other" ),
				Obj( 12, 5, 5, 8, 8, null ) );

			// 12 shares the rect but group 20 sits between it and the run in the tree order
			var entities = RoomEntityGrouper.Group( new[] { 10, 11, 20, 12 }, objects );

			Assert.That( entities.Count, Is.EqualTo( 1 ) );
			Assert.That( entities[0].Name, Is.EqualTo( "Mug" ) );
			Assert.That( entities[0].GroupIndices, Is.EqualTo( new[] { 0, 1 } ) );
		}

		[Test]
		public void GroupsWithoutAClassicObject_OrZeroSize_BreakRuns()
		{
			var objects = Objects(
				Obj( 1, 5, 5, 8, 8, null ),
				Obj( 2, 5, 5, 8, 8, null ),
				Obj( 3, 5, 5, 0, 0, null ),
				Obj( 4, 5, 5, 0, 0, null ) );

			// 99 has no classic object; 3 and 4 are zero-sized hotspots
			var entities = RoomEntityGrouper.Group( new[] { 1, 99, 2, 3, 4 }, objects );

			Assert.That( entities, Is.Empty );
		}

		[Test]
		public void CleanName_StripsPaddingAndHandlesNull()
		{
			Assert.That( ClassicObjectNames.CleanName( "mug@@@@@" ), Is.EqualTo( "mug" ) );
			Assert.That( ClassicObjectNames.CleanName( "  door  " ), Is.EqualTo( "door" ) );
			Assert.That( ClassicObjectNames.CleanName( null ), Is.EqualTo( "" ) );
		}

		//-------------------------------------------

		private static ClassicObject Obj( int id, int x, int y, int width, int height, string? name )
		{
			return new ClassicObject( id, x, y, width, height, name );
		}

		private static Dictionary<int, ClassicObject> Objects( params ClassicObject[] objects )
		{
			return objects.ToDictionary( o => o.ObjectId );
		}
	}
}
