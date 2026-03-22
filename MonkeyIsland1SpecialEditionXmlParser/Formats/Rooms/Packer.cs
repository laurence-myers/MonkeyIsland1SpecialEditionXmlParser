using System.Collections.Generic;
using System.IO;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms
{
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
			long startPosition = writer.BaseStream.Position;

			// 1. Write Header (80 bytes placeholder)
			long headerOffset = writer.BaseStream.Position;
			writer.Write( new byte[80] );

			// 2. Write Header Name (padded to 16 bytes)
			long nameAddress = writer.BaseStream.Position;
			WriteStringMonkey( writer, room.Header.Name );

			// 3. Write Static Sprite Header List (fixed size per item)
			long staticSpriteHeaderAddress = writer.BaseStream.Position;
			foreach( var staticSpriteHeader in room.StaticSpriteHeaderList )
			{
				writer.Write( 0 ); // Identifier placeholder
				writer.Write( 0 ); // Unkn1 placeholder
				writer.Write( 0 ); // Unkn2 placeholder
				writer.Write( 0 ); // StaticSpriteCount placeholder
				writer.Write( 0 ); // StaticSpriteAddress placeholder
			}

			// 4. Write Sprite Header List
			long spriteHeaderAddress = writer.BaseStream.Position;
			foreach( var spriteHeader in room.SpriteHeaderList )
			{
				writer.Write( 0 ); // Identifier placeholder
				writer.Write( 0 ); // SpriteCount placeholder
				writer.Write( 0 ); // SpriteAddress placeholder
			}

			// 5. Write Unknown6 Header List
			long unknown6HeaderAddress = writer.BaseStream.Position;
			foreach( var unknown6Header in room.Unknown6HeaderList )
			{
				writer.Write( (byte)unknown6Header.Unkn1 );
				writer.Write( (byte)unknown6Header.Unkn2 );
				writer.Write( (byte)unknown6Header.Unkn3 );
				writer.Write( (byte)unknown6Header.Unkn4 );
				writer.Write( unknown6Header.Unkn5 );
				writer.Write( 0 ); // Unknown6Count placeholder
				writer.Write( 0 ); // Unknown6Address placeholder
			}

			// 6. Write Unknown4 Header List
			long unknown4HeaderAddress = writer.BaseStream.Position;
			foreach( var unknown4Header in room.Unknown4HeaderList )
			{
				writer.Write( 0 ); // Unknown4NameAddress placeholder
				writer.Write( 0 ); // Unknown4Count placeholder
				writer.Write( 0 ); // Unknown4Address placeholder
			}

			// 7. Write Unknown5 Header List
			long unknown5HeaderAddress = writer.BaseStream.Position;
			foreach( var unknown5Header in room.Unknown5HeaderList )
			{
				writer.Write( 0 ); // Unknown5Count placeholder
				writer.Write( 0 ); // Unknown5Address placeholder
			}

			// 8. Write Static Sprite Lists
			List<List<long>> staticSpriteAddressList = new List<List<long>>();
			for( int i = 0; i < room.StaticSpriteList.Count; i++ )
			{
				var staticSpriteList = room.StaticSpriteList[i];
				List<long> innerAddressList = new List<long>();

				foreach( var staticSprite in staticSpriteList )
				{
					innerAddressList.Add( writer.BaseStream.Position );
					writer.Write( staticSprite.X );
					writer.Write( staticSprite.Y );
					writer.Write( staticSprite.Width );
					writer.Write( staticSprite.Height );
					writer.Write( 0 ); // TextureFileNameAddress placeholder
				}

				staticSpriteAddressList.Add( innerAddressList );
			}

			// 9. Write Static Sprite Texture File Names
			for( int i = 0; i < room.StaticSpriteList.Count; i++ )
			{
				var staticSpriteList = room.StaticSpriteList[i];
				var innerAddressList = staticSpriteAddressList[i];

				for( int j = 0; j < staticSpriteList.Count; j++ )
				{
					var staticSprite = staticSpriteList[j];
					long textureFileNameAddress = writer.BaseStream.Position;
					WriteStringMonkey( writer, staticSprite.TextureFileName ?? "" );

					// Backpatch TextureFileNameAddress
					long currentPos = writer.BaseStream.Position;
					writer.BaseStream.Position = innerAddressList[j] + 16; // X, Y, Width, Height = 4*4 = 16 bytes
					writer.Write( (int)( textureFileNameAddress - writer.BaseStream.Position ) );
					writer.BaseStream.Position = currentPos;
				}
			}

			// 10. Write Sprite Groups
			List<List<long>> spriteAddressList = new List<List<long>>();
			for( int i = 0; i < room.SpriteGroupList.Count; i++ )
			{
				var spriteGroup = room.SpriteGroupList[i];
				List<long> innerAddressList = new List<long>();

				foreach( var sprite in spriteGroup.SpriteList )
				{
					innerAddressList.Add( writer.BaseStream.Position );
					writer.Write( 0 ); // TextureFileNameAddress placeholder
					writer.Write( sprite.TextureX );
					writer.Write( sprite.TextureY );
					writer.Write( sprite.TextureWidth );
					writer.Write( sprite.TextureHeight );
					writer.Write( sprite.OffsetX );
					writer.Write( sprite.OffsetY );
					writer.Write( sprite.Layer );
				}

				spriteAddressList.Add( innerAddressList );
			}

			// 11. Write Sprite Texture File Names
			for( int i = 0; i < room.SpriteGroupList.Count; i++ )
			{
				var spriteGroup = room.SpriteGroupList[i];
				var innerAddressList = spriteAddressList[i];

				for( int j = 0; j < spriteGroup.SpriteList.Count; j++ )
				{
					var sprite = spriteGroup.SpriteList[j];
					long textureFileNameAddress = writer.BaseStream.Position;
					WriteStringMonkey( writer, sprite.TextureFileName ?? "" );

					// Backpatch TextureFileNameAddress
					long currentPos = writer.BaseStream.Position;
					writer.BaseStream.Position = innerAddressList[j];
					writer.Write( (int)( textureFileNameAddress - writer.BaseStream.Position ) );
					writer.BaseStream.Position = currentPos;
				}
			}

			// 12. Write Unknown6 Lists
			List<long> unknown6AddressList = new List<long>();
			for( int i = 0; i < room.Unknown6List.Count; i++ )
			{
				var unknown6 = room.Unknown6List[i];
				unknown6AddressList.Add( writer.BaseStream.Position );
				writer.Write( unknown6.ByteList.ToArray() );
			}

			// 13. Write Unknown4 Groups
			List<List<long>> unknown4AddressList = new List<List<long>>();
			for( int i = 0; i < room.Unknown4GroupList.Count; i++ )
			{
				var unknown4Group = room.Unknown4GroupList[i];
				List<long> innerAddressList = new List<long>();

				foreach( var unknown4 in unknown4Group.Unknown4List )
				{
					innerAddressList.Add( writer.BaseStream.Position );
					writer.Write( 0 ); // Unknown4_1Address placeholder
					writer.Write( 0 ); // Unknown4_2Address placeholder
					writer.Write( unknown4.Unkn3 );
					writer.Write( unknown4.Unkn4 );
				}

				unknown4AddressList.Add( innerAddressList );
			}

			// 14. Write Unknown4_x entities and backpatch
			for( int i = 0; i < room.Unknown4GroupList.Count; i++ )
			{
				var unknown4Group = room.Unknown4GroupList[i];
				var innerAddressList = unknown4AddressList[i];

				for( int j = 0; j < unknown4Group.Unknown4List.Count; j++ )
				{
					var unknown4 = unknown4Group.Unknown4List[j];

					if( unknown4.Unknown4_1 != null )
					{
						long unknown4_1Address = writer.BaseStream.Position;
						writer.Write( unknown4.Unknown4_1.Unkn1 );
						writer.Write( unknown4.Unknown4_1.Unkn2 );
						writer.Write( unknown4.Unknown4_1.Unkn3 );
						writer.Write( unknown4.Unknown4_1.Unkn4 );
						writer.Write( unknown4.Unknown4_1.Unkn5 );

						// Backpatch Unknown4_1Address
						long currentPos = writer.BaseStream.Position;
						writer.BaseStream.Position = innerAddressList[j];
						writer.Write( (int)( unknown4_1Address - writer.BaseStream.Position ) );
						writer.BaseStream.Position = currentPos;
					}

					if( unknown4.Unknown4_2 != null )
					{
						long unknown4_2Address = writer.BaseStream.Position;
						writer.Write( unknown4.Unknown4_2.Unkn1 );
						writer.Write( unknown4.Unknown4_2.Unkn2 );
						writer.Write( unknown4.Unknown4_2.Unkn3 );
						writer.Write( unknown4.Unknown4_2.Unkn4 );

						// Backpatch Unknown4_2Address
						long currentPos = writer.BaseStream.Position;
						writer.BaseStream.Position = innerAddressList[j] + 4; // After Unknown4_1Address
						writer.Write( (int)( unknown4_2Address - writer.BaseStream.Position ) );
						writer.BaseStream.Position = currentPos;
					}
				}
			}

			// 15. Write Unknown5 Lists
			List<long> unknown5AddressList = new List<long>();
			for( int i = 0; i < room.Unknown5List.Count; i++ )
			{
				var unknown5 = room.Unknown5List[i];
				unknown5AddressList.Add( writer.BaseStream.Position );
				foreach( var int32Value in unknown5.Int32List )
				{
					writer.Write( int32Value );
				}
			}

			// 16. Write Unknown4 Names (before endOfFile!)
			List<long> unknown4NameAddressList = new List<long>();
			for( int i = 0; i < room.Unknown4HeaderList.Count; i++ )
			{
				long position = writer.BaseStream.Position;
				unknown4NameAddressList.Add( position );
				WriteStringMonkey( writer, "" ); // Unknown4 names appear to be empty strings
			}

			long endOfFile = writer.BaseStream.Position;

			// --- BACKPATCHING ---

			// Patch Static Sprite Headers
			writer.BaseStream.Position = staticSpriteHeaderAddress;
			for( int i = 0; i < room.StaticSpriteHeaderList.Count; i++ )
			{
				var ssh = room.StaticSpriteHeaderList[i];
				writer.Write( ssh.Identifier );
				writer.Write( ssh.Unkn1 );
				writer.Write( ssh.Unkn2 );
				
				int count = ( i < room.StaticSpriteList.Count ) ? room.StaticSpriteList[i].Count : 0;
				writer.Write( count );

				long addr = ( i < staticSpriteAddressList.Count && staticSpriteAddressList[i].Count > 0 )
					? staticSpriteAddressList[i][0] : 0;
				writer.Write( addr > 0 ? (int)( addr - writer.BaseStream.Position ) : 0 );
			}

			// Patch Sprite Headers
			writer.BaseStream.Position = spriteHeaderAddress;
			for( int i = 0; i < room.SpriteHeaderList.Count; i++ )
			{
				var sh = room.SpriteHeaderList[i];
				writer.Write( sh.Identifier );
				
				int count = ( i < room.SpriteGroupList.Count ) ? room.SpriteGroupList[i].SpriteList.Count : 0;
				writer.Write( count );

				long addr = ( i < spriteAddressList.Count && spriteAddressList[i].Count > 0 )
					? spriteAddressList[i][0] : 0;
				writer.Write( addr > 0 ? (int)( addr - writer.BaseStream.Position ) : 0 );
			}

			// Patch Unknown6 Headers
			writer.BaseStream.Position = unknown6HeaderAddress;
			for( int i = 0; i < room.Unknown6HeaderList.Count; i++ )
			{
				var u6h = room.Unknown6HeaderList[i];
				writer.BaseStream.Position += 8; // Skip unkn1-4 (4 bytes) and unkn5 (4 bytes)
				writer.Write( room.Unknown6List[i].ByteList.Count );
				writer.Write( (int)( unknown6AddressList[i] - writer.BaseStream.Position ) );
			}

			// Patch Unknown4 Headers
			writer.BaseStream.Position = unknown4HeaderAddress;
			for( int i = 0; i < room.Unknown4HeaderList.Count; i++ )
			{
				var u4h = room.Unknown4HeaderList[i];
				writer.Write( (int)( unknown4NameAddressList[i] - writer.BaseStream.Position ) );
				writer.Write( u4h.Unknown4Count );

				long addr = ( i < unknown4AddressList.Count && unknown4AddressList[i].Count > 0 )
					? unknown4AddressList[i][0] : 0;
				writer.Write( addr > 0 ? (int)( addr - writer.BaseStream.Position ) : 0 );
			}

			// Patch Unknown5 Headers
			writer.BaseStream.Position = unknown5HeaderAddress;
			for( int i = 0; i < room.Unknown5HeaderList.Count; i++ )
			{
				var u5h = room.Unknown5HeaderList[i];
				writer.Write( u5h.Unknown5Count );
				writer.Write( (int)( unknown5AddressList[i] - writer.BaseStream.Position ) );
			}

			// Patch Main Header
			writer.BaseStream.Position = headerOffset;
			writer.Write( room.Header.Identifier );
			writer.Write( (int)( nameAddress - writer.BaseStream.Position ) );
			writer.Write( room.Header.Unkn03 );
			writer.Write( room.Header.Unkn04 );
			writer.Write( room.StaticSpriteHeaderList.Count );
			writer.Write( (int)( staticSpriteHeaderAddress - writer.BaseStream.Position ) );
			writer.Write( room.SpriteHeaderList.Count );
			writer.Write( (int)( spriteHeaderAddress - writer.BaseStream.Position ) );
			writer.Write( room.Header.Unkn09 );
			writer.Write( room.Header.Unkn10 );
			writer.Write( unknown6HeaderAddress > 0 ? (int)( unknown6HeaderAddress - writer.BaseStream.Position ) : 0 );
			writer.Write( room.Unknown6HeaderList.Count );
			writer.Write( unknown6HeaderAddress > 0 ? (int)( unknown6HeaderAddress - writer.BaseStream.Position ) : 0 );
			writer.Write( room.Unknown4HeaderList.Count );
			writer.Write( unknown4HeaderAddress > 0 ? (int)( unknown4HeaderAddress - writer.BaseStream.Position ) : 0 );
			writer.Write( room.Unknown5HeaderList.Count );
			writer.Write( unknown5HeaderAddress > 0 ? (int)( unknown5HeaderAddress - writer.BaseStream.Position ) : 0 );
			writer.Write( room.Header.AlwaysZero1 );
			writer.Write( room.Header.AlwaysZero2 );
			writer.Write( room.Header.AlwaysZero3 );

			writer.BaseStream.Position = endOfFile;
		}

		private static void WriteStringMonkey( this BinaryWriter writer, string text )
		{
			var startPosition = writer.BaseStream.Position;
			var bytes = System.Text.Encoding.ASCII.GetBytes( text );
			writer.Write( bytes );
			writer.Write( (byte)0 );

			// skip the padding
			writer.PadTheMonkey( startPosition );
		}

		private static void PadTheMonkey( this BinaryWriter writer, long startPosition )
		{
			var mod = ( writer.BaseStream.Position - startPosition ) % 16;
			if( mod != 0 )
			{
				var paddingCount = 16 - mod;
				for( var i = 0; i < paddingCount; i++ )
				{
					writer.Write( (byte)0 );
				}
			}
		}
	}
}
