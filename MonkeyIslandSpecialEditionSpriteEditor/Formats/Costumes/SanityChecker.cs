using System;
using System.Linq;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Entities;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes
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
				costume.SpriteGroupList.IsNotNull();

				if( costume.AnimationHeaderList.Count != costume.AnimationList.Count )
				{
					throw new Exception( "The animation header count does not match the animation count" );
				}

				foreach( var spriteGroup in costume.SpriteGroupList )
				{
					foreach( var sprite in spriteGroup.SpriteList )
					{
						if( !sprite.TextureNumber.IsInRange( -1, costume.TextureFileNameList.Count ) )
						{
							throw new Exception( "A sprite references a texture that is not defined" );
						}
						if( sprite.PathPointIndex < -1 || sprite.PathPointIndex >= ( costume.PathPointList?.Count ?? 0 ) )
						{
							throw new Exception( "A sprite references a path point that is not defined" );
						}
					}
				}

				foreach( var animation in costume.AnimationList )
				{
					foreach( var animationFrame in animation.AnimationFrameList )
					{
						var frameList = animationFrame.FrameList;
						if( frameList == null || frameList.All( f => f.SpriteIdentifier == -1 ) )
						{
							continue;
						}
						if( animationFrame.SpriteGroupIdentifier > costume.SpriteGroupList.Max( sg => sg.Identifier ) )
						{
							throw new Exception( "An animation frame references a sprite group that is not defined" );
						}

						// note: dangling frame-to-sprite references are NOT rejected; a few
						// retail costumes (87, 107, 34) contain them and the renderer skips
						// them via SpriteGroup.ResolveSprite
					}
				}
			}
		}

		private static void IsNotNull( this object value )
		{
			if( value == null )
			{
				throw new Exception( "value is null" );
			}
		}
	}
}
