using System;
using System.Linq;
using MonkeyIsland1SpecialEditionXmlParser.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser
{
	public static class SanityChecker
	{
		public static void Check( params Costume[] costumes )
		{
			foreach( var costume in costumes )
			{
				costume.IsNotNull();
				costume.Header.IsNotNull();
				costume.TextureFileNameList.IsNotNull();
				costume.AnimationHeaderList.IsNotNull();
				costume.AnimationList.IsNotNull();

				foreach( var spriteGroup in costume.SpriteGroupList )
				{
					foreach( var sprite in spriteGroup.SpriteList )
					{
						if( !sprite.TextureNumber.IsInRange( -1, costume.TextureFileNameList.Count ) )
						{
							throw new Exception( "A sprite references a texture that is not defined" );
						}
					}
				}

				foreach( var animation in costume.AnimationList )
				{
					foreach(var animationFrame in animation.AnimationFrameList)
					{
						if( animationFrame.FrameList.All( f => f.SpriteIdentifier == -1 ) )
						{
							continue;
						}
						if( animationFrame.SpriteGroupIdentifier > costume.SpriteGroupList.Max( sg => sg.Identifier ) )
						{
							throw new Exception( "An animation frame references a sprite group that is not defined" );
						}
					}
				}

				//foreach( var animation in costume.AnimationList )
				//{
				//    foreach( var animationFrame in animation.AnimationFrameList )
				//    {
				//        foreach( var frame in animationFrame.FrameList )
				//        {
				//            if( frame.SpriteIdentifier == -1 )
				//            {
				//                continue;
				//            }
				//            var spriteGroup = costume.SpriteGroupList.First( sg => sg.Identifier == animationFrame.SpriteGroupIdentifier);
				//            if( frame.SpriteIdentifier >= spriteGroup.SpriteList.Count )
				//            {
				//                throw new Exception( "An animation frame references a sprite that is not defined" );
				//            }
				//        }
				//    }
				//}

				switch( costume.Header.Identifier )
				{
					case 21:
						SanityChecker.Check21( costume );
						break;
					case 44:
						SanityChecker.Check44( costume );
						break;
					case 72:
						SanityChecker.Check72( costume );
						break;
					case 124:
						SanityChecker.Check124( costume );
						break;
					default:
						throw new Exception( "costume.Header.Identifier not expected" );
				}
			}
		}

		private static void Check21( Costume costume )
		{
			costume.Header.NameAddress.Is( 80 );
			costume.Header.TextureFileNameCount.Is( 3 );
			costume.Header.TextureHeaderAddress.Is( 96 );
			costume.Header.AnimationCount.Is( 27 );
			costume.Header.Name.Is( "test-skin" );

			costume.TextureFileNameList.Count.Is( 3 );
			costume.TextureFileNameList[0].Path.Is( "art/costumes/images/21_test-skin/costumes_a0.dxt" );
			costume.TextureFileNameList[1].Path.Is( "art/costumes/images/21_test-skin/costumes_a1.dxt" );
			costume.TextureFileNameList[2].Path.Is( "art/costumes/images/21_test-skin/costumes_a2.dxt" );

			costume.AnimationHeaderList.Count.Is( 27 );

			costume.AnimationList.Count.Is( 27 );
			costume.AnimationList[0].Name.Is( "InitLeft" );
		}

		private static void Check44( Costume costume )
		{
		}

		private static void Check72( Costume costume )
		{
		}

		private static void Check124( Costume costume )
		{
		}

		private static void IsNotNull( this object value )
		{
			if( value == null )
			{
				throw new Exception( "value is null" );
			}
		}

		private static void Is<T>( this T actual, T expected )
		{
			if( !actual.Equals( expected ) )
			{
				throw new Exception( actual + " != " + expected );
			}
		}
	}
}
