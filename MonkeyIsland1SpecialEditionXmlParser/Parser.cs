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
			costume.UnknownBeforeSpriteSheetsList = Parser.ReadUnknownBeforeSpriteSheetsList( reader, costume );
			costume.SpriteSheetList = Parser.ReadSpriteSheetList( reader, costume );
			costume.AnimationList = Parser.ReadAnimationList( reader, costume );
			return costume;
		}

		private static Header ReadHeader( BinaryReader reader, Costume costume )
		{
			var header = new Header()
			{
				ID = reader.ReadInt32(),
				NameAddress = reader.ReadInt32(),
				SpriteSheetFileNameCount = reader.ReadInt32(),
				AfterNameOffset = reader.ReadInt32(),
				AnimationCount = reader.ReadInt32(),
				UnknownAddress1 = reader.ReadInt32(),
				UnknownInteger1 = reader.ReadInt32(),
				UnknownInteger2 = reader.ReadInt32(),
				UnknownInteger3 = reader.ReadInt32(),
				UnknownInteger4 = reader.ReadInt32(),
				UnknownCount1 = reader.ReadInt32(),
				UnknownAddress2 = (int)reader.BaseStream.Position + reader.ReadInt32(),
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

			while( reader.BaseStream.Position < costume.Header.UnknownAddress2 )
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

		private static List<UnknownBeforeSpriteSheets> ReadUnknownBeforeSpriteSheetsList( BinaryReader reader, Costume costume )
		{
			var list = new List<UnknownBeforeSpriteSheets>();

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

			return list;
		}

		private static List<SpriteSheet> ReadSpriteSheetList( BinaryReader reader, Costume costume )
		{
			var list = new List<SpriteSheet>();

			for( var index = 0; index < costume.Header.SpriteSheetFileNameCount; index++ )
			{
				var spriteSheet = new SpriteSheet()
				{
					Index = index,
					Path = reader.ReadStringMonkey(),
				};
				list.Add( spriteSheet );
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
