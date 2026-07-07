using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ScriptScanner = MonkeyIsland1SpecialEditionXmlParser.Formats.Scumm.ScriptScanner;
using ScummParser = MonkeyIsland1SpecialEditionXmlParser.Formats.Scumm.Parser;

namespace Tests
{
	[TestFixture]
	public class ScriptScannerTests
	{
		[Test]
		public void Scan_EntryScriptWithLiteralPlacement_ExtractsActorCostumePositionAndDirection()
		{
			// Arrange: an entry script that dresses actor 3 in costume 24, places it at
			// (440, 132) in its own room and turns it to face front (animation 250)
			var script = new List<byte>();
			script.AddRange( new byte[] { 0x13, 3, 0x01, 24, 0xFF } );          // actorOps(3, costume(24))
			script.AddRange( new byte[] { 0x2D, 3, 10 } );                      // putActorInRoom(3, 10)
			script.AddRange( new byte[] { 0x01, 3, 0xB8, 0x01, 0x84, 0x00 } );  // putActor(3, 440, 132)
			script.AddRange( new byte[] { 0x11, 3, 250 } );                     // animateActor(3, 250)
			script.Add( 0x00 );                                                 // stopObjectCode

			var bytes = BuildEncodedResourceFile( roomNumber: 10, entryScript: script.ToArray() );

			// Act
			var result = ScriptScanner.ScanFromEncodedBytes( bytes );

			// Assert
			Assert.That( result.FailedScriptCount, Is.EqualTo( 0 ), "The synthetic script should decode cleanly" );
			Assert.That( result.PlacementsByRoom.ContainsKey( 10 ), Is.True );
			var placements = result.PlacementsByRoom[10];
			Assert.That( placements.Count, Is.EqualTo( 1 ) );

			var placement = placements[0];
			Assert.That( placement.ActorNumber, Is.EqualTo( 3 ) );
			Assert.That( placement.X, Is.EqualTo( 440 ) );
			Assert.That( placement.Y, Is.EqualTo( 132 ) );
			Assert.That( placement.CostumeId, Is.EqualTo( 24 ) );
			Assert.That( placement.CostumeInferred, Is.False );
			Assert.That( placement.Direction, Is.EqualTo( 2 ), "animation 250 faces front" );
			Assert.That( placement.Source, Is.EqualTo( "entry script" ) );
		}

		[Test]
		public void Scan_VariableCoordinatesAndParkedActors_AreSkipped()
		{
			// Arrange: putActor with a variable x (opcode bit 0x40) and a literal placement
			// at (0, 0), where scripts park actors before moving them at runtime
			var script = new List<byte>();
			script.AddRange( new byte[] { 0x41, 5, 0x32, 0x00, 0x84, 0x00 } );  // putActor(5, var50, 132)
			script.AddRange( new byte[] { 0x01, 6, 0x00, 0x00, 0x00, 0x00 } );  // putActor(6, 0, 0)
			script.Add( 0x00 );                                                 // stopObjectCode

			var bytes = BuildEncodedResourceFile( roomNumber: 10, entryScript: script.ToArray() );

			// Act
			var result = ScriptScanner.ScanFromEncodedBytes( bytes );

			// Assert
			Assert.That( result.FailedScriptCount, Is.EqualTo( 0 ) );
			Assert.That( result.PlacementsByRoom.ContainsKey( 10 ), Is.False, "Neither placement is usable" );
		}

		[Test]
		public void Scan_CostumeAssignedInAnotherRoomScript_IsInferred()
		{
			// Arrange: the entry script places actor 4 without a costume; a local script of
			// the same room assigns it one
			var entryScript = new List<byte>();
			entryScript.AddRange( new byte[] { 0x2D, 4, 10 } );                      // putActorInRoom(4, 10)
			entryScript.AddRange( new byte[] { 0x01, 4, 0x64, 0x00, 0x50, 0x00 } );  // putActor(4, 100, 80)
			entryScript.Add( 0x00 );

			var localScript = new List<byte>();
			localScript.AddRange( new byte[] { 0x13, 4, 0x01, 37, 0xFF } );          // actorOps(4, costume(37))
			localScript.Add( 0x00 );

			var bytes = BuildEncodedResourceFile( roomNumber: 10, entryScript: entryScript.ToArray(), localScript: localScript.ToArray() );

			// Act
			var result = ScriptScanner.ScanFromEncodedBytes( bytes );

			// Assert
			var placement = result.PlacementsByRoom[10].Single();
			Assert.That( placement.CostumeId, Is.EqualTo( 37 ) );
			Assert.That( placement.CostumeInferred, Is.True );
		}

		[Test]
		public void Scan_RealGameData_DecodesEveryScriptAndFindsThePirateLeaders()
		{
			var dataFileName = FindRealDataFile();
			if( dataFileName == null )
			{
				Assert.Ignore( "Classic game data (monkey1.001) not found on this machine" );
			}

			// Act
			var result = ScriptScanner.ScanFromEncodedBytes( File.ReadAllBytes( dataFileName! ) );

			// Assert: the instruction decoder must walk every script exactly to its end -
			// any drift means an opcode length is wrong
			Assert.That( result.ScriptCount, Is.GreaterThan( 700 ) );
			Assert.That( result.FailedScriptCount, Is.EqualTo( 0 ), "every retail script should decode cleanly" );

			// the pirate leaders in the bar (room 28): actor 3, costume 24 ("leaders-skin"),
			// seated at their table facing front
			Assert.That( result.PlacementsByRoom.ContainsKey( 28 ), Is.True );
			var leaders = result.PlacementsByRoom[28].FirstOrDefault( p => p.CostumeId == 24 );
			Assert.That( leaders, Is.Not.Null );
			Assert.That( leaders!.ActorNumber, Is.EqualTo( 3 ) );
			Assert.That( leaders.X, Is.EqualTo( 440 ) );
			Assert.That( leaders.Y, Is.EqualTo( 132 ) );
			Assert.That( leaders.Direction, Is.EqualTo( 2 ) );
		}

		//-------------------------------------------
		// synthetic block builders

		/// <summary>
		/// Builds an encoded resource file with one room carrying the given entry script
		/// (and optionally one local script, number 200).
		/// </summary>
		private static byte[] BuildEncodedResourceFile( int roomNumber, byte[] entryScript, byte[]? localScript = null )
		{
			var roomPayload = new List<byte[]>();
			roomPayload.Add( Block( "RMHD", RmhdPayload( 320, 200, 0 ) ) );
			roomPayload.Add( Block( "ENCD", entryScript ) );
			if( localScript != null )
			{
				var numbered = new byte[localScript.Length + 1];
				numbered[0] = 200;
				localScript.CopyTo( numbered, 1 );
				roomPayload.Add( Block( "LSCR", numbered ) );
			}
			var room = Block( "ROOM", roomPayload.ToArray() );
			var lflf = Block( "LFLF", room );

			// LECF header (8) + LOFF block + LFLF header (8) = the ROOM block position
			var loffLength = 8 + 1 + 5;
			var roomPosition = 8 + loffLength + 8;

			var loffPayload = new List<byte>();
			loffPayload.Add( 1 );
			loffPayload.Add( (byte)roomNumber );
			loffPayload.AddRange( System.BitConverter.GetBytes( (uint)roomPosition ) );
			var loff = Block( "LOFF", loffPayload.ToArray() );

			var lecf = Block( "LECF", loff, lflf );
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

		private static byte[] RmhdPayload( int width, int height, int objectCount )
		{
			var bytes = new List<byte>();
			bytes.AddRange( System.BitConverter.GetBytes( (ushort)width ) );
			bytes.AddRange( System.BitConverter.GetBytes( (ushort)height ) );
			bytes.AddRange( System.BitConverter.GetBytes( (ushort)objectCount ) );
			return bytes.ToArray();
		}

		/// <summary>
		/// Looks for the real classic data next to a Special Edition install; the integration
		/// test is skipped when it is not present.
		/// </summary>
		private static string? FindRealDataFile()
		{
			var candidates = new[]
			{
				@"F:\Games\Steam\steamapps\common\The Secret of Monkey Island Special Edition\classic\en\monkey1.001",
				@"C:\Program Files (x86)\Steam\steamapps\common\The Secret of Monkey Island Special Edition\classic\en\monkey1.001",
			};
			foreach( var candidate in candidates )
			{
				if( File.Exists( candidate ) )
				{
					return candidate;
				}
			}

			// the retail install embeds the classic files in the pak; extract on the fly
			var pakCandidates = new[]
			{
				@"F:\Games\Steam\steamapps\common\The Secret of Monkey Island Special Edition\Monkey1.pak",
				@"C:\Program Files (x86)\Steam\steamapps\common\The Secret of Monkey Island Special Edition\Monkey1.pak",
			};
			foreach( var pakCandidate in pakCandidates )
			{
				if( !File.Exists( pakCandidate ) )
				{
					continue;
				}
				var lpak = MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK.Parser.Parse( pakCandidate );
				if( lpak == null )
				{
					continue;
				}
				lpak.FileNameOnDisk = pakCandidate;
				var index = MonkeyIsland1SpecialEditionXmlParser.Helper.FindEntryIndex(
					lpak, name => name.EndsWith( ".001", System.StringComparison.OrdinalIgnoreCase ) );
				if( index < 0 )
				{
					continue;
				}
				var tempFileName = Path.Combine( Path.GetTempPath(), "mi1se-test-monkey1.001" );
				File.WriteAllBytes( tempFileName, MonkeyIsland1SpecialEditionXmlParser.Helper.ReadEntryBytes( lpak, index ) );
				return tempFileName;
			}

			return null;
		}
	}
}
