using System.Collections.Generic;
using System.Linq;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class RoomEntryEvaluatorTests
	{
		[Test]
		public void LiteralSetState_GivesOneScenario()
		{
			// setState(200, 1); stop
			var scenarios = Evaluate( new[] { 200 }, NoSeeds(),
				SetStateLiteral( 200, 1 ), Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].Conditions, Is.Empty );
			Assert.That( scenarios[0].ObjectStates[200], Is.EqualTo( 1 ) );
		}

		[Test]
		public void VariableLoop_ResolvesEveryObjectInTheRange()
		{
			// the room 28 bar pattern: a loop that shows a range of objects
			//   move v100 = 330
			//   L: setState(v100, 1); increment v100
			//   continue (exit) when v100 > 333, else jump back to L
			var scenarios = Evaluate( new[] { 330, 331, 332, 333, 334 }, NoSeeds(),
				Move( 100, 330 ),
				SetStateVariableObject( 100, 1 ),
				Increment( 100 ),
				IsLessLoop( 100, 333, jumpBackTo: 5 ),
				Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 1 ), "a concrete loop must not fork" );
			Assert.That( scenarios[0].Conditions, Is.Empty );
			Assert.That( scenarios[0].ObjectStates[330], Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].ObjectStates[331], Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].ObjectStates[332], Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].ObjectStates[333], Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].ObjectStates[334], Is.EqualTo( 0 ), "the loop stops before this object" );
		}

		[Test]
		public void UnknownVariableTest_ForksIntoTwoScenarios()
		{
			// if var 60 == 0 then setState(200, 1); stop
			var scenarios = Evaluate( new[] { 200 }, NoSeeds(),
				EqualZero( 60, jumpOverBytes: 4 ),
				SetStateLiteral( 200, 1 ),
				Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 2 ) );
			var shown = scenarios.Single( s => s.ObjectStates[200] == 1 );
			var hidden = scenarios.Single( s => s.ObjectStates[200] == 0 );
			Assert.That( shown.Conditions, Is.EqualTo( new[] { "var 60 == 0" } ) );
			Assert.That( hidden.Conditions, Is.EqualTo( new[] { "var 60 != 0" } ) );
		}

		[Test]
		public void SeededStateTest_ResolvesWithoutAFork()
		{
			// object 389 starts at state 0, so "ifNotState(389, 1)" is a closed test:
			// the walk keeps the next instruction and never forks
			var scenarios = Evaluate( new[] { 101 }, Seed( 389, 0 ),
				IfNotState( 389, 1, jumpOverBytes: 4 ),
				SetStateLiteral( 101, 1 ),
				Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].Conditions, Is.Empty );
			Assert.That( scenarios[0].ObjectStates[101], Is.EqualTo( 1 ) );
		}

		[Test]
		public void DrawObjectWithoutState_SetsStateOne()
		{
			// drawObject(150) with the no-parameter form
			var scenarios = Evaluate( new[] { 150 }, NoSeeds(),
				DrawObjectPlain( 150 ), Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].ObjectStates[150], Is.EqualTo( 1 ) );
		}

		[Test]
		public void WriteThroughUnknownVariable_SetsTheFlag()
		{
			// setState(v70, 1) with v70 never assigned: the target is unknown
			var scenarios = Evaluate( new[] { 200 }, NoSeeds(),
				SetStateVariableObject( 70, 1 ), Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].HasUnknownWrites, Is.True );
			Assert.That( scenarios[0].ObjectStates[200], Is.EqualTo( 0 ), "the requested object keeps its seed" );
		}

		[Test]
		public void OpaqueAssignment_DropsTheVariable()
		{
			// move v100 = 5; getRandomNr v100; if v100 == 0 ... -> the test is open again
			var scenarios = Evaluate( new[] { 200 }, NoSeeds(),
				Move( 100, 5 ),
				GetRandomNumber( 100 ),
				EqualZero( 100, jumpOverBytes: 4 ),
				SetStateLiteral( 200, 1 ),
				Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 2 ), "the random value must fork the walk" );
		}

		[Test]
		public void SeedsReachTheResult()
		{
			// an empty script keeps the exact table states
			var scenarios = Evaluate( new[] { 500, 501 }, Seed( 500, 1 ), Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].ObjectStates[500], Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].ObjectStates[501], Is.EqualTo( 0 ) );
		}

		[Test]
		public void StartScript_FollowsIntoTheLocalScript()
		{
			// entry: startScript(200); stop -- local 200: setState(210, 1); stop
			var entry = Bytes( StartScript( 200 ), Stop() );
			var local = Bytes( SetStateLiteral( 210, 1 ), Stop() );
			var data = Bytes( entry, local );

			var scenarios = RoomEntryEvaluator.EvaluateScript(
				data, 0, entry.Length, NoSeeds(), new[] { 210 },
				new Dictionary<int, (int Start, int End)> { { 200, ( entry.Length, data.Length ) } } );

			Assert.That( scenarios.Count, Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].ObjectStates[210], Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].HasSkippedScripts, Is.False );
		}

		[Test]
		public void StartScript_RecursionEndsAndSetsTheFlag()
		{
			// local 200 starts itself; the walk must end and report the skipped call
			var entry = Bytes( StartScript( 200 ), Stop() );
			var local = Bytes( SetStateLiteral( 220, 1 ), StartScript( 200 ), Stop() );
			var data = Bytes( entry, local );

			var scenarios = RoomEntryEvaluator.EvaluateScript(
				data, 0, entry.Length, NoSeeds(), new[] { 220 },
				new Dictionary<int, (int Start, int End)> { { 200, ( entry.Length, data.Length ) } } );

			Assert.That( scenarios.Count, Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].ObjectStates[220], Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].HasSkippedScripts, Is.True );
		}

		[Test]
		public void UnknownLocalScript_SetsTheFlag()
		{
			var scenarios = Evaluate( new[] { 200 }, NoSeeds(),
				StartScript( 205 ), Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].HasSkippedScripts, Is.True );
		}

		[Test]
		public void GetObjectOwner_ClosesTheTestFromTheDirectory()
		{
			// the mug pattern: show the mug only when it is still in the room (owner 15).
			// The directory gives owner 15, so the walk must not fork.
			var scenarios = Evaluate( new[] { 401 }, SeedOwner( 400, owner: 15 ),
				GetObjectOwner( 0, 400 ),
				IsEqual( 0, 15, jumpOverBytes: 4 ),
				SetStateLiteral( 401, 1 ),
				Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].Conditions, Is.Empty );
			Assert.That( scenarios[0].ObjectStates[401], Is.EqualTo( 1 ) );
		}

		[Test]
		public void GetObjectState_ReadsTheTrackedState()
		{
			// setState(300, 1); getObjectState v5 <- 300; if v5 == 1 then setState(301, 1)
			var scenarios = Evaluate( new[] { 301 }, NoSeeds(),
				SetStateLiteral( 300, 1 ),
				GetObjectState( 5, 300 ),
				IsEqual( 5, 1, jumpOverBytes: 4 ),
				SetStateLiteral( 301, 1 ),
				Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].Conditions, Is.Empty );
			Assert.That( scenarios[0].ObjectStates[301], Is.EqualTo( 1 ) );
		}

		//-------------------------------------------
		// helpers

		private static List<RoomEntryScenario> Evaluate( int[] objectIds, Dictionary<int, ClassicObjectStartState> seeds, params byte[][] instructions )
		{
			var data = instructions.SelectMany( i => i ).ToArray();
			return RoomEntryEvaluator.EvaluateScript( data, 0, data.Length, seeds, objectIds );
		}

		private static Dictionary<int, ClassicObjectStartState> NoSeeds()
		{
			return new Dictionary<int, ClassicObjectStartState>();
		}

		private static Dictionary<int, ClassicObjectStartState> Seed( int objectId, int state )
		{
			return new Dictionary<int, ClassicObjectStartState>
			{
				{ objectId, new ClassicObjectStartState( objectId, state, owner: 15, classFlags: 0 ) },
			};
		}

		private static Dictionary<int, ClassicObjectStartState> SeedOwner( int objectId, int owner )
		{
			return new Dictionary<int, ClassicObjectStartState>
			{
				{ objectId, new ClassicObjectStartState( objectId, state: 0, owner: owner, classFlags: 0 ) },
			};
		}

		private static byte[] Word( int value )
		{
			return new[] { (byte)( value & 0xFF ), (byte)( ( value >> 8 ) & 0xFF ) };
		}

		private static byte[] Bytes( params byte[][] parts )
		{
			return parts.SelectMany( p => p ).ToArray();
		}

		private static byte[] Stop()
		{
			return new byte[] { 0x00 };
		}

		/// <summary>move (0x1A): result variable, literal word value. 5 bytes.</summary>
		private static byte[] Move( int variableId, int value )
		{
			return Bytes( new byte[] { 0x1A }, Word( variableId ), Word( value ) );
		}

		/// <summary>increment (0x46): result variable. 3 bytes.</summary>
		private static byte[] Increment( int variableId )
		{
			return Bytes( new byte[] { 0x46 }, Word( variableId ) );
		}

		/// <summary>setState (0x07) with a literal object and state. 4 bytes.</summary>
		private static byte[] SetStateLiteral( int objectId, byte state )
		{
			return Bytes( new byte[] { 0x07 }, Word( objectId ), new[] { state } );
		}

		/// <summary>setState (0x87) whose object number comes from a variable. 4 bytes.</summary>
		private static byte[] SetStateVariableObject( int variableId, byte state )
		{
			return Bytes( new byte[] { 0x87 }, Word( variableId ), new[] { state } );
		}

		/// <summary>drawObject (0x05) with the no-parameter form. 4 bytes.</summary>
		private static byte[] DrawObjectPlain( int objectId )
		{
			return Bytes( new byte[] { 0x05 }, Word( objectId ), new byte[] { 0x1F } );
		}

		/// <summary>getRandomNr (0x16): result variable, literal seed byte. 4 bytes.</summary>
		private static byte[] GetRandomNumber( int variableId )
		{
			return Bytes( new byte[] { 0x16 }, Word( variableId ), new byte[] { 10 } );
		}

		/// <summary>equalZero (0x28): variable, jump offset. 5 bytes.</summary>
		private static byte[] EqualZero( int variableId, int jumpOverBytes )
		{
			return Bytes( new byte[] { 0x28 }, Word( variableId ), Word( jumpOverBytes ) );
		}

		/// <summary>isEqual (0x48): variable, literal value, jump offset. 7 bytes.</summary>
		private static byte[] IsEqual( int variableId, int value, int jumpOverBytes )
		{
			return Bytes( new byte[] { 0x48 }, Word( variableId ), Word( value ), Word( jumpOverBytes ) );
		}

		/// <summary>startScript (0x0A): literal script byte, empty argument list. 3 bytes.</summary>
		private static byte[] StartScript( byte scriptId )
		{
			return new byte[] { 0x0A, scriptId, 0xFF };
		}

		/// <summary>getObjectOwner (0x10): result variable, literal object word. 5 bytes.</summary>
		private static byte[] GetObjectOwner( int variableId, int objectId )
		{
			return Bytes( new byte[] { 0x10 }, Word( variableId ), Word( objectId ) );
		}

		/// <summary>getObjectState (0x0F): result variable, literal object word. 5 bytes.</summary>
		private static byte[] GetObjectState( int variableId, int objectId )
		{
			return Bytes( new byte[] { 0x0F }, Word( variableId ), Word( objectId ) );
		}

		/// <summary>ifNotState (0x2F): literal object, literal state, jump offset. 6 bytes.</summary>
		private static byte[] IfNotState( int objectId, byte state, int jumpOverBytes )
		{
			return Bytes( new byte[] { 0x2F }, Word( objectId ), new[] { state }, Word( jumpOverBytes ) );
		}

		/// <summary>
		/// isLess (0x44): the walk continues (exits the loop) when the variable is greater
		/// than the value, and jumps back to the given position when it is not. 7 bytes.
		/// </summary>
		private static byte[] IsLessLoop( int variableId, int value, int jumpBackTo )
		{
			// the instruction starts at 5 (move) + 4 (setState) + 3 (increment) = 12 and is
			// 7 bytes long, so the offset counts from position 19
			var offset = jumpBackTo - 19;
			return Bytes( new byte[] { 0x44 }, Word( variableId ), Word( value ), Word( offset & 0xFFFF ) );
		}
	}
}
