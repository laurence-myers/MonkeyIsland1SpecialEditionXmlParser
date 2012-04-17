using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using MonkeyIsland1SpecialEditionXmlParser.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser
{
	public static class Renderer
	{
		public static void Render( Costume costume, string animationName, string fileName )
		{
			var animation = costume.AnimationList.FirstOrDefault( a => a.Name == animationName );
			if( animation == null )
			{
				throw new Exception( "Animation not found" );
			}

			var textures = costume.TextureFileNameList.Select( f => Image.FromFile( f.Path.Replace( ".dxt", ".png" ) ) ).ToArray();
			if( textures.Any( t => t == null ) )
			{
				throw new Exception( "Unable to load one or more textures" );
			}

			foreach( var animationFrame in animation.AnimationFrameList )
			{
				var spriteGroup = costume.SpriteGroupList.FirstOrDefault( sg => sg.Identifier == animationFrame.SpriteGroupIdentifier );
				if( spriteGroup == null )
				{
					continue;
				}

				var sprites = animationFrame.FrameList
					.Where( f => f.SpriteIdentifier > -1 )
					.Select( f => spriteGroup.SpriteList[f.SpriteIdentifier] )
					.ToArray()
					;

				if( sprites.Length == 0 )
				{
					continue;
				}

				var width = sprites.Max( s => s.TextureWidth );
				var height = sprites.Sum( s => s.TextureHeight );

				var image = new Bitmap( width, height );
				var imageGraphics = Graphics.FromImage( image );
				imageGraphics.Clear( Color.Transparent );

				var y = 0;

				foreach( var sprite in sprites )
				{
					var texture = textures[sprite.TextureNumber];
					var destRect = new Rectangle(0, y, sprite.TextureWidth, sprite.TextureHeight);
					var srcRect = new Rectangle(sprite.TextureX, sprite.TextureY, sprite.TextureWidth, sprite.TextureHeight);
					imageGraphics.DrawImage( texture, destRect, srcRect, GraphicsUnit.Pixel );
					y += srcRect.Height;
				}

				image.Save( fileName + "-" + animation.Name + "-" + animationFrame.Index + ".png", ImageFormat.Png );
				image.Dispose();
			}
		}
	}
}
