using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes
{
	public static class Renderer
	{
		public static void Render( Costume costume, string animationName, string directory, string filePrefix, Padding spritePadding )
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

			var flip = animationName.EndsWith( "Left" );

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

				var width = sprites.Max( s => s.TextureWidth ) + spritePadding.Horizontal;
				var height = sprites.Sum( s => s.TextureHeight ) + sprites.Length * spritePadding.Vertical;

				var image = new Bitmap( width, height );
				var imageGraphics = Graphics.FromImage( image );
				imageGraphics.Clear( Color.Transparent );

				var y = 0;

				foreach( var sprite in sprites )
				{
					var texture = textures[sprite.TextureNumber];
					var destPoints
						= flip
						? new[] { new Point( sprite.TextureWidth + spritePadding.Right, y + spritePadding.Top ), new Point( 0 + spritePadding.Left, y + spritePadding.Top ), new Point( sprite.TextureWidth + spritePadding.Right, y + sprite.TextureHeight + spritePadding.Top ) }
						: new[] { new Point( 0 + spritePadding.Left, y + spritePadding.Top ), new Point( sprite.TextureWidth + spritePadding.Right, y + spritePadding.Top ), new Point( 0 + spritePadding.Left, y + sprite.TextureHeight + spritePadding.Top ) }
						;
						new Rectangle( 0, y, sprite.TextureWidth, sprite.TextureHeight );
					var srcRect = new Rectangle(sprite.TextureX, sprite.TextureY, sprite.TextureWidth, sprite.TextureHeight);
					imageGraphics.DrawImage( texture, destPoints, srcRect, GraphicsUnit.Pixel );
					y += srcRect.Height + spritePadding.Vertical;
				}

				if( !Directory.Exists( directory ) )
				{
					Directory.CreateDirectory( directory );
				}

				var currentFileName = Path.Combine(
					directory,
					string.Concat( filePrefix, "-", animation.Name, "-", animationFrame.Index, ".png" )
					);

				image.Save( currentFileName, ImageFormat.Png );
				image.Dispose();
			}
		}
	}
}
