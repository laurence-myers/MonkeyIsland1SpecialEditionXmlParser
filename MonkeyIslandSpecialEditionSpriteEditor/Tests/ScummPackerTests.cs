using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;
using NUnit.Framework;
using ScummPacker = MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Packer;
using ScummParser = MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Parser;

namespace Tests
{
	[TestFixture]
	public class ScummPackerTests
	{
		[Test]
		public void PatchRoomBoxes_ReplacesOnlyTheTargetRoomAndRoundTrips()
		{
			// Arrange: two rooms, each with two boxes
			var room10 = new[] { MakeBox( 0, 0, 10, 20, mask: 0, flags: 0, scale: 255 ), MakeBox( 10, 0, 30, 40, mask: 1, flags: 0, scale: 200 ) };
			var room20 = new[] { MakeBox( 5, 5, 15, 25, mask: 2, flags: 0x80, scale: 100 ) };
			var encoded = BuildResourceFile( ( 10, room10 ), ( 20, room20 ) );

			var edited = new[]
			{
				MakeBox( 1, 2, 11, 22, mask: 3, flags: 0, scale: 128 ),   // moved + rescaled
				MakeBox( 10, 0, 30, 40, mask: 1, flags: 0, scale: 200 ),  // unchanged
			};

			// Act
			var patched = ScummPacker.PatchRoomBoxes( encoded, 10, edited, room10 );

			// Assert: room 10 now has the edited boxes, room 20 is untouched
			var rooms = ScummParser.ReadRoomsFromEncodedBytes( patched );
			var patched10 = rooms.First( r => r.RoomNumber == 10 );
			AssertBoxesEqual( patched10.BoxList, edited );

			var patched20 = rooms.First( r => r.RoomNumber == 20 );
			AssertBoxesEqual( patched20.BoxList, room20 );
		}

		[Test]
		public void PatchRoomBoxes_NoOpEdit_ReturnsByteIdenticalFile()
		{
			var room10 = new[] { MakeBox( 0, 0, 10, 20, mask: 0, flags: 0, scale: 255 ) };
			var encoded = BuildResourceFile( ( 10, room10 ) );

			// re-supplying the same boxes must reproduce the original file exactly
			var patched = ScummPacker.PatchRoomBoxes( encoded, 10, room10.Select( CloneBox ).ToArray(), room10 );

			Assert.That( patched, Is.EqualTo( encoded ) );
		}

		[Test]
		public void PatchRoomBoxes_CountMismatch_Throws()
		{
			var room10 = new[] { MakeBox( 0, 0, 10, 20, mask: 0, flags: 0, scale: 255 ) };
			var encoded = BuildResourceFile( ( 10, room10 ) );
			var twoBoxes = new[] { room10[0], MakeBox( 1, 1, 2, 2, 0, 0, 0 ) };

			Assert.Throws<InvalidOperationException>( () => ScummPacker.PatchRoomBoxes( encoded, 10, twoBoxes, room10 ) );
		}

		[Test]
		public void PatchRoomBoxes_UnknownRoom_Throws()
		{
			var room10 = new[] { MakeBox( 0, 0, 10, 20, mask: 0, flags: 0, scale: 255 ) };
			var encoded = BuildResourceFile( ( 10, room10 ) );

			Assert.Throws<InvalidOperationException>( () => ScummPacker.PatchRoomBoxes( encoded, 99, room10, room10 ) );
		}

		[Test]
		public void PatchRoomBoxes_OriginalDoesNotMatchTheFile_Throws()
		{
			var room10 = new[] { MakeBox( 0, 0, 10, 20, mask: 0, flags: 0, scale: 255 ) };
			var encoded = BuildResourceFile( ( 10, room10 ) );

			// claim a different "original" than the file actually holds
			var wrongOriginal = new[] { MakeBox( 9, 9, 9, 9, mask: 7, flags: 0, scale: 1 ) };
			var edited = new[] { MakeBox( 1, 1, 1, 1, mask: 0, flags: 0, scale: 0 ) };

			Assert.Throws<InvalidOperationException>( () => ScummPacker.PatchRoomBoxes( encoded, 10, edited, wrongOriginal ) );
		}

		[Test]
		public void PatchRoomBoxes_CornerOutOfInt16Range_ThrowsAndWritesNothing()
		{
			var room10 = new[] { MakeBox( 0, 0, 10, 20, mask: 0, flags: 0, scale: 255 ) };
			var encoded = BuildResourceFile( ( 10, room10 ) );

			// a corner past the int16 range would wrap into corrupt geometry if written
			var edited = new[] { MakeBox( 0, 0, 40000, 20, mask: 0, flags: 0, scale: 255 ) };

			Assert.Throws<InvalidOperationException>( () => ScummPacker.PatchRoomBoxes( encoded, 10, edited, room10 ) );
		}

		[Test]
		public void PatchRoomBoxes_PreservesNegativePlaceholderCorners()
		{
			// box 0's classic placeholder sits at (-32000, -32000); the int16 write must round-trip it
			var room10 = new[] { MakeBox( -32000, -32000, -32000, -32000, mask: 0, flags: 0, scale: 255 ) };
			var encoded = BuildResourceFile( ( 10, room10 ) );

			var edited = new[] { MakeBox( -100, -200, 300, 400, mask: 0, flags: 0, scale: 255 ) };
			var patched = ScummPacker.PatchRoomBoxes( encoded, 10, edited, room10 );

			var rooms = ScummParser.ReadRoomsFromEncodedBytes( patched );
			AssertBoxesEqual( rooms.First( r => r.RoomNumber == 10 ).BoxList, edited );
		}

		//-------------------------------------------
		// helpers

		private static ClassicBox MakeBox( int ulx, int uly, int lrx, int lry, int mask, int flags, int scale )
		{
			var corners = new[]
			{
				new Point( ulx, uly ),
				new Point( lrx, uly ),
				new Point( lrx, lry ),
				new Point( ulx, lry ),
			};
			return new ClassicBox( corners, mask, flags, scale );
		}

		private static ClassicBox CloneBox( ClassicBox box )
		{
			return box.Clone();
		}

		private static void AssertBoxesEqual( IReadOnlyList<ClassicBox> actual, IReadOnlyList<ClassicBox> expected )
		{
			Assert.That( actual.Count, Is.EqualTo( expected.Count ) );
			for( var index = 0; index < expected.Count; index++ )
			{
				Assert.That( actual[index].CornerList, Is.EqualTo( expected[index].CornerList ), "corners of box " + index );
				Assert.That( actual[index].Mask, Is.EqualTo( expected[index].Mask ), "mask of box " + index );
				Assert.That( actual[index].Flags, Is.EqualTo( expected[index].Flags ), "flags of box " + index );
				Assert.That( actual[index].Scale, Is.EqualTo( expected[index].Scale ), "scale of box " + index );
			}
		}

		private static byte[] BuildResourceFile( params (int RoomNumber, ClassicBox[] Boxes)[] rooms )
		{
			// build each LFLF (ROOM = RMHD + BOXD) and a LOFF table pointing at each ROOM block
			var lflfs = rooms.Select( r => Block( "LFLF", Block( "ROOM", Block( "RMHD", RmhdPayload( 320, 200 ) ), Block( "BOXD", BoxdPayload( r.Boxes ) ) ) ) ).ToArray();

			var loffLength = 8 + 1 + rooms.Length * 5;
			var loffPayload = new List<byte> { (byte)rooms.Length };
			var position = 8 + loffLength; // after LECF header + LOFF block
			for( var index = 0; index < rooms.Length; index++ )
			{
				var roomOffset = position + 8; // ROOM sits just inside the LFLF header
				loffPayload.Add( (byte)rooms[index].RoomNumber );
				loffPayload.AddRange( BitConverter.GetBytes( (uint)roomOffset ) );
				position += lflfs[index].Length;
			}

			var loff = Block( "LOFF", loffPayload.ToArray() );
			var lecf = Block( "LECF", new[] { loff }.Concat( lflfs ).ToArray() );
			ScummParser.XorDecode( lecf, ScummParser.XorKey ); // encode
			return lecf;
		}

		private static byte[] BoxdPayload( ClassicBox[] boxes )
		{
			var bytes = new List<byte>();
			bytes.AddRange( BitConverter.GetBytes( (ushort)boxes.Length ) );
			foreach( var box in boxes )
			{
				foreach( var corner in box.CornerList )
				{
					bytes.AddRange( BitConverter.GetBytes( (short)corner.X ) );
					bytes.AddRange( BitConverter.GetBytes( (short)corner.Y ) );
				}
				bytes.Add( (byte)box.Mask );
				bytes.Add( (byte)box.Flags );
				bytes.AddRange( BitConverter.GetBytes( (ushort)box.Scale ) );
			}
			return bytes.ToArray();
		}

		private static byte[] RmhdPayload( int width, int height )
		{
			var bytes = new List<byte>();
			bytes.AddRange( BitConverter.GetBytes( (ushort)width ) );
			bytes.AddRange( BitConverter.GetBytes( (ushort)height ) );
			bytes.AddRange( BitConverter.GetBytes( (ushort)0 ) );
			return bytes.ToArray();
		}

		private static byte[] Block( string tag, params byte[][] payloads )
		{
			var payload = payloads.SelectMany( p => p ).ToArray();
			var size = payload.Length + 8;
			var bytes = new List<byte>();
			bytes.AddRange( Encoding.ASCII.GetBytes( tag ) );
			bytes.Add( (byte)( ( size >> 24 ) & 0xFF ) );
			bytes.Add( (byte)( ( size >> 16 ) & 0xFF ) );
			bytes.Add( (byte)( ( size >> 8 ) & 0xFF ) );
			bytes.Add( (byte)( size & 0xFF ) );
			bytes.AddRange( payload );
			return bytes.ToArray();
		}
	}
}
