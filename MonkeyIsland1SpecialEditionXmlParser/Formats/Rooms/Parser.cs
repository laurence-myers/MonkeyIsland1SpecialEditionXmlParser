using System.Collections.Generic;
using System.IO;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms
{
	public static class Parser
	{
		public static object Parse( string fileName )
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

		private static Room ReadRoom( BinaryReader reader )
		{
			// read header
			var header = new Header()
			{
				Identifier = reader.ReadInt32(),
				NameAddress = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				Unkn03 = reader.ReadInt32(),
				Unkn04 = reader.ReadInt32(),
				StaticTextureHeaderCount = reader.ReadInt32(),
				StaticTextureHeaderAddress = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				SpriteHeaderCount = reader.ReadInt32(),
				SpriteHeaderAddress = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				Unkn09 = reader.ReadInt32(),
				Unkn10 = reader.ReadInt32(),
				Unknown3Address1 = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				Unknown3Count = reader.ReadInt32(),
				Unknown3Address2 = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				Unknown4Count = reader.ReadInt32(),
				Unknown4Address = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				Unknown5Count = reader.ReadInt32(),
				Unknown5Address = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				AlwaysZero1 = reader.ReadInt32(),
				AlwaysZero2 = reader.ReadInt32(),
				AlwaysZero3 = reader.ReadInt32(),
				Name = reader.ReadStringMonkey(),
			};

			// read static texture header list
			reader.BaseStream.Position = header.StaticTextureHeaderAddress;
			var staticTextureHeaderList = new List<StaticTextureHeader>();
			for( var index = 0; index < header.StaticTextureHeaderCount; index++ )
			{
				var staticTextureHeader = new StaticTextureHeader()
				{
					Index = index,
					Identifier = reader.ReadInt32(),
					Unkn1 = reader.ReadInt32(),
					Unkn2 = reader.ReadInt32(),
					StaticTextureCount = reader.ReadInt32(),
					StaticTextureAddress = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				};
				staticTextureHeaderList.Add( staticTextureHeader );
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

			// read unknown3 list
			reader.BaseStream.Position = header.Unknown3Address2;
			var unknown3List = new List<Unknown3>();
			for( var index = 0; index < header.Unknown3Count; index++ )
			{
				var unknown3 = new Unknown3()
				{
					Unkn1 = reader.ReadByte(),
					Unkn2 = reader.ReadByte(),
					Unkn3 = reader.ReadByte(),
					Unkn4 = reader.ReadByte(),
					Unkn5 = reader.ReadInt32(),
					Unkn6 = reader.ReadInt32(),
					Addr7 = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				};
				unknown3List.Add( unknown3 );
			}

			// read unknown4 list
			reader.BaseStream.Position = header.Unknown4Address;
			var unknown4List = new List<Unknown4>();
			for( var index = 0; index < header.Unknown4Count; index++ )
			{
				var unknown4 = new Unknown4()
				{
					NameAddress = reader.ReadInt32PlusBytePosition( value => value > 0 ),
					DataCount = reader.ReadInt32(),
					DataAddress = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				};
				unknown4List.Add( unknown4 );
			}

			// read unknown5 list
			reader.BaseStream.Position = header.Unknown5Address;
			var unknown5List = new List<Unknown5>();
			for( var index = 0; index < header.Unknown5Count; index++ )
			{
				var unknown5 = new Unknown5()
				{
					Unkn1 = reader.ReadInt32(),
					Addr2 = reader.ReadInt32PlusBytePosition( value => value > 0 ),
					Unkn3 = reader.ReadInt32(),
					Unkn4 = reader.ReadInt32(),
				};
				unknown5List.Add( unknown5 );
			}

			// read static texture list
			var staticTextureList = new List<List<StaticTexture>>();
			for( var index = 0; index < staticTextureHeaderList.Count; index++ )
			{
				var staticTextureHeader = staticTextureHeaderList[index];
				var innerStaticTextureList = new List<StaticTexture>();
				reader.BaseStream.Position = staticTextureHeader.StaticTextureAddress;
				for( var index2 = 0; index2 < staticTextureHeader.StaticTextureCount; index2++ )
				{
					var staticTexture = new StaticTexture()
					{
						Index = index2,
						X = reader.ReadInt32(),
						Y = reader.ReadInt32(),
						Width = reader.ReadInt32(),
						Height = reader.ReadInt32(),
						TextureFileNameAddress = reader.ReadInt32PlusBytePosition( value => value > 0 ),
					};
					innerStaticTextureList.Add( staticTexture );
				}
				staticTextureList.Add( innerStaticTextureList );
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

			// initialize room
			var room = new Room()
			{
				Header = header,
				StaticTextureHeaderList = staticTextureHeaderList,
				SpriteHeaderList = spriteHeaderList,
				Unknown3List = unknown3List,
				Unknown4List = unknown4List,
				Unknown5List = unknown5List,
				StaticTextureList = staticTextureList,
				SpriteGroupList = spriteGroupList,
			};

			// validate and return room
			SanityChecker.Check( room );
			return room;
		}
	}
}
