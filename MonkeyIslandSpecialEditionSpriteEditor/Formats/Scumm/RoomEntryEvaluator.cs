using System.Collections.Generic;
using System.Linq;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm
{
	/// <summary>
	/// How the walk treats a test whose verdict it cannot compute.
	/// </summary>
	public enum ForkPolicy
	{
		/// <summary>
		/// Fork the walk at every open test, enumerating every reachable combination of answers.
		/// This is the exhaustive but noisy behaviour: engine/input variables, loop counters and
		/// unresolved object-class tests all fork, and the path budget is quickly spent on results
		/// that do not change what the room draws.
		/// </summary>
		Enumerate,

		/// <summary>
		/// Fork only on an open test of a plot-free variable or bit flag (a plain game global that
		/// the engine does not own, or a bit): the small set of atoms that actually decide what a
		/// room shows. Every other open test (an engine variable, a loop counter, an unresolved
		/// object-class or object-state test) takes its fall-through edge without forking, which
		/// keeps the walk on the real plot branches.
		/// </summary>
		PlotOnly,

		/// <summary>
		/// Never fork: every open test takes its fall-through edge, so the walk produces exactly
		/// one path. Combined with a set of locked variables this evaluates the room under one
		/// concrete assignment of the plot atoms.
		/// </summary>
		None,
	}

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
		public const int MaxPaths = 512;

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

		/// <summary>The shared empty pin table, so an unpinned evaluation allocates nothing.</summary>
		private static readonly IReadOnlyDictionary<int, int> EmptyLocked = new Dictionary<int, int>();

		/// <summary>
		/// Whether a variable is a plot atom: a plain game global the engine does not own itself, or
		/// a bit flag. These are the values the scripts use to record story progress, so they are the
		/// only ones the plot-only walk forks on. A script-local variable (the 0x4000 flag), an
		/// engine-owned global (a timer, the mouse, the room number) and an indexed variable (its
		/// number only known at run time) are not plot atoms.
		/// </summary>
		internal static bool IsPlotFree( int variableId )
		{
			if( variableId < 0 )
			{
				return false;
			}
			if( ( variableId & 0x8000 ) != 0 )
			{
				return true;
			}
			if( ( variableId & 0x4000 ) != 0 )
			{
				return false;
			}
			return !ScummEngineVariables.IsEngineVariable( variableId );
		}

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
			byte[]? encodedIndexBytes = null,
			ForkPolicy policy = ForkPolicy.Enumerate )
		{
			Parser.XorDecode( bytes, Parser.XorKey );
			if( encodedIndexBytes != null )
			{
				Parser.XorDecode( encodedIndexBytes, Parser.XorKey );
			}
			return Evaluate( bytes, roomNumber, startStates, objectIds, encodedIndexBytes, policy );
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
			byte[]? decodedIndex = null,
			ForkPolicy policy = ForkPolicy.Enumerate )
		{
			var scripts = LocateScripts( data, roomNumber, decodedIndex );
			if( scripts == null )
			{
				return new List<RoomEntryScenario> { SeededScenario( startStates, objectIds ) };
			}
			return EvaluateScript(
				data, scripts.Entry.Start, scripts.Entry.End, startStates, objectIds,
				scripts.LocalScripts, scripts.GlobalScripts, scripts.BootScript, policy );
		}

		/// <summary>
		/// The script byte ranges a room evaluation needs: its entry script, its local scripts, the
		/// global script directory (empty without the index file) and the boot script. Null when the
		/// room has no entry script. Located once and reused across the many walks state discovery
		/// runs for one room.
		/// </summary>
		internal class RoomScripts
		{
			public (int Start, int End) Entry;
			public Dictionary<int, (int Start, int End)> LocalScripts = null!;
			public Dictionary<int, (int Start, int End)> GlobalScripts = null!;
			public (int Start, int End)? BootScript;
		}

		private static RoomScripts? LocateScripts( byte[] data, int roomNumber, byte[]? decodedIndex )
		{
			var entry = ScriptScanner.FindEntryScript( data, roomNumber );
			if( entry == null )
			{
				return null;
			}
			var globalScripts = decodedIndex != null
				? ScriptScanner.FindGlobalScripts( data, decodedIndex )
				: new Dictionary<int, (int Start, int End)>();
			(int Start, int End) boot;
			return new RoomScripts
			{
				Entry = ( entry.Value.Start, entry.Value.End ),
				LocalScripts = ScriptScanner.FindLocalScripts( data, roomNumber ),
				GlobalScripts = globalScripts,
				BootScript = globalScripts.TryGetValue( BootScriptId, out boot ) ? boot : ( (int, int)?)null,
			};
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
			(int Start, int End)? bootScript = null,
			ForkPolicy policy = ForkPolicy.Enumerate )
		{
			var requestedIds = objectIds.Distinct().ToList();
			var walk = RunWalk(
				data, start, end, startStates,
				localScripts ?? new Dictionary<int, (int Start, int End)>(),
				globalScripts ?? new Dictionary<int, (int Start, int End)>(),
				bootScript, policy, EmptyLocked, null );
			if( walk == null )
			{
				return new List<RoomEntryScenario> { SeededScenario( startStates, requestedIds ) };
			}
			return MergePaths( walk.Finished, startStates, requestedIds );
		}

		/// <summary>
		/// Builds the work list and runs every path to its end, returning the finished walk (its
		/// finished paths and the plot tests it saw). Returns null when the entry script has no
		/// decodable body. The optional shared graph cache lets many walks over one room reuse the
		/// decoded control flow graphs instead of decoding each script again.
		/// </summary>
		private static Walk? RunWalk(
			byte[] data,
			int entryStart,
			int entryEnd,
			IReadOnlyDictionary<int, ClassicObjectStartState> startStates,
			Dictionary<int, (int Start, int End)> localScripts,
			Dictionary<int, (int Start, int End)> globalScripts,
			(int Start, int End)? bootScript,
			ForkPolicy policy,
			IReadOnlyDictionary<int, int> locked,
			Dictionary<int, ScriptControlFlowGraph>? sharedGraphs )
		{
			var walk = new Walk( data, startStates, localScripts, globalScripts, sharedGraphs );

			var entryGraph = walk.GetGraph( EntryScriptKey, entryStart, entryEnd );
			if( entryGraph.Blocks.Count == 0 )
			{
				return null;
			}

			var initial = new Path { Policy = policy, Locked = locked };
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

			return walk;
		}

		//-------------------------------------------
		// interactive state discovery

		/// <summary>The most plot atoms one room offers as controls, a guard against a runaway room.</summary>
		private const int MaxControls = 24;

		/// <summary>The most value classes one control offers.</summary>
		private const int MaxOptions = 8;

		/// <summary>
		/// Evaluates a room under one concrete assignment of plot atoms: the pinned variables hold
		/// their given values (reads return them, writes to them are ignored) and every open test
		/// takes its fall-through edge, so the walk produces exactly one appearance. This is what the
		/// interactive UI calls on every control change - one path, so it is fast.
		/// </summary>
		/// <param name="pinned">The plot atoms to hold fixed, keyed by engine variable number (a bit keeps the 0x8000 flag).</param>
		public static IReadOnlyDictionary<int, int?> EvaluateUnderAssignment(
			byte[] data,
			int roomNumber,
			IReadOnlyDictionary<int, ClassicObjectStartState> startStates,
			IEnumerable<int> objectIds,
			byte[]? decodedIndex,
			IReadOnlyDictionary<int, int> pinned )
		{
			var requestedIds = objectIds.Distinct().ToList();
			var scripts = LocateScripts( data, roomNumber, decodedIndex );
			if( scripts == null )
			{
				return SeededStates( startStates, requestedIds );
			}
			return RunAssignment( data, scripts, startStates, requestedIds, pinned, null ).States;
		}

		/// <summary>
		/// Evaluates one already-located script range under a pinned assignment. Internal so the
		/// tests can feed synthetic bytecode without building a whole resource file.
		/// </summary>
		internal static IReadOnlyDictionary<int, int?> EvaluateUnderAssignmentScript(
			byte[] data,
			int start,
			int end,
			IReadOnlyDictionary<int, ClassicObjectStartState> startStates,
			IEnumerable<int> objectIds,
			IReadOnlyDictionary<int, int> pinned,
			Dictionary<int, (int Start, int End)>? localScripts = null,
			Dictionary<int, (int Start, int End)>? globalScripts = null,
			(int Start, int End)? bootScript = null )
		{
			var scripts = new RoomScripts
			{
				Entry = ( start, end ),
				LocalScripts = localScripts ?? new Dictionary<int, (int Start, int End)>(),
				GlobalScripts = globalScripts ?? new Dictionary<int, (int Start, int End)>(),
				BootScript = bootScript,
			};
			return RunAssignment( data, scripts, startStates, objectIds.Distinct().ToList(), pinned, null ).States;
		}

		/// <summary>
		/// Discovers the interactive story states a room offers by decoding the still XOR encoded
		/// resource and index files in place, then delegating to <see cref="DiscoverStates"/>.
		/// </summary>
		public static RoomStateModel DiscoverStatesFromEncodedBytes(
			byte[] bytes,
			int roomNumber,
			IReadOnlyDictionary<int, ClassicObjectStartState> startStates,
			IEnumerable<int> objectIds,
			byte[]? encodedIndexBytes = null,
			IReadOnlyDictionary<int, string?>? objectNames = null )
		{
			Parser.XorDecode( bytes, Parser.XorKey );
			if( encodedIndexBytes != null )
			{
				Parser.XorDecode( encodedIndexBytes, Parser.XorKey );
			}
			return DiscoverStates( bytes, roomNumber, startStates, objectIds, encodedIndexBytes, objectNames );
		}

		/// <summary>
		/// Discovers the interactive story states a room offers: which plot atoms (plain game
		/// globals and bit flags) change what the room draws, the value classes each one can take,
		/// and the objects each class shows and hides compared with the game-start baseline. A
		/// plot-only enumeration and a baseline walk find the candidate atoms and the values the
		/// scripts test them against; each candidate value is then realised with a one-path pinned
		/// walk, and atoms that never change what is drawn are dropped. The result is a small model
		/// the UI turns into radio groups and checkboxes.
		/// </summary>
		/// <param name="objectNames">Optional object names (OBNA), used to label the controls.</param>
		public static RoomStateModel DiscoverStates(
			byte[] data,
			int roomNumber,
			IReadOnlyDictionary<int, ClassicObjectStartState> startStates,
			IEnumerable<int> objectIds,
			byte[]? decodedIndex,
			IReadOnlyDictionary<int, string?>? objectNames = null )
		{
			var requestedIds = objectIds.Distinct().ToList();
			var scripts = LocateScripts( data, roomNumber, decodedIndex );
			if( scripts == null )
			{
				return new RoomStateModel();
			}
			var verbStarts = ScriptScanner.FindObjectVerbScripts( data, roomNumber );
			return DiscoverStatesCore( data, scripts, startStates, requestedIds, objectNames, verbStarts );
		}

		/// <summary>
		/// Discovers the interactive states for one already-located script range. Internal so the
		/// tests can feed synthetic bytecode without building a whole resource file.
		/// </summary>
		internal static RoomStateModel DiscoverStatesScript(
			byte[] data,
			int start,
			int end,
			IReadOnlyDictionary<int, ClassicObjectStartState> startStates,
			IEnumerable<int> objectIds,
			IReadOnlyDictionary<int, string?>? objectNames = null,
			Dictionary<int, (int Start, int End)>? localScripts = null,
			Dictionary<int, (int Start, int End)>? globalScripts = null,
			(int Start, int End)? bootScript = null,
			IReadOnlyDictionary<int, HashSet<int>>? verbStarts = null )
		{
			var scripts = new RoomScripts
			{
				Entry = ( start, end ),
				LocalScripts = localScripts ?? new Dictionary<int, (int Start, int End)>(),
				GlobalScripts = globalScripts ?? new Dictionary<int, (int Start, int End)>(),
				BootScript = bootScript,
			};
			return DiscoverStatesCore( data, scripts, startStates, objectIds.Distinct().ToList(), objectNames,
				verbStarts ?? new Dictionary<int, HashSet<int>>() );
		}

		private static RoomStateModel DiscoverStatesCore(
			byte[] data,
			RoomScripts scripts,
			IReadOnlyDictionary<int, ClassicObjectStartState> startStates,
			List<int> requestedIds,
			IReadOnlyDictionary<int, string?>? objectNames,
			IReadOnlyDictionary<int, HashSet<int>> verbStarts )
		{
			var model = new RoomStateModel();

			// one decoded-graph cache shared by every walk this discovery runs for the room
			var graphs = new Dictionary<int, ScriptControlFlowGraph>();

			// the game-start baseline: what the room draws with every plot atom at its start value
			var baseline = RunAssignment( data, scripts, startStates, requestedIds, EmptyLocked, graphs );
			var baselineDrawn = DrawnSet( baseline.States );
			var baselineSignature = Signature( baselineDrawn );

			// discover candidate atoms and the values the scripts test them against: a broad
			// plot-only enumeration reaches the forked branches, and the baseline walk adds the
			// tests it reached on the way
			var thresholds = new Dictionary<int, SortedSet<int>>();
			var enumeration = RunWalk(
				data, scripts.Entry.Start, scripts.Entry.End, startStates,
				scripts.LocalScripts, scripts.GlobalScripts, scripts.BootScript,
				ForkPolicy.PlotOnly, EmptyLocked, graphs );
			model.Incomplete = HitCap( enumeration );
			HarvestThresholds( enumeration, thresholds );
			HarvestThresholds( baseline.Walk, thresholds );

			// build a control for each candidate; following the tests each option walk reaches
			// picks up atoms that only appear once another atom is pinned
			var processed = new HashSet<int>();
			var queue = new Queue<int>( thresholds.Keys );
			while( queue.Count > 0 && model.Controls.Count < MaxControls )
			{
				var variableId = queue.Dequeue();
				if( !processed.Add( variableId ) )
				{
					continue;
				}

				SortedSet<int> values;
				thresholds.TryGetValue( variableId, out values );
				var control = BuildControl(
					data, scripts, startStates, requestedIds, baselineDrawn, baselineSignature,
					variableId, values, objectNames, graphs, thresholds, model );
				if( control != null )
				{
					model.Controls.Add( control );
				}

				foreach( var candidate in thresholds.Keys )
				{
					if( !processed.Contains( candidate ) && !queue.Contains( candidate ) )
					{
						queue.Enqueue( candidate );
					}
				}
			}

			// candidates left unprocessed because the control cap was reached: the model is a
			// lower bound, so say so rather than presenting it as the whole picture
			if( queue.Count > 0 )
			{
				model.Incomplete = true;
			}

			// most-impactful first, then by variable number so the order is stable
			model.Controls.Sort( ( a, b ) =>
			{
				var impact = ControlImpact( b ) - ControlImpact( a );
				return impact != 0 ? impact : a.VariableId - b.VariableId;
			} );

			// two atoms that change exactly the same objects the same way are one story state read
			// two ways (a plot counter and a bit the same branch sets); keep only the first, so the
			// panel does not offer two controls that fight over the same objects
			var seenEffects = new HashSet<string>();
			model.Controls.RemoveAll( c => !seenEffects.Add( ControlEffectSignature( c ) ) );

			DiscoverScriptedStates( data, scripts, startStates, requestedIds, baseline.States, baselineDrawn, objectNames, verbStarts, model );
			return model;
		}

		/// <summary>The most verb-driven states one room offers as checkboxes.</summary>
		private const int MaxScriptedStates = 12;

		/// <summary>
		/// Discovers the verb-driven states a room can show: the appearances its local scripts
		/// produce when a verb starts them, on top of the game-start baseline. Each local script is
		/// run (following its cascade of started scripts) and the objects it draws or hides compared
		/// with the baseline are its effect. An effect contained in a larger one whose cascade
		/// actually started this script is dropped, exact duplicates are dropped, and any a plot
		/// control already offers is dropped too, so what remains is the set of distinct extra
		/// appearances a verb can reach.
		///
		/// This is an over-approximation, and deliberately so. A local runs under fork policy None,
		/// which takes the fall-through edge of any test it cannot answer, so a draw guarded by an
		/// engine variable or an object-state test the walk does not know still runs - the same
		/// choice that lets the baseline surface the bar's crowd. It also runs the local with zero
		/// arguments (a verb may pass some) and reads it from the local-script table, not the object
		/// verb scripts (OBCD) a verb also runs - though those draw directly only a handful of times
		/// across the whole game. The result is "the appearances these scripts can produce", offered
		/// for the modder to preview, not a proof of exactly what each verb does.
		/// </summary>
		private static void DiscoverScriptedStates(
			byte[] data,
			RoomScripts scripts,
			IReadOnlyDictionary<int, ClassicObjectStartState> startStates,
			List<int> requestedIds,
			Dictionary<int, int?> baselineStates,
			HashSet<int> baselineDrawn,
			IReadOnlyDictionary<int, string?>? objectNames,
			IReadOnlyDictionary<int, HashSet<int>> verbStarts,
			RoomStateModel model )
		{
			if( scripts.LocalScripts.Count == 0 )
			{
				return;
			}

			// seed each local walk with the exact baseline object states, so a local that reads an
			// object the entry script set (its state, not merely whether it is drawn) behaves the
			// way it would after entry
			var seeds = BuildBaselineSeeds( startStates, baselineStates );

			var effects = new List<ScriptedEffect>();
			var cascadeByLocal = new Dictionary<int, HashSet<int>>();
			foreach( var local in scripts.LocalScripts.OrderBy( l => l.Key ) )
			{
				var localScripts = new RoomScripts
				{
					Entry = ( local.Value.Start, local.Value.End ),
					LocalScripts = scripts.LocalScripts,
					GlobalScripts = scripts.GlobalScripts,
					BootScript = null,
				};
				// a fresh graph cache: the entry-graph key would otherwise collide between locals
				var result = RunAssignment( data, localScripts, seeds, requestedIds, EmptyLocked, null );
				cascadeByLocal[local.Key] = result.Walk != null ? result.Walk.EnteredScripts : new HashSet<int>();
				var drawn = DrawnSet( result.States );

				var shows = new HashSet<int>( drawn );
				shows.ExceptWith( baselineDrawn );
				var directHides = new HashSet<int>( baselineDrawn );
				directHides.ExceptWith( drawn );

				HashSet<int> effectShows;
				HashSet<int> effectHides;
				if( shows.Count > 0 )
				{
					// re-compose the room the way the game would on the next entry with these objects
					// set: seed the entry with the objects the local drew and run it, so the entry's
					// own conditional draws apply - e.g. room 12 draws the monkey head's nose only
					// while the mouth is closed, so opening the mouth (drawing 140-143) must drop it
					var entrySeeds = new Dictionary<int, ClassicObjectStartState>();
					foreach( var pair in startStates )
					{
						entrySeeds[pair.Key] = pair.Value;
					}
					foreach( var id in shows )
					{
						ClassicObjectStartState existing;
						var owner = entrySeeds.TryGetValue( id, out existing ) ? existing.Owner : 15;
						var classFlags = existing?.ClassFlags ?? 0u;
						var state = result.States.TryGetValue( id, out var s ) && s.HasValue && s.Value != 0 ? s.Value : 1;
						entrySeeds[id] = new ClassicObjectStartState( id, state, owner, classFlags );
					}
					var recomposed = DrawnSet( RunAssignment( data, scripts, entrySeeds, requestedIds, EmptyLocked, null ).States );

					effectShows = new HashSet<int>( recomposed );
					effectShows.ExceptWith( baselineDrawn );
					effectHides = new HashSet<int>( baselineDrawn );
					effectHides.ExceptWith( recomposed );
					effectHides.UnionWith( directHides );
				}
				else
				{
					effectShows = shows;
					effectHides = directHides;
				}

				if( effectShows.Count > 0 || effectHides.Count > 0 )
				{
					var isAnimation = effectShows.Count > 1 && HasFrameYield( data, local.Value.Start, local.Value.End );
					effects.Add( new ScriptedEffect( local.Key, effectShows, effectHides, cascadeByLocal[local.Key], isAnimation ) );
				}
			}

			// which object's verb reaches each local: an object that starts local X reaches X and
			// every script X's cascade goes on to start, so a state drawn deep in the cascade is
			// still named after the object the player acts on
			var triggerObjectsFor = new Dictionary<int, HashSet<int>>();
			foreach( var start in verbStarts )
			{
				var reach = new HashSet<int> { start.Key };
				HashSet<int> cascade;
				if( cascadeByLocal.TryGetValue( start.Key, out cascade ) )
				{
					reach.UnionWith( cascade );
				}
				foreach( var localId in reach )
				{
					HashSet<int> objects;
					if( !triggerObjectsFor.TryGetValue( localId, out objects ) )
					{
						objects = new HashSet<int>();
						triggerObjectsFor[localId] = objects;
					}
					objects.UnionWith( start.Value );
				}
			}

			// the plot controls already cover some of these; skip an effect they offer
			var controlEffects = new HashSet<string>();
			foreach( var control in model.Controls )
			{
				foreach( var option in control.Options )
				{
					controlEffects.Add( EffectSignature( option.ObjectsShown, option.ObjectsHidden ) );
				}
			}

			// keep the distinct effects, dropping a script that another kept script's cascade
			// actually started and whose objects nest inside that script's (a genuine helper), but
			// keeping a smaller effect from an unrelated script (a distinct verb outcome)
			var ordered = effects
				.OrderByDescending( e => e.Shows.Count + e.Hides.Count )
				.ToList();
			var kept = new List<ScriptedEffect>();
			var keptSignatures = new HashSet<string>();
			foreach( var effect in ordered )
			{
				if( kept.Any( k => k.Cascade.Contains( effect.LocalId )
					&& effect.Shows.IsSubsetOf( k.Shows ) && effect.Hides.IsSubsetOf( k.Hides ) ) )
				{
					continue;
				}
				var signature = EffectSignature( effect.Shows, effect.Hides );
				if( controlEffects.Contains( signature ) || !keptSignatures.Add( signature ) )
				{
					continue;
				}
				kept.Add( effect );
			}

			foreach( var effect in kept )
			{
				if( model.ScriptedStates.Count >= MaxScriptedStates )
				{
					model.Incomplete = true;
					break;
				}
				HashSet<int> triggerObjects;
				var trigger = triggerObjectsFor.TryGetValue( effect.LocalId, out triggerObjects )
					? TriggerName( triggerObjects, objectNames )
					: "";
				var state = new RoomScriptedState
				{
					Label = ScriptedStateLabel( effect.Shows, effect.Hides, effect.IsAnimation, objectNames ),
					LocalScriptId = effect.LocalId,
					IsAnimation = effect.IsAnimation,
					Trigger = trigger,
				};
				state.ObjectsShown.AddRange( effect.Shows.OrderBy( id => id ) );
				state.ObjectsHidden.AddRange( effect.Hides.OrderBy( id => id ) );
				model.ScriptedStates.Add( state );
			}

			// most objects first, then by label so the order is stable
			model.ScriptedStates.Sort( ( a, b ) =>
			{
				var impact = ( b.ObjectsShown.Count + b.ObjectsHidden.Count ) - ( a.ObjectsShown.Count + a.ObjectsHidden.Count );
				return impact != 0 ? impact : string.CompareOrdinal( a.Label, b.Label );
			} );
		}

		/// <summary>One local script's effect: what it draws and hides, and the scripts it started.</summary>
		private class ScriptedEffect
		{
			public ScriptedEffect( int localId, HashSet<int> shows, HashSet<int> hides, HashSet<int> cascade, bool isAnimation )
			{
				this.LocalId = localId;
				this.Shows = shows;
				this.Hides = hides;
				this.Cascade = cascade;
				this.IsAnimation = isAnimation;
			}

			public int LocalId
			{
				get;
			}

			public HashSet<int> Shows
			{
				get;
			}

			public HashSet<int> Hides
			{
				get;
			}

			public HashSet<int> Cascade
			{
				get;
			}

			public bool IsAnimation
			{
				get;
			}
		}

		/// <summary>
		/// Whether a script reveals objects a frame at a time: it draws objects and contains a
		/// breakHere (which yields a frame), so the objects appear in sequence rather than at once.
		/// </summary>
		private static bool HasFrameYield( byte[] data, int start, int end )
		{
			var decoder = new ScriptDecoder( data, start, end );
			decoder.DecodeScript();
			var hasBreak = false;
			var draws = 0;
			foreach( var instruction in decoder.Instructions )
			{
				if( instruction.Opcode == 0x80 )
				{
					hasBreak = true;
				}
				foreach( var scriptEvent in instruction.Events )
				{
					if( scriptEvent.Kind == ScriptEventKind.DrawObject || scriptEvent.Kind == ScriptEventKind.SetObjectState )
					{
						draws++;
					}
				}
			}
			return hasBreak && draws > 0;
		}

		private static Dictionary<int, ClassicObjectStartState> BuildBaselineSeeds(
			IReadOnlyDictionary<int, ClassicObjectStartState> startStates,
			Dictionary<int, int?> baselineStates )
		{
			var seeds = new Dictionary<int, ClassicObjectStartState>();
			foreach( var pair in startStates )
			{
				seeds[pair.Key] = pair.Value;
			}
			// overlay the exact states the entry walk computed (0 hides, a positive value is that
			// state); an unknown value keeps the object-directory seed
			foreach( var pair in baselineStates )
			{
				if( pair.Value == null )
				{
					continue;
				}
				ClassicObjectStartState existing;
				var owner = seeds.TryGetValue( pair.Key, out existing ) ? existing.Owner : 15;
				var classFlags = existing?.ClassFlags ?? 0u;
				seeds[pair.Key] = new ClassicObjectStartState( pair.Key, pair.Value.Value, owner, classFlags );
			}
			return seeds;
		}

		private static string EffectSignature( IEnumerable<int> shows, IEnumerable<int> hides )
		{
			return "s:" + string.Join( ",", shows.OrderBy( id => id ) ) + "|h:" + string.Join( ",", hides.OrderBy( id => id ) );
		}

		private static string ScriptedStateLabel(
			HashSet<int> shows,
			HashSet<int> hides,
			bool isAnimation,
			IReadOnlyDictionary<int, string?>? objectNames )
		{
			string primary;
			if( shows.Count > 0 )
			{
				var verb = isAnimation ? "Animate " : "Show ";
				var label = verb + LabelForObjects( shows, objectNames, out primary );
				return hides.Count > 0 ? label + " (hide " + hides.Count + ")" : label;
			}
			return "Hide " + LabelForObjects( hides, objectNames, out primary );
		}

		/// <summary>
		/// The name of the object a verb acts on to reach a state - the most common name among the
		/// trigger objects (a set of like-named corpses reads as "Corpse"). Empty when the trigger
		/// objects have no name, so the label falls back to the script number.
		/// </summary>
		private static string TriggerName( HashSet<int> objectIds, IReadOnlyDictionary<int, string?>? objectNames )
		{
			if( objectIds.Count == 0 || objectNames == null )
			{
				return "";
			}
			var counts = new Dictionary<string, int>();
			foreach( var id in objectIds )
			{
				string? raw;
				objectNames.TryGetValue( id, out raw );
				var name = CleanName( raw );
				if( name.Length > 0 )
				{
					int current;
					counts.TryGetValue( name, out current );
					counts[name] = current + 1;
				}
			}
			// name it only when the trigger objects agree on one name (a set of like-named corpses);
			// several different names means the state is reachable more than one way, so rather than
			// guess which verb "owns" it, fall back to the script number
			if( counts.Count != 1 )
			{
				return "";
			}
			return Capitalize( counts.Keys.First() );
		}

		/// <summary>A signature of what a control changes, so two atoms with the same effect merge.</summary>
		private static string ControlEffectSignature( RoomStateControl control )
		{
			var parts = control.Options
				.Select( o => "s:" + string.Join( ",", o.ObjectsShown ) + "|h:" + string.Join( ",", o.ObjectsHidden ) )
				.OrderBy( s => s, System.StringComparer.Ordinal );
			return string.Join( ";", parts );
		}

		/// <summary>
		/// Runs one pinned walk (fork policy None) and reads the requested objects' states from its
		/// single finished path. Returns the states and the walk (whose plot tests discovery reads).
		/// </summary>
		private static (Dictionary<int, int?> States, Walk? Walk) RunAssignment(
			byte[] data,
			RoomScripts scripts,
			IReadOnlyDictionary<int, ClassicObjectStartState> startStates,
			List<int> requestedIds,
			IReadOnlyDictionary<int, int> pinned,
			Dictionary<int, ScriptControlFlowGraph>? sharedGraphs )
		{
			var walk = RunWalk(
				data, scripts.Entry.Start, scripts.Entry.End, startStates,
				scripts.LocalScripts, scripts.GlobalScripts, scripts.BootScript,
				ForkPolicy.None, pinned, sharedGraphs );
			var path = walk != null && walk.Finished.Count > 0 ? walk.Finished[0] : null;

			var states = new Dictionary<int, int?>();
			foreach( var id in requestedIds )
			{
				if( path == null )
				{
					states[id] = SeededState( startStates, id );
				}
				else
				{
					var state = GetObjectState( id, path, startStates );
					states[id] = state == Unknown ? (int?)null : state;
				}
			}
			return ( states, walk );
		}

		/// <summary>
		/// Builds one control for a candidate plot atom, or null when the atom never changes what
		/// the room draws (so it is not a real story state). Each value class is realised with a
		/// pinned walk; the classes are grouped by the objects they draw, labelled from the object
		/// names, and kept only when at least two classes differ.
		/// </summary>
		private static RoomStateControl? BuildControl(
			byte[] data,
			RoomScripts scripts,
			IReadOnlyDictionary<int, ClassicObjectStartState> startStates,
			List<int> requestedIds,
			HashSet<int> baselineDrawn,
			string baselineSignature,
			int variableId,
			SortedSet<int>? thresholds,
			IReadOnlyDictionary<int, string?>? objectNames,
			Dictionary<int, ScriptControlFlowGraph> graphs,
			Dictionary<int, SortedSet<int>> allThresholds,
			RoomStateModel model )
		{
			var isBit = ( variableId & 0x8000 ) != 0;

			// the values to probe: a bit is 0 or 1; a plain global is 0 (the start value) plus each
			// threshold and its neighbours, so a <, <=, ==, >= or > boundary all land in a class
			var representatives = new List<int>();
			if( isBit )
			{
				representatives.Add( 0 );
				representatives.Add( 1 );
			}
			else
			{
				AddRepresentative( representatives, 0 );
				if( thresholds != null )
				{
					foreach( var threshold in thresholds )
					{
						AddRepresentative( representatives, threshold - 1 );
						AddRepresentative( representatives, threshold );
						AddRepresentative( representatives, threshold + 1 );
					}
				}
			}

			// group the representatives by the objects they draw; keep the first (smallest) value
			// per class, and the class's full drawn set
			var classesBySignature = new Dictionary<string, (int Representative, HashSet<int> Drawn)>();
			var classOrder = new List<string>();
			foreach( var value in representatives )
			{
				var pinned = new Dictionary<int, int> { { variableId, value } };
				var result = RunAssignment( data, scripts, startStates, requestedIds, pinned, graphs );
				HarvestThresholds( result.Walk, allThresholds );
				var drawn = DrawnSet( result.States );
				var signature = Signature( drawn );
				if( !classesBySignature.ContainsKey( signature ) )
				{
					classesBySignature[signature] = ( value, drawn );
					classOrder.Add( signature );
				}
			}

			// the atom gates the room only when its classes differ in what they draw
			if( classesBySignature.Count < 2 )
			{
				return null;
			}

			// the objects this atom toggles: drawn in some class but not in every class
			var union = new HashSet<int>();
			HashSet<int>? intersection = null;
			foreach( var signature in classOrder )
			{
				var drawn = classesBySignature[signature].Drawn;
				union.UnionWith( drawn );
				if( intersection == null )
				{
					intersection = new HashSet<int>( drawn );
				}
				else
				{
					intersection.IntersectWith( drawn );
				}
			}
			var toggled = new HashSet<int>( union );
			toggled.ExceptWith( intersection! );

			var control = new RoomStateControl
			{
				VariableId = variableId,
				IsCheckbox = isBit,
			};

			// the default is the class whose drawn set matches the game-start baseline; it stays 0
			// (the value-0 class, always probed first) when no class matches, which happens only if
			// a script writes the atom before the gating test
			var defaultFound = false;

			var optionLabels = new List<string>();
			foreach( var signature in classOrder )
			{
				if( control.Options.Count >= MaxOptions )
				{
					// more value classes than the panel shows: the model is a lower bound
					model.Incomplete = true;
					break;
				}
				var entry = classesBySignature[signature];
				var shown = new HashSet<int>( entry.Drawn );
				shown.ExceptWith( baselineDrawn );
				var hidden = new HashSet<int>( baselineDrawn );
				hidden.ExceptWith( entry.Drawn );

				var governed = new HashSet<int>( entry.Drawn );
				governed.IntersectWith( toggled );
				string primary;
				var label = LabelForObjects( governed, objectNames, out primary );

				var option = new RoomStateOption
				{
					RepresentativeValue = entry.Representative,
					Label = label,
				};
				option.ObjectsShown.AddRange( shown.OrderBy( id => id ) );
				option.ObjectsHidden.AddRange( hidden.OrderBy( id => id ) );
				control.Options.Add( option );
				optionLabels.Add( string.IsNullOrEmpty( primary ) ? "None" : primary );

				if( signature == baselineSignature )
				{
					control.DefaultOptionIndex = control.Options.Count - 1;
					defaultFound = true;
				}
			}

			if( !defaultFound )
			{
				// no realised class reproduced the baseline: fall back to the value-0 class
				control.DefaultOptionIndex = 0;
			}

			control.Name = BuildControlName( control, toggled, objectNames, optionLabels );
			return control;
		}

		/// <summary>
		/// The caption for a control. A checkbox describes what ticking it does to its objects; a
		/// radio group joins the names of the objects its options draw.
		/// </summary>
		private static string BuildControlName(
			RoomStateControl control,
			HashSet<int> toggled,
			IReadOnlyDictionary<int, string?>? objectNames,
			List<string> optionLabels )
		{
			if( control.IsCheckbox && control.Options.Count == 2 )
			{
				var checkedOption = control.Options[control.DefaultOptionIndex == 0 ? 1 : 0];
				string primary;
				if( checkedOption.ObjectsHidden.Count > 0 && checkedOption.ObjectsShown.Count == 0 )
				{
					LabelForObjects( new HashSet<int>( checkedOption.ObjectsHidden ), objectNames, out primary );
					return "Hide " + LowerFirst( primary );
				}
				if( checkedOption.ObjectsShown.Count > 0 && checkedOption.ObjectsHidden.Count == 0 )
				{
					LabelForObjects( new HashSet<int>( checkedOption.ObjectsShown ), objectNames, out primary );
					return "Show " + LowerFirst( primary );
				}
			}

			var distinct = optionLabels.Where( l => l != "None" ).Distinct().ToList();
			if( distinct.Count > 0 )
			{
				return string.Join( " / ", distinct );
			}
			string name;
			LabelForObjects( toggled, objectNames, out name );
			return string.IsNullOrEmpty( name ) ? "State" : name;
		}

		private static void AddRepresentative( List<int> values, int value )
		{
			// SCUMM v5 variables are signed 16-bit, so a negative threshold is legitimate; keep it
			if( !values.Contains( value ) )
			{
				values.Add( value );
			}
		}

		/// <summary>How many object visibilities a control changes, for ordering the controls.</summary>
		private static int ControlImpact( RoomStateControl control )
		{
			var count = 0;
			foreach( var option in control.Options )
			{
				count += option.ObjectsShown.Count + option.ObjectsHidden.Count;
			}
			return count;
		}

		private static HashSet<int> DrawnSet( Dictionary<int, int?> states )
		{
			var drawn = new HashSet<int>();
			foreach( var pair in states )
			{
				if( pair.Value.HasValue && pair.Value.Value != 0 )
				{
					drawn.Add( pair.Key );
				}
			}
			return drawn;
		}

		private static string Signature( HashSet<int> drawn )
		{
			return string.Join( ",", drawn.OrderBy( id => id ) );
		}

		private static void HarvestThresholds( Walk? walk, Dictionary<int, SortedSet<int>> into )
		{
			if( walk == null )
			{
				return;
			}
			foreach( var test in walk.PlotTests )
			{
				SortedSet<int> values;
				if( !into.TryGetValue( test.Subject, out values ) )
				{
					values = new SortedSet<int>();
					into[test.Subject] = values;
				}
				values.Add( test.Value );
			}
		}

		private static bool HitCap( Walk? walk )
		{
			return walk != null && walk.ForkBudgetHit;
		}

		private static Dictionary<int, int?> SeededStates(
			IReadOnlyDictionary<int, ClassicObjectStartState> startStates,
			List<int> requestedIds )
		{
			var states = new Dictionary<int, int?>();
			foreach( var id in requestedIds )
			{
				states[id] = SeededState( startStates, id );
			}
			return states;
		}

		private static int SeededState( IReadOnlyDictionary<int, ClassicObjectStartState> startStates, int objectId )
		{
			ClassicObjectStartState startState;
			return startStates.TryGetValue( objectId, out startState ) ? startState.State : 0;
		}

		/// <summary>
		/// A label for a set of objects: the most common cleaned object name, pluralised and with
		/// the count, e.g. "Pirates (10)". The chosen name is returned in <paramref name="primary"/>
		/// so the caller can build a control caption from it.
		/// </summary>
		private static string LabelForObjects(
			HashSet<int> objectIds,
			IReadOnlyDictionary<int, string?>? objectNames,
			out string primary )
		{
			primary = "";
			if( objectIds.Count == 0 )
			{
				return "None";
			}

			var counts = new Dictionary<string, int>();
			if( objectNames != null )
			{
				foreach( var id in objectIds )
				{
					string? raw;
					objectNames.TryGetValue( id, out raw );
					var name = CleanName( raw );
					if( name.Length > 0 )
					{
						int current;
						counts.TryGetValue( name, out current );
						counts[name] = current + 1;
					}
				}
			}

			if( counts.Count == 0 )
			{
				// no names to go on: fall back to the object numbers, so options stay distinguishable
				var sorted = objectIds.OrderBy( id => id ).ToList();
				if( sorted.Count == 1 )
				{
					primary = "Object " + sorted[0];
					return primary;
				}
				// a contiguous run reads clearest as a range (146-151), which also tells two nearly
				// identical states apart
				if( sorted[sorted.Count - 1] - sorted[0] == sorted.Count - 1 )
				{
					primary = "Objects " + sorted[0] + "–" + sorted[sorted.Count - 1];
					return primary;
				}
				if( sorted.Count <= 3 )
				{
					primary = "Objects " + string.Join( ", ", sorted );
					return primary;
				}
				primary = sorted.Count + " objects";
				return primary + " (" + string.Join( ", ", sorted.Take( 3 ) ) + "…)";
			}

			var best = counts.OrderByDescending( c => c.Value ).ThenBy( c => c.Key ).First().Key;
			primary = Capitalize( Pluralize( best, objectIds.Count ) );
			return primary + " (" + objectIds.Count + ")";
		}

		/// <summary>Strips the OBNA padding (trailing '@' and spaces) from an object name.</summary>
		private static string CleanName( string? name )
		{
			if( string.IsNullOrEmpty( name ) )
			{
				return "";
			}
			return name!.TrimEnd( '@', ' ', '\0' ).Trim();
		}

		private static string Pluralize( string word, int count )
		{
			if( count <= 1 || word.EndsWith( "s", System.StringComparison.OrdinalIgnoreCase ) )
			{
				return word;
			}
			return word + "s";
		}

		private static string Capitalize( string word )
		{
			return word.Length == 0 ? word : char.ToUpperInvariant( word[0] ) + word.Substring( 1 );
		}

		private static string LowerFirst( string word )
		{
			return word.Length == 0 ? word : char.ToLowerInvariant( word[0] ) + word.Substring( 1 );
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

			/// <summary>
			/// The working stack of the expression that is being computed. A null entry is a
			/// value the walk could not compute.
			/// </summary>
			public List<int?> ExpressionStack = new List<int?>();

			/// <summary>The objects whose classes a script changed, so the directory no longer describes them.</summary>
			public HashSet<int> ChangedClasses = new HashSet<int>();

			/// <summary>How this path treats a test it cannot answer. Constant for one evaluation.</summary>
			public ForkPolicy Policy = ForkPolicy.Enumerate;

			/// <summary>
			/// The variables pinned to a fixed value for this evaluation: a read of a locked variable
			/// returns the pinned value and a write to one is ignored (so a boot-script write cannot
			/// overwrite the pin). Shared and never mutated, so a fork keeps the same reference.
			/// </summary>
			public IReadOnlyDictionary<int, int> Locked = EmptyLocked;

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
					ExpressionStack = new List<int?>( this.ExpressionStack ),
					ChangedClasses = new HashSet<int>( this.ChangedClasses ),
					Policy = this.Policy,
					Locked = this.Locked,
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
			Dictionary<int, (int Start, int End)> globalScripts,
			Dictionary<int, ScriptControlFlowGraph>? sharedGraphs = null )
		{
			private readonly byte[] data = data;
			private readonly Dictionary<int, (int Start, int End)> localScripts = localScripts;
			private readonly Dictionary<int, (int Start, int End)> globalScripts = globalScripts;
			private readonly Dictionary<int, ScriptControlFlowGraph> graphs = sharedGraphs ?? new Dictionary<int, ScriptControlFlowGraph>();

			public readonly IReadOnlyDictionary<int, ClassicObjectStartState> StartStates = startStates;
			public readonly Stack<Path> Work = new Stack<Path>();
			public readonly List<Path> Finished = new List<Path>();

			/// <summary>
			/// Every conditional test on a plot atom (a plain game global or a bit) with a literal
			/// comparison value the walk reached, whether or not it forked, keyed by variable and
			/// value. State discovery reads this to learn which plot atoms gate the room and the
			/// threshold values the scripts test them against.
			/// </summary>
			public readonly HashSet<(int Subject, int Value)> PlotTests = new HashSet<(int Subject, int Value)>();

			/// <summary>
			/// Whether the walk ran out of its path budget and had to abandon a fork. State
			/// discovery reports this as an incomplete result: there may be more states than found.
			/// A path that only ran past its own step budget does not set this - that is a per-path
			/// safety net that in practice trips on a trailing wait loop, after the object setup.
			/// </summary>
			public bool ForkBudgetHit;

			/// <summary>
			/// Every script number this walk entered through a startScript call. State discovery
			/// reads it to tell a script's own cascade (the helpers it starts) from an unrelated
			/// script that merely draws a subset of the same objects.
			/// </summary>
			public readonly HashSet<int> EnteredScripts = new HashSet<int>();

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

							// remember every plot-atom test the walk reaches, so state discovery
							// learns which globals and bits gate the room and the values they test
							if( last.Condition != null
								&& last.Condition.Kind == ScriptConditionKind.Variable
								&& last.Condition.ValueIsLiteral
								&& last.Condition.Subject >= 0
								&& IsPlotFree( last.Condition.Subject ) )
							{
								this.PlotTests.Add( ( last.Condition.Subject, last.Condition.Value ) );
							}

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

							// under a restricted fork policy most open tests do not fork: a plot
							// atom forks under PlotOnly, and nothing forks under None. A non-forking
							// test takes its fall-through edge and records that choice, so a wait
							// loop that returns here leaves through the other edge next time instead
							// of spinning
							if( path.Policy != ForkPolicy.Enumerate
								&& !( path.Policy == ForkPolicy.PlotOnly && IsForkableOpenCondition( last.Condition, path ) ) )
							{
								path.OpenChoices[choiceKey] = true;
								Assume( last.Condition, path );
								MoveTo( frame, block.NextBlock );
								continue;
							}

							// the test is open: fork, unless the budget is used up
							if( this.Finished.Count + this.Work.Count + 2 > MaxPaths )
							{
								// no budget: assume the test failed and skip the guarded code
								this.ForkBudgetHit = true;
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
				this.EnteredScripts.Add( scriptId );
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
				case ScriptEventKind.MultiplyVariable:
				case ScriptEventKind.DivideVariable:
				case ScriptEventKind.AndVariable:
				case ScriptEventKind.OrVariable:
				{
					var target = scriptEvent.A.VariableId;
					var operand = Resolve( scriptEvent.B, path );
					var current = target >= 0 ? ResolveVariable( target, path ) : null;
					WriteVariable( target, Combine( scriptEvent.Kind, current, operand ), path );
					break;
				}

				case ScriptEventKind.SetVariableToRandom:
				{
					// the value is not known, but the engine keeps it from 0 to the maximum
					var target = scriptEvent.A.VariableId;
					var maximum = Resolve( scriptEvent.B, path );
					WriteVariable( target, null, path );
					if( target >= 0 && maximum != null && maximum.Value >= 0 )
					{
						path.Ranges[target] = ( 0, maximum.Value );
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

				case ScriptEventKind.ChangeObjectClass:
				{
					var objectId = Resolve( scriptEvent.A, path );
					if( objectId == null )
					{
						path.HasUnknownWrites = true;
					}
					else
					{
						path.ChangedClasses.Add( objectId.Value );
					}
					break;
				}

				case ScriptEventKind.ExpressionPush:
					path.ExpressionStack.Add( Resolve( scriptEvent.A, path ) );
					break;

				case ScriptEventKind.ExpressionApply:
				{
					var stack = path.ExpressionStack;
					if( stack.Count < 2 )
					{
						// a malformed expression: give up on it, but keep the stack usable
						stack.Clear();
						stack.Add( null );
						break;
					}
					var right = stack[stack.Count - 1];
					var left = stack[stack.Count - 2];
					stack.RemoveAt( stack.Count - 1 );
					stack[stack.Count - 1] = Arithmetic( scriptEvent.A.Value, left, right );
					break;
				}

				case ScriptEventKind.ExpressionStore:
				{
					var stack = path.ExpressionStack;
					var computable = scriptEvent.B.Value != 0;
					var result = computable && stack.Count > 0 ? stack[stack.Count - 1] : null;
					stack.Clear();
					WriteVariable( scriptEvent.A.VariableId, result, path );
					break;
				}

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
			if( path.Locked.ContainsKey( variableId ) )
			{
				// a pinned variable holds its value against every write, so a boot-script or
				// entry-script assignment cannot overwrite the assumption the caller pinned
				return;
			}
			if( variableId < 0 )
			{
				// A write whose target is only known when the game runs. The scripts use these
				// to fill their own tables, which sit apart from the plot variables a room test
				// reads, so the walk keeps what it knows and reports the write instead. Making
				// every variable unknown here would make almost every later test unanswerable.
				path.HasUnknownWrites = true;
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
		/// The value of a variable: the tracked value, or 0 for a variable that nothing has
		/// written, or null when the value cannot be known.
		///
		/// The zero default is what a new game gives: the engine clears the whole variable table
		/// before the boot script runs, so a game variable no script has written still holds 0.
		/// It does not apply to the variables the engine writes itself (the timers, the mouse,
		/// the room number), and it does not apply once an operation this walk cannot compute
		/// has written the variable.
		/// </summary>
		private static int? ResolveVariable( int variableId, Path path )
		{
			int locked;
			if( path.Locked.TryGetValue( variableId, out locked ) )
			{
				return locked;
			}
			int value;
			if( path.Variables.TryGetValue( variableId, out value ) )
			{
				return value;
			}
			if( !path.ClobberedVariables.Contains( variableId ) && !IsEngineOwned( variableId ) )
			{
				return 0;
			}
			return null;
		}

		/// <summary>
		/// Whether the engine writes the variable itself. A bit variable (the 0x8000 flag) and a
		/// script's local variable (the 0x4000 flag) always belong to the game.
		/// </summary>
		private static bool IsEngineOwned( int variableId )
		{
			if( ( variableId & 0xC000 ) != 0 )
			{
				return false;
			}
			return ScummEngineVariables.IsEngineVariable( variableId );
		}

		private static int? NullWhenUnknown( int value )
		{
			return value == Unknown ? (int?)null : value;
		}

		/// <summary>
		/// Applies an arithmetic or bitwise operation to a variable. A value the walk does not
		/// know gives null. The engine gives 0 for a division by zero.
		/// </summary>
		private static int? Combine( ScriptEventKind kind, int? current, int? operand )
		{
			if( current == null || operand == null )
			{
				return null;
			}
			switch( kind )
			{
				case ScriptEventKind.AddVariable:
					return current.Value + operand.Value;
				case ScriptEventKind.SubtractVariable:
					return current.Value - operand.Value;
				case ScriptEventKind.MultiplyVariable:
					return current.Value * operand.Value;
				case ScriptEventKind.DivideVariable:
					return operand.Value == 0 ? 0 : current.Value / operand.Value;
				case ScriptEventKind.AndVariable:
					return current.Value & operand.Value;
				case ScriptEventKind.OrVariable:
					return current.Value | operand.Value;
				default:
					return null;
			}
		}

		/// <summary>
		/// One arithmetic step of an expression: 2 adds, 3 subtracts, 4 multiplies and 5
		/// divides. A value the walk does not know, or a division by zero, gives null.
		/// </summary>
		private static int? Arithmetic( int operation, int? left, int? right )
		{
			if( left == null || right == null )
			{
				return null;
			}
			switch( operation )
			{
				case 2:
					return left.Value + right.Value;
				case 3:
					return left.Value - right.Value;
				case 4:
					return left.Value * right.Value;
				case 5:
					return right.Value == 0 ? (int?)null : left.Value / right.Value;
				default:
					return null;
			}
		}

		/// <summary>
		/// Whether the plot-only walk should fork at an open test. It forks only on a test of a
		/// plot atom whose value it does not know while the compared value is known: that is a
		/// genuine story branch. A test whose subject is already known (so the verdict came from a
		/// range, not a real unknown) or whose compared value is itself unknown teaches the fork
		/// nothing, so it does not fork.
		/// </summary>
		private static bool IsForkableOpenCondition( ScriptCondition? condition, Path path )
		{
			if( condition == null || condition.Kind != ScriptConditionKind.Variable )
			{
				return false;
			}
			if( condition.Subject < 0 || !IsPlotFree( condition.Subject ) )
			{
				return false;
			}
			if( ResolveVariable( condition.Subject, path ) != null )
			{
				return false;
			}
			return ResolveConditionValue( condition, path ) != null;
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
			if( condition.Kind == ScriptConditionKind.ObjectClass )
			{
				return condition.ValueIsLiteral ? EvaluateClassCondition( condition, path, startStates ) : null;
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

			if( condition.Kind == ScriptConditionKind.ObjectClass )
			{
				return EvaluateClassCondition( condition, path, startStates );
			}

			return null;
		}

		/// <summary>
		/// Answers an "is this object of these classes" test from the class flags in the object
		/// directory. A class value with the 0x80 flag asks for a class the object must have,
		/// and one without it asks for a class the object must not have (ScummVM o5_ifClassOfIs).
		/// An object whose classes a script changed cannot be answered.
		/// </summary>
		private static bool? EvaluateClassCondition(
			ScriptCondition condition,
			Path path,
			IReadOnlyDictionary<int, ClassicObjectStartState> startStates )
		{
			if( condition.Subject < 0 || condition.ClassValues == null || path.ChangedClasses.Contains( condition.Subject ) )
			{
				return null;
			}
			ClassicObjectStartState startState;
			if( !startStates.TryGetValue( condition.Subject, out startState ) )
			{
				return null;
			}

			var holds = true;
			foreach( var classValue in condition.ClassValues )
			{
				var classNumber = classValue & 0x7F;
				if( classNumber < 1 || classNumber > 32 )
				{
					return null;
				}
				var hasClass = ( startState.ClassFlags & ( 1u << ( classNumber - 1 ) ) ) != 0;
				var wanted = ( classValue & 0x80 ) != 0;
				if( hasClass != wanted )
				{
					holds = false;
					break;
				}
			}

			// the recorded comparison is Equal for the test as read, and Negate turns it around
			return condition.Comparison == ScriptComparison.Equal ? holds : !holds;
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
