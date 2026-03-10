using System.Collections.Generic;
using System.IO;
using System.Text;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes
{
	public static class Packer
	{
		/// <summary>
		/// Writes a Costume to a single .dat file.
		/// </summary>
		/// <param name="fileName">The path to the .dat file.</param>
		/// <param name="costume">The Costume object to write.</param>
		public static void WriteCostumeToBinaryFile( string fileName, Costume costume )
		{
			using( var stream = File.Create( fileName ) )
			using( var writer = new BinaryWriter( stream ) )
			{
				WriteCostume( writer, costume );
			}
		}

		public static void WriteCostume( BinaryWriter writer, Costume costume )
		{
			long startPosition = writer.BaseStream.Position;

			// 1. Write Header (80 bytes placeholder)
			long headerOffset = writer.BaseStream.Position;
			writer.Write( new byte[80] );

			// 2. Write Header Name (padded to 16 bytes)
			long nameAddress = writer.BaseStream.Position;
			WriteStringMonkey( writer, costume.Header.Name );

			// 3. Write Texture Header List (fixed size per item)
			long textureHeaderAddress = writer.BaseStream.Position;
			foreach( var textureHeader in costume.TextureHeaderList )
			{
				writer.Write( 0 ); // TextureSpriteCount1 placeholder
				writer.Write( 0 ); // TextureFileNameAddress1 placeholder
				writer.Write( 0 ); // TextureSpriteCount2 placeholder
				writer.Write( 0 ); // TextureFileNameAddress2 placeholder
			}

			// 4. Write Animation Header List
			long animationHeaderAddress = writer.BaseStream.Position;
			foreach( var animationHeader in costume.AnimationHeaderList )
			{
				writer.Write( 0 ); // NameAddress placeholder
				writer.Write( 0 ); // Identifier placeholder
				writer.Write( 0 ); // AnimationFrameCount placeholder
				writer.Write( 0 ); // AnimationFrameAddress placeholder
			}

			// 5. Write Sprite Group Header List
			long spriteGroupHeaderAddress = writer.BaseStream.Position;
			foreach( var spriteGroupHeader in costume.SpriteGroupHeaderList )
			{
				writer.Write( 0 ); // Identifier placeholder
				writer.Write( 0 ); // UnkownInteger1 placeholder
				writer.Write( 0 ); // SpriteCount placeholder
				writer.Write( 0 ); // SpriteAddress placeholder
			}

			// 6. Write Path Point List
			long pathPointAddress = writer.BaseStream.Position;
			if( costume.PathPointList != null && costume.PathPointList.Count > 0 )
			{
				long pathStart = writer.BaseStream.Position;
				foreach( var pathPoint in costume.PathPointList )
				{
					writer.Write( pathPoint.UnknownByte1 );
					writer.Write( pathPoint.UnknownByte2 );
					writer.Write( pathPoint.UnknownByte3 );
					writer.Write( pathPoint.UnknownByte4 );
					writer.Write( pathPoint.UnknownFloat1 );
					writer.Write( pathPoint.UnknownFloat2 );
				}
				PadTheMonkey( writer, pathStart );
			}

			// 7. Write Texture File Name List
			List<long> textureFileNameAddresses = new List<long>();
			foreach( var textureFileName in costume.TextureFileNameList )
			{
				textureFileNameAddresses.Add( writer.BaseStream.Position );
				WriteStringMonkey( writer, textureFileName.Path );
			}

			// 8. Write Animations (Names and Frame/FrameList)
			List<long> animationNameAddresses = new List<long>();
			List<long> animationFrameListAddresses = new List<long>();
			foreach( var animation in costume.AnimationList )
			{
				animationNameAddresses.Add( writer.BaseStream.Position );
				WriteStringMonkey( writer, animation.Name );

				animationFrameListAddresses.Add( writer.BaseStream.Position );
				long animFrameStart = writer.BaseStream.Position;

				// Write AnimationFrames (placeholders)
				foreach( var animFrame in animation.AnimationFrameList )
				{
					writer.Write( new byte[16] );
				}

				// Write FrameLists
				List<long> frameListAddresses = new List<long>();
				foreach( var animFrame in animation.AnimationFrameList )
				{
					frameListAddresses.Add( writer.BaseStream.Position );
					foreach( var frame in animFrame?.FrameList ?? [] )
					{
						writer.Write( frame.SpriteIdentifier );
						writer.Write( frame.UnknownInteger2 );
						writer.Write( frame.UnknownInteger3 );
					}
				}

				// Patch AnimationFrames
				long currentPos = writer.BaseStream.Position;
				writer.BaseStream.Position = animFrameStart;
				for( int i = 0; i < animation.AnimationFrameList.Count; i++ )
				{
					var animFrame = animation.AnimationFrameList[i];
					writer.Write( animFrame.SpriteGroupIdentifier );
					writer.Write( animFrame.UnknownInteger1 );
					writer.Write( animFrame.FrameCount );
					writer.Write( (int)( frameListAddresses[i] - writer.BaseStream.Position ) );
				}
				writer.BaseStream.Position = currentPos;
			}

			// 9. Write Sprite Groups (Sprite lists)
			List<long> spriteListAddresses = new List<long>();
			foreach( var spriteGroup in costume.SpriteGroupList )
			{
				spriteListAddresses.Add( writer.BaseStream.Position );
				foreach( var sprite in spriteGroup.SpriteList )
				{
					writer.Write( sprite.TextureNumber );
					writer.Write( sprite.TextureX );
					writer.Write( sprite.TextureY );
					writer.Write( sprite.TextureWidth );
					writer.Write( sprite.TextureHeight );
					writer.Write( sprite.ScreenX );
					writer.Write( sprite.ScreenY );
					writer.Write( sprite.UnknownInteger1 );
					writer.Write( sprite.UnknownInteger2 );
					writer.Write( sprite.UnknownInteger3 );
				}
			}

			long endOfFile = writer.BaseStream.Position;

			// --- BACKPATCHING ---

			// Patch Texture Headers
			writer.BaseStream.Position = textureHeaderAddress;
			for( int i = 0; i < costume.TextureHeaderList.Count; i++ )
			{
				var th = costume.TextureHeaderList[i];
				writer.Write( th.TextureSpriteCount1 );
				
				long addr1 = ( i * 2 < textureFileNameAddresses.Count ) ? textureFileNameAddresses[i * 2] : 0;
				writer.Write( addr1 > 0 ? (int)( addr1 - writer.BaseStream.Position ) : 0 );
				
				writer.Write( th.TextureSpriteCount2 );
				
				long addr2 = ( i * 2 + 1 < textureFileNameAddresses.Count ) ? textureFileNameAddresses[i * 2 + 1] : 0;
				writer.Write( addr2 > 0 ? (int)( addr2 - writer.BaseStream.Position ) : 0 );
			}

			// Patch Animation Headers
			writer.BaseStream.Position = animationHeaderAddress;
			for( int i = 0; i < costume.AnimationHeaderList.Count; i++ )
			{
				var ah = costume.AnimationHeaderList[i];
				writer.Write( (int)( animationNameAddresses[i] - writer.BaseStream.Position ) );
				writer.Write( ah.Identifier );
				writer.Write( ah.AnimationFrameCount );
				writer.Write( (int)( animationFrameListAddresses[i] - writer.BaseStream.Position ) );
			}

			// Patch Sprite Group Headers
			writer.BaseStream.Position = spriteGroupHeaderAddress;
			for( int i = 0; i < costume.SpriteGroupHeaderList.Count; i++ )
			{
				var sgh = costume.SpriteGroupHeaderList[i];
				writer.Write( sgh.Identifier );
				writer.Write( sgh.UnkownInteger1 );
				writer.Write( sgh.SpriteCount );
				writer.Write( (int)( spriteListAddresses[i] - writer.BaseStream.Position ) );
			}

			// Patch Main Header
			writer.BaseStream.Position = headerOffset;
			writer.Write( costume.Header.Identifier );
			writer.Write( (int)( nameAddress - writer.BaseStream.Position ) );
			writer.Write( costume.TextureFileNameList.Count );
			writer.Write( (int)( textureHeaderAddress - writer.BaseStream.Position ) );
			writer.Write( costume.AnimationList.Count );
			writer.Write( (int)( animationHeaderAddress - writer.BaseStream.Position ) );
			writer.Write( costume.Header.UnknownInteger1 );
			writer.Write( costume.SpriteGroupHeaderList.Count );
			writer.Write( (int)( spriteGroupHeaderAddress - writer.BaseStream.Position ) );
			writer.Write( costume.Header.UnknownInteger4 );
			writer.Write( costume.PathPointList?.Count ?? 0 );
			writer.Write( ( costume.PathPointList?.Count > 0 ) ? (int)( pathPointAddress - writer.BaseStream.Position ) : 0 );
			writer.Write( costume.Header.UnknownInteger5 );
			writer.Write( costume.Header.UnknownInteger6 );
			writer.Write( costume.Header.UnknownInteger7 );
			writer.Write( costume.Header.UnknownInteger8 );
			writer.Write( costume.Header.UnknownInteger9 );
			writer.Write( costume.Header.UnknownInteger10 );
			writer.Write( costume.Header.UnknownInteger11 );
			writer.Write( costume.Header.UnknownInteger12 );

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
