using System.Collections.Generic;
using System.IO;
using System.Text;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Scumm.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Scumm
{
	/// <summary>
	/// Reads room object placement data from the classic SCUMM v5 data files
	/// (monkey1.000 index / monkey1.001 resource file) that ship with the Special Edition.
	/// The Special Edition engine positions its HD room object sprites from this data,
	/// which is why the SE room files themselves carry no absolute positions.
	/// </summary>
	public static class Parser
	{
		/// <summary>
		/// Both classic data files are XOR encoded with this key.
		/// </summary>
		public const byte XorKey = 0x69;

		/// <summary>
		/// Reads all rooms and their objects from a classic resource file (monkey1.001).
		/// </summary>
		/// <param name="fileName">The path to the resource file.</param>
		/// <returns>The rooms found in the file, in file order.</returns>
		public static List<ClassicRoom> ReadRoomsFromDataFile( string fileName )
		{
			return ReadRoomsFromEncodedBytes( File.ReadAllBytes( fileName ) );
		}

		/// <summary>
		/// Reads all rooms and their objects from the still XOR encoded contents of a resource file.
		/// </summary>
		public static List<ClassicRoom> ReadRoomsFromEncodedBytes( byte[] bytes )
		{
			XorDecode( bytes, Parser.XorKey );
			using( var stream = new MemoryStream( bytes ) )
			using( var reader = new BinaryReader( stream ) )
			{
				return ReadRooms( reader );
			}
		}

		/// <summary>
		/// Reads all rooms and their objects from an already decoded resource stream.
		/// </summary>
		public static List<ClassicRoom> ReadRooms( BinaryReader reader )
		{
			var roomList = new List<ClassicRoom>();
			var roomNumberByOffset = new Dictionary<long, int>();

			// the file is a single LECF block containing a LOFF block (room number to
			// offset table) followed by one LFLF block per room
			foreach( var lecf in ReadBlocks( reader, 0, reader.BaseStream.Length ) )
			{
				if( lecf.Tag != "LECF" )
				{
					continue;
				}

				foreach( var child in ReadBlocks( reader, lecf.PayloadPosition, lecf.EndPosition ) )
				{
					if( child.Tag == "LOFF" )
					{
						ReadRoomOffsets( reader, child, roomNumberByOffset );
					}
					else if( child.Tag == "LFLF" )
					{
						var room = ReadRoomFromLflf( reader, child, roomNumberByOffset, fallbackRoomNumber: roomList.Count + 1 );
						if( room != null )
						{
							roomList.Add( room );
						}
					}
				}
			}

			return roomList;
		}

		/// <summary>
		/// Reads the room number to room name table (RNAM) from a classic index file (monkey1.000).
		/// </summary>
		/// <param name="fileName">The path to the index file.</param>
		/// <returns>Room names keyed by room number.</returns>
		public static Dictionary<int, string> ReadRoomNamesFromIndexFile( string fileName )
		{
			return ReadRoomNamesFromEncodedBytes( File.ReadAllBytes( fileName ) );
		}

		/// <summary>
		/// Reads the room name table from the still XOR encoded contents of an index file.
		/// </summary>
		public static Dictionary<int, string> ReadRoomNamesFromEncodedBytes( byte[] bytes )
		{
			XorDecode( bytes, Parser.XorKey );
			using( var stream = new MemoryStream( bytes ) )
			using( var reader = new BinaryReader( stream ) )
			{
				return ReadRoomNames( reader );
			}
		}

		/// <summary>
		/// Reads the room number to room name table (RNAM) from an already decoded index stream.
		/// </summary>
		public static Dictionary<int, string> ReadRoomNames( BinaryReader reader )
		{
			var roomNames = new Dictionary<int, string>();

			foreach( var block in ReadBlocks( reader, 0, reader.BaseStream.Length ) )
			{
				if( block.Tag != "RNAM" )
				{
					continue;
				}

				reader.BaseStream.Position = block.PayloadPosition;
				while( reader.BaseStream.Position < block.EndPosition )
				{
					var roomNumber = reader.ReadByte();
					if( roomNumber == 0 )
					{
						break;
					}

					// each name is 9 bytes, additionally XOR encoded with 0xFF
					var nameBytes = reader.ReadBytes( 9 );
					var builder = new StringBuilder();
					foreach( var nameByte in nameBytes )
					{
						var decoded = (byte)( nameByte ^ 0xFF );
						if( decoded == 0 )
						{
							break;
						}
						builder.Append( (char)decoded );
					}
					roomNames[roomNumber] = builder.ToString();
				}
			}

			return roomNames;
		}

		/// <summary>
		/// Decodes (or encodes; the operation is symmetric) a buffer in place by XORing every byte with the key.
		/// </summary>
		public static void XorDecode( byte[] bytes, byte key )
		{
			for( var index = 0; index < bytes.Length; index++ )
			{
				bytes[index] ^= key;
			}
		}

		private struct Block
		{
			public string Tag;
			public long Position;
			public long Size;

			public long PayloadPosition
			{
				get
				{
					return this.Position + 8;
				}
			}

			public long EndPosition
			{
				get
				{
					return this.Position + this.Size;
				}
			}
		}

		private static IEnumerable<Block> ReadBlocks( BinaryReader reader, long start, long end )
		{
			var position = start;
			while( position + 8 <= end )
			{
				reader.BaseStream.Position = position;
				var tagBytes = reader.ReadBytes( 4 );
				var tag = Encoding.ASCII.GetString( tagBytes );
				var size = ReadInt32BigEndian( reader );

				// a block size includes its own 8 byte header; anything smaller (or
				// running past the parent block) means we hit data we don't understand
				if( size < 8 || position + size > end )
				{
					yield break;
				}

				yield return new Block
				{
					Tag = tag,
					Position = position,
					Size = size,
				};

				position += size;
			}
		}

		private static int ReadInt32BigEndian( BinaryReader reader )
		{
			var bytes = reader.ReadBytes( 4 );
			return ( bytes[0] << 24 ) | ( bytes[1] << 16 ) | ( bytes[2] << 8 ) | bytes[3];
		}

		private static void ReadRoomOffsets( BinaryReader reader, Block loff, Dictionary<long, int> roomNumberByOffset )
		{
			reader.BaseStream.Position = loff.PayloadPosition;
			var count = reader.ReadByte();
			for( var index = 0; index < count; index++ )
			{
				var roomNumber = reader.ReadByte();
				var offset = reader.ReadUInt32();
				roomNumberByOffset[offset] = roomNumber;
			}
		}

		private static ClassicRoom? ReadRoomFromLflf( BinaryReader reader, Block lflf, Dictionary<long, int> roomNumberByOffset, int fallbackRoomNumber )
		{
			foreach( var child in ReadBlocks( reader, lflf.PayloadPosition, lflf.EndPosition ) )
			{
				if( child.Tag != "ROOM" )
				{
					continue;
				}

				// the LOFF table may point at the ROOM block, the LFLF payload or the LFLF block itself
				int roomNumber;
				if( !roomNumberByOffset.TryGetValue( child.Position, out roomNumber )
					&& !roomNumberByOffset.TryGetValue( lflf.PayloadPosition, out roomNumber )
					&& !roomNumberByOffset.TryGetValue( lflf.Position, out roomNumber ) )
				{
					roomNumber = fallbackRoomNumber;
				}

				return ReadRoom( reader, child, roomNumber );
			}

			return null;
		}

		private static ClassicRoom ReadRoom( BinaryReader reader, Block roomBlock, int roomNumber )
		{
			var width = 0;
			var height = 0;
			var objectList = new List<ClassicObject>();

			foreach( var child in ReadBlocks( reader, roomBlock.PayloadPosition, roomBlock.EndPosition ) )
			{
				if( child.Tag == "RMHD" )
				{
					reader.BaseStream.Position = child.PayloadPosition;
					width = reader.ReadUInt16();
					height = reader.ReadUInt16();
				}
				else if( child.Tag == "OBCD" )
				{
					var classicObject = ReadObject( reader, child );
					if( classicObject != null )
					{
						objectList.Add( classicObject );
					}
				}
			}

			return new ClassicRoom(
				roomNumber: roomNumber,
				width: width,
				height: height,
				objectList: objectList
			);
		}

		private static ClassicObject? ReadObject( BinaryReader reader, Block obcd )
		{
			ClassicObject? classicObject = null;
			string? name = null;

			foreach( var child in ReadBlocks( reader, obcd.PayloadPosition, obcd.EndPosition ) )
			{
				if( child.Tag == "CDHD" )
				{
					reader.BaseStream.Position = child.PayloadPosition;
					var objectId = reader.ReadUInt16();

					// x, y, width and height are stored in 8 pixel strip units
					var x = reader.ReadByte() * 8;
					var y = reader.ReadByte() * 8;
					var objectWidth = reader.ReadByte() * 8;
					var objectHeight = reader.ReadByte() * 8;

					classicObject = new ClassicObject(
						objectId: objectId,
						x: x,
						y: y,
						width: objectWidth,
						height: objectHeight,
						name: null
					);
				}
				else if( child.Tag == "OBNA" )
				{
					reader.BaseStream.Position = child.PayloadPosition;
					name = ReadZeroTerminatedString( reader, child.EndPosition );
				}
			}

			if( classicObject != null )
			{
				classicObject.Name = name;
			}

			return classicObject;
		}

		private static string ReadZeroTerminatedString( BinaryReader reader, long endPosition )
		{
			var builder = new StringBuilder();
			while( reader.BaseStream.Position < endPosition )
			{
				var @byte = reader.ReadByte();
				if( @byte == 0 )
				{
					break;
				}
				builder.Append( (char)@byte );
			}
			return builder.ToString();
		}
	}
}
