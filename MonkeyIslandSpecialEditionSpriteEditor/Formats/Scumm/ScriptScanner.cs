using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm
{
	/// <summary>
	/// Scans the classic SCUMM v5 scripts (monkey1.001) for actor placements. The Special
	/// Edition runs the classic scripts underneath, so the positions where NPCs stand (e.g.
	/// the pirate leaders in the bar) only exist as putActorInRoom/putActor opcodes in the
	/// entry and local scripts. The scanner walks every script with an exact instruction
	/// length decoder (validated to decode all 721 retail scripts to their block end) and
	/// records placements whose arguments are literals; placements computed at runtime from
	/// variables (mostly cutscene movement) cannot be extracted statically and are skipped.
	/// </summary>
	public static class ScriptScanner
	{
		/// <summary>
		/// The result of a scan: placements grouped by room number, plus decode statistics.
		/// </summary>
		public class ScanResult
		{
			/// <summary>
			/// Gets the placements grouped by classic room number, in setup order (entry
			/// script first, then local scripts by number).
			/// </summary>
			public Dictionary<int, List<ClassicActorPlacement>> PlacementsByRoom
			{
				get;
			} = new Dictionary<int, List<ClassicActorPlacement>>();

			/// <summary>
			/// Gets the object visibility changes found in the scripts (setState/drawObject/
			/// pickup/setOwner with literal operands), in scan order. Attributing them to rooms
			/// and reducing them to an initial-visibility set is left to the caller, which has the
			/// object-to-room map. Control-flow blind, so an object may carry conflicting changes.
			/// </summary>
			public List<ObjectDrawChange> ObjectDrawChanges
			{
				get;
			} = new List<ObjectDrawChange>();

			/// <summary>
			/// Gets or sets the total number of scripts found.
			/// </summary>
			public int ScriptCount
			{
				get;
				set;
			}

			/// <summary>
			/// Gets or sets the number of scripts that did not decode cleanly to their block
			/// end (their placements up to the failure point are still used).
			/// </summary>
			public int FailedScriptCount
			{
				get;
				set;
			}
		}

		/// <summary>
		/// Scans the still XOR encoded contents of a resource file (monkey1.001).
		/// The buffer is decoded in place.
		/// </summary>
		public static ScanResult ScanFromEncodedBytes( byte[] bytes )
		{
			Parser.XorDecode( bytes, Parser.XorKey );
			return Scan( bytes );
		}

		/// <summary>
		/// Scans an already decoded resource file for actor placements.
		/// </summary>
		public static ScanResult Scan( byte[] data )
		{
			var result = new ScanResult();
			var scripts = HarvestScripts( data );
			result.ScriptCount = scripts.Count;

			var placements = new List<PlacementRecord>();
			var assignments = new List<CostumeAssignment>();
			var order = 0;
			var objectOrder = 0;
			foreach( var script in scripts )
			{
				var decoder = new ScriptDecoder( data, script.Start, script.End );
				if( !decoder.DecodeScript() )
				{
					result.FailedScriptCount++;
				}
				ScanEvents( decoder.Events, script, placements, assignments, ref order );
				CollectObjectDrawChanges( decoder.Events, script, result.ObjectDrawChanges, ref objectOrder );
			}

			ResolveCostumeFallbacks( placements, assignments );

			foreach( var record in placements.OrderBy( p => p.Order ) )
			{
				if( !IsUsablePlacement( record ) )
				{
					continue;
				}

				List<ClassicActorPlacement> list;
				if( !result.PlacementsByRoom.TryGetValue( record.RoomNumber!.Value, out list ) )
				{
					list = new List<ClassicActorPlacement>();
					result.PlacementsByRoom.Add( record.RoomNumber.Value, list );
				}

				// drop exact duplicates (the same setup often runs from several branches)
				if( list.Any( existing =>
					existing.ActorNumber == record.Placement.ActorNumber
					&& existing.X == record.Placement.X
					&& existing.Y == record.Placement.Y
					&& existing.CostumeId == record.Placement.CostumeId ) )
				{
					continue;
				}

				list.Add( record.Placement );
			}

			return result;
		}

		private static bool IsUsablePlacement( PlacementRecord record )
		{
			var placement = record.Placement;
			if( record.RoomNumber == null || record.RoomNumber <= 0 )
			{
				return false;
			}
			if( placement.ActorNumber <= 0 )
			{
				return false;
			}

			// (0,0) is where scripts park actors that are about to be moved by variables
			if( placement.X == 0 && placement.Y == 0 )
			{
				return false;
			}
			return placement.X >= 0 && placement.Y >= 0 && placement.X < 3200 && placement.Y < 800;
		}

		//-------------------------------------------
		// script discovery

		private enum ScriptKind
		{
			Entry = 0,
			Local = 1,
			Exit = 2,
			Global = 3,
		}

		private class ScriptInfo(
			ScriptScanner.ScriptKind kind,
			int? roomNumber,
			int scriptId,
			int start,
			int end
		)
		{
			public ScriptKind Kind = kind;

			/// <summary>
			/// The room whose LFLF the script is stored in; only entry, exit and local
			/// scripts actually belong to that room.
			/// </summary>
			public int? RoomNumber = roomNumber;

			/// <summary>
			/// The local script number (200+), or 0 for unnumbered scripts.
			/// </summary>
			public int ScriptId = scriptId;

			public int Start = start;
			public int End = end;

			public string Describe()
			{
				switch( this.Kind )
				{
					case ScriptKind.Entry:
						return "entry script";
					case ScriptKind.Exit:
						return "exit script";
					case ScriptKind.Local:
						return string.Concat( "local script ", this.ScriptId );
					default:
						return "global script";
				}
			}
		}

		private struct BlockInfo
		{
			public string Tag;
			public int Position;
			public int Size;
		}

		private static IEnumerable<BlockInfo> ReadBlocks( byte[] data, int start, int end )
		{
			var position = start;
			while( position + 8 <= end )
			{
				var tag = Encoding.ASCII.GetString( data, position, 4 );
				var size = ( data[position + 4] << 24 ) | ( data[position + 5] << 16 )
					| ( data[position + 6] << 8 ) | data[position + 7];
				if( size < 8 || position + size > end )
				{
					yield break;
				}
				yield return new BlockInfo
				{
					Tag = tag,
					Position = position,
					Size = size,
				};
				position += size;
			}
		}

		private static List<ScriptInfo> HarvestScripts( byte[] data )
		{
			var scripts = new List<ScriptInfo>();
			var roomNumberByOffset = new Dictionary<int, int>();

			foreach( var lecf in ReadBlocks( data, 0, data.Length ) )
			{
				if( lecf.Tag != "LECF" )
				{
					continue;
				}

				foreach( var child in ReadBlocks( data, lecf.Position + 8, lecf.Position + lecf.Size ) )
				{
					if( child.Tag == "LOFF" )
					{
						var position = child.Position + 8;
						int count = data[position];
						position += 1;
						for( var index = 0; index < count; index++ )
						{
							int roomNumber = data[position];
							var offset = BitConverter.ToInt32( data, position + 1 );
							roomNumberByOffset[offset] = roomNumber;
							position += 5;
						}
					}
					else if( child.Tag == "LFLF" )
					{
						HarvestLflfScripts( data, child, roomNumberByOffset, scripts );
					}
				}
			}

			return scripts
				.OrderBy( s => s.RoomNumber ?? int.MaxValue )
				.ThenBy( s => (int)s.Kind )
				.ThenBy( s => s.ScriptId )
				.ToList();
		}

		private static void HarvestLflfScripts( byte[] data, BlockInfo lflf, Dictionary<int, int> roomNumberByOffset, List<ScriptInfo> scripts )
		{
			int? roomNumber = null;
			foreach( var child in ReadBlocks( data, lflf.Position + 8, lflf.Position + lflf.Size ) )
			{
				if( child.Tag == "ROOM" )
				{
					int found;
					if( roomNumberByOffset.TryGetValue( child.Position, out found )
						|| roomNumberByOffset.TryGetValue( lflf.Position + 8, out found )
						|| roomNumberByOffset.TryGetValue( lflf.Position, out found ) )
					{
						roomNumber = found;
					}

					foreach( var roomChild in ReadBlocks( data, child.Position + 8, child.Position + child.Size ) )
					{
						if( roomChild.Tag == "ENCD" )
						{
							scripts.Add( new ScriptInfo( ScriptKind.Entry, roomNumber, 0, roomChild.Position + 8, roomChild.Position + roomChild.Size ) );
						}
						else if( roomChild.Tag == "EXCD" )
						{
							scripts.Add( new ScriptInfo( ScriptKind.Exit, roomNumber, 0, roomChild.Position + 8, roomChild.Position + roomChild.Size ) );
						}
						else if( roomChild.Tag == "LSCR" && roomChild.Size > 9 )
						{
							// the first payload byte is the local script number
							scripts.Add( new ScriptInfo( ScriptKind.Local, roomNumber, data[roomChild.Position + 8], roomChild.Position + 9, roomChild.Position + roomChild.Size ) );
						}
					}
				}
				else if( child.Tag == "SCRP" )
				{
					scripts.Add( new ScriptInfo( ScriptKind.Global, roomNumber, 0, child.Position + 8, child.Position + child.Size ) );
				}
			}
		}

		//-------------------------------------------
		// event scanning

		private class PlacementRecord(
			ClassicActorPlacement placement,
			int? roomNumber,
			int order
		)
		{
			public ClassicActorPlacement Placement = placement;
			public int? RoomNumber = roomNumber;
			public int Order = order;
		}

		private class CostumeAssignment(
			int actorNumber,
			int costumeId,
			int? roomNumber
		)
		{
			public int ActorNumber = actorNumber;
			public int CostumeId = costumeId;

			/// <summary>
			/// The room whose entry/exit/local script made the assignment; null for global scripts.
			/// </summary>
			public int? RoomNumber = roomNumber;
		}

		/// <summary>
		/// Linearly replays one script's actor events, ignoring control flow: literal state
		/// (room, costume, elevation) is tracked per actor, and each literal putActor becomes
		/// a placement. Costume and direction changes right after a placement (the common
		/// "place, then dress and face" pattern) update that placement.
		/// </summary>
		private static void ScanEvents(
			List<ScriptEvent> events,
			ScriptInfo script,
			List<PlacementRecord> placements,
			List<CostumeAssignment> assignments,
			ref int order )
		{
			var roomOf = new Dictionary<int, int>();
			var costumeOf = new Dictionary<int, int>();
			var elevationOf = new Dictionary<int, int>();
			var lastPlacement = new Dictionary<int, ClassicActorPlacement>();
			var assignmentRoom = script.Kind == ScriptKind.Global ? (int?)null : script.RoomNumber;

			foreach( var scriptEvent in events )
			{
				switch( scriptEvent.Kind )
				{
					case ScriptEventKind.SetCostume:
					{
						if( !scriptEvent.A.IsLiteral || !scriptEvent.B.IsLiteral )
						{
							break;
						}
						var actor = scriptEvent.A.Value;
						var costume = scriptEvent.B.Value;
						assignments.Add( new CostumeAssignment( actor, costume, assignmentRoom ) );
						costumeOf[actor] = costume;

						ClassicActorPlacement placed;
						if( lastPlacement.TryGetValue( actor, out placed ) && placed.CostumeId == null )
						{
							placed.CostumeId = costume;
						}
						break;
					}
					case ScriptEventKind.SetElevation:
					{
						if( !scriptEvent.A.IsLiteral || !scriptEvent.B.IsLiteral )
						{
							break;
						}
						var actor = scriptEvent.A.Value;
						elevationOf[actor] = scriptEvent.B.Value;

						ClassicActorPlacement placed;
						if( lastPlacement.TryGetValue( actor, out placed ) )
						{
							placed.Elevation = scriptEvent.B.Value;
						}
						break;
					}
					case ScriptEventKind.PutActorInRoom:
					{
						if( scriptEvent.A.IsLiteral && scriptEvent.B.IsLiteral )
						{
							roomOf[scriptEvent.A.Value] = scriptEvent.B.Value;
						}
						break;
					}
					case ScriptEventKind.PutActor:
					{
						if( !scriptEvent.A.IsLiteral || !scriptEvent.B.IsLiteral || !scriptEvent.C.IsLiteral )
						{
							break;
						}
						var actor = scriptEvent.A.Value;

						// a room set in this script wins; otherwise a room-owned script
						// places actors in its own room
						int roomNumber;
						int? placementRoom = null;
						if( roomOf.TryGetValue( actor, out roomNumber ) )
						{
							placementRoom = roomNumber;
						}
						else if( script.Kind == ScriptKind.Entry || script.Kind == ScriptKind.Local )
						{
							placementRoom = script.RoomNumber;
						}

						int costume;
						int elevation;
						var placement = new ClassicActorPlacement(
							actorNumber: actor,
							x: scriptEvent.B.Value,
							y: scriptEvent.C.Value,
							costumeId: costumeOf.TryGetValue( actor, out costume ) ? costume : (int?)null,
							costumeInferred: false,
							direction: null,
							elevation: elevationOf.TryGetValue( actor, out elevation ) ? elevation : (int?)null,
							source: script.Describe()
						);
						placements.Add( new PlacementRecord( placement, placementRoom, order++ ) );
						lastPlacement[actor] = placement;
						break;
					}
					case ScriptEventKind.AnimateActor:
					{
						if( !scriptEvent.A.IsLiteral || !scriptEvent.B.IsLiteral )
						{
							break;
						}
						var anim = scriptEvent.B.Value;

						// 244..255 are the turn/face commands (direction = anim & 3); the
						// low chore animations (init/walk/stand) also encode a direction
						ClassicActorPlacement placed;
						if( lastPlacement.TryGetValue( scriptEvent.A.Value, out placed )
							&& ( ( anim >= 244 && anim <= 255 ) || ( anim >= 4 && anim < 16 ) ) )
						{
							placed.Direction = anim & 3;
						}
						break;
					}
				}
			}
		}

		/// <summary>
		/// Records one script's object visibility opcodes (setState/drawObject/pickup/setOwner)
		/// as <see cref="ObjectDrawChange"/> records, keeping only the ones with literal object
		/// operands. Control flow is ignored, so both branches of a conditional are recorded.
		/// </summary>
		private static void CollectObjectDrawChanges(
			List<ScriptEvent> events,
			ScriptInfo script,
			List<ObjectDrawChange> changes,
			ref int order )
		{
			var sourceKind = ToSourceKind( script.Kind );
			foreach( var scriptEvent in events )
			{
				if( !scriptEvent.A.IsLiteral )
				{
					continue;
				}
				var objectId = scriptEvent.A.Value;

				switch( scriptEvent.Kind )
				{
					case ScriptEventKind.SetObjectState:
						if( scriptEvent.B.IsLiteral )
						{
							changes.Add( new ObjectDrawChange( objectId, ObjectDrawKind.SetState, scriptEvent.B.Value, sourceKind, script.RoomNumber, script.ScriptId, order++ ) );
						}
						break;
					case ScriptEventKind.DrawObject:
						// drawObject shows the object; a literal "set state" form carries the state, else treat as drawn (1)
						changes.Add( new ObjectDrawChange( objectId, ObjectDrawKind.Draw, scriptEvent.B.IsLiteral ? scriptEvent.B.Value : 1, sourceKind, script.RoomNumber, script.ScriptId, order++ ) );
						break;
					case ScriptEventKind.PickupObject:
						changes.Add( new ObjectDrawChange( objectId, ObjectDrawKind.Pickup, 0, sourceKind, script.RoomNumber, script.ScriptId, order++ ) );
						break;
					case ScriptEventKind.SetOwnerOf:
						if( scriptEvent.B.IsLiteral )
						{
							changes.Add( new ObjectDrawChange( objectId, ObjectDrawKind.SetOwner, scriptEvent.B.Value, sourceKind, script.RoomNumber, script.ScriptId, order++ ) );
						}
						break;
				}
			}
		}

		private static ScriptSourceKind ToSourceKind( ScriptKind kind )
		{
			switch( kind )
			{
				case ScriptKind.Entry:
					return ScriptSourceKind.Entry;
				case ScriptKind.Exit:
					return ScriptSourceKind.Exit;
				case ScriptKind.Local:
					return ScriptSourceKind.Local;
				default:
					return ScriptSourceKind.Global;
			}
		}

		/// <summary>
		/// Fills in costumes for placements whose script never assigned one: an assignment
		/// from another script of the same room wins, then a globally unambiguous one.
		/// </summary>
		private static void ResolveCostumeFallbacks( List<PlacementRecord> placements, List<CostumeAssignment> assignments )
		{
			foreach( var record in placements )
			{
				if( record.Placement.CostumeId != null || record.RoomNumber == null )
				{
					continue;
				}

				var sameRoom = assignments
					.Where( a => a.ActorNumber == record.Placement.ActorNumber && a.RoomNumber == record.RoomNumber )
					.Select( a => a.CostumeId )
					.Distinct()
					.ToList();
				var candidates = sameRoom.Count > 0
					? sameRoom
					: assignments
						.Where( a => a.ActorNumber == record.Placement.ActorNumber )
						.Select( a => a.CostumeId )
						.Distinct()
						.ToList();

				if( candidates.Count == 1 )
				{
					record.Placement.CostumeId = candidates[0];
					record.Placement.CostumeInferred = true;
				}
			}
		}
	}

	//-------------------------------------------
	// bytecode decoding

	internal enum ScriptEventKind
	{
		PutActor,
		PutActorInRoom,
		SetCostume,
		SetElevation,
		AnimateActor,
		SetObjectState,
		DrawObject,
		PickupObject,
		SetOwnerOf,
	}

	/// <summary>
	/// One decoded actor-related opcode with its (possibly variable) operands.
	/// </summary>
	internal class ScriptEvent(
		ScriptEventKind kind,
		ScriptDecoder.Operand a,
		ScriptDecoder.Operand b,
		ScriptDecoder.Operand c
	)
	{
		public ScriptEventKind Kind = kind;
		public ScriptDecoder.Operand A = a;
		public ScriptDecoder.Operand B = b;
		public ScriptDecoder.Operand C = c;
	}

	/// <summary>
	/// Walks SCUMM v5 bytecode instruction by instruction, collecting the actor-related
	/// opcodes as events. The opcode table mirrors ScummVM's script_v5.cpp: the low 7 bits
	/// select the operation and the 0x80 (and per-opcode 0x40/0x20) bits mark parameters as
	/// variables, except for a handful of full-byte slots (breakHere, systemOps, expression,
	/// wait, ...) where the 0x80 half hosts a different operation. Decoding stops at the
	/// first unknown opcode, keeping the events found so far.
	/// </summary>
	internal class ScriptDecoder( byte[] data, int start, int end )
	{
		public struct Operand
		{
			public int Value;
			public bool IsLiteral;
		}

		private class DecodeException( string message ) : Exception( message );

		private readonly byte[] data = data;
		private int position = start;
		private readonly int end = end;

		public List<ScriptEvent> Events
		{
			get;
		} = new List<ScriptEvent>();

		/// <summary>
		/// Decodes the whole script. Returns true when every instruction decoded and the
		/// script ended exactly at the block end.
		/// </summary>
		public bool DecodeScript()
		{
			try
			{
				while( this.position < this.end )
				{
					this.DecodeInstruction();
				}
				return this.position == this.end;
			}
			catch( DecodeException )
			{
				return false;
			}
		}

		//-------------------------------------------
		// primitives

		private byte ReadByte()
		{
			if( this.position >= this.end )
			{
				throw new DecodeException( "read past end of script" );
			}
			return this.data[this.position++];
		}

		private int ReadWord()
		{
			if( this.position + 2 > this.end )
			{
				throw new DecodeException( "read past end of script" );
			}
			var value = this.data[this.position] | ( this.data[this.position + 1] << 8 );
			this.position += 2;
			return value;
		}

		private int ReadSignedWord()
		{
			return (short)this.ReadWord();
		}

		/// <summary>
		/// Reads a variable reference. Indexed variables (bit 0x2000) consume exactly one
		/// extra word: the engine clears the bit before resolving the index, so the chain
		/// never continues.
		/// </summary>
		private void ReadVariable()
		{
			var variable = this.ReadWord();
			if( ( variable & 0x2000 ) != 0 )
			{
				this.ReadWord();
			}
		}

		private void ReadResultVariable()
		{
			this.ReadVariable();
		}

		/// <summary>getVarOrDirectByte: a literal byte, or a variable when the mask bit is set.</summary>
		private Operand VarOrByte( int opcode, int mask )
		{
			if( ( opcode & mask ) != 0 )
			{
				this.ReadVariable();
				return new Operand();
			}
			return new Operand
			{
				Value = this.ReadByte(),
				IsLiteral = true,
			};
		}

		/// <summary>getVarOrDirectWord: a literal signed word, or a variable when the mask bit is set.</summary>
		private Operand VarOrWord( int opcode, int mask )
		{
			if( ( opcode & mask ) != 0 )
			{
				this.ReadVariable();
				return new Operand();
			}
			return new Operand
			{
				Value = this.ReadSignedWord(),
				IsLiteral = true,
			};
		}

		/// <summary>A 0xFF terminated list of word values, each prefixed by its own parameter byte.</summary>
		private void VarArgList()
		{
			for( var count = 0; count < 33; count++ )
			{
				var aux = this.ReadByte();
				if( aux == 0xFF )
				{
					return;
				}
				this.VarOrWord( aux, 0x80 );
			}
			throw new DecodeException( "unterminated argument list" );
		}

		/// <summary>A zero terminated string; 0xFF escapes outside {1,2,3,8} carry two payload bytes.</summary>
		private void SkipString()
		{
			while( true )
			{
				var character = this.ReadByte();
				if( character == 0 )
				{
					return;
				}
				if( character == 0xFF )
				{
					var code = this.ReadByte();
					if( code != 1 && code != 2 && code != 3 && code != 8 )
					{
						this.ReadByte();
						this.ReadByte();
					}
				}
			}
		}

		private void ReadJumpOffset()
		{
			this.ReadSignedWord();
		}

		private void AddEvent( ScriptEventKind kind, Operand a, Operand b = default, Operand c = default )
		{
			this.Events.Add( new ScriptEvent( kind, a, b, c ) );
		}

		//-------------------------------------------
		// dispatch

		private void DecodeInstruction()
		{
			var opcode = this.ReadByte();

			// full-byte slots where the 0x80 half hosts a different operation than the
			// (opcode & 0x7F) table entry
			switch( opcode )
			{
				case 0x80:                                        // breakHere
					return;
				case 0x98:                                        // systemOps
					this.ReadByte();
					return;
				case 0xA0:                                        // stopObjectCode
					return;
				case 0xA7:                                        // dummy
					return;
				case 0xA8:                                        // notEqualZero
					this.ReadVariable();
					this.ReadJumpOffset();
					return;
				case 0xAB:                                        // saveRestoreVerbs
					this.SaveRestoreVerbs();
					return;
				case 0xAC:                                        // expression
					this.Expression();
					return;
				case 0xAE:                                        // wait
					this.Wait();
					return;
				case 0xC0:                                        // endCutscene
					return;
				case 0xC6:                                        // decrement
					this.ReadResultVariable();
					return;
				case 0xCC:                                        // pseudoRoom
					this.PseudoRoom();
					return;
				case 0xD8:                                        // printEgo
					this.ParseString();
					return;
			}

			// the regular table: the low 7 bits select the operation (ScummVM script_v5.cpp)
			switch( opcode & 0x7F )
			{
				case 0x00:                                        // stopObjectCode
				case 0x20:                                        // stopMusic
				case 0x40:                                        // cutscene
					if( ( opcode & 0x7F ) == 0x40 )
					{
						this.VarArgList();
					}
					return;
				case 0x60:                                        // freezeScripts
					this.VarOrByte( opcode, 0x80 );
					return;

				case 0x01:                                        // putActor
				case 0x21:
				case 0x41:
				case 0x61:
				{
					var actor = this.VarOrByte( opcode, 0x80 );
					var x = this.VarOrWord( opcode, 0x40 );
					var y = this.VarOrWord( opcode, 0x20 );
					this.AddEvent( ScriptEventKind.PutActor, actor, x, y );
					return;
				}

				case 0x02:                                        // startMusic
					this.VarOrByte( opcode, 0x80 );
					return;
				case 0x22:                                        // getAnimCounter
					this.ReadResultVariable();
					this.VarOrByte( opcode, 0x80 );
					return;
				case 0x42:                                        // chainScript
					this.VarOrByte( opcode, 0x80 );
					this.VarArgList();
					return;
				case 0x62:                                        // stopScript
					this.VarOrByte( opcode, 0x80 );
					return;

				case 0x03:                                        // getActorRoom
				case 0x63:                                        // getActorFacing
					this.ReadResultVariable();
					this.VarOrByte( opcode, 0x80 );
					return;
				case 0x23:                                        // getActorY (takes a word in v5)
				case 0x43:                                        // getActorX
					this.ReadResultVariable();
					this.VarOrWord( opcode, 0x80 );
					return;

				case 0x04:                                        // isGreaterEqual
				case 0x44:                                        // isLess
				case 0x08:                                        // isNotEqual
				case 0x48:                                        // isEqual
				case 0x38:                                        // lessOrEqual
				case 0x78:                                        // isGreater
					this.ReadVariable();
					this.VarOrWord( opcode, 0x80 );
					this.ReadJumpOffset();
					return;

				case 0x24:                                        // loadRoomWithEgo
				case 0x64:
					this.VarOrWord( opcode, 0x80 );
					this.VarOrByte( opcode, 0x40 );
					this.ReadSignedWord();
					this.ReadSignedWord();
					return;

				case 0x05:                                        // drawObject
				case 0x45:
					this.DrawObject( opcode );
					return;
				case 0x25:                                        // pickupObject
				case 0x65:
				{
					var pickedObject = this.VarOrWord( opcode, 0x80 );
					this.VarOrByte( opcode, 0x40 );
					this.AddEvent( ScriptEventKind.PickupObject, pickedObject );
					return;
				}

				case 0x06:                                        // getActorElevation
					this.ReadResultVariable();
					this.VarOrByte( opcode, 0x80 );
					return;
				case 0x26:                                        // setVarRange
					this.SetVarRange( opcode );
					return;
				case 0x46:                                        // increment
					this.ReadResultVariable();
					return;
				case 0x66:                                        // getClosestObjActor (takes a word)
					this.ReadResultVariable();
					this.VarOrWord( opcode, 0x80 );
					return;

				case 0x07:                                        // setState
				case 0x47:
				{
					var stateObject = this.VarOrWord( opcode, 0x80 );
					var state = this.VarOrByte( opcode, 0x40 );
					this.AddEvent( ScriptEventKind.SetObjectState, stateObject, state );
					return;
				}
				case 0x27:                                        // stringOps
					this.StringOps();
					return;
				case 0x67:                                        // getStringWidth
					this.ReadResultVariable();
					this.VarOrByte( opcode, 0x80 );
					return;

				case 0x28:                                        // equalZero
					this.ReadVariable();
					this.ReadJumpOffset();
					return;
				case 0x68:                                        // isScriptRunning
					this.ReadResultVariable();
					this.VarOrByte( opcode, 0x80 );
					return;

				case 0x09:                                        // faceActor
				case 0x49:
					this.VarOrByte( opcode, 0x80 );
					this.VarOrWord( opcode, 0x40 );
					return;
				case 0x29:                                        // setOwnerOf
				case 0x69:
				{
					var ownedObject = this.VarOrWord( opcode, 0x80 );
					var owner = this.VarOrByte( opcode, 0x40 );
					this.AddEvent( ScriptEventKind.SetOwnerOf, ownedObject, owner );
					return;
				}

				case 0x0A:                                        // startScript
				case 0x2A:
				case 0x4A:
				case 0x6A:
					this.VarOrByte( opcode, 0x80 );
					this.VarArgList();
					return;

				case 0x0B:                                        // getVerbEntrypoint
				case 0x4B:
					this.ReadResultVariable();
					this.VarOrWord( opcode, 0x80 );
					this.VarOrWord( opcode, 0x40 );
					return;
				case 0x2B:                                        // delayVariable
					this.ReadVariable();
					return;
				case 0x6B:                                        // debug
					this.VarOrWord( opcode, 0x80 );
					return;

				case 0x0C:                                        // resourceRoutines
					this.ResourceRoutines();
					return;
				case 0x2C:                                        // cursorCommand
					this.CursorCommand();
					return;
				case 0x4C:                                        // soundKludge
					this.VarArgList();
					return;
				case 0x6C:                                        // getActorWidth
					this.ReadResultVariable();
					this.VarOrByte( opcode, 0x80 );
					return;

				case 0x0D:                                        // walkActorToActor
				case 0x4D:
					this.VarOrByte( opcode, 0x80 );
					this.VarOrByte( opcode, 0x40 );
					this.ReadByte();
					return;
				case 0x2D:                                        // putActorInRoom
				case 0x6D:
				{
					var actor = this.VarOrByte( opcode, 0x80 );
					var room = this.VarOrByte( opcode, 0x40 );
					this.AddEvent( ScriptEventKind.PutActorInRoom, actor, room );
					return;
				}

				case 0x0E:                                        // putActorAtObject
				case 0x4E:
					this.VarOrByte( opcode, 0x80 );
					this.VarOrWord( opcode, 0x40 );
					return;
				case 0x2E:                                        // delay (three raw bytes)
					this.ReadByte();
					this.ReadByte();
					this.ReadByte();
					return;
				case 0x6E:                                        // stopObjectScript
					this.VarOrWord( opcode, 0x80 );
					return;

				case 0x0F:                                        // getObjectState
					this.ReadResultVariable();
					this.VarOrWord( opcode, 0x80 );
					return;
				case 0x2F:                                        // ifNotState
				case 0x4F:                                        // ifState
				case 0x6F:                                        // ifNotState
					this.VarOrWord( opcode, 0x80 );
					this.VarOrByte( opcode, 0x40 );
					this.ReadJumpOffset();
					return;

				case 0x10:                                        // getObjectOwner
					this.ReadResultVariable();
					this.VarOrWord( opcode, 0x80 );
					return;
				case 0x30:                                        // matrixOps
					this.MatrixOps();
					return;
				case 0x50:                                        // pickupObjectOld
				{
					var pickedObject = this.VarOrWord( opcode, 0x80 );
					this.AddEvent( ScriptEventKind.PickupObject, pickedObject );
					return;
				}
				case 0x70:                                        // lights
					this.VarOrByte( opcode, 0x80 );
					this.ReadByte();
					this.ReadByte();
					return;

				case 0x11:                                        // animateActor
				case 0x51:
				{
					var actor = this.VarOrByte( opcode, 0x80 );
					var animation = this.VarOrByte( opcode, 0x40 );
					this.AddEvent( ScriptEventKind.AnimateActor, actor, animation );
					return;
				}
				case 0x31:                                        // getInventoryCount
				case 0x71:                                        // getActorCostume
					this.ReadResultVariable();
					this.VarOrByte( opcode, 0x80 );
					return;

				case 0x12:                                        // panCameraTo
				case 0x32:                                        // setCameraAt
					this.VarOrWord( opcode, 0x80 );
					return;
				case 0x52:                                        // actorFollowCamera
				case 0x72:                                        // loadRoom
					this.VarOrByte( opcode, 0x80 );
					return;

				case 0x13:                                        // actorOps
				case 0x53:
					this.ActorOps( opcode );
					return;
				case 0x33:                                        // roomOps
				case 0x73:
					this.RoomOps();
					return;

				case 0x14:                                        // print
					this.VarOrByte( opcode, 0x80 );
					this.ParseString();
					return;
				case 0x34:                                        // getDist
				case 0x74:
					this.ReadResultVariable();
					this.VarOrWord( opcode, 0x80 );
					this.VarOrWord( opcode, 0x40 );
					return;
				case 0x54:                                        // setObjectName
					this.VarOrWord( opcode, 0x80 );
					this.SkipString();
					return;

				case 0x15:                                        // actorFromPos
				case 0x55:
					this.ReadResultVariable();
					this.VarOrWord( opcode, 0x80 );
					this.VarOrWord( opcode, 0x40 );
					return;
				case 0x35:                                        // findObject
				case 0x75:
					this.ReadResultVariable();
					this.VarOrByte( opcode, 0x80 );
					this.VarOrByte( opcode, 0x40 );
					return;

				case 0x16:                                        // getRandomNr
				case 0x56:                                        // getActorMoving
					this.ReadResultVariable();
					this.VarOrByte( opcode, 0x80 );
					return;
				case 0x36:                                        // walkActorToObject
				case 0x76:
					this.VarOrByte( opcode, 0x80 );
					this.VarOrWord( opcode, 0x40 );
					return;

				case 0x17:                                        // and
				case 0x57:                                        // or
				case 0x1A:                                        // move
				case 0x3A:                                        // subtract
				case 0x5A:                                        // add
				case 0x1B:                                        // multiply
				case 0x5B:                                        // divide
					this.ReadResultVariable();
					this.VarOrWord( opcode, 0x80 );
					return;
				case 0x37:                                        // startObject
				case 0x77:
					this.VarOrWord( opcode, 0x80 );
					this.VarOrByte( opcode, 0x40 );
					this.VarArgList();
					return;

				case 0x18:                                        // jumpRelative
					this.ReadJumpOffset();
					return;
				case 0x58:                                        // beginOverride / endOverride
					this.ReadByte();
					return;

				case 0x19:                                        // doSentence
				case 0x39:
				case 0x59:
				case 0x79:
					this.DoSentence( opcode );
					return;

				case 0x3B:                                        // getActorScale
				case 0x7B:                                        // getActorWalkBox
					this.ReadResultVariable();
					this.VarOrByte( opcode, 0x80 );
					return;

				case 0x1C:                                        // startSound
				case 0x3C:                                        // stopSound
					this.VarOrByte( opcode, 0x80 );
					return;
				case 0x5C:                                        // oldRoomEffect
					this.OldRoomEffect();
					return;
				case 0x7C:                                        // isSoundRunning
					this.ReadResultVariable();
					this.VarOrByte( opcode, 0x80 );
					return;

				case 0x1D:                                        // ifClassOfIs
					this.VarOrWord( opcode, 0x80 );
					this.VarArgList();
					this.ReadJumpOffset();
					return;
				case 0x3D:                                        // findInventory
				case 0x7D:
					this.ReadResultVariable();
					this.VarOrByte( opcode, 0x80 );
					this.VarOrByte( opcode, 0x40 );
					return;
				case 0x5D:                                        // setClass
					this.VarOrWord( opcode, 0x80 );
					this.VarArgList();
					return;

				case 0x1E:                                        // walkActorTo
				case 0x3E:
				case 0x5E:
				case 0x7E:
					this.VarOrByte( opcode, 0x80 );
					this.VarOrWord( opcode, 0x40 );
					this.VarOrWord( opcode, 0x20 );
					return;

				case 0x1F:                                        // isActorInBox
				case 0x5F:
					this.VarOrByte( opcode, 0x80 );
					this.VarOrByte( opcode, 0x40 );
					this.ReadJumpOffset();
					return;
				case 0x3F:                                        // drawBox
				case 0x7F:
					this.DrawBox( opcode );
					return;

				case 0x7A:                                        // verbOps
					this.VerbOps( opcode );
					return;

				default:
					throw new DecodeException( string.Concat( "unhandled opcode 0x", opcode.ToString( "X2" ) ) );
			}
		}

		//-------------------------------------------
		// multi-part opcodes

		private void DrawObject( int opcode )
		{
			var drawnObject = this.VarOrWord( opcode, 0x80 );
			var subOpcode = this.ReadByte();
			var state = default( Operand );
			switch( subOpcode & 0x1F )
			{
				case 1:                                           // draw at position
					this.VarOrWord( subOpcode, 0x80 );
					this.VarOrWord( subOpcode, 0x40 );
					break;
				case 2:                                           // set state
					state = this.VarOrWord( subOpcode, 0x80 );
					break;
				case 0x1F:                                        // no parameters
					break;
				default:
					throw new DecodeException( "unhandled drawObject sub-opcode" );
			}
			// drawObject makes the object visible (state carried when the "set state" form is used)
			this.AddEvent( ScriptEventKind.DrawObject, drawnObject, state );
		}

		private void SetVarRange( int opcode )
		{
			this.ReadResultVariable();
			int count = this.ReadByte();
			for( var index = 0; index < count; index++ )
			{
				if( ( opcode & 0x80 ) != 0 )
				{
					this.ReadWord();
				}
				else
				{
					this.ReadByte();
				}
			}
		}

		private void StringOps()
		{
			var subOpcode = this.ReadByte();
			switch( subOpcode & 0x1F )
			{
				case 1:                                           // store string
					this.VarOrByte( subOpcode, 0x80 );
					this.SkipString();
					return;
				case 2:                                           // copy string
				case 5:                                           // create empty string
					this.VarOrByte( subOpcode, 0x80 );
					this.VarOrByte( subOpcode, 0x40 );
					return;
				case 3:                                           // set string char
					this.VarOrByte( subOpcode, 0x80 );
					this.VarOrByte( subOpcode, 0x40 );
					this.VarOrByte( subOpcode, 0x20 );
					return;
				case 4:                                           // get string char
					this.ReadResultVariable();
					this.VarOrByte( subOpcode, 0x80 );
					this.VarOrByte( subOpcode, 0x40 );
					return;
				default:
					throw new DecodeException( "unhandled stringOps sub-opcode" );
			}
		}

		private void CursorCommand()
		{
			var subOpcode = this.ReadByte();
			switch( subOpcode & 0x1F )
			{
				case 1:                                           // cursor show
				case 2:                                           // cursor hide
				case 3:                                           // userput on
				case 4:                                           // userput off
				case 5:                                           // cursor soft on
				case 6:                                           // cursor soft off
				case 7:                                           // userput soft on
				case 8:                                           // userput soft off
					return;
				case 10:                                          // set cursor image
					this.VarOrByte( subOpcode, 0x80 );
					this.VarOrByte( subOpcode, 0x40 );
					return;
				case 11:                                          // set cursor hotspot
					this.VarOrByte( subOpcode, 0x80 );
					this.VarOrByte( subOpcode, 0x40 );
					this.VarOrByte( subOpcode, 0x20 );
					return;
				case 12:                                          // init cursor
				case 13:                                          // init charset
					this.VarOrByte( subOpcode, 0x80 );
					return;
				case 14:                                          // load charset
					this.VarArgList();
					return;
				default:
					throw new DecodeException( "unhandled cursorCommand sub-opcode" );
			}
		}

		private void MatrixOps()
		{
			var subOpcode = this.ReadByte();
			switch( subOpcode & 0x1F )
			{
				case 1:                                           // set box flags
				case 2:                                           // set box scale
				case 3:                                           // set box scale slot
					this.VarOrByte( subOpcode, 0x80 );
					this.VarOrByte( subOpcode, 0x40 );
					return;
				case 4:                                           // create box matrix
					return;
				default:
					throw new DecodeException( "unhandled matrixOps sub-opcode" );
			}
		}

		private void RoomOps()
		{
			var subOpcode = this.ReadByte();
			switch( subOpcode & 0x1F )
			{
				case 1:                                           // room scroll
				case 2:                                           // room color
				case 3:                                           // set screen
					this.VarOrWord( subOpcode, 0x80 );
					this.VarOrWord( subOpcode, 0x40 );
					return;
				case 4:                                           // set palette color
				{
					this.VarOrWord( subOpcode, 0x80 );
					this.VarOrWord( subOpcode, 0x40 );
					this.VarOrWord( subOpcode, 0x20 );
					var second = this.ReadByte();
					this.VarOrByte( second, 0x80 );
					return;
				}
				case 5:                                           // shake on
				case 6:                                           // shake off
					return;
				case 7:                                           // room scale
				{
					this.VarOrByte( subOpcode, 0x80 );
					this.VarOrByte( subOpcode, 0x40 );
					var second = this.ReadByte();
					this.VarOrByte( second, 0x80 );
					this.VarOrByte( second, 0x40 );
					var third = this.ReadByte();
					this.VarOrByte( third, 0x40 );
					return;
				}
				case 8:                                           // intensity
					this.VarOrByte( subOpcode, 0x80 );
					this.VarOrByte( subOpcode, 0x40 );
					this.VarOrByte( subOpcode, 0x20 );
					return;
				case 9:                                           // save/load game
					this.VarOrByte( subOpcode, 0x80 );
					this.VarOrByte( subOpcode, 0x40 );
					return;
				case 10:                                          // fade effect
					this.VarOrWord( subOpcode, 0x80 );
					return;
				case 11:                                          // rgb intensity
				case 12:                                          // room shadow
				{
					this.VarOrWord( subOpcode, 0x80 );
					this.VarOrWord( subOpcode, 0x40 );
					this.VarOrWord( subOpcode, 0x20 );
					var second = this.ReadByte();
					this.VarOrByte( second, 0x80 );
					this.VarOrByte( second, 0x40 );
					return;
				}
				case 13:                                          // save string
				case 14:                                          // load string
					this.VarOrByte( subOpcode, 0x80 );
					this.SkipString();
					return;
				case 15:                                          // transform
				{
					this.VarOrByte( subOpcode, 0x80 );
					var second = this.ReadByte();
					this.VarOrByte( second, 0x80 );
					this.VarOrByte( second, 0x40 );
					var third = this.ReadByte();
					this.VarOrByte( third, 0x80 );
					return;
				}
				case 16:                                          // cycle speed
					this.VarOrByte( subOpcode, 0x80 );
					this.VarOrByte( subOpcode, 0x40 );
					return;
				default:
					throw new DecodeException( "unhandled roomOps sub-opcode" );
			}
		}

		private void OldRoomEffect()
		{
			var subOpcode = this.ReadByte();
			if( ( subOpcode & 0x1F ) == 3 )
			{
				this.VarOrWord( subOpcode, 0x80 );
			}
		}

		private void ActorOps( int opcode )
		{
			var actor = this.VarOrByte( opcode, 0x80 );
			while( true )
			{
				var subOpcode = this.ReadByte();
				if( subOpcode == 0xFF )
				{
					return;
				}
				switch( subOpcode & 0x1F )
				{
					case 0:                                       // dummy
						this.VarOrByte( subOpcode, 0x80 );
						break;
					case 1:                                       // costume
						this.AddEvent( ScriptEventKind.SetCostume, actor, this.VarOrByte( subOpcode, 0x80 ) );
						break;
					case 2:                                       // step distance
					case 5:                                       // talk animation
					case 11:                                      // palette
					case 17:                                      // scale
						this.VarOrByte( subOpcode, 0x80 );
						this.VarOrByte( subOpcode, 0x40 );
						break;
					case 3:                                       // sound
					case 4:                                       // walk animation
					case 6:                                       // stand animation
					case 12:                                      // talk color
					case 14:                                      // init animation
					case 16:                                      // width
					case 19:                                      // always zclip
					case 22:                                      // animation speed
					case 23:                                      // shadow mode
						this.VarOrByte( subOpcode, 0x80 );
						break;
					case 7:                                       // animations
						this.VarOrByte( subOpcode, 0x80 );
						this.VarOrByte( subOpcode, 0x40 );
						this.VarOrByte( subOpcode, 0x20 );
						break;
					case 8:                                       // default init
					case 10:                                      // animation default
					case 18:                                      // never zclip
					case 20:                                      // ignore boxes
					case 21:                                      // follow boxes
						break;
					case 9:                                       // elevation
						this.AddEvent( ScriptEventKind.SetElevation, actor, this.VarOrWord( subOpcode, 0x80 ) );
						break;
					case 13:                                      // actor name
						this.SkipString();
						break;
					default:
						throw new DecodeException( "unhandled actorOps sub-opcode" );
				}
			}
		}

		private void VerbOps( int opcode )
		{
			this.VarOrByte( opcode, 0x80 );
			while( true )
			{
				var subOpcode = this.ReadByte();
				if( subOpcode == 0xFF )
				{
					return;
				}
				switch( subOpcode & 0x1F )
				{
					case 1:                                       // image
					case 20:                                      // name from string resource
						this.VarOrWord( subOpcode, 0x80 );
						break;
					case 2:                                       // name
						this.SkipString();
						break;
					case 3:                                       // color
					case 4:                                       // highlight color
					case 16:                                      // dim color
					case 18:                                      // key
					case 23:                                      // back color
						this.VarOrByte( subOpcode, 0x80 );
						break;
					case 5:                                       // position
						this.VarOrWord( subOpcode, 0x80 );
						this.VarOrWord( subOpcode, 0x40 );
						break;
					case 6:                                       // on
					case 7:                                       // off
					case 8:                                       // delete
					case 9:                                       // new
					case 17:                                      // dim
					case 19:                                      // center
						break;
					case 22:                                      // assign object
						this.VarOrWord( subOpcode, 0x80 );
						this.VarOrByte( subOpcode, 0x40 );
						break;
					default:
						throw new DecodeException( "unhandled verbOps sub-opcode" );
				}
			}
		}

		private void ParseString()
		{
			while( true )
			{
				var subOpcode = this.ReadByte();
				if( subOpcode == 0xFF )
				{
					return;
				}
				switch( subOpcode & 0xF )
				{
					case 0:                                       // at position
					case 3:                                       // erase
					case 8:                                       // say voice (CD talkie)
						this.VarOrWord( subOpcode, 0x80 );
						this.VarOrWord( subOpcode, 0x40 );
						break;
					case 1:                                       // color
						this.VarOrByte( subOpcode, 0x80 );
						break;
					case 2:                                       // clip right
						this.VarOrWord( subOpcode, 0x80 );
						break;
					case 4:                                       // center
					case 6:                                       // left aligned
					case 7:                                       // overhead
						break;
					case 15:                                      // the text itself ends the list
						this.SkipString();
						return;
					default:
						throw new DecodeException( "unhandled string sub-opcode" );
				}
			}
		}

		private void DoSentence( int opcode )
		{
			var verb = this.VarOrByte( opcode, 0x80 );
			if( verb.IsLiteral && verb.Value == 0xFE )
			{
				// the "stop sentence" form has no object parameters
				return;
			}
			this.VarOrWord( opcode, 0x40 );
			this.VarOrWord( opcode, 0x20 );
		}

		private void DrawBox( int opcode )
		{
			this.VarOrWord( opcode, 0x80 );
			this.VarOrWord( opcode, 0x40 );
			var second = this.ReadByte();
			this.VarOrWord( second, 0x80 );
			this.VarOrWord( second, 0x40 );
			this.VarOrByte( second, 0x20 );
		}

		private void ResourceRoutines()
		{
			var subOpcode = this.ReadByte();
			if( subOpcode != 17 )
			{
				this.VarOrByte( subOpcode, 0x80 );
			}
			if( ( subOpcode & 0x3F ) == 20 )
			{
				// load fl-object takes an extra room parameter
				this.VarOrWord( subOpcode, 0x40 );
			}
		}

		private void SaveRestoreVerbs()
		{
			var subOpcode = this.ReadByte();
			this.VarOrByte( subOpcode, 0x80 );
			this.VarOrByte( subOpcode, 0x40 );
			this.VarOrByte( subOpcode, 0x20 );
		}

		private void Expression()
		{
			this.ReadResultVariable();
			while( true )
			{
				var subOpcode = this.ReadByte();
				if( subOpcode == 0xFF )
				{
					return;
				}
				switch( subOpcode & 0x1F )
				{
					case 1:                                       // push value
						this.VarOrWord( subOpcode, 0x80 );
						break;
					case 2:                                       // add
					case 3:                                       // subtract
					case 4:                                       // multiply
					case 5:                                       // divide
						break;
					case 6:                                       // a nested regular instruction
						this.DecodeInstruction();
						break;
					default:
						throw new DecodeException( "unhandled expression sub-opcode" );
				}
			}
		}

		private void Wait()
		{
			var subOpcode = this.ReadByte();
			switch( subOpcode & 0x1F )
			{
				case 1:                                           // wait for actor
					this.VarOrByte( subOpcode, 0x80 );
					return;
				case 2:                                           // wait for message
				case 3:                                           // wait for camera
				case 4:                                           // wait for sentence
					return;
				default:
					throw new DecodeException( "unhandled wait sub-opcode" );
			}
		}

		private void PseudoRoom()
		{
			this.ReadByte();
			while( this.ReadByte() != 0 )
			{
			}
		}
	}
}
