using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm
{
	/// <summary>
	/// The outcome of applying or removing a walkbox patch.
	/// </summary>
	public class ClassicPatchResult
	{
		private ClassicPatchResult()
		{
		}

		public bool Ok
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets whether the classic data changed and <see cref="EncodedBytes"/> should be written.
		/// False for a successful no-op (nothing to do).
		/// </summary>
		public bool Changed
		{
			get;
			private set;
		}

		public byte[]? EncodedBytes
		{
			get;
			private set;
		}

		public string Message
		{
			get;
			private set;
		} = "";

		public static ClassicPatchResult Fail( string message )
		{
			return new ClassicPatchResult { Ok = false, Changed = false, Message = message };
		}

		public static ClassicPatchResult NoChange( string message )
		{
			return new ClassicPatchResult { Ok = true, Changed = false, Message = message };
		}

		public static ClassicPatchResult Changed_( byte[] encoded, string message )
		{
			return new ClassicPatchResult { Ok = true, Changed = true, EncodedBytes = encoded, Message = message };
		}
	}

	/// <summary>
	/// Builds, applies and removes redistributable walkbox patches. All edits are size-neutral
	/// BOXD record swaps, so patching only rewrites the changed 20-byte records and never moves a
	/// byte. Works on the still-XOR-encoded resource bytes; decode/encode is owned here.
	/// </summary>
	public static class ClassicPatcher
	{
		/// <summary>
		/// Builds a patch capturing the walkbox differences between a pristine resource file and
		/// an edited one, fingerprinted against the pristine so it can only be applied to matching
		/// game data. Returns null when nothing differs. Only the edited records and one-way
		/// hashes are stored - never the pristine bytes.
		/// </summary>
		public static ClassicPatch? BuildPatch( byte[] pristineEncoded, byte[] editedEncoded, ClassicPatchInfo info, string fingerprintLabel, string toolVersion )
		{
			var pristine = Decode( pristineEncoded );
			var edited = Decode( editedEncoded );

			var patch = new ClassicPatch
			{
				ToolVersion = toolVersion,
				Info = info,
			};
			var fingerprint = new ClassicPatchFingerprint
			{
				Label = fingerprintLabel,
				DataFileLength = pristineEncoded.Length,
				DataFileSha256 = Sha256Hex( pristineEncoded, 0, pristineEncoded.Length ),
			};

			foreach( var roomNumber in EnumerateRoomNumbers( pristine ) )
			{
				var pristineBoxd = Locate( pristine, roomNumber );
				var editedBoxd = Locate( edited, roomNumber );
				if( pristineBoxd == null || editedBoxd == null )
				{
					continue;
				}
				if( pristineBoxd.Value.Count != editedBoxd.Value.Count )
				{
					// adding or removing boxes is not supported; skip such a room
					continue;
				}

				var changedBoxes = new List<ClassicPatchBox>();
				for( var index = 0; index < editedBoxd.Value.Count; index++ )
				{
					if( !RecordsEqual( pristine, pristineBoxd.Value.RecordsStart, edited, editedBoxd.Value.RecordsStart, index ) )
					{
						changedBoxes.Add( ReadBox( edited, editedBoxd.Value.RecordsStart, index ) );
					}
				}
				if( changedBoxes.Count == 0 )
				{
					continue;
				}

				patch.RoomEdits.Add( new ClassicPatchRoomEdit
				{
					Number = roomNumber,
					PostBoxdSha256 = HashBoxd( edited, editedBoxd.Value ),
					Boxes = changedBoxes,
				} );
				fingerprint.Rooms.Add( new ClassicPatchFingerprintRoom
				{
					Number = roomNumber,
					BoxdSha256 = HashBoxd( pristine, pristineBoxd.Value ),
					BoxCount = pristineBoxd.Value.Count,
				} );
			}

			if( patch.RoomEdits.Count == 0 )
			{
				return null;
			}

			patch.BaseFingerprints.Add( fingerprint );
			return patch;
		}

		/// <summary>
		/// Applies a patch. <paramref name="pristinePakEncoded"/> is the user's own untouched game
		/// data (used only to confirm the variant and classify rooms); <paramref name="currentEncoded"/>
		/// is the data the game currently loads (a prior override, or the pak). Splices the edited
		/// records into the current data, preserving edits to other rooms, and returns the encoded
		/// result to write. Refuses on a version mismatch or a room changed by something else.
		/// </summary>
		public static ClassicPatchResult Apply( byte[] pristinePakEncoded, byte[] currentEncoded, ClassicPatch patch )
		{
			var validationError = patch.Validate();
			if( validationError != null )
			{
				return ClassicPatchResult.Fail( validationError );
			}

			var fingerprint = MatchFingerprint( pristinePakEncoded, patch );
			if( fingerprint == null )
			{
				return ClassicPatchResult.Fail( "This patch was made for a different version of the classic game data and cannot be applied to yours." );
			}

			var pristine = Decode( pristinePakEncoded );
			var current = Decode( currentEncoded );

			var toApply = new List<ClassicPatchRoomEdit>();
			foreach( var roomEdit in patch.RoomEdits )
			{
				var currentBoxd = Locate( current, roomEdit.Number );
				var pristineBoxd = Locate( pristine, roomEdit.Number );
				if( currentBoxd == null || pristineBoxd == null )
				{
					return ClassicPatchResult.Fail( string.Concat( "Room ", roomEdit.Number, " has no walkboxes in your game data." ) );
				}

				var currentHash = HashBoxd( current, currentBoxd.Value );
				var pristineHash = HashBoxd( pristine, pristineBoxd.Value );
				if( string.Equals( currentHash, roomEdit.PostBoxdSha256, StringComparison.OrdinalIgnoreCase ) )
				{
					continue; // this room is already patched
				}
				if( string.Equals( currentHash, pristineHash, StringComparison.OrdinalIgnoreCase ) )
				{
					toApply.Add( roomEdit );
				}
				else
				{
					return ClassicPatchResult.Fail( string.Concat(
						"Room ", roomEdit.Number, "'s walkboxes have been changed by something else (another mod or a manual edit). Remove that change first, then apply this patch." ) );
				}
			}

			if( toApply.Count == 0 )
			{
				return ClassicPatchResult.NoChange( "This patch is already applied." );
			}

			foreach( var roomEdit in toApply )
			{
				var boxd = Locate( current, roomEdit.Number )!.Value;
				var spliceError = SpliceRoom( current, boxd, roomEdit );
				if( spliceError != null )
				{
					return ClassicPatchResult.Fail( spliceError );
				}
				if( !string.Equals( HashBoxd( current, boxd ), roomEdit.PostBoxdSha256, StringComparison.OrdinalIgnoreCase ) )
				{
					return ClassicPatchResult.Fail( string.Concat( "The patch did not produce the expected result for room ", roomEdit.Number, "; nothing was written." ) );
				}
			}

			return ClassicPatchResult.Changed_( Encode( current ), string.Concat( "Applied ", toApply.Count, " room(s)." ) );
		}

		/// <summary>
		/// Removes a patch by restoring the patched rooms to their pristine records (from the
		/// user's own pak), leaving edits to other rooms intact. Rooms that are not currently in
		/// the patched state are left alone.
		/// </summary>
		public static ClassicPatchResult Remove( byte[] pristinePakEncoded, byte[] currentEncoded, ClassicPatch patch )
		{
			var validationError = patch.Validate();
			if( validationError != null )
			{
				return ClassicPatchResult.Fail( validationError );
			}

			var fingerprint = MatchFingerprint( pristinePakEncoded, patch );
			if( fingerprint == null )
			{
				return ClassicPatchResult.Fail( "This patch was made for a different version of the classic game data." );
			}

			var pristine = Decode( pristinePakEncoded );
			var current = Decode( currentEncoded );

			var restored = 0;
			foreach( var roomEdit in patch.RoomEdits )
			{
				var currentBoxd = Locate( current, roomEdit.Number );
				var pristineBoxd = Locate( pristine, roomEdit.Number );
				if( currentBoxd == null || pristineBoxd == null )
				{
					continue;
				}
				if( !string.Equals( HashBoxd( current, currentBoxd.Value ), roomEdit.PostBoxdSha256, StringComparison.OrdinalIgnoreCase ) )
				{
					continue; // not in the patched state (pristine already, or foreign) - leave it
				}
				if( currentBoxd.Value.Count != pristineBoxd.Value.Count )
				{
					continue;
				}

				// refuse to read/write past either truncated block (a corrupt count word)
				var recordsLength = currentBoxd.Value.Count * Packer.BoxRecordSize;
				if( currentBoxd.Value.RecordsStart + recordsLength > currentBoxd.Value.EndPosition
					|| pristineBoxd.Value.RecordsStart + recordsLength > pristineBoxd.Value.EndPosition )
				{
					return ClassicPatchResult.Fail( string.Concat( "Room ", roomEdit.Number, "'s BOXD block is truncated; refusing to restore." ) );
				}

				// copy the pristine records back over the patched ones (size-neutral, same offsets)
				Array.Copy( pristine, pristineBoxd.Value.RecordsStart, current, currentBoxd.Value.RecordsStart, recordsLength );
				restored++;
			}

			if( restored == 0 )
			{
				return ClassicPatchResult.NoChange( "This patch is not applied." );
			}

			return ClassicPatchResult.Changed_( Encode( current ), string.Concat( "Removed the patch from ", restored, " room(s)." ) );
		}

		private static ClassicPatchFingerprint? MatchFingerprint( byte[] pristinePakEncoded, ClassicPatch patch )
		{
			var sha = Sha256Hex( pristinePakEncoded, 0, pristinePakEncoded.Length );
			return patch.BaseFingerprints.FirstOrDefault(
				f => f.DataFileLength == pristinePakEncoded.Length && string.Equals( f.DataFileSha256, sha, StringComparison.OrdinalIgnoreCase ) );
		}

		private static string? SpliceRoom( byte[] decoded, BoxdLocation boxd, ClassicPatchRoomEdit roomEdit )
		{
			foreach( var box in roomEdit.Boxes )
			{
				if( box.Index < 0 || box.Index >= boxd.Count )
				{
					return string.Concat( "Room ", roomEdit.Number, " box index ", box.Index, " is out of range (", boxd.Count, " boxes)." );
				}
				if( boxd.RecordsStart + ( box.Index + 1 ) * Packer.BoxRecordSize > boxd.EndPosition )
				{
					return string.Concat( "Room ", roomEdit.Number, "'s BOXD block is truncated; refusing to patch." );
				}
				var rangeError = ValidateBox( box, roomEdit.Number );
				if( rangeError != null )
				{
					return rangeError;
				}
				WriteBox( decoded, boxd.RecordsStart, box );
			}
			return null;
		}

		private static string? ValidateBox( ClassicPatchBox box, int roomNumber )
		{
			int[] coords = { box.X1, box.Y1, box.X2, box.Y2, box.X3, box.Y3, box.X4, box.Y4 };
			foreach( var value in coords )
			{
				if( value < short.MinValue || value > short.MaxValue )
				{
					return string.Concat( "Room ", roomNumber, " box ", box.Index, " has a corner outside the storable range." );
				}
			}
			if( box.Mask < 0 || box.Mask > 255 || box.Flags < 0 || box.Flags > 255 || box.Scale < 0 || box.Scale > 65535 )
			{
				return string.Concat( "Room ", roomNumber, " box ", box.Index, " has a mask/flags/scale outside the storable range." );
			}
			return null;
		}

		private static void WriteBox( byte[] decoded, int recordsStart, ClassicPatchBox box )
		{
			var position = recordsStart + box.Index * Packer.BoxRecordSize;
			Packer.WriteInt16Le( decoded, position, box.X1 );
			Packer.WriteInt16Le( decoded, position + 2, box.Y1 );
			Packer.WriteInt16Le( decoded, position + 4, box.X2 );
			Packer.WriteInt16Le( decoded, position + 6, box.Y2 );
			Packer.WriteInt16Le( decoded, position + 8, box.X3 );
			Packer.WriteInt16Le( decoded, position + 10, box.Y3 );
			Packer.WriteInt16Le( decoded, position + 12, box.X4 );
			Packer.WriteInt16Le( decoded, position + 14, box.Y4 );
			decoded[position + 16] = (byte)box.Mask;
			decoded[position + 17] = (byte)box.Flags;
			Packer.WriteInt16Le( decoded, position + 18, box.Scale );
		}

		private static ClassicPatchBox ReadBox( byte[] decoded, int recordsStart, int index )
		{
			var position = recordsStart + index * Packer.BoxRecordSize;
			return new ClassicPatchBox
			{
				Index = index,
				X1 = Packer.ReadInt16Le( decoded, position ),
				Y1 = Packer.ReadInt16Le( decoded, position + 2 ),
				X2 = Packer.ReadInt16Le( decoded, position + 4 ),
				Y2 = Packer.ReadInt16Le( decoded, position + 6 ),
				X3 = Packer.ReadInt16Le( decoded, position + 8 ),
				Y3 = Packer.ReadInt16Le( decoded, position + 10 ),
				X4 = Packer.ReadInt16Le( decoded, position + 12 ),
				Y4 = Packer.ReadInt16Le( decoded, position + 14 ),
				Mask = decoded[position + 16],
				Flags = decoded[position + 17],
				Scale = Packer.ReadUInt16Le( decoded, position + 18 ),
			};
		}

		private static bool RecordsEqual( byte[] a, int aRecordsStart, byte[] b, int bRecordsStart, int index )
		{
			var aPosition = aRecordsStart + index * Packer.BoxRecordSize;
			var bPosition = bRecordsStart + index * Packer.BoxRecordSize;
			for( var offset = 0; offset < Packer.BoxRecordSize; offset++ )
			{
				if( a[aPosition + offset] != b[bPosition + offset] )
				{
					return false;
				}
			}
			return true;
		}

		private struct BoxdLocation
		{
			public int RecordsStart;
			public int Count;
			public long EndPosition;
		}

		private static BoxdLocation? Locate( byte[] decoded, int roomNumber )
		{
			using( var stream = new MemoryStream( decoded, writable: false ) )
			using( var reader = new BinaryReader( stream ) )
			{
				var boxd = Parser.FindRoomBoxd( reader, roomNumber );
				if( boxd == null )
				{
					return null;
				}
				reader.BaseStream.Position = boxd.Value.PayloadPosition;
				int count = reader.ReadUInt16();
				var recordsStart = (int)boxd.Value.PayloadPosition + 2;
				if( recordsStart + (long)count * Packer.BoxRecordSize > boxd.Value.EndPosition )
				{
					// truncated block; report the real bound so callers can refuse
					return new BoxdLocation { RecordsStart = recordsStart, Count = count, EndPosition = boxd.Value.EndPosition };
				}
				return new BoxdLocation { RecordsStart = recordsStart, Count = count, EndPosition = boxd.Value.EndPosition };
			}
		}

		private static string HashBoxd( byte[] decoded, BoxdLocation boxd )
		{
			var payloadStart = boxd.RecordsStart - 2;
			var length = 2 + boxd.Count * Packer.BoxRecordSize;
			if( payloadStart + length > boxd.EndPosition )
			{
				length = (int)( boxd.EndPosition - payloadStart );
			}
			return Sha256Hex( decoded, payloadStart, length );
		}

		private static IEnumerable<int> EnumerateRoomNumbers( byte[] decoded )
		{
			using( var stream = new MemoryStream( decoded, writable: false ) )
			using( var reader = new BinaryReader( stream ) )
			{
				// materialize so the reader/stream can be disposed
				return Parser.EnumerateRoomNumbers( reader ).ToList();
			}
		}

		private static byte[] Decode( byte[] encoded )
		{
			var copy = (byte[])encoded.Clone();
			Parser.XorDecode( copy, Parser.XorKey );
			return copy;
		}

		private static byte[] Encode( byte[] decoded )
		{
			// symmetric; encode the (owned) decoded buffer in place and hand it back
			Parser.XorDecode( decoded, Parser.XorKey );
			return decoded;
		}

		private static string Sha256Hex( byte[] bytes, int offset, int length )
		{
			using( var sha = SHA256.Create() )
			{
				var hash = sha.ComputeHash( bytes, offset, length );
				var builder = new System.Text.StringBuilder( hash.Length * 2 );
				foreach( var value in hash )
				{
					builder.Append( value.ToString( "x2" ) );
				}
				return builder.ToString();
			}
		}
	}
}
