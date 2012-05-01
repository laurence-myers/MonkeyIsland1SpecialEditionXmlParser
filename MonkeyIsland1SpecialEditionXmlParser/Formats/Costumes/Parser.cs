using System.Collections.Generic;
using System.IO;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes
{
	public static class Parser
	{
		public static Costume ReadCostume( BinaryReader reader )
		{
			var costume = new Costume();
			costume.Header = Parser.ReadHeader( reader, costume );
			costume.TextureHeaderList = Parser.ReadTextureHeaderList( reader, costume );
			costume.AnimationHeaderList = Parser.ReadAnimationHeaderList( reader, costume );
			costume.SpriteGroupHeaderList = Parser.ReadSpriteGroupHeaderList( reader, costume );
			costume.PathPointList = Parser.ReadPathPointList( reader, costume );
			costume.TextureFileNameList = Parser.ReadTextureFileNameList( reader, costume );
			costume.AnimationList = Parser.ReadAnimationList( reader, costume );
			costume.SpriteGroupList = Parser.ReadSpriteGroupList( reader, costume );
			return costume;
		}

		private static Header ReadHeader( BinaryReader reader, Costume costume )
		{
			var header = new Header()
			{
				Identifier = reader.ReadInt32(),
				NameAddress = (int)reader.BaseStream.Position + reader.ReadInt32(),
				TextureFileNameCount = reader.ReadInt32(),
				TextureHeaderAddress = (int)reader.BaseStream.Position + reader.ReadInt32(),
				AnimationCount = reader.ReadInt32(),
				AnimationHeaderAddress = (int)reader.BaseStream.Position + reader.ReadInt32(),
				UnknownInteger1 = reader.ReadInt32(),
				SpriteGroupHeaderCount = reader.ReadInt32(),
				SpriteGroupHeaderAddress = (int)reader.BaseStream.Position + reader.ReadInt32(),
				UnknownInteger4 = reader.ReadInt32(),
				PathPointCount = reader.ReadInt32(),
				PathPointAddress = (int)reader.BaseStream.Position + reader.ReadInt32(),
				UnknownInteger5 = reader.ReadInt32(),
				UnknownInteger6 = reader.ReadInt32(),
				UnknownInteger7 = reader.ReadInt32(),
				UnknownInteger8 = reader.ReadInt32(),
				UnknownInteger9 = reader.ReadInt32(),
				UnknownInteger10 = reader.ReadInt32(),
				UnknownInteger11 = reader.ReadInt32(),
				UnknownInteger12 = reader.ReadInt32(),
				Name = reader.ReadStringMonkey(),
			};
			return header;
		}

		private static List<TextureHeader> ReadTextureHeaderList( BinaryReader reader, Costume costume )
		{
			var list = new List<TextureHeader>();

			while( reader.BaseStream.Position < costume.Header.AnimationHeaderAddress )
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

		private static List<AnimationHeader> ReadAnimationHeaderList( BinaryReader reader, Costume costume )
		{
			var list = new List<AnimationHeader>();

			for( var index = 0; index < costume.Header.AnimationCount; index++ )
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

		private static List<SpriteGroupHeader> ReadSpriteGroupHeaderList( BinaryReader reader, Costume costume )
		{
			var list = new List<SpriteGroupHeader>();

			for( var index = 0; index < costume.Header.SpriteGroupHeaderCount; index++ )
			{
				var spriteGroupHeader = new SpriteGroupHeader()
				{
					Identifier = reader.ReadInt32(),
					UnkownInteger1 = reader.ReadInt32(),
					SpriteCount = reader.ReadInt32(),
					SpriteAddress = (int)reader.BaseStream.Position + reader.ReadInt32(),
				};
				list.Add( spriteGroupHeader );
			}

			return list;
		}

		private static List<PathPoint> ReadPathPointList( BinaryReader reader, Costume costume )
		{
			var list = new List<PathPoint>();

			if( costume.Header.PathPointCount > 0 )
			{
				var position = reader.BaseStream.Position;
				for( var index = 0; index < costume.Header.PathPointCount; index++ )
				{
					var pathPoint = new PathPoint()
					{
						UnknownByte1 = reader.ReadByte(),
						UnknownByte2 = reader.ReadByte(),
						UnknownByte3 = reader.ReadByte(),
						UnknownByte4 = reader.ReadByte(),
						UnknownFloat1 = reader.ReadSingle(),
						UnknownFloat2 = reader.ReadSingle(),
					};
					list.Add( pathPoint );
				}
				reader.PadTheMonkey( position );
			}

			return list;
		}

		private static List<TextureFileName> ReadTextureFileNameList( BinaryReader reader, Costume costume )
		{
			var list = new List<TextureFileName>();

			for( var index = 0; index < costume.Header.TextureFileNameCount; index++ )
			{
				var textureFileName = new TextureFileName()
				{
					Index = index,
					Path = reader.ReadStringMonkey(),
				};
				list.Add( textureFileName );
			}

			return list;
		}

		private static List<Animation> ReadAnimationList( BinaryReader reader, Costume costume )
		{
			var list = new List<Animation>();

			for( var index = 0; index < costume.AnimationHeaderList.Count; index++ )
			{
				// get current animation header
				var animationHeader = costume.AnimationHeaderList[index];

				// position reader at animation name
				reader.BaseStream.Position = animationHeader.NameAddress;

				// create animation entity
				var animation = new Animation()
				{
					Name = reader.ReadStringMonkey(),
					AnimationFrameList = Parser.ReadAnimationFrameList( reader, costume, animationHeader ),
				};
				list.Add( animation );
			}

			return list;
		}

		private static List<AnimationFrame> ReadAnimationFrameList( BinaryReader reader, Costume costume, AnimationHeader animationHeader )
		{
			var list = new List<AnimationFrame>();

			// read animation frames
			for( var index = 0; index < animationHeader.AnimationFrameCount; index++ )
			{
				// position reader at the first animation frame
				reader.BaseStream.Position = animationHeader.AnimationFrameAddress + index * 16;

				var animationFrame = new AnimationFrame()
				{
					Index = index,
					SpriteGroupIdentifier = reader.ReadInt32(),
					UnknownInteger1 = reader.ReadInt32(),
					FrameCount = reader.ReadInt32(),
					FrameAddress = (int)reader.BaseStream.Position + reader.ReadInt32(),
				};
				animationFrame.FrameList = Parser.ReadFrameList( reader, costume, animationFrame );
				list.Add( animationFrame );
			}

			return list;
		}

		private static List<Frame> ReadFrameList( BinaryReader reader, Costume costume, AnimationFrame animationFrame )
		{
			var list = new List<Frame>();

			reader.BaseStream.Position = animationFrame.FrameAddress;

			for( var index = 0; index < animationFrame.FrameCount; index++ )
			{
				var frame = new Frame()
				{
					SpriteIdentifier = reader.ReadInt32(),
					UnknownInteger2 = reader.ReadInt32(),
					UnknownInteger3 = reader.ReadInt32(),
				};
				list.Add( frame );
			}

			return list;
		}

		private static List<SpriteGroup> ReadSpriteGroupList( BinaryReader reader, Costume costume )
		{
			var list = new List<SpriteGroup>();

			for( var index = 0; index < costume.SpriteGroupHeaderList.Count; index++ )
			{
				var spriteGroupHeader = costume.SpriteGroupHeaderList[index];
				var spriteGroup = new SpriteGroup()
				{
					Index = index,
					Identifier = spriteGroupHeader.Identifier,
					SpriteList = Parser.ReadSpriteList( reader, costume, spriteGroupHeader ),
				};
				list.Add( spriteGroup );
			}

			return list;
		}

		private static List<Sprite> ReadSpriteList( BinaryReader reader, Costume costume, SpriteGroupHeader spriteGroupHeader )
		{
			var list = new List<Sprite>();

			// position the reader at the first sprite
			reader.BaseStream.Position = spriteGroupHeader.SpriteAddress;

			for( var index = 0; index < spriteGroupHeader.SpriteCount; index++ )
			{
				var sprite = new Sprite()
				{
					TextureNumber = reader.ReadInt32(),
					TextureX = reader.ReadInt32(),
					TextureY = reader.ReadInt32(),
					TextureWidth = reader.ReadInt32(),
					TextureHeight = reader.ReadInt32(),
					ScreenX = reader.ReadSingle(),
					ScreenY = reader.ReadSingle(),
					UnknownInteger1 = reader.ReadInt32(),
					UnknownInteger2 = reader.ReadInt32(),
					UnknownInteger3 = reader.ReadInt32(),
				};
				list.Add( sprite );
			}

			return list;
		}
	}
}
