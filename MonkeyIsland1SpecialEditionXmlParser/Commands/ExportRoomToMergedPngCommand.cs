using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportRoomToMergedPngCommand( string exportFileName, LPAKFile lpakFile, Room room ) : BaseCommand
	{
		protected override bool InnerExecute()
		{
			if( string.IsNullOrWhiteSpace( exportFileName ) )
			{
				return false;
			}
			if( room.StaticSpriteList == null )
			{
				return false;
			}

			var width = room.StaticSpriteList.Max( ssl => ssl.Max( ss => ss.X + ss.Width ) );
			var height = room.StaticSpriteList.Max( ssl => ssl.Max( ss => ss.Y + ss.Height ) );

			var image = new Bitmap( width, height );
			var graphics = Graphics.FromImage( image );

			foreach( var staticSpriteList in room.StaticSpriteList )
			{
				foreach( var staticSprite in staticSpriteList )
				{
					if( staticSprite == null )
					{
						continue;
					}

					var textureFileName = staticSprite.TextureFileName;
					var texture = lpakFile.LoadImage( textureFileName );
					if( texture == null )
					{
						continue;
					}

					var destRect = new RectangleF( staticSprite.X, staticSprite.Y, staticSprite.Width, staticSprite.Height );
					var srcRect = new RectangleF( 0.0f, 0.0f, texture.Width, texture.Height );
					graphics.DrawImage( texture, destRect, srcRect, GraphicsUnit.Pixel );
				}
			}

			image.Save( exportFileName, ImageFormat.Png );
			return true;
		}
	}
}
