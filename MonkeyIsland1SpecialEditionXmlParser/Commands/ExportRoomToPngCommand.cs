using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;
using System.IO;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportRoomToPngCommand : BaseCommand
	{
		public string ExportPath
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
			if( string.IsNullOrWhiteSpace( this.ExportPath ) )
			{
				return false;
			}
			if( !Directory.Exists( this.ExportPath ) )
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

					var exportFileName = Path.Combine( this.ExportPath, string.Concat( this.Room.Header.Identifier, "_", this.Room.Header.Name, "_", index, "_", index2, ".png" ) );
					texture.Save( exportFileName, ImageFormat.Png );
				}
			}

			return true;
		}
	}
}
