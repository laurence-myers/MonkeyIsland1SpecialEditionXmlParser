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
		public void EngineVariableTest_ForksIntoTwoScenarios()
		{
			// variable 6 is VAR_MACHINE_SPEED, which the engine writes itself, so its value is
			// not known and the walk must offer both answers
			var scenarios = Evaluate( new[] { 200 }, NoSeeds(),
				EqualZero( 6, jumpOverBytes: 4 ),
				SetStateLiteral( 200, 1 ),
				Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 2 ) );
			var shown = scenarios.Single( s => s.ObjectStates[200] == 1 );
			var hidden = scenarios.Single( s => s.ObjectStates[200] == 0 );
			Assert.That( shown.Conditions, Is.EqualTo( new[] { "VAR_MACHINE_SPEED == 0" } ) );
			Assert.That( hidden.Conditions, Is.EqualTo( new[] { "VAR_MACHINE_SPEED != 0" } ) );
		}

		[Test]
		public void UnwrittenGameVariable_ReadsAsZeroAndDoesNotFork()
		{
			// variable 196 is a plot variable no script wrote. A new game clears the whole
			// variable table, so it holds 0 and the test has one answer.
			var scenarios = Evaluate( new[] { 200 }, NoSeeds(),
				EqualZero( 196, jumpOverBytes: 4 ),
				SetStateLiteral( 200, 1 ),
				Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].Conditions, Is.Empty );
			Assert.That( scenarios[0].ObjectStates[200], Is.EqualTo( 1 ) );
		}

		[Test]
		public void UnwrittenBitVariable_ReadsAsZero()
		{
			// a bit variable belongs to the game, so it is cleared too
			var scenarios = Evaluate( new[] { 200 }, NoSeeds(),
				EqualZero( 0x8000 | 453, jumpOverBytes: 4 ),
				SetStateLiteral( 200, 1 ),
				Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].ObjectStates[200], Is.EqualTo( 1 ) );
		}

		[Test]
		public void MultiplyAndDivide_AreComputed()
		{
			// local 0 = 100; local 0 = local 0 * 3; local 0 = local 0 / 2  -> 150
			var scenarios = Evaluate( new[] { 150 }, NoSeeds(),
				Move( 0x4000, 100 ),
				Arithmetic( 0x1B, 0x4000, 3 ),
				Arithmetic( 0x5B, 0x4000, 2 ),
				SetStateVariableObject( 0x4000, 1 ),
				Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].ObjectStates[150], Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].HasUnknownWrites, Is.False );
		}

		[Test]
		public void RandomNumber_KeepsItsRangeAndAnswersATestOutsideIt()
		{
			// getRandomNr(v, 5) then "if v < 10": the exact value is unknown but the range is
			// 0 to 5, so the test has one answer
			var scenarios = Evaluate( new[] { 200 }, NoSeeds(),
				GetRandomNumber( 0x4000, maximum: 5 ),
				IsLessThan( 0x4000, 10, jumpOverBytes: 4 ),
				SetStateLiteral( 200, 1 ),
				Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].ObjectStates[200], Is.EqualTo( 1 ) );
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

		[Test]
		public void Expression_ComputesTheObjectNumber()
		{
			// the bar crowd pattern from room 28, local script 204:
			//   local 0 = 2; local 1 = 330 + local 0 * 3; setState(local 1, 1)
			// 330 + 2 * 3 = 336, which is one of the pirates
			var scenarios = Evaluate( new[] { 333, 336 }, NoSeeds(),
				Move( 0x4000, 2 ),
				CrowdExpression(),
				SetStateVariableObject( 0x4001, 1 ),
				Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].ObjectStates[336], Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].ObjectStates[333], Is.EqualTo( 0 ) );
			Assert.That( scenarios[0].HasUnknownWrites, Is.False );
		}

		[Test]
		public void Expression_InALoop_ResolvesTheWholeCrowd()
		{
			// the whole loop: local 0 counts 0..3 and the expression names each object
			var scenarios = Evaluate( new[] { 330, 333, 336, 339, 342 }, NoSeeds(),
				Move( 0x4000, 0 ),
				CrowdExpression(),
				SetStateVariableObject( 0x4001, 1 ),
				Increment( 0x4000 ),
				CrowdLoopTest( jumpBackTo: 5 ),
				Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].ObjectStates[330], Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].ObjectStates[333], Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].ObjectStates[336], Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].ObjectStates[339], Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].ObjectStates[342], Is.EqualTo( 0 ), "the loop stops before this object" );
		}

		[Test]
		public void Expression_WithANestedInstruction_MakesTheTargetUnknown()
		{
			// sub-opcode 6 runs another instruction whose result the walk does not follow
			var scenarios = Evaluate( new[] { 200 }, NoSeeds(),
				Move( 0x4001, 200 ),
				NestedExpression(),
				SetStateVariableObject( 0x4001, 1 ),
				Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].HasUnknownWrites, Is.True );
			Assert.That( scenarios[0].ObjectStates[200], Is.EqualTo( 0 ) );
		}

		//-------------------------------------------
		// fork policy, pinning and state discovery

		[Test]
		public void PlotOnly_DoesNotForkOnAnEngineVariable()
		{
			// variable 6 is VAR_MACHINE_SPEED, which the engine owns. Under the plot-only policy an
			// open test on it does not fork; the walk takes its fall-through edge and draws the object
			var scenarios = EvaluatePolicy( new[] { 200 }, NoSeeds(), ForkPolicy.PlotOnly,
				EqualZero( 6, jumpOverBytes: 4 ),
				SetStateLiteral( 200, 1 ),
				Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 1 ) );
			Assert.That( scenarios[0].ObjectStates[200], Is.EqualTo( 1 ) );
		}

		[Test]
		public void PlotOnly_ForksOnAnUnknownPlotVariable()
		{
			// variable 196 is a plain game global (a plot atom). Made unknown by a random write, an
			// open test on it is a real story branch, so the plot-only walk forks
			var scenarios = EvaluatePolicy( new[] { 200 }, NoSeeds(), ForkPolicy.PlotOnly,
				Move( 196, 5 ),
				GetRandomNumber( 196 ),
				EqualZero( 196, jumpOverBytes: 4 ),
				SetStateLiteral( 200, 1 ),
				Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 2 ) );
		}

		[Test]
		public void None_NeverForks()
		{
			// the same unknown plot variable, under the None policy, produces exactly one path
			var scenarios = EvaluatePolicy( new[] { 200 }, NoSeeds(), ForkPolicy.None,
				Move( 196, 5 ),
				GetRandomNumber( 196 ),
				EqualZero( 196, jumpOverBytes: 4 ),
				SetStateLiteral( 200, 1 ),
				Stop() );

			Assert.That( scenarios.Count, Is.EqualTo( 1 ) );
		}

		[Test]
		public void PinnedVariable_IsReadAtItsPinnedValue()
		{
			// if var196 == 0 then draw 200. Pinned to 1 the test fails and the object stays hidden;
			// pinned to 0 it is drawn
			var data = Bytes( EqualZero( 196, jumpOverBytes: 4 ), SetStateLiteral( 200, 1 ), Stop() );

			var hidden = RoomEntryEvaluator.EvaluateUnderAssignmentScript(
				data, 0, data.Length, NoSeeds(), new[] { 200 }, Pin( 196, 1 ) );
			var shown = RoomEntryEvaluator.EvaluateUnderAssignmentScript(
				data, 0, data.Length, NoSeeds(), new[] { 200 }, Pin( 196, 0 ) );

			Assert.That( hidden[200], Is.EqualTo( 0 ) );
			Assert.That( shown[200], Is.EqualTo( 1 ) );
		}

		[Test]
		public void PinnedVariable_IgnoresWrites()
		{
			// move var196 = 5; if var196 == 0 draw 200. With var196 pinned to 0 the write is ignored,
			// so the pin holds and the object is drawn
			var data = Bytes( Move( 196, 5 ), EqualZero( 196, jumpOverBytes: 4 ), SetStateLiteral( 200, 1 ), Stop() );

			var states = RoomEntryEvaluator.EvaluateUnderAssignmentScript(
				data, 0, data.Length, NoSeeds(), new[] { 200 }, Pin( 196, 0 ) );

			Assert.That( states[200], Is.EqualTo( 1 ) );
		}

		[Test]
		public void DiscoverStates_FindsARadioForAGatingGlobal()
		{
			// if var196 == 0 then draw 200; the atom gates one object, so discovery offers a radio
			var data = Bytes( EqualZero( 196, jumpOverBytes: 4 ), SetStateLiteral( 200, 1 ), Stop() );
			var names = new Dictionary<int, string?> { { 200, "widget" } };

			var model = RoomEntryEvaluator.DiscoverStatesScript( data, 0, data.Length, NoSeeds(), new[] { 200 }, names );

			Assert.That( model.Controls.Count, Is.EqualTo( 1 ) );
			var control = model.Controls[0];
			Assert.That( control.IsCheckbox, Is.False );
			Assert.That( control.VariableId, Is.EqualTo( 196 ) );
			Assert.That( control.Options.Count, Is.EqualTo( 2 ) );

			// the class that draws the object is the game-start default (var196 is 0)
			var defaultOption = control.Options[control.DefaultOptionIndex];
			Assert.That( defaultOption.Label, Does.Contain( "Widget" ) );
			Assert.That( defaultOption.ObjectsHidden, Is.Empty );

			var otherOption = control.Options[control.DefaultOptionIndex == 0 ? 1 : 0];
			Assert.That( otherOption.ObjectsHidden, Does.Contain( 200 ) );
		}

		[Test]
		public void DiscoverStates_FindsACheckboxForAGatingBit()
		{
			// if bit453 == 0 then draw 200; a bit flag is offered as a checkbox
			var data = Bytes( EqualZero( 0x8000 | 453, jumpOverBytes: 4 ), SetStateLiteral( 200, 1 ), Stop() );

			var model = RoomEntryEvaluator.DiscoverStatesScript( data, 0, data.Length, NoSeeds(), new[] { 200 } );

			Assert.That( model.Controls.Count, Is.EqualTo( 1 ) );
			Assert.That( model.Controls[0].IsCheckbox, Is.True );
			Assert.That( model.Controls[0].VariableId, Is.EqualTo( 0x8000 | 453 ) );
			Assert.That( model.Controls[0].Options.Count, Is.EqualTo( 2 ) );
		}

		[Test]
		public void PinnedVariable_ResolvesAThresholdTest()
		{
			// if var196 < 5 then draw 200 (isGreater keeps the next instruction when var < value).
			// Pinned inside the range it is drawn; pinned outside it is hidden.
			var data = Bytes( IsLessThan( 196, 5, jumpOverBytes: 4 ), SetStateLiteral( 200, 1 ), Stop() );

			var inside = RoomEntryEvaluator.EvaluateUnderAssignmentScript(
				data, 0, data.Length, NoSeeds(), new[] { 200 }, Pin( 196, 3 ) );
			var outside = RoomEntryEvaluator.EvaluateUnderAssignmentScript(
				data, 0, data.Length, NoSeeds(), new[] { 200 }, Pin( 196, 7 ) );

			Assert.That( inside[200], Is.EqualTo( 1 ) );
			Assert.That( outside[200], Is.EqualTo( 0 ) );
		}

		[Test]
		public void ObjectClassTest_ResolvesFromTheDirectory()
		{
			// ifClassOfIs(400, has class 6) then draw 200. The directory decides the test, so the
			// walk does not fork: an object with the class draws 200, one without it does not.
			var withClass = Evaluate( new[] { 200 }, SeedClass( 400, classFlags: 1u << 5 ),
				IfClassOfIs( 400, 0x80 | 6, jumpOverBytes: 4 ),
				SetStateLiteral( 200, 1 ),
				Stop() );
			var withoutClass = Evaluate( new[] { 200 }, SeedClass( 400, classFlags: 0 ),
				IfClassOfIs( 400, 0x80 | 6, jumpOverBytes: 4 ),
				SetStateLiteral( 200, 1 ),
				Stop() );

			Assert.That( withClass.Count, Is.EqualTo( 1 ) );
			Assert.That( withClass[0].ObjectStates[200], Is.EqualTo( 1 ) );
			Assert.That( withoutClass.Count, Is.EqualTo( 1 ) );
			Assert.That( withoutClass[0].ObjectStates[200], Is.EqualTo( 0 ) );
		}

		[Test]
		public void DiscoverStates_FindsAScriptedStateForALocalDraw()
		{
			// the entry draws nothing; local 200 draws object 201 (a verb-driven appearance)
			var entry = Bytes( Stop() );
			var local = Bytes( SetStateLiteral( 201, 1 ), Stop() );
			var data = Bytes( entry, local );
			var names = new Dictionary<int, string?> { { 201, "lever" } };

			var model = RoomEntryEvaluator.DiscoverStatesScript(
				data, 0, entry.Length, NoSeeds(), new[] { 200, 201 }, names,
				new Dictionary<int, (int Start, int End)> { { 200, ( entry.Length, data.Length ) } } );

			Assert.That( model.Controls, Is.Empty );
			Assert.That( model.ScriptedStates.Count, Is.EqualTo( 1 ) );
			Assert.That( model.ScriptedStates[0].ObjectsShown, Is.EqualTo( new[] { 201 } ) );
			Assert.That( model.ScriptedStates[0].Label, Does.Contain( "Lever" ) );
		}

		[Test]
		public void DiscoverStates_ScriptedStateFollowsTheCascadeAndDropsTheHelper()
		{
			// local 200 draws 210 and starts local 201, which draws 211. The state is the whole
			// cascade {210,211}; the helper 201's {211} is dropped because 200's cascade started it.
			var entry = Bytes( Stop() );
			var local200 = Bytes( StartScript( 201 ), SetStateLiteral( 210, 1 ), Stop() );
			var local201 = Bytes( SetStateLiteral( 211, 1 ), Stop() );
			var data = Bytes( entry, local200, local201 );

			var model = RoomEntryEvaluator.DiscoverStatesScript(
				data, 0, entry.Length, NoSeeds(), new[] { 210, 211 }, null,
				new Dictionary<int, (int Start, int End)>
				{
					{ 200, ( entry.Length, entry.Length + local200.Length ) },
					{ 201, ( entry.Length + local200.Length, data.Length ) },
				} );

			Assert.That( model.ScriptedStates.Count, Is.EqualTo( 1 ) );
			Assert.That( model.ScriptedStates[0].ObjectsShown, Is.EqualTo( new[] { 210, 211 } ) );
		}

		[Test]
		public void DiscoverStates_KeepsADistinctSmallerStateFromAnUnrelatedLocal()
		{
			// local 200 draws {210,211}; unrelated local 201 draws {210}. Even though {210} nests in
			// {210,211}, 200 never started 201, so both are distinct verb outcomes and both are kept.
			var entry = Bytes( Stop() );
			var local200 = Bytes( SetStateLiteral( 210, 1 ), SetStateLiteral( 211, 1 ), Stop() );
			var local201 = Bytes( SetStateLiteral( 210, 1 ), Stop() );
			var data = Bytes( entry, local200, local201 );

			var model = RoomEntryEvaluator.DiscoverStatesScript(
				data, 0, entry.Length, NoSeeds(), new[] { 210, 211 }, null,
				new Dictionary<int, (int Start, int End)>
				{
					{ 200, ( entry.Length, entry.Length + local200.Length ) },
					{ 201, ( entry.Length + local200.Length, data.Length ) },
				} );

			Assert.That( model.ScriptedStates.Count, Is.EqualTo( 2 ) );
		}

		[Test]
		public void DiscoverStates_ScriptedStateHidesAnObjectTheEntryOnlyDrawsInTheOtherState()
		{
			// room 12's nose pattern: the entry draws 144 only while object 142 is not open (state
			// 1); local 200 opens the mouth (140-143). The state must re-run the entry so the nose
			// drops - shows {140-143}, hides {144}.
			var entry = Bytes( IfNotState( 142, 1, jumpOverBytes: 4 ), SetStateLiteral( 144, 1 ), Stop() );
			var local = Bytes(
				SetStateLiteral( 140, 1 ), SetStateLiteral( 141, 1 ),
				SetStateLiteral( 142, 1 ), SetStateLiteral( 143, 1 ), Stop() );
			var data = Bytes( entry, local );

			var model = RoomEntryEvaluator.DiscoverStatesScript(
				data, 0, entry.Length, NoSeeds(), new[] { 140, 141, 142, 143, 144 }, null,
				new Dictionary<int, (int Start, int End)> { { 200, ( entry.Length, data.Length ) } } );

			Assert.That( model.ScriptedStates.Count, Is.EqualTo( 1 ) );
			Assert.That( model.ScriptedStates[0].ObjectsShown, Is.EqualTo( new[] { 140, 141, 142, 143 } ) );
			Assert.That( model.ScriptedStates[0].ObjectsHidden, Is.EqualTo( new[] { 144 } ) );
		}

		[Test]
		public void DiscoverStates_MarksAFrameYieldingDrawLoopAsAnimation()
		{
			// a local that draws two objects with a breakHere between them is an animation
			var entry = Bytes( Stop() );
			var local = Bytes( SetStateLiteral( 210, 1 ), BreakHere(), SetStateLiteral( 211, 1 ), Stop() );
			var data = Bytes( entry, local );

			var model = RoomEntryEvaluator.DiscoverStatesScript(
				data, 0, entry.Length, NoSeeds(), new[] { 210, 211 }, null,
				new Dictionary<int, (int Start, int End)> { { 200, ( entry.Length, data.Length ) } } );

			Assert.That( model.ScriptedStates.Count, Is.EqualTo( 1 ) );
			Assert.That( model.ScriptedStates[0].IsAnimation, Is.True );
			Assert.That( model.ScriptedStates[0].LocalScriptId, Is.EqualTo( 200 ) );
		}

		[Test]
		public void DiscoverStates_ScriptedStateCanHideABaselineObject()
		{
			// the entry draws 210; local 200 hides it. The state's effect is a hide.
			var entry = Bytes( SetStateLiteral( 210, 1 ), Stop() );
			var local200 = Bytes( SetStateLiteral( 210, 0 ), Stop() );
			var data = Bytes( entry, local200 );

			var model = RoomEntryEvaluator.DiscoverStatesScript(
				data, 0, entry.Length, NoSeeds(), new[] { 210 }, null,
				new Dictionary<int, (int Start, int End)> { { 200, ( entry.Length, data.Length ) } } );

			Assert.That( model.ScriptedStates.Count, Is.EqualTo( 1 ) );
			Assert.That( model.ScriptedStates[0].ObjectsShown, Is.Empty );
			Assert.That( model.ScriptedStates[0].ObjectsHidden, Is.EqualTo( new[] { 210 } ) );
			Assert.That( model.ScriptedStates[0].Label, Does.Contain( "Hide" ) );
		}

		[Test]
		public void DiscoverStates_NoScriptedStateWhenALocalRedrawsTheBaseline()
		{
			// the entry draws 201; local 200 draws the same object, so there is no new appearance
			var entry = Bytes( SetStateLiteral( 201, 1 ), Stop() );
			var local = Bytes( SetStateLiteral( 201, 1 ), Stop() );
			var data = Bytes( entry, local );

			var model = RoomEntryEvaluator.DiscoverStatesScript(
				data, 0, entry.Length, NoSeeds(), new[] { 201 }, null,
				new Dictionary<int, (int Start, int End)> { { 200, ( entry.Length, data.Length ) } } );

			Assert.That( model.ScriptedStates, Is.Empty );
		}

		[Test]
		public void DiscoverStates_DropsAVariableThatChangesNothing()
		{
			// var197 is tested but the guarded code draws nothing, so it is not a story state
			var data = Bytes( EqualZero( 197, jumpOverBytes: 5 ), Move( 0x4000, 5 ), Stop() );

			var model = RoomEntryEvaluator.DiscoverStatesScript( data, 0, data.Length, NoSeeds(), new[] { 200 } );

			Assert.That( model.Controls, Is.Empty );
		}

		//-------------------------------------------
		// helpers

		private static List<RoomEntryScenario> Evaluate( int[] objectIds, Dictionary<int, ClassicObjectStartState> seeds, params byte[][] instructions )
		{
			var data = instructions.SelectMany( i => i ).ToArray();
			return RoomEntryEvaluator.EvaluateScript( data, 0, data.Length, seeds, objectIds );
		}

		private static List<RoomEntryScenario> EvaluatePolicy( int[] objectIds, Dictionary<int, ClassicObjectStartState> seeds, ForkPolicy policy, params byte[][] instructions )
		{
			var data = instructions.SelectMany( i => i ).ToArray();
			return RoomEntryEvaluator.EvaluateScript( data, 0, data.Length, seeds, objectIds, policy: policy );
		}

		private static Dictionary<int, int> Pin( int variableId, int value )
		{
			return new Dictionary<int, int> { { variableId, value } };
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

		private static Dictionary<int, ClassicObjectStartState> SeedClass( int objectId, uint classFlags )
		{
			return new Dictionary<int, ClassicObjectStartState>
			{
				{ objectId, new ClassicObjectStartState( objectId, state: 0, owner: 15, classFlags: classFlags ) },
			};
		}

		/// <summary>
		/// ifClassOfIs (0x1D): literal object word, a 0xFF-terminated list of literal class words
		/// (each prefixed by its own parameter byte), then a jump offset. A class value with the
		/// 0x80 flag asks for a class the object must have. 9 bytes for one class.
		/// </summary>
		private static byte[] IfClassOfIs( int objectId, int classValue, int jumpOverBytes )
		{
			return Bytes(
				new byte[] { 0x1D }, Word( objectId ),
				new byte[] { 0x01 }, Word( classValue ), new byte[] { 0xFF },
				Word( jumpOverBytes ) );
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

		/// <summary>getRandomNr (0x16): result variable, literal maximum byte. 4 bytes.</summary>
		private static byte[] GetRandomNumber( int variableId, byte maximum = 10 )
		{
			return Bytes( new byte[] { 0x16 }, Word( variableId ), new[] { maximum } );
		}

		/// <summary>and (0x17), multiply (0x1B), or (0x57), divide (0x5B): result variable, literal word.</summary>
		private static byte[] Arithmetic( byte opcode, int variableId, int value )
		{
			return Bytes( new[] { opcode }, Word( variableId ), Word( value ) );
		}

		/// <summary>
		/// isGreater (0x78): the walk keeps the next instruction when the variable is less than
		/// the value. 7 bytes.
		/// </summary>
		private static byte[] IsLessThan( int variableId, int value, int jumpOverBytes )
		{
			return Bytes( new byte[] { 0x78 }, Word( variableId ), Word( value ), Word( jumpOverBytes ) );
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

		/// <summary>
		/// The expression from room 28: local 1 = 330 + local 0 * 3. These are the exact retail
		/// bytes. 15 bytes.
		/// </summary>
		private static byte[] CrowdExpression()
		{
			return new byte[]
			{
				0xAC, 0x01, 0x40,       // expression, result = local 1
				0x01, 0x4A, 0x01,       // push literal 330
				0x81, 0x00, 0x40,       // push local 0
				0x01, 0x03, 0x00,       // push literal 3
				0x04,                   // multiply
				0x02,                   // add
				0xFF,
			};
		}

		/// <summary>An expression whose sub-opcode 6 runs a nested instruction. 8 bytes.</summary>
		private static byte[] NestedExpression()
		{
			return new byte[]
			{
				0xAC, 0x01, 0x40,       // expression, result = local 1
				0x01, 0x0A, 0x00,       // push literal 10
				0x06, 0x80,             // a nested instruction (breakHere)
				0xFF,
			};
		}

		/// <summary>
		/// isLess (0x44) on local 0 against 3: the walk leaves the loop when local 0 is greater
		/// than 3, and jumps back otherwise. 7 bytes, placed after a 15 byte expression, a
		/// 4 byte setState and a 3 byte increment.
		/// </summary>
		private static byte[] CrowdLoopTest( int jumpBackTo )
		{
			var offset = jumpBackTo - ( 5 + 15 + 4 + 3 + 7 );
			return Bytes( new byte[] { 0x44 }, Word( 0x4000 ), Word( 3 ), Word( offset & 0xFFFF ) );
		}

		/// <summary>startScript (0x0A): literal script byte, empty argument list. 3 bytes.</summary>
		private static byte[] StartScript( byte scriptId )
		{
			return new byte[] { 0x0A, scriptId, 0xFF };
		}

		/// <summary>breakHere (0x80): yields a frame. 1 byte.</summary>
		private static byte[] BreakHere()
		{
			return new byte[] { 0x80 };
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
