using System.Drawing;
using System.Drawing.Imaging;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class ExportToPngCommand : BaseCommand
	{
		public Bitmap ExportImage
		{
			get;
			set;
		}

		public string ExportFileName
		{
			get;
			set;
		}

		protected override bool InnerExecute()
		{
			if( this.ExportImage == null )
			{
				return false;
			}
			if( string.IsNullOrWhiteSpace( this.ExportFileName ) )
			{
				return false;
			}

			this.ExportImage.Save( this.ExportFileName, ImageFormat.Png );
			return true;
		}
	}
}
