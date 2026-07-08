using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities;

namespace MonkeyIslandSpecialEditionSpriteEditor.Commands
{
	public class ExportRoomToMergedPngCommand( string exportFileName, LPAKFile lpakFile, Room room ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			if( string.IsNullOrWhiteSpace( exportFileName ) )
			{
				return CommandResult.Fail( "Invalid export file name" );
			}
			if( room.StaticSpriteList == null )
			{
				return CommandResult.Fail( "Room has no static sprite list" );
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
			return CommandResult.Success( $"Exported room to merged PNG: {exportFileName}" );
		}
	}
}
