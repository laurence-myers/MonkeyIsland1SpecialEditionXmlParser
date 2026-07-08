using System.Drawing;
using System.Drawing.Imaging;

namespace MonkeyIslandSpecialEditionSpriteEditor.Commands
{
	public class ExportToPngCommand( Bitmap exportImage, string exportFileName ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			if( exportImage == null )
			{
				return CommandResult.Fail( "Invalid export image" );
			}
			if( string.IsNullOrWhiteSpace( exportFileName ) )
			{
				return CommandResult.Fail( "Invalid export file name" );
			}

			exportImage.Save( exportFileName, ImageFormat.Png );
			return CommandResult.Success( $"Exported to PNG: {exportFileName}" );
		}
	}
}
