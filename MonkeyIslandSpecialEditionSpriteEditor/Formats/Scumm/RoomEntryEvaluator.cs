using System.Collections.Generic;
using System.Linq;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm
{
	/// <summary>
	/// One possible appearance of a room when the player first enters it: the object states one
	/// path through the entry script produces, with the tests that path assumed on the way.
	/// </summary>
	public class RoomEntryScenario
	{
		/// <summary>
		/// Gets the tests this scenario assumed, in the order the script made them. An empty
		/// list means the entry script reached this result without an open test.
		/// </summary>
		public List<string> Conditions
		{
			get;
		} = new List<string>();

		/// <summary>
		/// Gets the state of each requested object: 0 hides the object, a different value draws
		/// the image of that state, and null means the script wrote a value the evaluator could
		/// not compute.
		/// </summary>
		public Dictionary<int, int?> ObjectStates
		{
			get;
		} = new Dictionary<int, int?>();

		/// <summary>
		/// Gets or sets the number of script paths that gave this same result.
		/// </summary>
		public int PathCount
		{
			get;
			set;
		} = 1;

		/// <summary>
		/// Gets or sets whether the walk stopped early (too many steps or too many forks).
		/// The states are then a lower bound, not a full answer.
		/// </summary>
		public bool Truncated
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets whether the path wrote a state to an object number the evaluator could
		/// not compute. Some object may then hold a wrong state.
		/// </summary>
		public bool HasUnknownWrites
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets whether the path started a local script the walk could not follow (an
		/// unknown script number, a recursion, or a too-deep call chain). The states that script
		/// sets are then missing.
		/// </summary>
		public bool HasSkippedScripts
		{
			get;
			set;
		}

		public override string ToString()
		{
			return string.Concat(
				this.Conditions.Count == 0 ? "(no conditions)" : string.Join( " and ", this.Conditions ),
				" -> ", this.ObjectStates.Count, " object states"
			);
		}
	}

	/// <summary>
	/// Computes the object states a room shows when the player enters it at the start of a new
	/// game. The object directory (DOBJ) gives the exact start values of every object's state
	/// and owner; the room's entry script then changes the states at run time, often with loops
	/// over object ranges, and often from local scripts it starts. The evaluator walks the entry
	/// script over its control flow graph, follows startScript into the room's own local
	/// scripts, keeps the values of the variables the scripts assign (so the loops resolve to
	/// real object numbers), answers getObjectState/getObjectOwner reads from the tracked
	/// tables, and forks the walk at a test on a value it does not know - each fork becomes a
	/// scenario with that test as its label.
	///
	/// The walk is honest about its limits: a global script it cannot reach is skipped, a local
	/// script it cannot follow sets a flag, a write it cannot compute sets a flag, and a walk
	/// that grows past its budget is marked truncated.
	/// </summary>
	public static class RoomEntryEvaluator
	{
		/// <summary>The most script paths a walk may fork into.</summary>
		public const int MaxPaths = 128;

		/// <summary>The most instructions one path may execute (a guard against endless loops).</summary>
		public const int MaxStepsPerPath = 20000;

		/// <summary>The most stacked script calls one path may hold.</summary>
		public const int MaxCallDepth = 8;

		/// <summary>
		/// The most times one path may take the same conditional test. The largest honest loops
		/// walk an object range (about 50 objects); a loop past this cap waits for another
		/// script and can never end here, so the walk leaves it through the other edge.
		/// </summary>
		public const int MaxVisitsPerTest = 64;

		/// <summary>The frame key of the entry script (local scripts use their own numbers).</summary>
		private const int EntryScriptKey = -1;

		/// <summary>The frame key of the boot script pre-run.</summary>
		private const int BootScriptKey = -2;

		/// <summary>The marker for "a value the evaluator could not compute".</summary>
		private const int Unknown = int.MinValue;

		/// <summary>
		/// Evaluates the entry script of a room from the still XOR encoded resource and index
		/// file contents. The buffers are decoded in place. The index file supplies the script
		/// directory, so the walk can run the boot script first and follow global scripts; the
		/// walk works without it, but knows less.
		/// </summary>
		public static List<RoomEntryScenario> EvaluateFromEncodedBytes(
			byte[] bytes,
			int roomNumber,
			IReadOnlyDictionary<int, ClassicObjectStartState> startStates,
			IEnumerable<int> objectIds,
			byte[]? encodedIndexBytes = null )
		{
			Parser.XorDecode( bytes, Parser.XorKey );
			if( encodedIndexBytes != null )
			{
				Parser.XorDecode( encodedIndexBytes, Parser.XorKey );
			}
			return Evaluate( bytes, roomNumber, startStates, objectIds, encodedIndexBytes );
		}

		/// <summary>
		/// Evaluates the entry script of a room from an already decoded resource file: the boot
		/// script (global script 1) runs first for the variable and state setup a new game does,
		/// then the entry script, following startScript calls into the room's local scripts and
		/// the global scripts.
		/// </summary>
		/// <param name="data">The decoded resource file (monkey1.001).</param>
		/// <param name="roomNumber">The room whose entry script to walk.</param>
		/// <param name="startStates">The object directory (DOBJ); objects not in the table start at state 0, owner 15.</param>
		/// <param name="objectIds">The objects the caller wants states for.</param>
		/// <param name="decodedIndex">The decoded index file (monkey1.000) with the script directory, or null.</param>
		public static List<RoomEntryScenario> Evaluate(
			byte[] data,
			int roomNumber,
			IReadOnlyDictionary<int, ClassicObjectStartState> startStates,
			IEnumerable<int> objectIds,
			byte[]? decodedIndex = null )
		{
			var entry = ScriptScanner.FindEntryScript( data, roomNumber );
			if( entry == null )
			{
				return new List<RoomEntryScenario> { SeededScenario( startStates, objectIds ) };
			}
			var localScripts = ScriptScanner.FindLocalScripts( data, roomNumber );
			var globalScripts = decodedIndex != null
				? ScriptScanner.FindGlobalScripts( data, decodedIndex )
				: new Dictionary<int, (int Start, int End)>();
			(int Start, int End) boot;
			return EvaluateScript(
				data, entry.Value.Start, entry.Value.End, startStates, objectIds,
				localScripts, globalScripts,
				globalScripts.TryGetValue( BootScriptId, out boot ) ? boot : ((int, int)?)null );
		}

		/// <summary>The number of the boot script the engine runs when a new game starts.</summary>
		public const int BootScriptId = 1;

		/// <summary>
		/// Evaluates one script byte range. Internal so the tests can feed synthetic bytecode
		/// without building a whole resource file.
		/// </summary>
		internal static List<RoomEntryScenario> EvaluateScript(
			byte[] data,
			int start,
			int end,
			IReadOnlyDictionary<int, ClassicObjectStartState> startStates,
			IEnumerable<int> objectIds,
			Dictionary<int, (int Start, int End)>? localScripts = null,
			Dictionary<int, (int Start, int End)>? globalScripts = null,
			(int Start, int End)? bootScript = null )
		{
			var requestedIds = objectIds.Distinct().ToList();
			var walk = new Walk(
				data, startStates,
				localScripts ?? new Dictionary<int, (int Start, int End)>(),
				globalScripts ?? new Dictionary<int, (int Start, int End)>() );

			var entryGraph = walk.GetGraph( EntryScriptKey, start, end );
			if( entryGraph.Blocks.Count == 0 )
			{
				return new List<RoomEntryScenario> { SeededScenario( startStates, requestedIds ) };
			}

			var initial = new Path();
			initial.Frames.Add( new Frame { ScriptKey = EntryScriptKey, Graph = entryGraph, FollowCalls = true } );

			// every script starts with zeroed local variables the engine gives it
			var zeroLocals = new Dictionary<int, int>();
			for( var index = 0; index < 16; index++ )
			{
				zeroLocals[0x4000 | index] = 0;
				initial.Variables[0x4000 | index] = 0;
			}

			// the boot frame sits on top, so it runs before the entry script, the way a new game
			// runs it first. Its own body sets the variable and object tables; the calls it makes
			// (the intro cutscene) are not followed, so the walk stays bounded.
			if( bootScript != null )
			{
				var bootGraph = walk.GetGraph( BootScriptKey, bootScript.Value.Start, bootScript.Value.End );
				if( bootGraph.Blocks.Count > 0 )
				{
					initial.Frames.Add( new Frame
					{
						ScriptKey = BootScriptKey,
						Graph = bootGraph,
						FollowCalls = false,
						SavedLocals = zeroLocals,
						SavedLocalRanges = new Dictionary<int, (long Min, long Max)>(),
					} );
				}
			}
			walk.Work.Push( initial );

			while( walk.Work.Count > 0 )
			{
				walk.RunPath( walk.Work.Pop() );
			}

			return MergePaths( walk.Finished, startStates, requestedIds );
		}

		//-------------------------------------------
		// the walk

		/// <summary>One stacked script call: a position inside one script's graph.</summary>
		private class Frame
		{
			public int ScriptKey;
			public ScriptControlFlowGraph Graph = null!;
			public int Block;
			public int Instruction;

			/// <summary>
			/// Whether startScript calls made by this frame are followed. The boot pre-run does
			/// not follow them, so the intro cutscene stays out of the walk.
			/// </summary>
			public bool FollowCalls = true;

			/// <summary>
			/// The caller's local variables, saved when this frame started: the called script
			/// uses the same local variable numbers, so the caller's values come back when the
			/// frame ends. Null for the entry frame.
			/// </summary>
			public Dictionary<int, int>? SavedLocals;
			public Dictionary<int, (long Min, long Max)>? SavedLocalRanges;

			public Frame Clone()
			{
				return new Frame
				{
					ScriptKey = this.ScriptKey,
					Graph = this.Graph,
					Block = this.Block,
					Instruction = this.Instruction,
					FollowCalls = this.FollowCalls,
					SavedLocals = this.SavedLocals == null ? null : new Dictionary<int, int>( this.SavedLocals ),
					SavedLocalRanges = this.SavedLocalRanges == null ? null : new Dictionary<int, (long Min, long Max)>( this.SavedLocalRanges ),
				};
			}
		}

		private class Path
		{
			public int Steps;
			public bool Truncated;
			public bool HasUnknownWrites;
			public bool HasSkippedScripts;
			public List<Frame> Frames = new List<Frame>();
			public Dictionary<int, int> Variables = new Dictionary<int, int>();
			public Dictionary<int, int> ObjectStates = new Dictionary<int, int>();
			public Dictionary<int, int> ObjectOwners = new Dictionary<int, int>();
			public List<ScriptCondition?> Conditions = new List<ScriptCondition?>();

			/// <summary>
			/// The open tests this path already forked at, keyed by script and position, with
			/// the edge the path took. A wait loop retests the same open condition forever; on
			/// the second visit the walk takes the other edge and leaves the loop instead of
			/// forking again.
			/// </summary>
			public Dictionary<long, bool> OpenChoices = new Dictionary<long, bool>();

			/// <summary>
			/// The value range each assumed test allows for its variable, so a later test on the
			/// same variable cannot contradict the assumption. A range of one value promotes the
			/// variable to a concrete value.
			/// </summary>
			public Dictionary<int, (long Min, long Max)> Ranges = new Dictionary<int, (long Min, long Max)>();

			/// <summary>
			/// The number of times each conditional test ran on this path, keyed by script and
			/// position, for the visit cap.
			/// </summary>
			public Dictionary<long, int> VisitCounts = new Dictionary<long, int>();

			/// <summary>
			/// The argument values of the startScript call that is being read: the argument
			/// events come first, the call event last and uses them.
			/// </summary>
			public List<int?> PendingArgs = new List<int?>();

			/// <summary>
			/// The variables an opcode the evaluator does not compute has written. During the
			/// boot pre-run an untouched variable reads as 0 (the engine zeroes the tables
			/// before the boot script runs), but a clobbered one stays unknown.
			/// </summary>
			public HashSet<int> ClobberedVariables = new HashSet<int>();

			/// <summary>
			/// Gets or sets whether the walk is inside the boot pre-run, where an untouched
			/// global variable reads as 0.
			/// </summary>
			public bool InBootScript;

			public Path Fork()
			{
				return new Path
				{
					Steps = this.Steps,
					Truncated = this.Truncated,
					HasUnknownWrites = this.HasUnknownWrites,
					HasSkippedScripts = this.HasSkippedScripts,
					Frames = this.Frames.Select( f => f.Clone() ).ToList(),
					Variables = new Dictionary<int, int>( this.Variables ),
					ObjectStates = new Dictionary<int, int>( this.ObjectStates ),
					ObjectOwners = new Dictionary<int, int>( this.ObjectOwners ),
					Conditions = new List<ScriptCondition?>( this.Conditions ),
					OpenChoices = new Dictionary<long, bool>( this.OpenChoices ),
					Ranges = new Dictionary<int, (long Min, long Max)>( this.Ranges ),
					VisitCounts = new Dictionary<long, int>( this.VisitCounts ),
					PendingArgs = new List<int?>( this.PendingArgs ),
					ClobberedVariables = new HashSet<int>( this.ClobberedVariables ),
					InBootScript = this.InBootScript,
				};
			}

			/// <summary>
			/// Records an assumed test, once: a loop that retests the same condition must not
			/// grow the label.
			/// </summary>
			public void AddCondition( ScriptCondition? condition )
			{
				var text = condition?.ToString();
				foreach( var existing in this.Conditions )
				{
					if( ( existing?.ToString() ) == text )
					{
						return;
					}
				}
				this.Conditions.Add( condition );
			}
		}

		/// <summary>The shared walk state: the script graphs, the work list and the finished paths.</summary>
		private class Walk(
			byte[] data,
			IReadOnlyDictionary<int, ClassicObjectStartState> startStates,
			Dictionary<int, (int Start, int End)> localScripts,
			Dictionary<int, (int Start, int End)> globalScripts )
		{
			private readonly byte[] data = data;
			private readonly Dictionary<int, (int Start, int End)> localScripts = localScripts;
			private readonly Dictionary<int, (int Start, int End)> globalScripts = globalScripts;
			private readonly Dictionary<int, ScriptControlFlowGraph> graphs = new Dictionary<int, ScriptControlFlowGraph>();

			public readonly IReadOnlyDictionary<int, ClassicObjectStartState> StartStates = startStates;
			public readonly Stack<Path> Work = new Stack<Path>();
			public readonly List<Path> Finished = new List<Path>();

			public ScriptControlFlowGraph GetGraph( int scriptKey, int start, int end )
			{
				ScriptControlFlowGraph graph;
				if( !this.graphs.TryGetValue( scriptKey, out graph ) )
				{
					var decoder = new ScriptDecoder( this.data, start, end );
					decoder.DecodeScript();
					graph = ScriptControlFlowGraph.Build( decoder.Instructions );
					this.graphs[scriptKey] = graph;
				}
				return graph;
			}

			/// <summary>
			/// Runs one path to its end, pushing a forked path to the work list at every open
			/// test while the budget lasts.
			/// </summary>
			public void RunPath( Path path )
			{
				while( true )
				{
					if( path.Frames.Count == 0 )
					{
						this.Finished.Add( path );
						return;
					}
					var frame = path.Frames[path.Frames.Count - 1];
					if( frame.Block < 0 || frame.Block >= frame.Graph.Blocks.Count )
					{
						PopFrame( path );
						continue;
					}

					path.InBootScript = !frame.FollowCalls;
					var block = frame.Graph.Blocks[frame.Block];
					if( frame.Instruction < block.Instructions.Count )
					{
						if( ++path.Steps > MaxStepsPerPath )
						{
							path.Truncated = true;
							this.Finished.Add( path );
							return;
						}
						var instruction = block.Instructions[frame.Instruction];
						frame.Instruction++;
						foreach( var scriptEvent in instruction.Events )
						{
							if( scriptEvent.Kind == ScriptEventKind.StartScript )
							{
								this.EnterScript( scriptEvent, path );
							}
							else
							{
								Apply( scriptEvent, path, this.StartStates );
							}
						}
						continue;
					}

					// the block's instructions are done: follow its edge
					var last = block.Last!;
					switch( last.FlowKind )
					{
						case ScriptFlowKind.Stop:
							// stopObjectCode ends the current script only
							PopFrame( path );
							continue;
						case ScriptFlowKind.Jump:
							MoveTo( frame, block.JumpBlock );
							continue;
						case ScriptFlowKind.ConditionalJump:
						{
							var choiceKey = ( (long)frame.ScriptKey << 32 ) | (uint)last.Position;
							var verdict = EvaluateCondition( last.Condition, path, this.StartStates );
							if( verdict != null )
							{
								// the visit cap breaks a loop that waits for another script:
								// past the cap the walk leaves through the other edge
								int visits;
								path.VisitCounts.TryGetValue( choiceKey, out visits );
								if( ++visits > MaxVisitsPerTest )
								{
									path.VisitCounts.Remove( choiceKey );
									MoveTo( frame, verdict.Value ? block.JumpBlock : block.NextBlock );
									continue;
								}
								path.VisitCounts[choiceKey] = visits;
								MoveTo( frame, verdict.Value ? block.NextBlock : block.JumpBlock );
								continue;
							}

							// the same open test again: the path is in a wait loop, so it takes
							// the other edge and leaves instead of forking once more
							bool tookNext;
							if( path.OpenChoices.TryGetValue( choiceKey, out tookNext ) )
							{
								MoveTo( frame, tookNext ? block.JumpBlock : block.NextBlock );
								continue;
							}

							// the test is open: fork, unless the budget is used up
							if( this.Finished.Count + this.Work.Count + 2 > MaxPaths )
							{
								// no budget: assume the test failed and skip the guarded code
								path.Truncated = true;
								path.AddCondition( last.Condition?.Negate() );
								path.OpenChoices[choiceKey] = false;
								Assume( last.Condition?.Negate(), path );
								MoveTo( frame, block.JumpBlock );
								continue;
							}
							var fork = path.Fork();
							fork.AddCondition( last.Condition?.Negate() );
							fork.OpenChoices[choiceKey] = false;
							Assume( last.Condition?.Negate(), fork );
							MoveTo( fork.Frames[fork.Frames.Count - 1], block.JumpBlock );
							this.Work.Push( fork );

							path.AddCondition( last.Condition );
							path.OpenChoices[choiceKey] = true;
							Assume( last.Condition, path );
							MoveTo( frame, block.NextBlock );
							continue;
						}
						default:
							MoveTo( frame, block.NextBlock );
							continue;
					}
				}
			}

			private static void MoveTo( Frame frame, int block )
			{
				frame.Block = block;
				frame.Instruction = 0;
			}

			/// <summary>
			/// Ends the top frame and gives the caller its local variables back.
			/// </summary>
			private static void PopFrame( Path path )
			{
				var frame = path.Frames[path.Frames.Count - 1];
				path.Frames.RemoveAt( path.Frames.Count - 1 );

				if( frame.SavedLocals != null )
				{
					RemoveLocalEntries( path );
					foreach( var saved in frame.SavedLocals )
					{
						path.Variables[saved.Key] = saved.Value;
					}
					foreach( var saved in frame.SavedLocalRanges! )
					{
						path.Ranges[saved.Key] = saved.Value;
					}
				}
			}

			/// <summary>
			/// A local variable of the running script: the 0x4000 flag without the 0x8000 bit
			/// variable flag.
			/// </summary>
			private static bool IsLocalVariable( int variableId )
			{
				return ( variableId & 0x4000 ) != 0 && ( variableId & 0x8000 ) == 0;
			}

			private static void RemoveLocalEntries( Path path )
			{
				foreach( var key in path.Variables.Keys.Where( IsLocalVariable ).ToList() )
				{
					path.Variables.Remove( key );
				}
				foreach( var key in path.Ranges.Keys.Where( IsLocalVariable ).ToList() )
				{
					path.Ranges.Remove( key );
				}
			}

			/// <summary>
			/// Fills the local variables of a starting script: the call arguments first, then
			/// zeros, because the engine zeroes the locals a call does not fill.
			/// </summary>
			private static void SeedLocals( List<int?> arguments, Path path )
			{
				for( var index = 0; index < 16; index++ )
				{
					WriteVariable( 0x4000 | index, index < arguments.Count ? arguments[index] : 0, path );
				}
			}

			/// <summary>
			/// Follows a startScript call into one of the room's local scripts, when the script
			/// number is known, the script exists in this room, the call chain is not too deep,
			/// and the script is not already on the chain (a recursion). A local script the walk
			/// cannot follow sets a flag; global scripts live in other resource parts and are
			/// skipped without one.
			/// </summary>
			private void EnterScript( ScriptEvent scriptEvent, Path path )
			{
				// the argument events came first; take them even when the call is not followed
				var arguments = path.PendingArgs;
				path.PendingArgs = new List<int?>();

				// the boot pre-run does not follow its calls (the intro cutscene)
				if( !path.Frames[path.Frames.Count - 1].FollowCalls )
				{
					return;
				}

				if( !scriptEvent.A.IsLiteral )
				{
					path.HasSkippedScripts = true;
					return;
				}
				var scriptId = scriptEvent.A.Value;

				(int Start, int End) range;
				if( !this.localScripts.TryGetValue( scriptId, out range )
					&& !this.globalScripts.TryGetValue( scriptId, out range ) )
				{
					// a local number that does not exist, or a global one with no directory
					if( scriptId >= 200 || this.globalScripts.Count > 0 )
					{
						path.HasSkippedScripts = true;
					}
					return;
				}
				if( path.Frames.Count >= MaxCallDepth || path.Frames.Any( f => f.ScriptKey == scriptId ) )
				{
					path.HasSkippedScripts = true;
					return;
				}

				var graph = this.GetGraph( scriptId, range.Start, range.End );
				if( graph.Blocks.Count == 0 )
				{
					return;
				}

				// the called script has its own local variables: save the caller's, clear them,
				// and seed the call arguments into local 0, 1, 2, ...
				var frame = new Frame
				{
					ScriptKey = scriptId,
					Graph = graph,
					FollowCalls = true,
					SavedLocals = path.Variables
						.Where( v => IsLocalVariable( v.Key ) )
						.ToDictionary( v => v.Key, v => v.Value ),
					SavedLocalRanges = path.Ranges
						.Where( v => IsLocalVariable( v.Key ) )
						.ToDictionary( v => v.Key, v => v.Value ),
				};
				RemoveLocalEntries( path );
				SeedLocals( arguments, path );
				path.Frames.Add( frame );
			}
		}

		//-------------------------------------------
		// event application

		private static void Apply( ScriptEvent scriptEvent, Path path, IReadOnlyDictionary<int, ClassicObjectStartState> startStates )
		{
			switch( scriptEvent.Kind )
			{
				case ScriptEventKind.SetVariable:
				{
					WriteVariable( scriptEvent.A.VariableId, Resolve( scriptEvent.B, path ), path );
					break;
				}
				case ScriptEventKind.AddVariable:
				case ScriptEventKind.SubtractVariable:
				{
					var target = scriptEvent.A.VariableId;
					var delta = Resolve( scriptEvent.B, path );
					var current = target >= 0 ? ResolveVariable( target, path ) : null;
					if( current != null && delta != null )
					{
						WriteVariable( target, scriptEvent.Kind == ScriptEventKind.AddVariable
							? current.Value + delta.Value
							: current.Value - delta.Value, path );
					}
					else
					{
						WriteVariable( target, null, path );
					}
					break;
				}
				case ScriptEventKind.IncrementVariable:
				case ScriptEventKind.DecrementVariable:
				{
					var target = scriptEvent.A.VariableId;
					var current = target >= 0 ? ResolveVariable( target, path ) : null;
					if( current != null )
					{
						WriteVariable( target, current.Value + ( scriptEvent.Kind == ScriptEventKind.IncrementVariable ? 1 : -1 ), path );
					}
					else
					{
						WriteVariable( target, null, path );
					}
					break;
				}
				case ScriptEventKind.InvalidateVariable:
					WriteVariable( scriptEvent.A.VariableId, null, path );
					break;

				case ScriptEventKind.StartScriptArg:
					path.PendingArgs.Add( Resolve( scriptEvent.A, path ) );
					break;

				case ScriptEventKind.GetObjectState:
				{
					var objectId = Resolve( scriptEvent.B, path );
					WriteVariable(
						scriptEvent.A.VariableId,
						objectId == null ? (int?)null : NullWhenUnknown( GetObjectState( objectId.Value, path, startStates ) ),
						path );
					break;
				}
				case ScriptEventKind.GetObjectOwner:
				{
					var objectId = Resolve( scriptEvent.B, path );
					WriteVariable(
						scriptEvent.A.VariableId,
						objectId == null ? (int?)null : NullWhenUnknown( GetObjectOwner( objectId.Value, path, startStates ) ),
						path );
					break;
				}

				case ScriptEventKind.SetObjectState:
				case ScriptEventKind.DrawObject:
				{
					var objectId = Resolve( scriptEvent.A, path );
					var state = Resolve( scriptEvent.B, path );
					if( objectId == null )
					{
						// a write to an object the evaluator cannot name may hit anything
						path.HasUnknownWrites = true;
					}
					else
					{
						path.ObjectStates[objectId.Value] = state ?? Unknown;
					}
					break;
				}
				case ScriptEventKind.SetOwnerOf:
				{
					var objectId = Resolve( scriptEvent.A, path );
					var owner = Resolve( scriptEvent.B, path );
					if( objectId == null )
					{
						path.HasUnknownWrites = true;
					}
					else
					{
						path.ObjectOwners[objectId.Value] = owner ?? Unknown;
					}
					break;
				}
				case ScriptEventKind.PickupObject:
				{
					// the engine gives the object to the player; the exact owner value is not known
					var objectId = Resolve( scriptEvent.A, path );
					if( objectId != null )
					{
						path.ObjectOwners[objectId.Value] = Unknown;
					}
					break;
				}
			}
		}

		/// <summary>
		/// Writes a variable, or drops it when the value is not known. A write also drops the
		/// assumed range, because the assumption was about the old value. A write whose target
		/// is not known (an indexed variable) can hit any variable, so it drops them all.
		/// </summary>
		private static void WriteVariable( int variableId, int? value, Path path )
		{
			if( variableId < 0 )
			{
				path.Variables.Clear();
				path.Ranges.Clear();
				path.ClobberedVariables.Add( -1 );
				return;
			}
			path.Ranges.Remove( variableId );
			if( value == null )
			{
				path.Variables.Remove( variableId );
				path.ClobberedVariables.Add( variableId );
			}
			else
			{
				path.Variables[variableId] = value.Value;
				path.ClobberedVariables.Remove( variableId );
			}
		}

		private static int? Resolve( ScriptDecoder.Operand operand, Path path )
		{
			if( operand.IsLiteral )
			{
				return operand.Value;
			}
			return operand.VariableId >= 0 ? ResolveVariable( operand.VariableId, path ) : null;
		}

		/// <summary>
		/// The value of a variable: the tracked value, or 0 during the boot pre-run for a
		/// variable nothing has touched (the engine zeroes the tables before the boot script),
		/// or null.
		/// </summary>
		private static int? ResolveVariable( int variableId, Path path )
		{
			int value;
			if( path.Variables.TryGetValue( variableId, out value ) )
			{
				return value;
			}
			if( path.InBootScript
				&& !path.ClobberedVariables.Contains( variableId )
				&& !path.ClobberedVariables.Contains( -1 ) )
			{
				return 0;
			}
			return null;
		}

		private static int? NullWhenUnknown( int value )
		{
			return value == Unknown ? (int?)null : value;
		}

		/// <summary>
		/// Evaluates the test of a conditional jump: true keeps the next instruction, false
		/// takes the jump, null is an open test the walk must fork on. An open test on a
		/// variable also asks the assumed value range, so an earlier assumption on the same
		/// variable answers a later test instead of forking an impossible path.
		/// </summary>
		private static bool? EvaluateCondition(
			ScriptCondition? condition,
			Path path,
			IReadOnlyDictionary<int, ClassicObjectStartState> startStates )
		{
			if( condition == null )
			{
				return null;
			}
			var value = ResolveConditionValue( condition, path );
			if( value == null )
			{
				return null;
			}

			if( condition.Kind == ScriptConditionKind.Variable )
			{
				if( condition.Subject < 0 )
				{
					return null;
				}
				var known = ResolveVariable( condition.Subject, path );
				if( known != null )
				{
					return Compare( known.Value, condition.Comparison, value.Value );
				}

				// the exact value is not known: try the assumed range
				var range = GetRange( condition.Subject, path );
				var lowVerdict = Compare( range.Min, condition.Comparison, value.Value );
				var highVerdict = Compare( range.Max, condition.Comparison, value.Value );
				if( IsRangeDecisive( condition.Comparison ) && lowVerdict == highVerdict )
				{
					return lowVerdict;
				}
				return null;
			}

			if( condition.Kind == ScriptConditionKind.ObjectState )
			{
				if( condition.Subject < 0 )
				{
					return null;
				}
				var state = GetObjectState( condition.Subject, path, startStates );
				return state == Unknown ? (bool?)null : Compare( state, condition.Comparison, value.Value );
			}

			return null;
		}

		/// <summary>
		/// The value a test compares with: the literal, or the value of the variable it reads
		/// when that is known, or null.
		/// </summary>
		private static int? ResolveConditionValue( ScriptCondition condition, Path path )
		{
			if( condition.ValueIsLiteral )
			{
				return condition.Value;
			}
			return condition.ValueVariableId >= 0 ? ResolveVariable( condition.ValueVariableId, path ) : null;
		}

		private static bool Compare( long subject, ScriptComparison comparison, long value )
		{
			switch( comparison )
			{
				case ScriptComparison.Equal:
					return subject == value;
				case ScriptComparison.NotEqual:
					return subject != value;
				case ScriptComparison.Less:
					return subject < value;
				case ScriptComparison.LessOrEqual:
					return subject <= value;
				case ScriptComparison.Greater:
					return subject > value;
				default:
					return subject >= value;
			}
		}

		/// <summary>
		/// Whether an equal verdict at both ends of a range decides the comparison for every
		/// value between them. That holds for the ordered comparisons; for equal and not-equal
		/// it only holds when the range shrank to one value, which the promotion to a concrete
		/// variable covers.
		/// </summary>
		private static bool IsRangeDecisive( ScriptComparison comparison )
		{
			return comparison != ScriptComparison.Equal && comparison != ScriptComparison.NotEqual;
		}

		/// <summary>
		/// The value range a variable can hold on this path: the assumed range when one exists,
		/// the 0..1 range for a bit variable, and the full range otherwise.
		/// </summary>
		private static (long Min, long Max) GetRange( int variableId, Path path )
		{
			(long Min, long Max) range;
			if( path.Ranges.TryGetValue( variableId, out range ) )
			{
				return range;
			}
			if( ( variableId & 0x8000 ) != 0 )
			{
				return ( 0, 1 );
			}
			return ( long.MinValue / 4, long.MaxValue / 4 );
		}

		/// <summary>
		/// Records an assumed test as a value range on its variable, so a later test on the
		/// same variable stays consistent with it. A range of one value promotes the variable
		/// to a concrete value.
		/// </summary>
		private static void Assume( ScriptCondition? condition, Path path )
		{
			if( condition == null || condition.Kind != ScriptConditionKind.Variable || condition.Subject < 0 )
			{
				return;
			}
			var resolved = ResolveConditionValue( condition, path );
			if( resolved == null )
			{
				return;
			}
			var value = resolved.Value;

			var range = GetRange( condition.Subject, path );
			switch( condition.Comparison )
			{
				case ScriptComparison.Equal:
					range = ( value, value );
					break;
				case ScriptComparison.NotEqual:
					// only a boundary value can tighten a range
					if( range.Min == value )
					{
						range.Min++;
					}
					else if( range.Max == value )
					{
						range.Max--;
					}
					break;
				case ScriptComparison.Less:
					range.Max = System.Math.Min( range.Max, value - 1L );
					break;
				case ScriptComparison.LessOrEqual:
					range.Max = System.Math.Min( range.Max, value );
					break;
				case ScriptComparison.Greater:
					range.Min = System.Math.Max( range.Min, value + 1L );
					break;
				default:
					range.Min = System.Math.Max( range.Min, value );
					break;
			}

			if( range.Min == range.Max && range.Min >= int.MinValue && range.Max <= int.MaxValue )
			{
				// the assumption pins the value: promote it to a concrete variable
				path.Variables[condition.Subject] = (int)range.Min;
				path.Ranges.Remove( condition.Subject );
			}
			else
			{
				path.Ranges[condition.Subject] = range;
			}
		}

		private static int GetObjectState( int objectId, Path path, IReadOnlyDictionary<int, ClassicObjectStartState> startStates )
		{
			int state;
			if( path.ObjectStates.TryGetValue( objectId, out state ) )
			{
				return state;
			}
			ClassicObjectStartState startState;
			return startStates.TryGetValue( objectId, out startState ) ? startState.State : 0;
		}

		private static int GetObjectOwner( int objectId, Path path, IReadOnlyDictionary<int, ClassicObjectStartState> startStates )
		{
			int owner;
			if( path.ObjectOwners.TryGetValue( objectId, out owner ) )
			{
				return owner;
			}

			// an object outside the directory keeps the "in its room" owner
			ClassicObjectStartState startState;
			return startStates.TryGetValue( objectId, out startState ) ? startState.Owner : 15;
		}

		//-------------------------------------------
		// results

		private static RoomEntryScenario SeededScenario(
			IReadOnlyDictionary<int, ClassicObjectStartState> startStates,
			IEnumerable<int> objectIds )
		{
			var scenario = new RoomEntryScenario();
			foreach( var objectId in objectIds )
			{
				ClassicObjectStartState startState;
				scenario.ObjectStates[objectId] = startStates.TryGetValue( objectId, out startState ) ? startState.State : 0;
			}
			return scenario;
		}

		/// <summary>
		/// Turns the finished paths into scenarios: the states of the requested objects, with
		/// paths that give the same states merged into one scenario. The scenario with the
		/// fewest assumed tests comes first.
		/// </summary>
		private static List<RoomEntryScenario> MergePaths(
			List<Path> finished,
			IReadOnlyDictionary<int, ClassicObjectStartState> startStates,
			List<int> requestedIds )
		{
			var scenarios = new List<RoomEntryScenario>();
			var byStates = new Dictionary<string, RoomEntryScenario>();

			foreach( var path in finished )
			{
				var scenario = new RoomEntryScenario
				{
					Truncated = path.Truncated,
					HasUnknownWrites = path.HasUnknownWrites,
					HasSkippedScripts = path.HasSkippedScripts,
				};
				foreach( var condition in path.Conditions )
				{
					scenario.Conditions.Add( condition?.ToString() ?? "an unknown test" );
				}
				foreach( var objectId in requestedIds )
				{
					var state = GetObjectState( objectId, path, startStates );
					scenario.ObjectStates[objectId] = state == Unknown ? (int?)null : state;
				}

				var signature = string.Join( ";", requestedIds.Select( id =>
					id + "=" + ( scenario.ObjectStates[id]?.ToString() ?? "?" ) ) );
				RoomEntryScenario existing;
				if( byStates.TryGetValue( signature, out existing ) )
				{
					existing.PathCount++;
					existing.Truncated |= scenario.Truncated;
					existing.HasUnknownWrites |= scenario.HasUnknownWrites;
					existing.HasSkippedScripts |= scenario.HasSkippedScripts;

					// the scenario with the fewest assumed tests is the best label for the result
					if( scenario.Conditions.Count < existing.Conditions.Count )
					{
						existing.Conditions.Clear();
						existing.Conditions.AddRange( scenario.Conditions );
					}
				}
				else
				{
					byStates[signature] = scenario;
					scenarios.Add( scenario );
				}
			}

			return scenarios
				.OrderBy( s => s.Conditions.Count )
				.ThenByDescending( s => s.PathCount )
				.ToList();
		}
	}
}
