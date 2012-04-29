using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;
using System;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms
{
	public static class Parser
	{
		public static Room Parse( string fileName )
		{
			Stream stream = null;
			BinaryReader reader = null;
			Room room;

			try
			{
				stream = File.OpenRead( fileName );
				reader = new BinaryReader( stream );
				room = Parser.ReadRoom( reader );
			}
			finally
			{
				if( reader != null )
				{
					reader.Dispose();
					reader = null;
				}
				if( stream != null )
				{
					stream.Close();
					stream.Dispose();
					stream = null;
				}
			}

			return room;
		}

		public static Room ReadRoom( BinaryReader reader )
		{
			// read header
			var header = new Header()
			{
				Identifier = reader.ReadInt32(),
				NameAddress = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				Unkn03 = reader.ReadInt32(),
				Unkn04 = reader.ReadInt32(),
				StaticSpriteHeaderCount = reader.ReadInt32(),
				StaticSpriteHeaderAddress = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				SpriteHeaderCount = reader.ReadInt32(),
				SpriteHeaderAddress = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				Unkn09 = reader.ReadInt32(),
				Unkn10 = reader.ReadInt32(),
				Unknown6HeaderAddress1 = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				Unknown6HeaderCount = reader.ReadInt32(),
				Unknown6HeaderAddress2 = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				Unknown4HeaderCount = reader.ReadInt32(),
				Unknown4HeaderAddress = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				Unknown5HeaderCount = reader.ReadInt32(),
				Unknown5HeaderAddress = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				AlwaysZero1 = reader.ReadInt32(),
				AlwaysZero2 = reader.ReadInt32(),
				AlwaysZero3 = reader.ReadInt32(),
				Name = reader.ReadStringMonkey(),
			};

			// read static sprite header list
			reader.BaseStream.Position = header.StaticSpriteHeaderAddress;
			var staticSpriteHeaderList = new List<StaticSpriteHeader>();
			for( var index = 0; index < header.StaticSpriteHeaderCount; index++ )
			{
				var staticSpriteHeader = new StaticSpriteHeader()
				{
					Index = index,
					Identifier = reader.ReadInt32(),
					Unkn1 = reader.ReadInt32(),
					Unkn2 = reader.ReadInt32(),
					StaticSpriteCount = reader.ReadInt32(),
					StaticSpriteAddress = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				};
				staticSpriteHeaderList.Add( staticSpriteHeader );
			}

			// read sprite header list
			reader.BaseStream.Position = header.SpriteHeaderAddress;
			var spriteHeaderList = new List<SpriteHeader>();
			for( var index = 0; index < header.SpriteHeaderCount; index++ )
			{
				var spriteHeader = new SpriteHeader()
				{
					Index = index,
					Identifier = reader.ReadInt32(),
					SpriteCount = reader.ReadInt32(),
					SpriteAddress = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				};
				spriteHeaderList.Add( spriteHeader );
			}

			// read unknown6 header list
			reader.BaseStream.Position = header.Unknown6HeaderAddress2;
			var unknown6HeaderList = new List<Unknown6Header>();
			for( var index = 0; index < header.Unknown6HeaderCount; index++ )
			{
				var unknown6Header = new Unknown6Header()
				{
					Unkn1 = reader.ReadByte(),
					Unkn2 = reader.ReadByte(),
					Unkn3 = reader.ReadByte(),
					Unkn4 = reader.ReadByte(),
					Unkn5 = reader.ReadInt32(),
					Unknown6Count = reader.ReadInt32(),
					Unknown6Address = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				};
				unknown6Header.Unkn12 = BitConverter.ToInt16( new[] { unknown6Header.Unkn1, unknown6Header.Unkn2 }, 0 );
				unknown6Header.Unkn34 = BitConverter.ToInt16( new[] { unknown6Header.Unkn3, unknown6Header.Unkn4 }, 0 );
				unknown6HeaderList.Add( unknown6Header );
			}

			// read unknown4 header list
			reader.BaseStream.Position = header.Unknown4HeaderAddress;
			var unknown4HeaderList = new List<Unknown4Header>();
			for( var index = 0; index < header.Unknown4HeaderCount; index++ )
			{
				var unknown4Header = new Unknown4Header()
				{
					Unknown4NameAddress = reader.ReadInt32PlusBytePosition( value => value > 0 ),
					Unknown4Count = reader.ReadInt32(),
					Unknown4Address = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				};
				unknown4HeaderList.Add( unknown4Header );
			}

			// read unknown5 header list
			reader.BaseStream.Position = header.Unknown5HeaderAddress;
			var unknown5HeaderList = new List<Unknown5Header>();
			for( var index = 0; index < header.Unknown5HeaderCount; index++ )
			{
				var unknown5Header = new Unknown5Header()
				{
					Unknown5Count = reader.ReadInt32(),
					Unknown5Address = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				};
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
					var staticSprite = new StaticSprite()
					{
						Index = index2,
						X = reader.ReadInt32(),
						Y = reader.ReadInt32(),
						Width = reader.ReadInt32(),
						Height = reader.ReadInt32(),
						TextureFileNameAddress = reader.ReadInt32PlusBytePosition( value => value > 0 ),
					};
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
					var sprite = new Sprite()
					{
						Index = index2,
						TextureFileNameAddress = reader.ReadInt32PlusBytePosition( value => value > 0 ),
						TextureX = reader.ReadInt32(),
						TextureY = reader.ReadInt32(),
						TextureWidth = reader.ReadInt32(),
						TextureHeight = reader.ReadInt32(),
						OffsetX = reader.ReadSingle(),
						OffsetY = reader.ReadSingle(),
						Layer = reader.ReadInt32(),
					};
					spriteList.Add( sprite );
				}

				var spriteGroup = new SpriteGroup()
				{
					SpriteList = spriteList,
				};
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
				var unknown6 = new Unknown6()
				{
					Index = index,
					ByteList = reader.ReadBytes( unknown3.Unknown6Count ).ToList(),
				};
				unknown6.Int16List = unknown6.ByteList.ToInt16List();
				unknown6.Int32List = unknown6.ByteList.ToInt32List();
				unknown6.FloatList = unknown6.ByteList.ToFloatList();
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
					var unknown4 = new Unknown4()
					{
						Index = index2,
						Unknown4_1Address = reader.ReadInt32PlusBytePosition( value => value > 0 ),
						Unknown4_2Address = reader.ReadInt32PlusBytePosition( value => value > 0 ),
						Unkn3 = reader.ReadSingle(),
						Unkn4 = reader.ReadSingle(),
					};
					unknown4List.Add( unknown4 );
				}

				var unknown4Group = new Unknown4Group()
				{
					Unknown4List = unknown4List,
				};
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
						var unknown4_1 = new Unknown4_1()
						{
							Unkn1 = reader.ReadInt32(),
							Unkn2 = reader.ReadInt32(),
							Unkn3 = reader.ReadInt32(),
							Unkn4 = reader.ReadInt32(),
							Unkn5 = reader.ReadInt32(),
						};
						unknown4.Unknown4_1 = unknown4_1;
					}
					if( unknown4.Unknown4_2Address > 0 )
					{
						reader.BaseStream.Position = unknown4.Unknown4_2Address;
						var unknown4_2 = new Unknown4_2()
						{
							Unkn1 = reader.ReadInt32(),
							Unkn2 = reader.ReadInt32(),
							Unkn3 = reader.ReadInt32(),
							Unkn4 = reader.ReadInt32(),
						};
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
				var unknown5 = new Unknown5()
				{
					Index = index,
					Int32List = reader.ReadInt32s( unknown5Header.Unknown5Count ).ToList(),
				};
				unknown5List.Add( unknown5 );
			}

			// initialize room
			var room = new Room()
			{
				Header = header,
				StaticSpriteHeaderList = staticSpriteHeaderList,
				SpriteHeaderList = spriteHeaderList,
				Unknown6HeaderList = unknown6HeaderList,
				Unknown4HeaderList = unknown4HeaderList,
				Unknown5HeaderList = unknown5HeaderList,
				StaticSpriteList = staticSpriteList,
				SpriteGroupList = spriteGroupList,
				Unknown6List = unknown6List,
				Unknown4GroupList = unknown4GroupList,
				Unknown5List = unknown5List,
			};

			// validate and return room
			SanityChecker.Check( room );
			return room;
		}
	}
}
