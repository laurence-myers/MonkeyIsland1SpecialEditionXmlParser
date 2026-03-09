using System.Drawing;
using System.Drawing.Imaging;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportToPngCommand( Bitmap exportImage, string exportFileName ) : BaseCommand
	{
		protected override bool InnerExecute()
		{
			if( exportImage == null )
			{
				return false;
			}
			if( string.IsNullOrWhiteSpace( exportFileName ) )
			{
				return false;
			}

			exportImage.Save( exportFileName, ImageFormat.Png );
			return true;
		}
	}
}
