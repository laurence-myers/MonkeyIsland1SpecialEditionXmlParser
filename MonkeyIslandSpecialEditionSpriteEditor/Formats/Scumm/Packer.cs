using System;
using System.Collections.Generic;
using System.IO;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm
{
	/// <summary>
	/// Writes edited classic SCUMM data back. Only walkbox (BOXD) corner/mask/flags/scale edits
	/// are supported, which are size-neutral: the 20 byte records are patched in place, so every
	/// other byte of the file - including the box matrix and all block offsets - is untouched.
	/// </summary>
	public static class Packer
	{
		internal const int BoxRecordSize = 20;

		/// <summary>
		/// Returns a new, still XOR encoded resource file with one room's walkboxes replaced.
		/// The file is decoded into a private buffer, the room's BOXD records are verified to
		/// still match <paramref name="expectedOriginal"/> (so a stale load or another mod's edit
		/// is refused rather than silently overwritten), the new records are spliced in, and the
		/// buffer is re-encoded. Throws on any mismatch; never partially writes.
		/// </summary>
		/// <param name="encodedBytes">The XOR encoded resource file (monkey1.001) bytes.</param>
		/// <param name="roomNumber">The classic room number whose boxes to replace.</param>
		/// <param name="newBoxes">The replacement boxes (same count as the file holds).</param>
		/// <param name="expectedOriginal">The boxes as originally loaded, for the integrity check.</param>
		public static byte[] PatchRoomBoxes( byte[] encodedBytes, int roomNumber, IReadOnlyList<ClassicBox> newBoxes, IReadOnlyList<ClassicBox> expectedOriginal )
		{
			if( newBoxes.Count != expectedOriginal.Count )
			{
				throw new InvalidOperationException( "The edited and original walkbox counts differ; adding or removing boxes is not supported." );
			}

			// own exactly one decode of a private copy; re-encode the same buffer at the end
			var decoded = (byte[])encodedBytes.Clone();
			Parser.XorDecode( decoded, Parser.XorKey );

			long payloadPosition;
			long endPosition;
			int fileCount;
			using( var stream = new MemoryStream( decoded, writable: false ) )
			using( var reader = new BinaryReader( stream ) )
			{
				var boxd = Parser.FindRoomBoxd( reader, roomNumber );
				if( boxd == null )
				{
					throw new InvalidOperationException( string.Concat( "No walkboxes (BOXD) were found for room ", roomNumber, "." ) );
				}
				payloadPosition = boxd.Value.PayloadPosition;
				endPosition = boxd.Value.EndPosition;
				reader.BaseStream.Position = payloadPosition;
				fileCount = reader.ReadUInt16();
			}

			if( fileCount != newBoxes.Count )
			{
				throw new InvalidOperationException( string.Concat(
					"Room ", roomNumber, " holds ", fileCount, " walkboxes but ", newBoxes.Count, " were supplied." ) );
			}

			var recordsStart = payloadPosition + 2;
			if( recordsStart + (long)fileCount * BoxRecordSize > endPosition )
			{
				// the file's BOXD block is shorter than its own count claims; refuse rather than
				// write past the block
				throw new InvalidOperationException( string.Concat( "Room ", roomNumber, "'s BOXD block is truncated; refusing to patch." ) );
			}

			// verify the file still contains exactly the boxes we loaded before overwriting
			var expectedBytes = SerializeBoxes( expectedOriginal );
			for( var index = 0; index < expectedBytes.Length; index++ )
			{
				if( decoded[recordsStart + index] != expectedBytes[index] )
				{
					throw new InvalidOperationException(
						"The classic data's walkboxes have changed since they were loaded (a different game version, or another edit). Reload the room and try again." );
				}
			}

			var newBytes = SerializeBoxes( newBoxes );
			Array.Copy( newBytes, 0, decoded, recordsStart, newBytes.Length );

			// symmetric re-encode
			Parser.XorDecode( decoded, Parser.XorKey );
			return decoded;
		}

		private static byte[] SerializeBoxes( IReadOnlyList<ClassicBox> boxes )
		{
			var buffer = new byte[boxes.Count * BoxRecordSize];
			for( var index = 0; index < boxes.Count; index++ )
			{
				ValidateBox( boxes[index], index );
				WriteBox( buffer, index * BoxRecordSize, boxes[index] );
			}
			return buffer;
		}

		/// <summary>
		/// Refuses to write a box whose fields do not fit their on-disk widths, so an out-of-range
		/// value (e.g. a corner dragged past the int16 range) is rejected rather than silently
		/// wrapped into corrupt geometry.
		/// </summary>
		private static void ValidateBox( ClassicBox box, int index )
		{
			foreach( var corner in box.CornerList )
			{
				if( corner.X < short.MinValue || corner.X > short.MaxValue || corner.Y < short.MinValue || corner.Y > short.MaxValue )
				{
					throw new InvalidOperationException( string.Concat( "Walkbox ", index, " has a corner outside the range a classic box can store (", short.MinValue, "..", short.MaxValue, ")." ) );
				}
			}
			if( box.Mask < 0 || box.Mask > 255 )
			{
				throw new InvalidOperationException( string.Concat( "Walkbox ", index, " mask ", box.Mask, " is out of range (0..255)." ) );
			}
			if( box.Flags < 0 || box.Flags > 255 )
			{
				throw new InvalidOperationException( string.Concat( "Walkbox ", index, " flags ", box.Flags, " are out of range (0..255)." ) );
			}
			if( box.Scale < 0 || box.Scale > 65535 )
			{
				throw new InvalidOperationException( string.Concat( "Walkbox ", index, " scale ", box.Scale, " is out of range (0..65535)." ) );
			}
		}

		private static void WriteBox( byte[] buffer, int position, ClassicBox box )
		{
			var corners = box.CornerList;
			for( var corner = 0; corner < 4; corner++ )
			{
				var point = corner < corners.Length ? corners[corner] : System.Drawing.Point.Empty;
				WriteInt16( buffer, position + corner * 4, point.X );
				WriteInt16( buffer, position + corner * 4 + 2, point.Y );
			}
			buffer[position + 16] = (byte)box.Mask;
			buffer[position + 17] = (byte)box.Flags;
			WriteInt16( buffer, position + 18, box.Scale );
		}

		private static void WriteInt16( byte[] buffer, int position, int value )
		{
			buffer[position] = (byte)( value & 0xFF );
			buffer[position + 1] = (byte)( ( value >> 8 ) & 0xFF );
		}

		// little-endian record-field helpers, shared with ClassicPatcher
		internal static void WriteInt16Le( byte[] buffer, int position, int value )
		{
			WriteInt16( buffer, position, value );
		}

		internal static int ReadInt16Le( byte[] buffer, int position )
		{
			return (short)( buffer[position] | ( buffer[position + 1] << 8 ) );
		}

		internal static int ReadUInt16Le( byte[] buffer, int position )
		{
			return buffer[position] | ( buffer[position + 1] << 8 );
		}
	}
}
