using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportRoomToMergedPngCommand : BaseCommand
	{
		public string ExportFileName
		{
			get;
			set;
		}

		public LPAKFile LPAKFile
		{
			get;
			set;
		}

		public Room Room
		{
			get;
			set;
		}

		protected override bool InnerExecute()
		{
			if( this.LPAKFile == null )
			{
				return false;
			}
			if( string.IsNullOrWhiteSpace( this.ExportFileName ) )
			{
				return false;
			}
			if( this.Room == null )
			{
				return false;
			}
			if( this.Room.StaticSpriteList == null )
			{
				return false;
			}

			var width = this.Room.StaticSpriteList.Max( ssl => ssl.Max( ss => ss.X + ss.Width ) );
			var height = this.Room.StaticSpriteList.Max( ssl => ssl.Max( ss => ss.Y + ss.Height ) );

			var image = new Bitmap( width, height );
			var graphics = Graphics.FromImage( image );

			for( var index = 0; index < this.Room.StaticSpriteList.Count; index++ )
			{
				var staticSpriteList = this.Room.StaticSpriteList[index];
				for( var index2 = 0; index2 < staticSpriteList.Count; index2++ )
				{
					var staticSprite = staticSpriteList[index2];
					if( staticSprite == null )
					{
						continue;
					}

					var textureFileName = staticSprite.TextureFileName;
					var texture = this.LPAKFile.LoadImage( textureFileName );
					if( texture == null )
					{
						continue;
					}

					var destRect = new RectangleF( staticSprite.X, staticSprite.Y, staticSprite.Width, staticSprite.Height );
					var srcRect = new RectangleF( 0.0f, 0.0f, texture.Width, texture.Height );
					graphics.DrawImage( texture, destRect, srcRect, GraphicsUnit.Pixel );
				}
			}

			image.Save( this.ExportFileName, ImageFormat.Png );
			return true;
		}
	}
}
