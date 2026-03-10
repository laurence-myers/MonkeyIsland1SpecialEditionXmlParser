using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;
using System.IO;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportRoomToPngCommand( string exportPath, LPAKFile lpakFile, Room room ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			if( string.IsNullOrWhiteSpace( exportPath ) )
			{
				return CommandResult.Fail( "Invalid export path" );
			}
			if( !Directory.Exists( exportPath ) )
			{
				return CommandResult.Fail( "Export path does not exist" );
			}
			if( room.StaticSpriteList == null )
			{
				return CommandResult.Fail( "Room has no static sprites" );
			}

			var width = room.StaticSpriteList.Max( ssl => ssl.Max( ss => ss.X + ss.Width ) );
			var height = room.StaticSpriteList.Max( ssl => ssl.Max( ss => ss.Y + ss.Height ) );

			for( var index = 0; index < room.StaticSpriteList.Count; index++ )
			{
				var staticSpriteList = room.StaticSpriteList[index];
				for( var index2 = 0; index2 < staticSpriteList.Count; index2++ )
				{
					var staticSprite = staticSpriteList[index2];
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

					var exportFileName = Path.Combine( exportPath, string.Concat( room.Header.Identifier, "_", room.Header.Name, "_", index, "_", index2, ".png" ) );
					texture.Save( exportFileName, ImageFormat.Png );
				}
			}

			return CommandResult.Success( $"Exported room to {exportPath}" );
		}
	}
}
