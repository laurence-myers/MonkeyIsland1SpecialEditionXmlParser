using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ScummParser = MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Parser;

namespace Tests
{
	[TestFixture]
	public class ScummParserTests
	{
		[Test]
		public void XorDecode_IsSymmetric()
		{
			// Arrange
			var original = Encoding.ASCII.GetBytes( "LECF test payload" );
			var bytes = original.ToArray();

			// Act
			ScummParser.XorDecode( bytes, ScummParser.XorKey );
			Assert.That( bytes, Is.Not.EqualTo( original ), "Encoding should change the bytes" );
			ScummParser.XorDecode( bytes, ScummParser.XorKey );

			// Assert
			Assert.That( bytes, Is.EqualTo( original ), "Decoding should restore the bytes" );
		}

		[Test]
		public void ReadRoomsFromDataFile_ReadsRoomsAndObjects()
		{
			// Arrange
			var tempFileName = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".001" );
			try
			{
				File.WriteAllBytes( tempFileName, BuildEncodedResourceFile() );

				// Act
				var rooms = ScummParser.ReadRoomsFromDataFile( tempFileName );

				// Assert
				Assert.That( rooms.Count, Is.EqualTo( 2 ), "Both LFLF rooms should be read" );

				var room1 = rooms[0];
				Assert.That( room1.RoomNumber, Is.EqualTo( 10 ), "Room number should come from the LOFF table" );
				Assert.That( room1.Width, Is.EqualTo( 320 ) );
				Assert.That( room1.Height, Is.EqualTo( 200 ) );
				Assert.That( room1.ObjectList.Count, Is.EqualTo( 2 ) );

				var object1 = room1.ObjectList[0];
				Assert.That( object1.ObjectId, Is.EqualTo( 101 ) );
				Assert.That( object1.X, Is.EqualTo( 2 * 8 ), "X should be converted from strip units to pixels" );
				Assert.That( object1.Y, Is.EqualTo( 3 * 8 ) );
				Assert.That( object1.Width, Is.EqualTo( 4 * 8 ) );
				Assert.That( object1.Height, Is.EqualTo( 5 * 8 ) );
				Assert.That( object1.Name, Is.EqualTo( "door" ) );

				var object2 = room1.ObjectList[1];
				Assert.That( object2.ObjectId, Is.EqualTo( 102 ) );
				Assert.That( object2.Name, Is.Null, "An object without an OBNA block should have no name" );

				var room2 = rooms[1];
				Assert.That( room2.RoomNumber, Is.EqualTo( 20 ) );
				Assert.That( room2.ObjectList.Count, Is.EqualTo( 0 ), "A room without OBCD blocks should have no objects" );
			}
			finally
			{
				if( File.Exists( tempFileName ) )
				{
					File.Delete( tempFileName );
				}
			}
		}

		[Test]
		public void ReadRooms_WithoutLoffMatch_FallsBackToSequentialNumbering()
		{
			// Arrange: no LOFF block at all
			var room = Block( "ROOM", Block( "RMHD", RmhdPayload( 320, 200, 0 ) ) );
			var lflf = Block( "LFLF", room );
			var lecf = Block( "LECF", lflf );

			using( var stream = new MemoryStream( lecf ) )
			using( var reader = new BinaryReader( stream ) )
			{
				// Act
				var rooms = ScummParser.ReadRooms( reader );

				// Assert
				Assert.That( rooms.Count, Is.EqualTo( 1 ) );
				Assert.That( rooms[0].RoomNumber, Is.EqualTo( 1 ), "Fallback numbering should be 1-based file order" );
			}
		}

		[Test]
		public void ReadRooms_WithTrailingJunk_StopsWithoutThrowing()
		{
			// Arrange: a valid LECF followed by junk that is not a valid block
			var room = Block( "ROOM", Block( "RMHD", RmhdPayload( 320, 200, 0 ) ) );
			var lecf = Block( "LECF", Block( "LFLF", room ) );
			var bytes = lecf.Concat( new byte[] { 1, 2, 3, 4, 5 } ).ToArray();

			using( var stream = new MemoryStream( bytes ) )
			using( var reader = new BinaryReader( stream ) )
			{
				// Act
				var rooms = ScummParser.ReadRooms( reader );

				// Assert
				Assert.That( rooms.Count, Is.EqualTo( 1 ) );
			}
		}

		[Test]
		public void ReadRoomNamesFromIndexFile_ReadsRnamTable()
		{
			// Arrange
			var payload = new List<byte>();
			payload.Add( 10 );
			payload.AddRange( RoomNameBytes( "bar" ) );
			payload.Add( 20 );
			payload.AddRange( RoomNameBytes( "dock" ) );
			payload.Add( 0 ); // terminator

			var rnam = Block( "RNAM", payload.ToArray() );
			var bytes = rnam.ToArray();
			ScummParser.XorDecode( bytes, ScummParser.XorKey ); // encode

			var tempFileName = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".000" );
			try
			{
				File.WriteAllBytes( tempFileName, bytes );

				// Act
				var roomNames = ScummParser.ReadRoomNamesFromIndexFile( tempFileName );

				// Assert
				Assert.That( roomNames.Count, Is.EqualTo( 2 ) );
				Assert.That( roomNames[10], Is.EqualTo( "bar" ) );
				Assert.That( roomNames[20], Is.EqualTo( "dock" ) );
			}
			finally
			{
				if( File.Exists( tempFileName ) )
				{
					File.Delete( tempFileName );
				}
			}
		}

		[Test]
		public void ReadCostumes_ParsesLimbsAndCels()
		{
			byte[] dataBytes;
			byte[] indexBytes;
			BuildEncodedCostumeFiles( out dataBytes, out indexBytes );

			var costumes = ScummParser.ReadCostumesFromEncodedBytes( dataBytes, indexBytes );

			Assert.That( costumes.Count, Is.EqualTo( 1 ) );
			var costume = costumes[0];
			Assert.That( costume.CostumeId, Is.EqualTo( 21 ) );
			Assert.That( costume.RoomNumber, Is.EqualTo( 59 ) );
			Assert.That( costume.MaximumAnimationNumber, Is.EqualTo( 4 ) );
			Assert.That( costume.Format, Is.EqualTo( 0x58 ) );
			Assert.That( costume.Mirror, Is.True );

			Assert.That( costume.LimbList.Count, Is.EqualTo( 2 ), "only the two limbs with cels are kept" );

			var limb0 = costume.FindLimb( 0 );
			Assert.That( limb0, Is.Not.Null );
			Assert.That( limb0!.CelList.Count, Is.EqualTo( 2 ) );
			Assert.That( limb0.CelList[0]!.Width, Is.EqualTo( 23 ) );
			Assert.That( limb0.CelList[0]!.Height, Is.EqualTo( 57 ) );
			Assert.That( limb0.CelList[0]!.RelX, Is.EqualTo( -11 ) );
			Assert.That( limb0.CelList[0]!.RelY, Is.EqualTo( -57 ) );
			Assert.That( limb0.CelList[0]!.MoveX, Is.EqualTo( 1 ) );
			Assert.That( limb0.CelList[0]!.MoveY, Is.EqualTo( -2 ) );
			Assert.That( limb0.CelList[1]!.Width, Is.EqualTo( 10 ) );

			var limb1 = costume.FindLimb( 1 );
			Assert.That( limb1, Is.Not.Null );
			Assert.That( limb1!.CelList.Count, Is.EqualTo( 1 ) );
			Assert.That( limb1.CelList[0]!.Width, Is.EqualTo( 5 ) );
			Assert.That( limb1.CelList[0]!.RelY, Is.EqualTo( -6 ) );

			// only animation 4 is defined; its limb sequences keep the raw command bytes
			Assert.That( costume.AnimationList.Count, Is.EqualTo( 1 ) );
			var animation = costume.AnimationList[0];
			Assert.That( animation.AnimationNumber, Is.EqualTo( 4 ) );
			var animationLimb0 = animation.FindLimb( 0 );
			Assert.That( animationLimb0, Is.Not.Null );
			Assert.That( animationLimb0!.CommandList, Is.EqualTo( new[] { 0, 1 } ) );
			Assert.That( animationLimb0.Loop, Is.True, "no high bit on the length byte means the sequence loops" );
			var animationLimb1 = animation.FindLimb( 1 );
			Assert.That( animationLimb1, Is.Not.Null );
			Assert.That( animationLimb1!.CommandList, Is.EqualTo( new[] { 0 } ) );
		}

		//-------------------------------------------
		// synthetic block builders

		/// <summary>
		/// Builds a resource file with one room (number 59) whose LFLF contains a COST
		/// resource, plus an index file whose DCOS directory maps costume 21 to it.
		/// All costume-internal offsets are relative to the block position plus two.
		/// </summary>
		private static void BuildEncodedCostumeFiles( out byte[] dataBytes, out byte[] indexBytes )
		{
			// the payload starts at base offset 6 (base = block position + 2), so a base
			// offset b lives at payload index b - 6
			const int numAnim = 4;
			var payload = new List<byte>();
			payload.Add( numAnim );                                // [0] highest animation number
			payload.Add( 0x58 | 0x80 );                            // [1] format 0x58, mirrored
			payload.AddRange( new byte[16] );                      // [2..17] palette
			payload.AddRange( System.BitConverter.GetBytes( (ushort)76 ) ); // [18] anim commands offset
			// [20..51] 16 limb table offsets; unused limbs point at the picture data
			payload.AddRange( System.BitConverter.GetBytes( (ushort)79 ) );
			payload.AddRange( System.BitConverter.GetBytes( (ushort)83 ) );
			for( var limb = 2; limb < 16; limb++ )
			{
				payload.AddRange( System.BitConverter.GetBytes( (ushort)85 ) );
			}
			// [52..61] anim offsets for anims 0..4; only anim 4 is defined
			for( var animation = 0; animation < numAnim; animation++ )
			{
				payload.AddRange( System.BitConverter.GetBytes( (ushort)0 ) );
			}
			payload.AddRange( System.BitConverter.GetBytes( (ushort)68 ) );
			// base offset 68: anim 4 definition - limbs 0 and 1
			payload.AddRange( System.BitConverter.GetBytes( (ushort)0xC000 ) );
			payload.AddRange( System.BitConverter.GetBytes( (ushort)0 ) );  // limb 0: commands 0..1
			payload.Add( 1 );
			payload.AddRange( System.BitConverter.GetBytes( (ushort)2 ) );  // limb 1: command 2
			payload.Add( 0 );
			// base offset 76: anim commands (cel indexes)
			payload.Add( 0 );
			payload.Add( 1 );
			payload.Add( 0 );
			// base offset 79: limb 0 cel table; 83: limb 1 cel table; 85: picture data
			payload.AddRange( System.BitConverter.GetBytes( (ushort)85 ) );
			payload.AddRange( System.BitConverter.GetBytes( (ushort)97 ) );
			payload.AddRange( System.BitConverter.GetBytes( (ushort)109 ) );
			payload.AddRange( CelPayload( width: 23, height: 57, relX: -11, relY: -57, moveX: 1, moveY: -2 ) );
			payload.AddRange( CelPayload( width: 10, height: 20, relX: 3, relY: -20, moveX: 0, moveY: 0 ) );
			payload.AddRange( CelPayload( width: 5, height: 6, relX: -1, relY: -6, moveX: 0, moveY: 0 ) );
			var cost = Block( "COST", payload.ToArray() );

			var room = Block( "ROOM", Block( "RMHD", RmhdPayload( 320, 200, 0 ) ) );
			var lflf = Block( "LFLF", room.Concat( cost ).ToArray() );

			// LECF header (8) + LOFF block + LFLF header (8) = the ROOM block position
			var loffLength = 8 + 1 + 5;
			var roomPosition = 8 + loffLength + 8;
			var costOffsetInRoom = room.Length; // COST follows ROOM inside the LFLF

			var loffPayload = new List<byte>();
			loffPayload.Add( 1 );
			loffPayload.Add( 59 );
			loffPayload.AddRange( System.BitConverter.GetBytes( (uint)roomPosition ) );
			var loff = Block( "LOFF", loffPayload.ToArray() );

			dataBytes = Block( "LECF", loff, lflf );
			ScummParser.XorDecode( dataBytes, ScummParser.XorKey ); // encode

			// index: DCOS with costume 21 pointing at room 59; every other slot is empty
			const int costumeCount = 22;
			var dcosPayload = new List<byte>();
			dcosPayload.AddRange( System.BitConverter.GetBytes( (ushort)costumeCount ) );
			for( var costumeId = 0; costumeId < costumeCount; costumeId++ )
			{
				dcosPayload.Add( costumeId == 21 ? (byte)59 : (byte)0 );
			}
			for( var costumeId = 0; costumeId < costumeCount; costumeId++ )
			{
				dcosPayload.AddRange( System.BitConverter.GetBytes( costumeId == 21 ? (uint)costOffsetInRoom : 0u ) );
			}
			indexBytes = Block( "DCOS", dcosPayload.ToArray() );
			ScummParser.XorDecode( indexBytes, ScummParser.XorKey ); // encode
		}

		private static byte[] CelPayload( int width, int height, int relX, int relY, int moveX, int moveY )
		{
			var bytes = new List<byte>();
			bytes.AddRange( System.BitConverter.GetBytes( (ushort)width ) );
			bytes.AddRange( System.BitConverter.GetBytes( (ushort)height ) );
			bytes.AddRange( System.BitConverter.GetBytes( (short)relX ) );
			bytes.AddRange( System.BitConverter.GetBytes( (short)relY ) );
			bytes.AddRange( System.BitConverter.GetBytes( (short)moveX ) );
			bytes.AddRange( System.BitConverter.GetBytes( (short)moveY ) );
			return bytes.ToArray();
		}

		private static byte[] BuildEncodedResourceFile()
		{
			// room 1: RMHD 320x200, two objects (one with a name, one without)
			var obcd1 = Block( "OBCD",
				Block( "CDHD", CdhdPayload( objectId: 101, xStrips: 2, yStrips: 3, widthStrips: 4, heightStrips: 5 ) ),
				Block( "OBNA", Encoding.ASCII.GetBytes( "door\0" ) )
			);
			var obcd2 = Block( "OBCD",
				Block( "CDHD", CdhdPayload( objectId: 102, xStrips: 1, yStrips: 1, widthStrips: 2, heightStrips: 2 ) )
			);
			var room1 = Block( "ROOM",
				Block( "RMHD", RmhdPayload( 320, 200, 2 ) ),
				obcd1,
				obcd2
			);
			var lflf1 = Block( "LFLF", room1 );

			// room 2: no objects
			var room2 = Block( "ROOM", Block( "RMHD", RmhdPayload( 320, 200, 0 ) ) );
			var lflf2 = Block( "LFLF", room2 );

			// the LOFF table points at the absolute position of each ROOM block:
			// LECF header (8) + LOFF block + LFLF header (8)
			var loffLength = 8 + 1 + 2 * 5;
			var lflf1Position = 8 + loffLength;
			var room1Offset = lflf1Position + 8;
			var lflf2Position = lflf1Position + lflf1.Length;
			var room2Offset = lflf2Position + 8;

			var loffPayload = new List<byte>();
			loffPayload.Add( 2 );
			loffPayload.Add( 10 );
			loffPayload.AddRange( System.BitConverter.GetBytes( (uint)room1Offset ) );
			loffPayload.Add( 20 );
			loffPayload.AddRange( System.BitConverter.GetBytes( (uint)room2Offset ) );
			var loff = Block( "LOFF", loffPayload.ToArray() );

			var lecf = Block( "LECF", loff, lflf1, lflf2 );

			ScummParser.XorDecode( lecf, ScummParser.XorKey ); // encode
			return lecf;
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

		private static byte[] CdhdPayload( int objectId, byte xStrips, byte yStrips, byte widthStrips, byte heightStrips )
		{
			var bytes = new List<byte>();
			bytes.AddRange( System.BitConverter.GetBytes( (ushort)objectId ) );
			bytes.Add( xStrips );
			bytes.Add( yStrips );
			bytes.Add( widthStrips );
			bytes.Add( heightStrips );
			bytes.Add( 0 ); // flags
			bytes.Add( 0 ); // parent
			bytes.AddRange( System.BitConverter.GetBytes( (short)0 ) ); // walk_x
			bytes.AddRange( System.BitConverter.GetBytes( (short)0 ) ); // walk_y
			bytes.Add( 0 ); // actor direction
			return bytes.ToArray();
		}

		private static byte[] RmhdPayload( int width, int height, int objectCount )
		{
			var bytes = new List<byte>();
			bytes.AddRange( System.BitConverter.GetBytes( (ushort)width ) );
			bytes.AddRange( System.BitConverter.GetBytes( (ushort)height ) );
			bytes.AddRange( System.BitConverter.GetBytes( (ushort)objectCount ) );
			return bytes.ToArray();
		}

		private static byte[] RoomNameBytes( string name )
		{
			var bytes = new byte[9];
			for( var index = 0; index < 9; index++ )
			{
				var @byte = index < name.Length ? (byte)name[index] : (byte)0;
				bytes[index] = (byte)( @byte ^ 0xFF );
			}
			return bytes;
		}
	}
}
