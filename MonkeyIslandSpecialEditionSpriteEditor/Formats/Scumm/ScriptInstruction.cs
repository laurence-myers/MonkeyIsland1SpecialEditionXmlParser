using System.Collections.Generic;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm
{
	/// <summary>
	/// What an instruction does to the flow of a script.
	/// </summary>
	internal enum ScriptFlowKind
	{
		/// <summary>Continues with the next instruction.</summary>
		Normal,

		/// <summary>Always continues at the jump target.</summary>
		Jump,

		/// <summary>
		/// Continues at the next instruction when its condition is true, or at the jump target
		/// when the condition is false. The classic engine jumps only when the test fails.
		/// </summary>
		ConditionalJump,

		/// <summary>Stops the script.</summary>
		Stop,
	}

	/// <summary>How a condition compares a variable with a value.</summary>
	internal enum ScriptComparison
	{
		Equal,
		NotEqual,
		Less,
		LessOrEqual,
		Greater,
		GreaterOrEqual,
	}

	/// <summary>What a condition tests.</summary>
	internal enum ScriptConditionKind
	{
		/// <summary>The test is not one this decoder reads (an actor test, a class test).</summary>
		Unknown,

		/// <summary>The test compares a variable with a value.</summary>
		Variable,

		/// <summary>The test compares the state of an object with a value.</summary>
		ObjectState,
	}

	/// <summary>
	/// The test of a conditional jump. The test is written for the path that continues with the
	/// next instruction. The other path, which goes to the jump target, has the opposite test.
	/// </summary>
	internal class ScriptCondition(
		ScriptConditionKind kind,
		int subject,
		ScriptComparison comparison,
		int value,
		bool valueIsLiteral,
		int valueVariableId = -1
	)
	{
		/// <summary>
		/// Gets the variable the compared value comes from when it is not a literal, or -1.
		/// An evaluator that knows that variable can still close the test.
		/// </summary>
		public int ValueVariableId
		{
			get;
		} = valueVariableId;

		/// <summary>Gets the kind of test.</summary>
		public ScriptConditionKind Kind
		{
			get;
		} = kind;

		/// <summary>
		/// Gets the variable number for a variable test, or the object number for an object
		/// state test. The value is -1 when the decoder could not read it.
		/// </summary>
		public int Subject
		{
			get;
		} = subject;

		/// <summary>Gets the comparison the test makes.</summary>
		public ScriptComparison Comparison
		{
			get;
		} = comparison;

		/// <summary>Gets the value the subject is compared with.</summary>
		public int Value
		{
			get;
		} = value;

		/// <summary>
		/// Gets whether the value is a literal. A value that comes from another variable is not
		/// known before the game runs.
		/// </summary>
		public bool ValueIsLiteral
		{
			get;
		} = valueIsLiteral;

		/// <summary>
		/// Makes the opposite test, which is the test of the path that goes to the jump target.
		/// </summary>
		public ScriptCondition Negate()
		{
			return new ScriptCondition( this.Kind, this.Subject, Opposite( this.Comparison ), this.Value, this.ValueIsLiteral, this.ValueVariableId );
		}

		private static ScriptComparison Opposite( ScriptComparison comparison )
		{
			switch( comparison )
			{
				case ScriptComparison.Equal:
					return ScriptComparison.NotEqual;
				case ScriptComparison.NotEqual:
					return ScriptComparison.Equal;
				case ScriptComparison.Less:
					return ScriptComparison.GreaterOrEqual;
				case ScriptComparison.LessOrEqual:
					return ScriptComparison.Greater;
				case ScriptComparison.Greater:
					return ScriptComparison.LessOrEqual;
				default:
					return ScriptComparison.Less;
			}
		}

		public override string ToString()
		{
			var name = this.Kind == ScriptConditionKind.ObjectState
				? "state(obj " + this.Subject + ")"
				: this.Kind == ScriptConditionKind.Variable ? VariableName( this.Subject ) : "?";
			var operand = this.ValueIsLiteral
				? this.Value.ToString()
				: this.ValueVariableId >= 0 ? VariableName( this.ValueVariableId ) : "(a variable)";
			return string.Concat( name, " ", Symbol( this.Comparison ), " ", operand );
		}

		/// <summary>
		/// Names a variable the way the engine addresses it: the 0x8000 flag marks a bit
		/// variable and the 0x4000 flag a script-local variable.
		/// </summary>
		internal static string VariableName( int variableId )
		{
			if( variableId < 0 )
			{
				return "an indexed variable";
			}
			if( ( variableId & 0x8000 ) != 0 )
			{
				return "bit " + ( variableId & 0x7FFF );
			}
			if( ( variableId & 0x4000 ) != 0 )
			{
				return "local " + ( variableId & 0x0FFF );
			}
			return "var " + variableId;
		}

		private static string Symbol( ScriptComparison comparison )
		{
			switch( comparison )
			{
				case ScriptComparison.Equal:
					return "==";
				case ScriptComparison.NotEqual:
					return "!=";
				case ScriptComparison.Less:
					return "<";
				case ScriptComparison.LessOrEqual:
					return "<=";
				case ScriptComparison.Greater:
					return ">";
				default:
					return ">=";
			}
		}
	}

	/// <summary>
	/// One decoded instruction of a script, with the position and the length the decoder measured
	/// and, for a jump, where it goes and what it tests. The events the instruction makes (an
	/// object state change, an actor placement) keep their own list in the decoder.
	/// </summary>
	internal class ScriptInstruction(
		int position,
		int length,
		byte opcode,
		ScriptFlowKind flowKind,
		int jumpTarget,
		ScriptCondition? condition,
		List<ScriptEvent> events
	)
	{
		/// <summary>Gets the position of the first byte of the instruction in the script data.</summary>
		public int Position
		{
			get;
		} = position;

		/// <summary>Gets the number of bytes the instruction uses.</summary>
		public int Length
		{
			get;
		} = length;

		/// <summary>Gets the position of the byte after the instruction.</summary>
		public int End
		{
			get
			{
				return this.Position + this.Length;
			}
		}

		/// <summary>Gets the first byte of the instruction.</summary>
		public byte Opcode
		{
			get;
		} = opcode;

		/// <summary>Gets what the instruction does to the flow of the script.</summary>
		public ScriptFlowKind FlowKind
		{
			get;
		} = flowKind;

		/// <summary>Gets the position the jump goes to, or -1 when the instruction is not a jump.</summary>
		public int JumpTarget
		{
			get;
		} = jumpTarget;

		/// <summary>
		/// Gets the test of a conditional jump, written for the path that continues with the next
		/// instruction. It is null when the instruction is not a conditional jump.
		/// </summary>
		public ScriptCondition? Condition
		{
			get;
		} = condition;

		/// <summary>Gets the events the instruction makes, in order.</summary>
		public List<ScriptEvent> Events
		{
			get;
		} = events;

		public override string ToString()
		{
			return string.Concat(
				this.Position, ": 0x", this.Opcode.ToString( "X2" ), " ", this.FlowKind,
				this.JumpTarget >= 0 ? " -> " + this.JumpTarget : "",
				this.Condition != null ? " if " + this.Condition : ""
			);
		}
	}
}
