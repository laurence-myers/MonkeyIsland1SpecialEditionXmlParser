using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms
{
	public static class Parser
	{
		public static Room ReadRoom( BinaryReader reader )
		{
			// read header
			var header = new Header(
				identifier: reader.ReadInt32(),
				nameAddress: reader.ReadInt32PlusBytePosition( value => value > 0 ),
				unkn03: reader.ReadInt32(),
				unkn04: reader.ReadInt32(),
				staticSpriteHeaderCount: reader.ReadInt32(),
				staticSpriteHeaderAddress: reader.ReadInt32PlusBytePosition( value => value > 0 ),
				spriteHeaderCount: reader.ReadInt32(),
				spriteHeaderAddress: reader.ReadInt32PlusBytePosition( value => value > 0 ),
				unkn09: reader.ReadInt32(),
				unkn10: reader.ReadInt32(),
				unknown6HeaderAddress1: reader.ReadInt32PlusBytePosition( value => value > 0 ),
				unknown6HeaderCount: reader.ReadInt32(),
				unknown6HeaderAddress2: reader.ReadInt32PlusBytePosition( value => value > 0 ),
				unknown4HeaderCount: reader.ReadInt32(),
				unknown4HeaderAddress: reader.ReadInt32PlusBytePosition( value => value > 0 ),
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
					unkn1: reader.ReadInt32(),
					unkn2: reader.ReadInt32(),
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
			reader.BaseStream.Position = header.Unknown6HeaderAddress2;
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

			// read unknown4 header list
			reader.BaseStream.Position = header.Unknown4HeaderAddress;
			var unknown4HeaderList = new List<Unknown4Header>();
			for( var index = 0; index < header.Unknown4HeaderCount; index++ )
			{
				var unknown4Header = new Unknown4Header(
					unknown4NameAddress: reader.ReadInt32PlusBytePosition( value => value > 0 ),
					unknown4Count: reader.ReadInt32(),
					unknown4Address: reader.ReadInt32PlusBytePosition( value => value > 0 )
				);
				unknown4HeaderList.Add( unknown4Header );
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

			// read unknown1 list
			var spriteGroupList = new List<SpriteGroup>();
			for( var index = 0; index < spriteHeaderList.Count; index++ )
			{
				var unknown2 = spriteHeaderList[index];
				var spriteList = new List<Sprite>();
				reader.BaseStream.Position = unknown2.SpriteAddress;
				for( var index2 = 0; index2 < unknown2.SpriteCount; index2++ )
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

			// read unknown 6 list
			var unknown6List = new List<Unknown6>();
			for( var index = 0; index < unknown6HeaderList.Count; index++ )
			{
				var unknown3 = unknown6HeaderList[index];
				reader.BaseStream.Position = unknown3.Unknown6Address;
				var unknown6 = new Unknown6(
					index: index,
					byteList: reader.ReadBytes( unknown3.Unknown6Count ).ToList()
				);
				unknown6List.Add( unknown6 );
			}

			// read unknown4 group list
			var unknown4GroupList = new List<Unknown4Group>();
			for( var index = 0; index < unknown4HeaderList.Count; index++ )
			{
				var unknown4Header = unknown4HeaderList[index];
				var unknown4List = new List<Unknown4>();
				reader.BaseStream.Position = unknown4Header.Unknown4Address;
				for( var index2 = 0; index2 < unknown4Header.Unknown4Count; index2++ )
				{
					var unknown4 = new Unknown4(
						index: index2,
						unknown41Address: reader.ReadInt32PlusBytePosition( value => value > 0 ),
						unknown42Address: reader.ReadInt32PlusBytePosition( value => value > 0 ),
						unkn3: reader.ReadSingle(),
						unkn4: reader.ReadSingle()
						);
					unknown4List.Add( unknown4 );
				}

				var unknown4Group = new Unknown4Group(
					unknown4List: unknown4List
				);
				unknown4GroupList.Add( unknown4Group );
			}

			// read unknown4_x entities
			foreach( var unknown4Group in unknown4GroupList )
			{
				foreach( var unknown4 in unknown4Group.Unknown4List )
				{
					if( unknown4.Unknown4_1Address > 0 )
					{
						reader.BaseStream.Position = unknown4.Unknown4_1Address;
						var unknown4_1 = new Unknown4_1(
							unkn1: reader.ReadInt32(),
							unkn2: reader.ReadInt32(),
							unkn3: reader.ReadInt32(),
							unkn4: reader.ReadInt32(),
							unkn5: reader.ReadInt32()
						);
						unknown4.Unknown4_1 = unknown4_1;
					}
					if( unknown4.Unknown4_2Address > 0 )
					{
						reader.BaseStream.Position = unknown4.Unknown4_2Address;
						var unknown4_2 = new Unknown4_2(
							unkn1: reader.ReadInt32(),
							unkn2: reader.ReadInt32(),
							unkn3: reader.ReadInt32(),
							unkn4: reader.ReadInt32()
						);
						unknown4.Unknown4_2 = unknown4_2;
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
				unknown4HeaderList: unknown4HeaderList,
				unknown5HeaderList: unknown5HeaderList,
				staticSpriteList: staticSpriteList,
				spriteGroupList: spriteGroupList,
				unknown6List: unknown6List,
				unknown4GroupList: unknown4GroupList,
				unknown5List: unknown5List
			);

			// validate and return room
			SanityChecker.Check( room );
			return room;
		}
	}
}
