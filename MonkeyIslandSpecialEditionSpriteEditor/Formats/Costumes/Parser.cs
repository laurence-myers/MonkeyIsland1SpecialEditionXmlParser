using System.Collections.Generic;
using System.IO;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Entities;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes
{
	public static class Parser
	{
		/// <summary>
		/// Reads a Costume from a single .dat file.
		/// </summary>
		/// <param name="fileName">The path to the .dat file.</param>
		/// <returns>The parsed Costume object.</returns>
		public static Costume ReadCostumeFromBinaryFile( string fileName )
		{
			using( var stream = File.OpenRead( fileName ) )
			using( var reader = new BinaryReader( stream ) )
			{
				return ReadCostume( reader );
			}
		}

		/// <summary>
		/// Reads a Costume from an XML file.
		/// </summary>
		/// <param name="fileName">The path to the XML file.</param>
		/// <returns>The parsed Costume object.</returns>
		public static Costume ReadCostumeFromXmlFile( string fileName )
		{
			return Helper.ReadObjectFromFile<Costume>( fileName );
		}

		public static Costume ReadCostume( BinaryReader reader )
		{
			var header = Parser.ReadHeader( reader );
			var textureHeaderList = Parser.ReadTextureHeaderList( reader, header );
			var animationHeaderList = Parser.ReadAnimationHeaderList( reader, header );
			var spriteGroupHeaderList = Parser.ReadSpriteGroupHeaderList( reader, header );
			var pathPointList = Parser.ReadPathPointList( reader, header );
			var textureFileNameList = Parser.ReadTextureFileNameList( reader, header );
			var animationList = Parser.ReadAnimationList( reader, animationHeaderList );
			var spriteGroupList = Parser.ReadSpriteGroupList( reader, spriteGroupHeaderList );

			return new Costume(
				header: header,
				textureHeaderList: textureHeaderList,
				animationHeaderList: animationHeaderList,
				spriteGroupHeaderList: spriteGroupHeaderList,
				pathPointList: pathPointList,
				textureFileNameList: textureFileNameList,
				animationList: animationList,
				spriteGroupList: spriteGroupList
			);
		}

		private static Header ReadHeader( BinaryReader reader )
		{
			var header = new Header(
				identifier: reader.ReadInt32(),
				nameAddress: (int)reader.BaseStream.Position + reader.ReadInt32(),
				textureFileNameCount: reader.ReadInt32(),
				textureHeaderAddress: (int)reader.BaseStream.Position + reader.ReadInt32(),
				animationCount: reader.ReadInt32(),
				animationHeaderAddress: (int)reader.BaseStream.Position + reader.ReadInt32(),
				unknownInteger1: reader.ReadInt32(), spriteGroupHeaderCount: reader.ReadInt32(),
				spriteGroupHeaderAddress: (int)reader.BaseStream.Position + reader.ReadInt32(),
				pathPointTypeCount: reader.ReadInt32(), pathPointCount: reader.ReadInt32(),
				pathPointAddress: (int)reader.BaseStream.Position + reader.ReadInt32(),
				unknownInteger5: reader.ReadInt32(), unknownInteger6: reader.ReadInt32(),
				unknownFloat7: reader.ReadSingle(), unknownFloat8: reader.ReadSingle(),
				unknownFloat9: reader.ReadSingle(), unknownInteger10: reader.ReadInt32(),
				unknownInteger11: reader.ReadInt32(), unknownInteger12: reader.ReadInt32(),
				name: reader.ReadStringMonkey() );
			return header;
		}

		private static List<TextureHeader> ReadTextureHeaderList( BinaryReader reader, Header header )
		{
			var list = new List<TextureHeader>();

			while( reader.BaseStream.Position < header.AnimationHeaderAddress )
			{
				var textureHeader = new TextureHeader()
				{
					TextureSpriteCount1 = reader.ReadInt32(),
					TextureFileNameAddress1 = reader.ReadInt32PlusBytePosition( value => value > 0 ),
					TextureSpriteCount2 = reader.ReadInt32(),
					TextureFileNameAddress2 = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				};
				list.Add( textureHeader );
			}

			return list;
		}

		private static List<AnimationHeader> ReadAnimationHeaderList( BinaryReader reader, Header header )
		{
			var list = new List<AnimationHeader>();

			for( var index = 0; index < header.AnimationCount; index++ )
			{
				var animationHeader = new AnimationHeader()
				{
					NameAddress = (int)reader.BaseStream.Position + reader.ReadInt32(),
					Identifier = reader.ReadInt32(),
					AnimationFrameCount = reader.ReadInt32(),
					AnimationFrameAddress = (int)reader.BaseStream.Position + reader.ReadInt32(),
				};
				list.Add( animationHeader );
			}

			return list;
		}

		private static List<SpriteGroupHeader> ReadSpriteGroupHeaderList( BinaryReader reader, Header header )
		{
			var list = new List<SpriteGroupHeader>();

			for( var index = 0; index < header.SpriteGroupHeaderCount; index++ )
			{
				var spriteGroupHeader = new SpriteGroupHeader()
				{
					Identifier = reader.ReadInt32(),
					FirstSpriteIdentifier = reader.ReadInt32(),
					SpriteCount = reader.ReadInt32(),
					SpriteAddress = reader.ReadInt32PlusBytePosition( value => value > 0 ),
				};
				list.Add( spriteGroupHeader );
			}

			return list;
		}

		private static List<PathPoint> ReadPathPointList( BinaryReader reader, Header header )
		{
			var list = new List<PathPoint>();

			if( header.PathPointCount > 0 )
			{
				var position = reader.BaseStream.Position;
				for( var index = 0; index < header.PathPointCount; index++ )
				{
					var pathPoint = new PathPoint()
					{
						Type = reader.ReadByte(),
						Flag = reader.ReadByte(),
						UnknownByte3 = reader.ReadByte(),
						UnknownByte4 = reader.ReadByte(),
						X = reader.ReadSingle(),
						Y = reader.ReadSingle(),
					};
					list.Add( pathPoint );
				}

				// unlike strings, lists are only padded when their size is not already a
				// multiple of 16 (PadTheMonkey would skip 16 extra bytes in that case)
				var mod = ( reader.BaseStream.Position - position ) % 16;
				if( mod != 0 )
				{
					reader.BaseStream.Position += 16 - mod;
				}
			}

			return list;
		}

		private static List<TextureFileName> ReadTextureFileNameList( BinaryReader reader, Header header )
		{
			var list = new List<TextureFileName>();

			for( var index = 0; index < header.TextureFileNameCount; index++ )
			{
				var textureFileName = new TextureFileName(
					index: index,
					path: reader.ReadStringMonkey()
				);
				list.Add( textureFileName );
			}

			return list;
		}

		private static List<Animation> ReadAnimationList( BinaryReader reader, List<AnimationHeader> animationHeaders )
		{
			var list = new List<Animation>();

			for( var index = 0; index < animationHeaders.Count; index++ )
			{
				// get current animation header
				var animationHeader = animationHeaders[index];

				// position reader at animation name
				reader.BaseStream.Position = animationHeader.NameAddress;

				// create animation entity
				var animation = new Animation(
					name: reader.ReadStringMonkey(),
					animationFrameList: Parser.ReadAnimationFrameList( reader, animationHeader )
				);
				list.Add( animation );
			}

			return list;
		}

		private static List<AnimationFrame> ReadAnimationFrameList( BinaryReader reader, AnimationHeader animationHeader )
		{
			var list = new List<AnimationFrame>();

			// read animation frames
			for( var index = 0; index < animationHeader.AnimationFrameCount; index++ )
			{
				// position reader at the first animation frame
				reader.BaseStream.Position = animationHeader.AnimationFrameAddress + index * 16;

				var animationFrame = new AnimationFrame(
					index: index,
					spriteGroupIdentifier: reader.ReadInt32(),
					playbackFlags: reader.ReadInt32(),
					frameCount: reader.ReadInt32(),
					frameAddress: (int)reader.BaseStream.Position + reader.ReadInt32()
				);
				animationFrame.FrameList = Parser.ReadFrameList( reader, animationFrame );
				list.Add( animationFrame );
			}

			return list;
		}

		private static List<Frame> ReadFrameList( BinaryReader reader, AnimationFrame animationFrame )
		{
			var list = new List<Frame>();

			for( var index = 0; index < animationFrame.FrameCount; index++ )
			{
				reader.BaseStream.Position = animationFrame.FrameAddress + index * 12;

				var spriteIdentifier = reader.ReadInt32();
				var command = reader.ReadInt32();

				// the third field is a relative pointer to a shared sound name string (or 0)
				var soundNameFieldPosition = reader.BaseStream.Position;
				var soundNameOffset = reader.ReadInt32();
				string? soundName = null;
				if( soundNameOffset != 0 )
				{
					reader.BaseStream.Position = soundNameFieldPosition + soundNameOffset;
					soundName = reader.ReadStringMonkeyNoPadding();
				}

				var frame = new Frame(
					spriteIdentifier: spriteIdentifier,
					command: command,
					soundName: soundName
				);
				list.Add( frame );
			}

			return list;
		}

		private static List<SpriteGroup> ReadSpriteGroupList( BinaryReader reader, List<SpriteGroupHeader> spriteGroupHeaders )
		{
			var list = new List<SpriteGroup>();

			for( var index = 0; index < spriteGroupHeaders.Count; index++ )
			{
				var spriteGroupHeader = spriteGroupHeaders[index];
				var spriteGroup = new SpriteGroup(
					index: index,
					identifier: spriteGroupHeader.Identifier,
					firstSpriteIdentifier: spriteGroupHeader.FirstSpriteIdentifier,
					spriteList: Parser.ReadSpriteList( reader, spriteGroupHeader )
				);
				list.Add( spriteGroup );
			}

			return list;
		}

		private static List<Sprite> ReadSpriteList( BinaryReader reader, SpriteGroupHeader spriteGroupHeader )
		{
			var list = new List<Sprite>();

			// position the reader at the first sprite
			reader.BaseStream.Position = spriteGroupHeader.SpriteAddress;

			for( var index = 0; index < spriteGroupHeader.SpriteCount; index++ )
			{
				var sprite = new Sprite(
					textureNumber: reader.ReadInt32(),
					textureX: reader.ReadInt32(),
					textureY: reader.ReadInt32(),
					textureWidth: reader.ReadInt32(),
					textureHeight: reader.ReadInt32(),
					screenX: reader.ReadSingle(),
					screenY: reader.ReadSingle(),
					moveX: reader.ReadSingle(),
					moveY: reader.ReadSingle(),
					pathPointIndex: reader.ReadInt32()
				);
				list.Add( sprite );
			}

			return list;
		}
	}
}
