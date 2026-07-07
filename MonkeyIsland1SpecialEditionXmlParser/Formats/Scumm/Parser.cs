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
		/// Reads all costumes from classic data files (monkey1.001 resource file plus
		/// monkey1.000 index file; the index is required because only its DCOS directory
		/// maps costume numbers to rooms and offsets).
		/// </summary>
		public static List<ClassicCostume> ReadCostumesFromFiles( string dataFileName, string indexFileName )
		{
			return ReadCostumesFromEncodedBytes( File.ReadAllBytes( dataFileName ), File.ReadAllBytes( indexFileName ) );
		}

		/// <summary>
		/// Reads all costumes from the still XOR encoded contents of the resource and index files.
		/// </summary>
		public static List<ClassicCostume> ReadCostumesFromEncodedBytes( byte[] dataBytes, byte[] indexBytes )
		{
			XorDecode( dataBytes, Parser.XorKey );
			XorDecode( indexBytes, Parser.XorKey );
			using( var dataStream = new MemoryStream( dataBytes ) )
			using( var dataReader = new BinaryReader( dataStream ) )
			using( var indexStream = new MemoryStream( indexBytes ) )
			using( var indexReader = new BinaryReader( indexStream ) )
			{
				return ReadCostumes( dataReader, indexReader );
			}
		}

		/// <summary>
		/// Reads all costumes from already decoded resource and index streams.
		/// </summary>
		public static List<ClassicCostume> ReadCostumes( BinaryReader dataReader, BinaryReader indexReader )
		{
			var costumeList = new List<ClassicCostume>();

			// the DCOS directory in the index file maps each costume number to a room
			// number and an offset relative to that room's ROOM block
			var directory = ReadCostumeDirectory( indexReader );

			// the LOFF table in the resource file maps room numbers to absolute offsets
			var offsetByRoomNumber = new Dictionary<int, long>();
			foreach( var lecf in ReadBlocks( dataReader, 0, dataReader.BaseStream.Length ) )
			{
				if( lecf.Tag != "LECF" )
				{
					continue;
				}
				foreach( var child in ReadBlocks( dataReader, lecf.PayloadPosition, lecf.EndPosition ) )
				{
					if( child.Tag == "LOFF" )
					{
						dataReader.BaseStream.Position = child.PayloadPosition;
						var count = dataReader.ReadByte();
						for( var index = 0; index < count; index++ )
						{
							var roomNumber = dataReader.ReadByte();
							var offset = dataReader.ReadUInt32();
							offsetByRoomNumber[roomNumber] = offset;
						}
					}
				}
			}

			foreach( var entry in directory )
			{
				long roomOffset;
				if( !offsetByRoomNumber.TryGetValue( entry.RoomNumber, out roomOffset ) )
				{
					continue;
				}

				try
				{
					var costume = ReadCostume( dataReader, roomOffset + entry.Offset, entry.CostumeId, entry.RoomNumber );
					if( costume != null )
					{
						costumeList.Add( costume );
					}
				}
				catch( System.Exception )
				{
					// a single malformed costume should not lose the rest
				}
			}

			return costumeList;
		}

		private struct CostumeDirectoryEntry
		{
			public int CostumeId;
			public int RoomNumber;
			public long Offset;
		}

		private static List<CostumeDirectoryEntry> ReadCostumeDirectory( BinaryReader indexReader )
		{
			var directory = new List<CostumeDirectoryEntry>();

			foreach( var block in ReadBlocks( indexReader, 0, indexReader.BaseStream.Length ) )
			{
				if( block.Tag != "DCOS" )
				{
					continue;
				}

				indexReader.BaseStream.Position = block.PayloadPosition;
				var count = indexReader.ReadUInt16();
				var roomNumbers = indexReader.ReadBytes( count );
				for( var costumeId = 0; costumeId < count; costumeId++ )
				{
					var offset = indexReader.ReadUInt32();
					if( roomNumbers[costumeId] == 0 && offset == 0 )
					{
						continue;
					}
					directory.Add( new CostumeDirectoryEntry
					{
						CostumeId = costumeId,
						RoomNumber = roomNumbers[costumeId],
						Offset = offset,
					} );
				}
			}

			return directory;
		}

		/// <summary>
		/// Reads a single COST resource. All offsets inside the costume are relative to the
		/// block position plus two (matching the classic engine's base pointer).
		/// </summary>
		private static ClassicCostume? ReadCostume( BinaryReader reader, long position, int costumeId, int roomNumber )
		{
			if( position + 8 > reader.BaseStream.Length )
			{
				return null;
			}

			reader.BaseStream.Position = position;
			var tag = Encoding.ASCII.GetString( reader.ReadBytes( 4 ) );
			var blockSize = ReadInt32BigEndian( reader );
			if( tag != "COST" || blockSize < 8 || position + blockSize > reader.BaseStream.Length )
			{
				return null;
			}

			var basePosition = position + 2;
			long blockEnd = position + blockSize;

			int ReadByteAt( long offset )
			{
				reader.BaseStream.Position = basePosition + offset;
				return reader.ReadByte();
			}

			int ReadUInt16At( long offset )
			{
				reader.BaseStream.Position = basePosition + offset;
				return reader.ReadUInt16();
			}

			var maximumAnimationNumber = ReadByteAt( 6 );
			var formatByte = ReadByteAt( 7 );
			var format = formatByte & 0x7F;
			var mirror = ( formatByte & 0x80 ) != 0;
			var numberOfColors = format == 0x59 ? 32 : 16;

			var animationCommandsOffset = ReadUInt16At( numberOfColors + 8 );
			var limbTableOffsets = new int[16];
			for( var limb = 0; limb < 16; limb++ )
			{
				limbTableOffsets[limb] = ReadUInt16At( numberOfColors + 10 + limb * 2 );
			}

			// walk the animation definitions to find the highest cel index each limb shows;
			// a limb's cel table can extend past the next limb's table offset otherwise
			var maximumCelIndexes = new int[16];
			for( var limb = 0; limb < 16; limb++ )
			{
				maximumCelIndexes[limb] = -1;
			}
			for( var animation = 0; animation <= maximumAnimationNumber; animation++ )
			{
				var animationOffset = ReadUInt16At( numberOfColors + 42 + animation * 2 );
				if( animationOffset == 0 || basePosition + animationOffset >= blockEnd )
				{
					continue;
				}

				reader.BaseStream.Position = basePosition + animationOffset;
				var mask = reader.ReadUInt16();
				for( var limb = 0; limb < 16 && reader.BaseStream.Position < blockEnd; limb++, mask <<= 1 )
				{
					if( ( mask & 0x8000 ) == 0 )
					{
						continue;
					}
					var start = reader.ReadUInt16();
					if( start == 0xFFFF )
					{
						continue;
					}
					var lengthByte = reader.ReadByte();
					var length = lengthByte & 0x7F;

					var commandsPosition = reader.BaseStream.Position;
					for( var step = 0; step <= length; step++ )
					{
						var commandOffset = basePosition + animationCommandsOffset + start + step;
						if( commandOffset >= blockEnd )
						{
							break;
						}
						reader.BaseStream.Position = commandOffset;
						var command = reader.ReadByte();
						if( command < 0x71 && command > maximumCelIndexes[limb] )
						{
							maximumCelIndexes[limb] = command;
						}
					}
					reader.BaseStream.Position = commandsPosition;
				}
			}

			// each limb's cel table runs up to the next distinct table offset; the last
			// table is bounded by its own first cel's picture data
			var limbList = new List<ClassicLimb>();
			for( var limb = 0; limb < 16; limb++ )
			{
				var tableOffset = limbTableOffsets[limb];
				var boundOffset = int.MaxValue;
				for( var other = 0; other < 16; other++ )
				{
					if( limbTableOffsets[other] > tableOffset && limbTableOffsets[other] < boundOffset )
					{
						boundOffset = limbTableOffsets[other];
					}
				}
				if( boundOffset == int.MaxValue )
				{
					var firstCelOffset = ReadUInt16At( tableOffset );
					boundOffset = firstCelOffset > tableOffset ? firstCelOffset : tableOffset;
				}

				var celCount = ( boundOffset - tableOffset ) / 2;
				if( maximumCelIndexes[limb] + 1 > celCount )
				{
					celCount = maximumCelIndexes[limb] + 1;
				}
				celCount = (int)System.Math.Min( celCount, ( blockEnd - ( basePosition + tableOffset ) ) / 2 );
				if( celCount <= 0 )
				{
					continue;
				}

				var celList = new List<ClassicCel?>();
				for( var celIndex = 0; celIndex < celCount; celIndex++ )
				{
					var celOffset = ReadUInt16At( tableOffset + celIndex * 2 );
					if( celOffset == 0 || basePosition + celOffset + 12 > blockEnd )
					{
						celList.Add( null );
						continue;
					}
					reader.BaseStream.Position = basePosition + celOffset;
					var width = reader.ReadUInt16();
					var height = reader.ReadUInt16();
					var relX = reader.ReadInt16();
					var relY = reader.ReadInt16();
					var moveX = reader.ReadInt16();
					var moveY = reader.ReadInt16();
					celList.Add( new ClassicCel(
						index: celIndex,
						width: width,
						height: height,
						relX: relX,
						relY: relY,
						moveX: moveX,
						moveY: moveY
					) );
				}
				limbList.Add( new ClassicLimb( limbNumber: limb, celList: celList ) );
			}

			return new ClassicCostume(
				costumeId: costumeId,
				roomNumber: roomNumber,
				maximumAnimationNumber: maximumAnimationNumber,
				format: format,
				mirror: mirror,
				limbList: limbList
			);
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
			var boxList = new List<ClassicBox>();

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
				else if( child.Tag == "BOXD" )
				{
					boxList = ReadBoxes( reader, child );
				}
			}

			var room = new ClassicRoom(
				roomNumber: roomNumber,
				width: width,
				height: height,
				objectList: objectList
			);
			room.BoxList = boxList;
			return room;
		}

		/// <summary>
		/// Reads the walkboxes (BOXD): a 16 bit box count followed by 20 byte records of
		/// four 16 bit corner points, the mask (z-plane) byte, a flags byte and a 16 bit
		/// scale value.
		/// </summary>
		private static List<ClassicBox> ReadBoxes( BinaryReader reader, Block boxd )
		{
			var boxList = new List<ClassicBox>();
			reader.BaseStream.Position = boxd.PayloadPosition;
			int count = reader.ReadUInt16();
			for( var index = 0; index < count; index++ )
			{
				if( reader.BaseStream.Position + 20 > boxd.EndPosition )
				{
					break;
				}
				var corners = new System.Drawing.Point[4];
				for( var corner = 0; corner < 4; corner++ )
				{
					int x = reader.ReadInt16();
					int y = reader.ReadInt16();
					corners[corner] = new System.Drawing.Point( x, y );
				}
				var mask = reader.ReadByte();
				var flags = reader.ReadByte();
				var scale = reader.ReadUInt16();
				boxList.Add( new ClassicBox(
					cornerList: corners,
					mask: mask,
					flags: flags,
					scale: scale
				) );
			}
			return boxList;
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
