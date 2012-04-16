using System.Collections.Generic;
using System.IO;
using MonkeyIsland1SpecialEditionXmlParser.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser
{
	public static class Parser
	{
		public static Costume Parse( string fileName )
		{
			Stream stream = null;
			BinaryReader reader = null;
			Costume costume = null;

			try
			{
				stream = File.OpenRead( fileName );
				reader = new BinaryReader( stream );
				costume = Parser.ReadCostume( reader );
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

			return costume;
		}

		private static Costume ReadCostume( BinaryReader reader )
		{
			var costume = new Costume();
			costume.Header = Parser.ReadHeader( reader, costume );
			costume.UnknownAfterNameList = Parser.ReadUnknownAfterNameList( reader, costume );
			costume.AnimationHeaderList = Parser.ReadAnimationHeaderList( reader, costume );
			costume.SpriteGroupHeaderList = Parser.ReadSpriteGroupHeaderList( reader, costume );
			costume.UnknownBeforeSpriteSheetsList = Parser.ReadUnknownBeforeSpriteSheetsList( reader, costume );
			costume.TextureFileNameList = Parser.ReadTextureFileNameList( reader, costume );
			costume.AnimationList = Parser.ReadAnimationList( reader, costume );
			return costume;
		}

		private static Header ReadHeader( BinaryReader reader, Costume costume )
		{
			var header = new Header()
			{
				Identifier = reader.ReadInt32(),
				NameAddress = (int)reader.BaseStream.Position + reader.ReadInt32(),
				TextureFileNameCount = reader.ReadInt32(),
				AfterNameAddress = (int)reader.BaseStream.Position + reader.ReadInt32(),
				AnimationCount = reader.ReadInt32(),
				AnimationHeaderAddress = (int)reader.BaseStream.Position + reader.ReadInt32(),
				UnknownInteger1 = reader.ReadInt32(),
				PointsOnPathCount = reader.ReadInt32(),
				PathAddress = (int)reader.BaseStream.Position + reader.ReadInt32(),
				UnknownInteger4 = reader.ReadInt32(),
				UnknownCount1 = reader.ReadInt32(),
				TextureFileNameAddress = (int)reader.BaseStream.Position + reader.ReadInt32(),
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

		private static List<UnknownAfterName> ReadUnknownAfterNameList( BinaryReader reader, Costume costume )
		{
			var list = new List<UnknownAfterName>();

			while( reader.BaseStream.Position < costume.Header.AnimationHeaderAddress )
			{
				var unknownAfterName = new UnknownAfterName()
				{
					UnknownInteger1 = reader.ReadInt32(),
					UnknownInteger2 = reader.ReadInt32(),
					UnknownInteger3 = reader.ReadInt32(),
					UnknownInteger4 = reader.ReadInt32(),
				};
				list.Add( unknownAfterName );
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

			for( var index = 0; index < costume.Header.PointsOnPathCount; index++ )
			{
				var spriteGroupHeader = new SpriteGroupHeader()
				{
					Identifier = reader.ReadInt32(),
					UnkownInteger1 = reader.ReadInt32(),
					SpriteCount = reader.ReadInt32(),
					SpriteAddress = reader.ReadInt32(),
				};
				list.Add( spriteGroupHeader );
			}

			return list;
		}

		private static List<UnknownBeforeSpriteSheets> ReadUnknownBeforeSpriteSheetsList( BinaryReader reader, Costume costume )
		{
			var list = new List<UnknownBeforeSpriteSheets>();

			if( costume.Header.UnknownCount1 > 0 )
			{
				for( var index = 0; index < costume.Header.UnknownCount1; index++ )
				{
					var unknownAfterName = new UnknownBeforeSpriteSheets()
					{
						UnknownInteger = reader.ReadInt32(),
						UnknownColor1 = reader.ReadColor(),
						UnknownColor2 = reader.ReadColor(),
					};
					list.Add( unknownAfterName );
				}
				reader.PadTheMonkey();
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

			for( var index = 0; index < costume.Header.AnimationCount; index++ )
			{
				var animation = new Animation()
				{
					Name = reader.ReadStringMonkey(),
					AnimationFrameList = Parser.ReadAnimationFrameList( reader, costume ),
				};
				list.Add( animation );
			}

			return list;
		}

		private static List<AnimationFrame> ReadAnimationFrameList( BinaryReader reader, Costume costume )
		{
			var list = new List<AnimationFrame>();

			AnimationFrame previousAnimationFrame = null;
			while( true )
			{
				var peekedInt32 = reader.PeekInt32();
				if( !peekedInt32.IsInRange( 0, 256 ) ) // NOTE 256 is just a guess
				{
					break;
				}
				if( previousAnimationFrame != null && previousAnimationFrame.UnknownInteger1 > peekedInt32 )
				{
					break;
				}

				var animationFrame = previousAnimationFrame = new AnimationFrame()
				{
					UnknownInteger1 = reader.ReadInt32(),
					UnknownInteger2 = reader.ReadInt32(),
					UnknownInteger3 = reader.ReadInt32(),
					UnknownInteger4 = reader.ReadInt32(),
				};
				list.Add( animationFrame );
			}

			return list;
		}
	}
}
