using System.Drawing;
using System.IO;
using System.Linq;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;
using NUnit.Framework;
using ScummParser = MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Parser;

namespace Tests
{
	[TestFixture]
	public class ClassicBoxTests
	{
		private static ClassicBox Box( int mask, int flags, params (int x, int y)[] corners )
		{
			return new ClassicBox(
				cornerList: corners.Select( c => new Point( c.x, c.y ) ).ToArray(),
				mask: mask,
				flags: flags,
				scale: 255
			);
		}

		[Test]
		public void Contains_InsideAndOutsideAQuad()
		{
			var box = Box( 1, 0, (10, 10), (30, 10), (30, 20), (10, 20) );

			Assert.That( box.Contains( 20, 15 ), Is.True );
			Assert.That( box.Contains( 5, 15 ), Is.False );
			Assert.That( box.Contains( 20, 25 ), Is.False );
		}

		[Test]
		public void Contains_DegenerateLineAndPointBoxes_ContainNothing()
		{
			// walkboxes are often "stand here" line segments; the placeholder box 0 is a point
			var line = Box( 0, 0, (367, 140), (466, 140), (466, 140), (367, 140) );
			var point = Box( 0, 0, (-32000, -32000), (-32000, -32000), (-32000, -32000), (-32000, -32000) );

			Assert.That( line.Contains( 400, 140 ), Is.False );
			Assert.That( point.Contains( -32000, -32000 ), Is.False );
		}

		[Test]
		public void DistanceTo_MeasuresToTheClosestEdge()
		{
			var line = Box( 0, 0, (367, 140), (466, 140), (466, 140), (367, 140) );

			Assert.That( line.DistanceTo( 440, 132 ), Is.EqualTo( 8.0 ).Within( 0.001 ) );
			Assert.That( line.DistanceTo( 466, 140 ), Is.EqualTo( 0.0 ).Within( 0.001 ) );

			var quad = Box( 1, 0, (10, 10), (30, 10), (30, 20), (10, 20) );
			Assert.That( quad.DistanceTo( 20, 15 ), Is.EqualTo( 0.0 ), "inside the quad" );
		}

		[Test]
		public void GetBoxMaskAt_PicksTheNearestWalkableBox()
		{
			var room = new ClassicRoom(
				roomNumber: 1,
				width: 320,
				height: 200,
				objectList: new System.Collections.Generic.List<ClassicObject>()
			);
			room.BoxList.Add( Box( 0, 0, (-32000, -32000), (-32000, -32000), (-32000, -32000), (-32000, -32000) ) );
			room.BoxList.Add( Box( 0, 0, (367, 140), (466, 140), (466, 140), (367, 140) ) );
			room.BoxList.Add( Box( 1, 0, (350, 109), (526, 109), (526, 111), (350, 116) ) );
			room.BoxList.Add( Box( 3, 0x80, (440, 130), (441, 130), (441, 131), (440, 131) ) );  // invisible: skipped

			// the pirate leaders' spot: nearest walkable box is the mask 0 bench line
			Assert.That( room.GetBoxMaskAt( 440, 132 ), Is.EqualTo( 0 ) );

			// the walkway behind the bar table: nearest is the mask 1 strip
			Assert.That( room.GetBoxMaskAt( 428, 113 ), Is.EqualTo( 1 ) );
		}

		[Test]
		public void GetBoxMaskAt_WithoutBoxes_ReturnsZero()
		{
			var room = new ClassicRoom(
				roomNumber: 1,
				width: 320,
				height: 200,
				objectList: new System.Collections.Generic.List<ClassicObject>()
			);

			Assert.That( room.GetBoxMaskAt( 100, 100 ), Is.EqualTo( 0 ) );
		}

		[Test]
		public void ReadRooms_RealGameData_BoxMasksMatchTheGameScenes()
		{
			var dataFileName = TestData.FindRealDataFile();
			if( dataFileName == null )
			{
				Assert.Ignore( "Classic game data (monkey1.001) not found on this machine" );
			}

			var rooms = ScummParser.ReadRoomsFromDataFile( dataFileName! );

			// the bar: the pirate leaders' bench is unmasked (they draw in front of their
			// table, hands on it), the walkway behind the table is masked
			var bar = rooms.First( r => r.RoomNumber == 28 );
			Assert.That( bar.BoxList.Count, Is.EqualTo( 12 ) );
			Assert.That( bar.GetBoxMaskAt( 440, 132 ), Is.EqualTo( 0 ), "pirate leaders draw in front of the table" );
			Assert.That( bar.GetBoxMaskAt( 428, 125 ), Is.EqualTo( 1 ), "the cook stands behind the table" );

			// the store: the keeper's spot behind the counter is masked
			var store = rooms.First( r => r.RoomNumber == 30 );
			Assert.That( store.GetBoxMaskAt( 289, 135 ), Is.EqualTo( 2 ), "the storekeeper stands behind the counter" );
		}
	}
}
