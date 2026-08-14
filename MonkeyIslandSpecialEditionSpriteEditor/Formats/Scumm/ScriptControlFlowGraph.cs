using System.Collections.Generic;
using System.Linq;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm
{
	/// <summary>
	/// A straight run of instructions that the script always does together. The run ends at a
	/// jump, at a stop, or where another instruction jumps into the code.
	/// </summary>
	internal class ScriptBlock(
		int index,
		int position,
		List<ScriptInstruction> instructions
	)
	{
		/// <summary>Gets the number of the block in the graph.</summary>
		public int Index
		{
			get;
		} = index;

		/// <summary>Gets the position of the first instruction of the block.</summary>
		public int Position
		{
			get;
		} = position;

		/// <summary>Gets the instructions of the block, in order.</summary>
		public List<ScriptInstruction> Instructions
		{
			get;
		} = instructions;

		/// <summary>Gets the last instruction of the block, which decides where the script goes next.</summary>
		public ScriptInstruction? Last
		{
			get
			{
				return this.Instructions.Count == 0 ? null : this.Instructions[this.Instructions.Count - 1];
			}
		}

		/// <summary>
		/// Gets or sets the block the script goes to when it does not jump, or -1 when the block
		/// stops or always jumps.
		/// </summary>
		public int NextBlock
		{
			get;
			set;
		} = -1;

		/// <summary>
		/// Gets or sets the block the script goes to when it jumps, or -1 when the block has no
		/// jump. A jump that leaves the script has the value -2.
		/// </summary>
		public int JumpBlock
		{
			get;
			set;
		} = -1;

		public override string ToString()
		{
			return string.Concat( "block ", this.Index, " at ", this.Position, " (", this.Instructions.Count,
				" instructions, next=", this.NextBlock, " jump=", this.JumpBlock, ")" );
		}
	}

	/// <summary>
	/// The control flow graph of one script: the instructions cut into blocks, with an edge for
	/// each way the script can go. The linear scan that this replaces could not see the branches,
	/// so it had to read both sides of every test as if they always happened.
	/// </summary>
	internal class ScriptControlFlowGraph
	{
		/// <summary>A jump that goes outside the script.</summary>
		public const int OutsideScript = -2;

		private ScriptControlFlowGraph( List<ScriptBlock> blocks )
		{
			this.Blocks = blocks;
		}

		/// <summary>Gets the blocks, in position order. Block 0 is where the script starts.</summary>
		public List<ScriptBlock> Blocks
		{
			get;
		}

		/// <summary>
		/// Gets or sets the number of jump targets that did not land on the first byte of an
		/// instruction. A correct decode of a correct script gives 0.
		/// </summary>
		public int BadJumpTargetCount
		{
			get;
			private set;
		}

		/// <summary>
		/// Builds the graph from the instructions of one script.
		/// </summary>
		public static ScriptControlFlowGraph Build( List<ScriptInstruction> instructions )
		{
			if( instructions.Count == 0 )
			{
				return new ScriptControlFlowGraph( new List<ScriptBlock>() );
			}

			var instructionByPosition = new Dictionary<int, ScriptInstruction>();
			foreach( var instruction in instructions )
			{
				instructionByPosition[instruction.Position] = instruction;
			}

			// a block starts at the first instruction, at every jump target, and after every
			// instruction that jumps or stops
			var badJumpTargets = 0;
			var leaders = new HashSet<int> { instructions[0].Position };
			foreach( var instruction in instructions )
			{
				if( instruction.FlowKind == ScriptFlowKind.Jump || instruction.FlowKind == ScriptFlowKind.ConditionalJump )
				{
					if( instructionByPosition.ContainsKey( instruction.JumpTarget ) )
					{
						leaders.Add( instruction.JumpTarget );
					}
					else
					{
						badJumpTargets++;
					}
					leaders.Add( instruction.End );
				}
				else if( instruction.FlowKind == ScriptFlowKind.Stop )
				{
					leaders.Add( instruction.End );
				}
			}

			// cut the instruction list at the leaders
			var blocks = new List<ScriptBlock>();
			var current = new List<ScriptInstruction>();
			foreach( var instruction in instructions )
			{
				if( current.Count > 0 && leaders.Contains( instruction.Position ) )
				{
					blocks.Add( new ScriptBlock( blocks.Count, current[0].Position, current ) );
					current = new List<ScriptInstruction>();
				}
				current.Add( instruction );
			}
			if( current.Count > 0 )
			{
				blocks.Add( new ScriptBlock( blocks.Count, current[0].Position, current ) );
			}

			var blockByPosition = blocks.ToDictionary( b => b.Position, b => b.Index );

			// join the blocks
			for( var index = 0; index < blocks.Count; index++ )
			{
				var block = blocks[index];
				var last = block.Last!;
				switch( last.FlowKind )
				{
					case ScriptFlowKind.Stop:
						break;
					case ScriptFlowKind.Jump:
						block.JumpBlock = TargetBlock( blockByPosition, last.JumpTarget );
						break;
					case ScriptFlowKind.ConditionalJump:
						block.JumpBlock = TargetBlock( blockByPosition, last.JumpTarget );
						block.NextBlock = index + 1 < blocks.Count ? index + 1 : -1;
						break;
					default:
						block.NextBlock = index + 1 < blocks.Count ? index + 1 : -1;
						break;
				}
			}

			var graph = new ScriptControlFlowGraph( blocks );
			graph.BadJumpTargetCount = badJumpTargets;
			return graph;
		}

		private static int TargetBlock( Dictionary<int, int> blockByPosition, int position )
		{
			int index;
			return blockByPosition.TryGetValue( position, out index ) ? index : OutsideScript;
		}
	}
}
