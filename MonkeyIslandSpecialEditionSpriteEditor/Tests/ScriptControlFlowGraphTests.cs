using System.Collections.Generic;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class ScriptControlFlowGraphTests
	{
		[Test]
		public void Build_StraightCode_MakesOneBlock()
		{
			// three instructions that do not branch, then a stop
			var graph = Build( BreakHere(), BreakHere(), Stop() );

			Assert.That( graph.Blocks.Count, Is.EqualTo( 1 ) );
			Assert.That( graph.Blocks[0].Instructions.Count, Is.EqualTo( 3 ) );
			Assert.That( graph.Blocks[0].NextBlock, Is.EqualTo( -1 ) );
			Assert.That( graph.Blocks[0].JumpBlock, Is.EqualTo( -1 ) );
			Assert.That( graph.BadJumpTargetCount, Is.EqualTo( 0 ) );
		}

		[Test]
		public void Build_UnconditionalJump_GoesOnlyToTheTarget()
		{
			// jump over one instruction: jump(+1) at 0, breakHere at 3, stop at 4.
			// The instruction after the jump starts a block, and so does the target, so the
			// three instructions give three blocks and the jump skips the middle one.
			var graph = Build( Jump( 1 ), BreakHere(), Stop() );

			Assert.That( graph.Blocks.Count, Is.EqualTo( 3 ) );
			Assert.That( graph.Blocks[0].NextBlock, Is.EqualTo( -1 ), "an unconditional jump has no fall through" );
			Assert.That( graph.Blocks[0].JumpBlock, Is.EqualTo( 2 ), "the jump skips the middle block" );
			Assert.That( graph.Blocks[2].Position, Is.EqualTo( 4 ) );
			Assert.That( graph.BadJumpTargetCount, Is.EqualTo( 0 ) );
		}

		[Test]
		public void Build_ConditionalJump_HasAFallThroughAndAJump()
		{
			// if var 5 == 7 then breakHere; stop
			var graph = Build( IsEqual( variableId: 5, value: 7, jumpOverBytes: 1 ), BreakHere(), Stop() );

			Assert.That( graph.Blocks.Count, Is.EqualTo( 3 ) );
			var branch = graph.Blocks[0];
			Assert.That( branch.NextBlock, Is.EqualTo( 1 ) );
			Assert.That( branch.JumpBlock, Is.EqualTo( 2 ) );

			var condition = branch.Last!.Condition!;
			Assert.That( condition.Kind, Is.EqualTo( ScriptConditionKind.Variable ) );
			Assert.That( condition.Subject, Is.EqualTo( 5 ) );
			Assert.That( condition.Comparison, Is.EqualTo( ScriptComparison.Equal ) );
			Assert.That( condition.Value, Is.EqualTo( 7 ) );
			Assert.That( condition.ValueIsLiteral, Is.True );
		}

		[Test]
		public void Build_ComparisonOpcodes_RecordTheTestThatKeepsTheNextInstruction()
		{
			// the engine computes "value OP variable" and jumps when that is false, so the
			// recorded test puts the variable first and turns the comparison around
			Assert.That( ComparisonOf( 0x48 ), Is.EqualTo( ScriptComparison.Equal ), "isEqual" );
			Assert.That( ComparisonOf( 0x08 ), Is.EqualTo( ScriptComparison.NotEqual ), "isNotEqual" );
			Assert.That( ComparisonOf( 0x04 ), Is.EqualTo( ScriptComparison.LessOrEqual ), "isGreaterEqual" );
			Assert.That( ComparisonOf( 0x44 ), Is.EqualTo( ScriptComparison.Greater ), "isLess" );
			Assert.That( ComparisonOf( 0x38 ), Is.EqualTo( ScriptComparison.GreaterOrEqual ), "lessOrEqual" );
			Assert.That( ComparisonOf( 0x78 ), Is.EqualTo( ScriptComparison.Less ), "isGreater" );
		}

		[Test]
		public void Build_EqualZero_TestsTheVariableAgainstZero()
		{
			var graph = Build( EqualZero( variableId: 12, jumpOverBytes: 1 ), BreakHere(), Stop() );

			var condition = graph.Blocks[0].Last!.Condition!;
			Assert.That( condition.Kind, Is.EqualTo( ScriptConditionKind.Variable ) );
			Assert.That( condition.Subject, Is.EqualTo( 12 ) );
			Assert.That( condition.Comparison, Is.EqualTo( ScriptComparison.Equal ) );
			Assert.That( condition.Value, Is.EqualTo( 0 ) );
		}

		[Test]
		public void Build_IfNotState_ReadsTheObjectAndTheState()
		{
			// opcode 0x2F reads both the object and the state as literals
			var graph = Build( IfNotState( objectId: 389, state: 1, jumpOverBytes: 1 ), BreakHere(), Stop() );

			var condition = graph.Blocks[0].Last!.Condition!;
			Assert.That( condition.Kind, Is.EqualTo( ScriptConditionKind.ObjectState ) );
			Assert.That( condition.Subject, Is.EqualTo( 389 ) );
			Assert.That( condition.Comparison, Is.EqualTo( ScriptComparison.NotEqual ) );
			Assert.That( condition.Value, Is.EqualTo( 1 ) );
			Assert.That( condition.ValueIsLiteral, Is.True );
		}

		[Test]
		public void Build_JumpOutsideTheScript_IsMarkedAndCounted()
		{
			// a jump far past the end of the script
			var graph = Build( Jump( 500 ), Stop() );

			Assert.That( graph.BadJumpTargetCount, Is.EqualTo( 1 ) );
			Assert.That( graph.Blocks[0].JumpBlock, Is.EqualTo( ScriptControlFlowGraph.OutsideScript ) );
		}

		[Test]
		public void Build_BackwardJump_MakesALoopEdge()
		{
			// breakHere; jump back to the start
			var graph = Build( BreakHere(), Jump( -4 ) );

			Assert.That( graph.BadJumpTargetCount, Is.EqualTo( 0 ) );
			Assert.That( graph.Blocks[graph.Blocks.Count - 1].JumpBlock, Is.EqualTo( 0 ) );
		}

		[Test]
		public void Negate_TurnsEveryComparisonAround()
		{
			Assert.That( Negated( ScriptComparison.Equal ), Is.EqualTo( ScriptComparison.NotEqual ) );
			Assert.That( Negated( ScriptComparison.NotEqual ), Is.EqualTo( ScriptComparison.Equal ) );
			Assert.That( Negated( ScriptComparison.Less ), Is.EqualTo( ScriptComparison.GreaterOrEqual ) );
			Assert.That( Negated( ScriptComparison.LessOrEqual ), Is.EqualTo( ScriptComparison.Greater ) );
			Assert.That( Negated( ScriptComparison.Greater ), Is.EqualTo( ScriptComparison.LessOrEqual ) );
			Assert.That( Negated( ScriptComparison.GreaterOrEqual ), Is.EqualTo( ScriptComparison.Less ) );
		}

		//-------------------------------------------
		// helpers

		private static ScriptComparison Negated( ScriptComparison comparison )
		{
			return new ScriptCondition( ScriptConditionKind.Variable, 1, comparison, 0, true ).Negate().Comparison;
		}

		private static ScriptComparison ComparisonOf( byte opcode )
		{
			var graph = Build( Compare( opcode, variableId: 3, value: 4, jumpOverBytes: 1 ), BreakHere(), Stop() );
			return graph.Blocks[0].Last!.Condition!.Comparison;
		}

		private static ScriptControlFlowGraph Build( params byte[][] instructions )
		{
			var data = new List<byte>();
			foreach( var instruction in instructions )
			{
				data.AddRange( instruction );
			}
			var bytes = data.ToArray();

			var decoder = new ScriptDecoder( bytes, 0, bytes.Length );
			Assert.That( decoder.DecodeScript(), Is.True, "the test bytes must decode cleanly" );
			return ScriptControlFlowGraph.Build( decoder.Instructions );
		}

		private static byte[] BreakHere()
		{
			return new byte[] { 0x80 };
		}

		private static byte[] Stop()
		{
			return new byte[] { 0x00 };
		}

		private static byte[] Jump( int offset )
		{
			return new byte[] { 0x18, (byte)( offset & 0xFF ), (byte)( ( offset >> 8 ) & 0xFF ) };
		}

		/// <summary>isEqual: opcode, variable word, value word, jump offset word.</summary>
		private static byte[] IsEqual( int variableId, int value, int jumpOverBytes )
		{
			return Compare( 0x48, variableId, value, jumpOverBytes );
		}

		private static byte[] Compare( byte opcode, int variableId, int value, int jumpOverBytes )
		{
			return new byte[]
			{
				opcode,
				(byte)( variableId & 0xFF ), (byte)( ( variableId >> 8 ) & 0xFF ),
				(byte)( value & 0xFF ), (byte)( ( value >> 8 ) & 0xFF ),
				(byte)( jumpOverBytes & 0xFF ), (byte)( ( jumpOverBytes >> 8 ) & 0xFF ),
			};
		}

		/// <summary>equalZero: opcode, variable word, jump offset word.</summary>
		private static byte[] EqualZero( int variableId, int jumpOverBytes )
		{
			return new byte[]
			{
				0x28,
				(byte)( variableId & 0xFF ), (byte)( ( variableId >> 8 ) & 0xFF ),
				(byte)( jumpOverBytes & 0xFF ), (byte)( ( jumpOverBytes >> 8 ) & 0xFF ),
			};
		}

		/// <summary>ifNotState (0x2F): opcode, object word, state byte, jump offset word.</summary>
		private static byte[] IfNotState( int objectId, byte state, int jumpOverBytes )
		{
			return new byte[]
			{
				0x2F,
				(byte)( objectId & 0xFF ), (byte)( ( objectId >> 8 ) & 0xFF ),
				state,
				(byte)( jumpOverBytes & 0xFF ), (byte)( ( jumpOverBytes >> 8 ) & 0xFF ),
			};
		}
	}
}
