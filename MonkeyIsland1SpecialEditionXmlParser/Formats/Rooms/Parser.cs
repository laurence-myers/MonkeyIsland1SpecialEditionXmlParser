using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms
{
	public static class Parser
	{
		/// <summary>
		/// Reads a Room from a single .dat file.
		/// </summary>
		/// <param name="fileName">The path to the .dat file.</param>
		/// <returns>The parsed Room object.</returns>
		public static Room ReadRoomFromBinaryFile( string fileName )
		{
			using( var stream = File.OpenRead( fileName ) )
			using( var reader = new BinaryReader( stream ) )
			{
				return ReadRoom( reader );
			}
		}

		/// <summary>
		/// Reads a Room from an XML file.
		/// </summary>
		/// <param name="fileName">The path to the XML file.</param>
		/// <returns>The parsed Room object.</returns>
		public static Room ReadRoomFromXmlFile( string fileName )
		{
			return Helper.ReadObjectFromFile<Room>( fileName );
		}

		public static Room ReadRoom( BinaryReader reader )
		{
			// read header
			var header = new Header(
				identifier: reader.ReadInt32(),
				nameAddress: reader.ReadInt32PlusBytePosition( value => value > 0 ),
				width: reader.ReadInt32(),
				height: reader.ReadInt32(),
				staticSpriteHeaderCount: reader.ReadInt32(),
				staticSpriteHeaderAddress: reader.ReadInt32PlusBytePosition( value => value > 0 ),
				spriteHeaderCount: reader.ReadInt32(),
				spriteHeaderAddress: reader.ReadInt32PlusBytePosition( value => value > 0 ),
				unknown9: reader.ReadInt32(),
				unknown6HeaderCountA: reader.ReadInt32(),
				unknown6HeaderAddressA: reader.ReadInt32PlusBytePosition( value => value > 0 ),
				unknown6HeaderCount: reader.ReadInt32(),
				unknown6HeaderAddress: reader.ReadInt32PlusBytePosition( value => value > 0 ),
				roomObjectHeaderCount: reader.ReadInt32(),
				roomObjectHeaderAddress: reader.ReadInt32PlusBytePosition( value => value > 0 ),
				unknown5HeaderCount: reader.ReadInt32(),
				unknown5HeaderAddress: reader.ReadInt32PlusBytePosition( value => value > 0 ),
				alwaysZero1: reader.ReadInt32(),
				alwaysZero2: reader.ReadInt32(),
				alwaysZero3: reader.ReadInt32(),
				name: reader.ReadStringMonkey()
			);

			// read static sprite header list
			reader.BaseStream.Position = header.StaticSpriteHeaderAddress;
			var staticSpriteHeaderList = new List<StaticSpriteHeader>();
			for( var index = 0; index < header.StaticSpriteHeaderCount; index++ )
			{
				var staticSpriteHeader = new StaticSpriteHeader(
					index: index,
					identifier: reader.ReadInt32(),
					sourceWidth: reader.ReadInt32(),
					sourceHeight: reader.ReadInt32(),
					staticSpriteCount: reader.ReadInt32(),
					staticSpriteAddress: reader.ReadInt32PlusBytePosition( value => value > 0 )
				);
				staticSpriteHeaderList.Add( staticSpriteHeader );
			}

			// read sprite header list
			reader.BaseStream.Position = header.SpriteHeaderAddress;
			var spriteHeaderList = new List<SpriteHeader>();
			for( var index = 0; index < header.SpriteHeaderCount; index++ )
			{
				var spriteHeader = new SpriteHeader(
					index: index,
					identifier: reader.ReadInt32(),
					spriteCount: reader.ReadInt32(),
					spriteAddress: reader.ReadInt32PlusBytePosition( value => value > 0 )
				);
				spriteHeaderList.Add( spriteHeader );
			}

			// read unknown6 header list
			reader.BaseStream.Position = header.Unknown6HeaderAddress;
			var unknown6HeaderList = new List<Unknown6Header>();
			for( var index = 0; index < header.Unknown6HeaderCount; index++ )
			{
				var unknown6Header = new Unknown6Header(
					unkn1: reader.ReadByte(),
					unkn2: reader.ReadByte(),
					unkn3: reader.ReadByte(),
					unkn4: reader.ReadByte(),
					unkn5: reader.ReadInt32(),
					unknown6Count: reader.ReadInt32(),
					unknown6Address: reader.ReadInt32PlusBytePosition( value => value > 0 )
				);
				unknown6HeaderList.Add( unknown6Header );
			}

			// read room object header list
			reader.BaseStream.Position = header.RoomObjectHeaderAddress;
			var roomObjectHeaderList = new List<RoomObjectHeader>();
			for( var index = 0; index < header.RoomObjectHeaderCount; index++ )
			{
				var roomObjectHeader = new RoomObjectHeader(
					nameAddress: reader.ReadInt32PlusBytePosition( value => value > 0 ),
					roomObjectCount: reader.ReadInt32(),
					roomObjectAddress: reader.ReadInt32PlusBytePosition( value => value > 0 )
				);
				roomObjectHeaderList.Add( roomObjectHeader );
			}

			// read unknown5 header list
			reader.BaseStream.Position = header.Unknown5HeaderAddress;
			var unknown5HeaderList = new List<Unknown5Header>();
			for( var index = 0; index < header.Unknown5HeaderCount; index++ )
			{
				var unknown5Header = new Unknown5Header(
					unknown5Count: reader.ReadInt32(),
					unknown5Address: reader.ReadInt32PlusBytePosition( value => value > 0 )
				);
				unknown5HeaderList.Add( unknown5Header );
			}

			// read static sprite list
			var staticSpriteList = new List<List<StaticSprite>>();
			for( var index = 0; index < staticSpriteHeaderList.Count; index++ )
			{
				var staticSpriteHeader = staticSpriteHeaderList[index];
				var innerStaticSpriteList = new List<StaticSprite>();
				reader.BaseStream.Position = staticSpriteHeader.StaticSpriteAddress;
				for( var index2 = 0; index2 < staticSpriteHeader.StaticSpriteCount; index2++ )
				{
					var staticSprite = new StaticSprite(
						index: index2,
						x: reader.ReadInt32(),
						y: reader.ReadInt32(),
						width: reader.ReadInt32(),
						height: reader.ReadInt32(),
						textureFileNameAddress: reader.ReadInt32PlusBytePosition( value => value > 0 )
					);
					innerStaticSpriteList.Add( staticSprite );
				}
				staticSpriteList.Add( innerStaticSpriteList );
			}
			foreach( var staticSprite in staticSpriteList )
			{
				foreach( var innerStaticSprite in staticSprite )
				{
					reader.BaseStream.Position = innerStaticSprite.TextureFileNameAddress;
					innerStaticSprite.TextureFileName = reader.ReadStringMonkey();
				}
			}

			// read sprite list
			var spriteGroupList = new List<SpriteGroup>();
			for( var index = 0; index < spriteHeaderList.Count; index++ )
			{
				var spriteHeader = spriteHeaderList[index];
				var spriteList = new List<Sprite>();
				reader.BaseStream.Position = spriteHeader.SpriteAddress;
				for( var index2 = 0; index2 < spriteHeader.SpriteCount; index2++ )
				{
					var sprite = new Sprite(
						index: index2,
						textureFileNameAddress: reader.ReadInt32PlusBytePosition( value => value > 0 ),
						textureX: reader.ReadInt32(),
						textureY: reader.ReadInt32(),
						textureWidth: reader.ReadInt32(),
						textureHeight: reader.ReadInt32(),
						offsetX: reader.ReadSingle(),
						offsetY: reader.ReadSingle(),
						layer: reader.ReadInt32()
					);
					spriteList.Add( sprite );
				}

				var spriteGroup = new SpriteGroup(
					spriteList: spriteList
				);
				spriteGroupList.Add( spriteGroup );
			}
			foreach( var spriteGroup in spriteGroupList )
			{
				foreach( var sprite in spriteGroup.SpriteList )
				{
					reader.BaseStream.Position = sprite.TextureFileNameAddress;
					sprite.TextureFileName = reader.ReadStringMonkey();
				}
			}

			// read unknown6 list
			var unknown6List = new List<Unknown6>();
			for( var index = 0; index < unknown6HeaderList.Count; index++ )
			{
				var unknown6Header = unknown6HeaderList[index];
				reader.BaseStream.Position = unknown6Header.Unknown6Address;
				var unknown6 = new Unknown6(
					index: index,
					byteList: reader.ReadBytes( unknown6Header.Unknown6Count ).ToList()
				);
				unknown6List.Add( unknown6 );
			}

			// read room object group list
			var roomObjectGroupList = new List<RoomObjectGroup>();
			for( var index = 0; index < roomObjectHeaderList.Count; index++ )
			{
				var roomObjectHeader = roomObjectHeaderList[index];

				// the object's name is stored separately, referenced by the header
				if( roomObjectHeader.NameAddress > 0 )
				{
					reader.BaseStream.Position = roomObjectHeader.NameAddress;
					roomObjectHeader.Name = reader.ReadStringMonkey();
				}

				var roomObjectList = new List<RoomObject>();
				reader.BaseStream.Position = roomObjectHeader.RoomObjectAddress;
				for( var index2 = 0; index2 < roomObjectHeader.RoomObjectCount; index2++ )
				{
					var roomObject = new RoomObject(
						index: index2,
						spriteAddress: reader.ReadInt32PlusBytePosition( value => value > 0 ),
						imageAddress: reader.ReadInt32PlusBytePosition( value => value > 0 ),
						offsetX: reader.ReadSingle(),
						offsetY: reader.ReadSingle()
					);
					roomObjectList.Add( roomObject );
				}

				var roomObjectGroup = new RoomObjectGroup(
					roomObjectList: roomObjectList
				);
				roomObjectGroupList.Add( roomObjectGroup );
			}

			// read the room object sprite / image records referenced by each object
			foreach( var roomObjectGroup in roomObjectGroupList )
			{
				foreach( var roomObject in roomObjectGroup.RoomObjectList )
				{
					if( roomObject.SpriteAddress > 0 )
					{
						reader.BaseStream.Position = roomObject.SpriteAddress;
						var sprite = new RoomObjectSprite(
							// this texture offset is signed - it usually points backwards
							// into the sprite texture name pool
							textureFileNameAddress: reader.ReadInt32PlusBytePosition( value => value != 0 ),
							x: reader.ReadInt32(),
							y: reader.ReadInt32(),
							width: reader.ReadInt32(),
							height: reader.ReadInt32()
						);
						roomObject.Sprite = sprite;
					}
					if( roomObject.ImageAddress > 0 )
					{
						reader.BaseStream.Position = roomObject.ImageAddress;
						var image = new RoomObjectImage(
							sourceWidth: reader.ReadInt32(),
							sourceHeight: reader.ReadInt32(),
							chunkAddress: ReadChunkCountThenAddress( reader, out var chunkCount )
						);
						reader.BaseStream.Position = image.ChunkAddress;
						for( var chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++ )
						{
							var chunk = new RoomObjectImageChunk(
								index: chunkIndex,
								x: reader.ReadInt32(),
								y: reader.ReadInt32(),
								width: reader.ReadInt32(),
								height: reader.ReadInt32(),
								textureFileNameAddress: reader.ReadInt32PlusBytePosition( value => value != 0 )
							);
							image.ChunkList.Add( chunk );
						}
						roomObject.Image = image;
					}
				}
			}

			// read the room object sprite / image texture file names
			foreach( var roomObjectGroup in roomObjectGroupList )
			{
				foreach( var roomObject in roomObjectGroup.RoomObjectList )
				{
					if( roomObject.Sprite != null && roomObject.Sprite.TextureFileNameAddress != 0 )
					{
						reader.BaseStream.Position = roomObject.Sprite.TextureFileNameAddress;
						roomObject.Sprite.TextureFileName = reader.ReadStringMonkey();
					}
					if( roomObject.Image != null )
					{
						foreach( var chunk in roomObject.Image.ChunkList )
						{
							if( chunk.TextureFileNameAddress != 0 )
							{
								reader.BaseStream.Position = chunk.TextureFileNameAddress;
								chunk.TextureFileName = reader.ReadStringMonkey();
							}
						}
					}
				}
			}

			// read unknown5 list
			var unknown5List = new List<Unknown5>();
			for( var index = 0; index < unknown5HeaderList.Count; index++ )
			{
				var unknown5Header = unknown5HeaderList[index];
				reader.BaseStream.Position = unknown5Header.Unknown5Address;
				var unknown5 = new Unknown5(
					index: index,
					int32List: reader.ReadInt32s( unknown5Header.Unknown5Count ).ToList()
				);
				unknown5List.Add( unknown5 );
			}

			// initialize room
			var room = new Room(
				header: header,
				staticSpriteHeaderList: staticSpriteHeaderList,
				spriteHeaderList: spriteHeaderList,
				unknown6HeaderList: unknown6HeaderList,
				roomObjectHeaderList: roomObjectHeaderList,
				unknown5HeaderList: unknown5HeaderList,
				staticSpriteList: staticSpriteList,
				spriteGroupList: spriteGroupList,
				unknown6List: unknown6List,
				roomObjectGroupList: roomObjectGroupList,
				unknown5List: unknown5List
			);

			// validate and return room
			SanityChecker.Check( room );
			return room;
		}

		/// <summary>
		/// Reads a room object image's chunk count followed by the relative address of the
		/// chunk array, returning the resolved chunk array address.
		/// </summary>
		private static int ReadChunkCountThenAddress( BinaryReader reader, out int chunkCount )
		{
			chunkCount = reader.ReadInt32();
			return reader.ReadInt32PlusBytePosition( value => value > 0 );
		}
	}
}
