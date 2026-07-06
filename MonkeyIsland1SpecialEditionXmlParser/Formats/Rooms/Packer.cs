using System.Collections.Generic;
using System.IO;
using System.Text;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms
{
	/// <summary>
	/// Writes a <see cref="Room"/> back to the game's binary format.
	///
	/// The layout rules were derived by round-tripping every room in Monkey1.pak
	/// byte-for-byte:
	///  * every item (table, array, blob, string) starts on a 16-byte boundary;
	///  * strings are written NUL-terminated with no trailing padding - the following
	///    item's alignment supplies the pad, so the very last item ends the file
	///    (no padding after EOF);
	///  * texture file names are pooled and shared: a name is written once and every
	///    reference points at that single copy (references may point backwards, so the
	///    stored offsets are signed);
	///  * every relative address is stored as an offset from the field's own position;
	///    0 means "absent".
	/// </summary>
	public static class Packer
	{
		/// <summary>
		/// Writes a Room to a single .dat file.
		/// </summary>
		/// <param name="fileName">The path to the .dat file.</param>
		/// <param name="room">The Room object to write.</param>
		public static void WriteRoomToBinaryFile( string fileName, Room room )
		{
			using( var stream = File.Create( fileName ) )
			using( var writer = new BinaryWriter( stream ) )
			{
				WriteRoom( writer, room );
			}
		}

		public static void WriteRoom( BinaryWriter writer, Room room )
		{
			long start = writer.BaseStream.Position;

			// a single, shared texture-name pool: name -> absolute byte position
			var stringPool = new Dictionary<string, long>();

			// remembered field / target positions, keyed by the entity they belong to
			var staticSpriteHeaderAddressFields = new List<long>();
			var staticSpriteArrayPositions = new List<long>();
			var staticSpritePositions = new Dictionary<StaticSprite, long>();
			var staticSpriteTextureTargets = new Dictionary<StaticSprite, long>();

			var spriteHeaderAddressFields = new List<long>();
			var spriteArrayPositions = new List<long>();
			var spritePositions = new Dictionary<Sprite, long>();
			var spriteTextureTargets = new Dictionary<Sprite, long>();

			var unknown6HeaderAddressFields = new List<long>();
			var unknown6ArrayPositions = new List<long>();

			var roomObjectHeaderNameFields = new List<long>();
			var roomObjectHeaderAddressFields = new List<long>();
			var roomObjectHeaderNamePositions = new List<long>();
			var roomObjectArrayPositions = new List<long>();
			var roomObjectPositions = new Dictionary<RoomObject, long>();
			var roomObjectSubPositions = new Dictionary<RoomObject, long>();
			var roomObjectSpriteTextureTargets = new Dictionary<RoomObject, long>();
			var roomObjectImageChunkArrayPositions = new Dictionary<RoomObjectImage, long>();
			var roomObjectImageChunkPositions = new Dictionary<RoomObjectImageChunk, long>();
			var roomObjectImageChunkTextureTargets = new Dictionary<RoomObjectImageChunk, long>();

			var unknown5HeaderAddressFields = new List<long>();
			var unknown5ArrayPositions = new List<long>();

			// 1. Header placeholder (80 bytes)
			writer.Write( new byte[80] );

			// 2. Room name
			long nameAddress = writer.BaseStream.Position;
			WriteCString( writer, room.Header.Name ?? "" );

			// 3. Static sprite header table. Section element counts are derived from the
			//    data lists rather than the header's own count field, so a stale or
			//    non-serialized count can never desync the written file.
			Align16( writer, start );
			long staticSpriteHeaderAddress = writer.BaseStream.Position;
			for( int i = 0; i < room.StaticSpriteHeaderList.Count; i++ )
			{
				var staticSpriteHeader = room.StaticSpriteHeaderList[i];
				long position = writer.BaseStream.Position;
				writer.Write( staticSpriteHeader.Identifier );
				writer.Write( staticSpriteHeader.SourceWidth );
				writer.Write( staticSpriteHeader.SourceHeight );
				writer.Write( room.StaticSpriteList[i].Count );
				writer.Write( 0 ); // StaticSpriteAddress placeholder
				staticSpriteHeaderAddressFields.Add( position + 16 );
			}

			// 4. Sprite header table
			Align16( writer, start );
			long spriteHeaderAddress = writer.BaseStream.Position;
			for( int i = 0; i < room.SpriteHeaderList.Count; i++ )
			{
				var spriteHeader = room.SpriteHeaderList[i];
				long position = writer.BaseStream.Position;
				writer.Write( spriteHeader.Identifier );
				writer.Write( room.SpriteGroupList[i].SpriteList.Count );
				writer.Write( 0 ); // SpriteAddress placeholder
				spriteHeaderAddressFields.Add( position + 8 );
			}

			// 5. Unknown6 header table
			Align16( writer, start );
			long unknown6HeaderAddress = writer.BaseStream.Position;
			for( int i = 0; i < room.Unknown6HeaderList.Count; i++ )
			{
				var unknown6Header = room.Unknown6HeaderList[i];
				long position = writer.BaseStream.Position;
				writer.Write( unknown6Header.Unkn1 );
				writer.Write( unknown6Header.Unkn2 );
				writer.Write( unknown6Header.Unkn3 );
				writer.Write( unknown6Header.Unkn4 );
				writer.Write( unknown6Header.Unkn5 );
				writer.Write( room.Unknown6List[i].ByteList.Count );
				writer.Write( 0 ); // Unknown6Address placeholder
				unknown6HeaderAddressFields.Add( position + 12 );
			}

			// 6. Room object header table
			Align16( writer, start );
			long roomObjectHeaderAddress = writer.BaseStream.Position;
			for( int i = 0; i < room.RoomObjectHeaderList.Count; i++ )
			{
				long position = writer.BaseStream.Position;
				writer.Write( 0 ); // NameAddress placeholder
				writer.Write( room.RoomObjectGroupList[i].RoomObjectList.Count );
				writer.Write( 0 ); // RoomObjectAddress placeholder
				roomObjectHeaderNameFields.Add( position );
				roomObjectHeaderAddressFields.Add( position + 8 );
			}

			// 7. Unknown5 header table
			Align16( writer, start );
			long unknown5HeaderAddress = writer.BaseStream.Position;
			for( int i = 0; i < room.Unknown5HeaderList.Count; i++ )
			{
				long position = writer.BaseStream.Position;
				writer.Write( room.Unknown5List[i].Int32List.Count );
				writer.Write( 0 ); // Unknown5Address placeholder
				unknown5HeaderAddressFields.Add( position + 4 );
			}

			// 8. Static sprite arrays (one aligned array per group)
			foreach( var staticSpriteList in room.StaticSpriteList )
			{
				Align16( writer, start );
				staticSpriteArrayPositions.Add( writer.BaseStream.Position );
				foreach( var staticSprite in staticSpriteList )
				{
					staticSpritePositions[staticSprite] = writer.BaseStream.Position;
					writer.Write( staticSprite.X );
					writer.Write( staticSprite.Y );
					writer.Write( staticSprite.Width );
					writer.Write( staticSprite.Height );
					writer.Write( 0 ); // TextureFileNameAddress placeholder
				}
			}

			// 9. Sprite arrays
			foreach( var spriteGroup in room.SpriteGroupList )
			{
				Align16( writer, start );
				spriteArrayPositions.Add( writer.BaseStream.Position );
				foreach( var sprite in spriteGroup.SpriteList )
				{
					spritePositions[sprite] = writer.BaseStream.Position;
					writer.Write( 0 ); // TextureFileNameAddress placeholder
					writer.Write( sprite.TextureX );
					writer.Write( sprite.TextureY );
					writer.Write( sprite.TextureWidth );
					writer.Write( sprite.TextureHeight );
					writer.Write( sprite.OffsetX );
					writer.Write( sprite.OffsetY );
					writer.Write( sprite.Layer );
				}
			}

			// 10. Unknown6 byte blobs
			foreach( var unknown6 in room.Unknown6List )
			{
				Align16( writer, start );
				unknown6ArrayPositions.Add( writer.BaseStream.Position );
				writer.Write( unknown6.ByteList.ToArray() );
			}

			// 11. Room object names and instance arrays (interleaved per header)
			for( int i = 0; i < room.RoomObjectHeaderList.Count; i++ )
			{
				var roomObjectHeader = room.RoomObjectHeaderList[i];
				if( roomObjectHeader.Name != null )
				{
					Align16( writer, start );
					roomObjectHeaderNamePositions.Add( writer.BaseStream.Position );
					WriteCString( writer, roomObjectHeader.Name );
				}
				else
				{
					roomObjectHeaderNamePositions.Add( 0 );
				}

				Align16( writer, start );
				roomObjectArrayPositions.Add( writer.BaseStream.Position );
				var roomObjectList = room.RoomObjectGroupList[i].RoomObjectList;
				foreach( var roomObject in roomObjectList )
				{
					roomObjectPositions[roomObject] = writer.BaseStream.Position;
					writer.Write( 0 ); // SpriteAddress placeholder
					writer.Write( 0 ); // ImageAddress placeholder
					writer.Write( roomObject.OffsetX );
					writer.Write( roomObject.OffsetY );
				}
			}

			// 12. Unknown5 int32 arrays
			foreach( var unknown5 in room.Unknown5List )
			{
				Align16( writer, start );
				unknown5ArrayPositions.Add( writer.BaseStream.Position );
				foreach( var value in unknown5.Int32List )
				{
					writer.Write( value );
				}
			}

			// 13. Static sprite texture name pool
			foreach( var staticSpriteList in room.StaticSpriteList )
			{
				foreach( var staticSprite in staticSpriteList )
				{
					staticSpriteTextureTargets[staticSprite] =
						PoolString( writer, start, stringPool, staticSprite.TextureFileName ?? "" );
				}
			}

			// 14. Sprite texture name pool
			foreach( var spriteGroup in room.SpriteGroupList )
			{
				foreach( var sprite in spriteGroup.SpriteList )
				{
					spriteTextureTargets[sprite] =
						PoolString( writer, start, stringPool, sprite.TextureFileName ?? "" );
				}
			}

			// 15. Room object sprite / image records (the 20 / 16 byte sub-records)
			foreach( var roomObject in EnumerateRoomObjects( room ) )
			{
				if( roomObject.Sprite != null )
				{
					Align16( writer, start );
					roomObjectSubPositions[roomObject] = writer.BaseStream.Position;
					writer.Write( 0 ); // TextureFileNameAddress placeholder (signed)
					writer.Write( roomObject.Sprite.X );
					writer.Write( roomObject.Sprite.Y );
					writer.Write( roomObject.Sprite.Width );
					writer.Write( roomObject.Sprite.Height );
				}
				else if( roomObject.Image != null )
				{
					Align16( writer, start );
					roomObjectSubPositions[roomObject] = writer.BaseStream.Position;
					writer.Write( roomObject.Image.SourceWidth );
					writer.Write( roomObject.Image.SourceHeight );
					writer.Write( roomObject.Image.ChunkList.Count );
					writer.Write( 0 ); // ChunkAddress placeholder
				}
			}

			// 16. Per room object, in order: sprite -> pool its texture name;
			//     image -> write its chunk array
			foreach( var roomObject in EnumerateRoomObjects( room ) )
			{
				if( roomObject.Sprite != null )
				{
					if( roomObject.Sprite.TextureFileName != null )
					{
						roomObjectSpriteTextureTargets[roomObject] =
							PoolString( writer, start, stringPool, roomObject.Sprite.TextureFileName );
					}
				}
				else if( roomObject.Image != null )
				{
					Align16( writer, start );
					roomObjectImageChunkArrayPositions[roomObject.Image] = writer.BaseStream.Position;
					foreach( var chunk in roomObject.Image.ChunkList )
					{
						roomObjectImageChunkPositions[chunk] = writer.BaseStream.Position;
						writer.Write( chunk.X );
						writer.Write( chunk.Y );
						writer.Write( chunk.Width );
						writer.Write( chunk.Height );
						writer.Write( 0 ); // TextureFileNameAddress placeholder (signed)
					}
				}
			}

			// 17. Chunk texture name pool
			foreach( var roomObject in EnumerateRoomObjects( room ) )
			{
				if( roomObject.Image != null )
				{
					foreach( var chunk in roomObject.Image.ChunkList )
					{
						if( chunk.TextureFileName != null )
						{
							roomObjectImageChunkTextureTargets[chunk] =
								PoolString( writer, start, stringPool, chunk.TextureFileName );
						}
					}
				}
			}

			// --- BACKPATCHING ---

			for( int i = 0; i < room.StaticSpriteHeaderList.Count; i++ )
			{
				PatchRel( writer, staticSpriteHeaderAddressFields[i], staticSpriteArrayPositions[i] );
			}
			foreach( var kvp in staticSpritePositions )
			{
				PatchRel( writer, kvp.Value + 16, staticSpriteTextureTargets[kvp.Key] );
			}

			for( int i = 0; i < room.SpriteHeaderList.Count; i++ )
			{
				PatchRel( writer, spriteHeaderAddressFields[i], spriteArrayPositions[i] );
			}
			foreach( var kvp in spritePositions )
			{
				PatchRel( writer, kvp.Value, spriteTextureTargets[kvp.Key] );
			}

			for( int i = 0; i < room.Unknown6HeaderList.Count; i++ )
			{
				PatchRel( writer, unknown6HeaderAddressFields[i], unknown6ArrayPositions[i] );
			}

			for( int i = 0; i < room.RoomObjectHeaderList.Count; i++ )
			{
				PatchRel( writer, roomObjectHeaderNameFields[i], roomObjectHeaderNamePositions[i] );
				PatchRel( writer, roomObjectHeaderAddressFields[i], roomObjectArrayPositions[i] );
			}
			foreach( var roomObject in EnumerateRoomObjects( room ) )
			{
				long entryPosition = roomObjectPositions[roomObject];
				if( roomObject.Sprite != null )
				{
					long subPosition = roomObjectSubPositions[roomObject];
					PatchRel( writer, entryPosition, subPosition );
					roomObjectSpriteTextureTargets.TryGetValue( roomObject, out var textureTarget );
					PatchRel( writer, subPosition, textureTarget );
				}
				else if( roomObject.Image != null )
				{
					long subPosition = roomObjectSubPositions[roomObject];
					PatchRel( writer, entryPosition + 4, subPosition );
					if( roomObject.Image.ChunkList.Count > 0 )
					{
						PatchRel( writer, subPosition + 12, roomObjectImageChunkArrayPositions[roomObject.Image] );
					}
					foreach( var chunk in roomObject.Image.ChunkList )
					{
						roomObjectImageChunkTextureTargets.TryGetValue( chunk, out var chunkTextureTarget );
						PatchRel( writer, roomObjectImageChunkPositions[chunk] + 16, chunkTextureTarget );
					}
				}
			}

			for( int i = 0; i < room.Unknown5HeaderList.Count; i++ )
			{
				PatchRel( writer, unknown5HeaderAddressFields[i], unknown5ArrayPositions[i] );
			}

			// Main header
			writer.BaseStream.Position = start;
			writer.Write( room.Header.Identifier );
			WriteRel( writer, nameAddress );
			writer.Write( room.Header.Width );
			writer.Write( room.Header.Height );
			writer.Write( room.StaticSpriteHeaderList.Count );
			WriteRel( writer, staticSpriteHeaderAddress );
			writer.Write( room.SpriteHeaderList.Count );
			WriteRel( writer, spriteHeaderAddress );
			writer.Write( room.Header.Unknown9 );
			writer.Write( room.Header.Unknown6HeaderCountA );
			WriteRel( writer, unknown6HeaderAddress ); // empty "A" section shares the table
			writer.Write( room.Unknown6HeaderList.Count );
			WriteRel( writer, unknown6HeaderAddress );
			writer.Write( room.RoomObjectHeaderList.Count );
			WriteRel( writer, roomObjectHeaderAddress );
			writer.Write( room.Unknown5HeaderList.Count );
			WriteRel( writer, unknown5HeaderAddress );
			writer.Write( room.Header.AlwaysZero1 );
			writer.Write( room.Header.AlwaysZero2 );
			writer.Write( room.Header.AlwaysZero3 );

			writer.BaseStream.Position = writer.BaseStream.Length;
		}

		private static IEnumerable<RoomObject> EnumerateRoomObjects( Room room )
		{
			foreach( var roomObjectGroup in room.RoomObjectGroupList )
			{
				foreach( var roomObject in roomObjectGroup.RoomObjectList )
				{
					yield return roomObject;
				}
			}
		}

		/// <summary>
		/// Writes a relative address for the current field position, or 0 when the target is
		/// absent. Advances the stream by 4 bytes, so it may be used for sequential header writes.
		/// </summary>
		private static void WriteRel( this BinaryWriter writer, long target )
		{
			long fieldPosition = writer.BaseStream.Position;
			writer.Write( target != 0 ? (int)( target - fieldPosition ) : 0 );
		}

		/// <summary>
		/// Seeks to <paramref name="fieldPosition"/>, writes the relative address of
		/// <paramref name="target"/> (0 when absent), then restores the stream position.
		/// The offset is signed - texture references frequently point backwards.
		/// </summary>
		private static void PatchRel( BinaryWriter writer, long fieldPosition, long target )
		{
			long current = writer.BaseStream.Position;
			writer.BaseStream.Position = fieldPosition;
			writer.Write( target != 0 ? (int)( target - fieldPosition ) : 0 );
			writer.BaseStream.Position = current;
		}

		/// <summary>
		/// Returns the pooled position of <paramref name="text"/>, writing it once (16-byte
		/// aligned, NUL-terminated, no trailing padding) on first use.
		/// </summary>
		private static long PoolString( BinaryWriter writer, long start, Dictionary<string, long> pool, string text )
		{
			if( !pool.TryGetValue( text, out var position ) )
			{
				Align16( writer, start );
				position = writer.BaseStream.Position;
				WriteCString( writer, text );
				pool[text] = position;
			}
			return position;
		}

		private static void WriteCString( BinaryWriter writer, string text )
		{
			writer.Write( Encoding.ASCII.GetBytes( text ) );
			writer.Write( (byte)0 );
		}

		private static void Align16( BinaryWriter writer, long start )
		{
			var mod = ( writer.BaseStream.Position - start ) % 16;
			if( mod != 0 )
			{
				writer.Write( new byte[16 - mod] );
			}
		}
	}
}
