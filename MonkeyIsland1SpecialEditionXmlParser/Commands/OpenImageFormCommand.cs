using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.UI;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenImageFormCommand( LPAKFile lpakFile, string? fileName ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			if( string.IsNullOrWhiteSpace( fileName ) )
			{
				return CommandResult.Fail( "Invalid file name" );
			}

			var form = new ImageViewerForm( lpakFile, fileName! )
			{
				Text = fileName,
				MdiParent = MainForm.Instance,
				WindowState = FormWindowState.Normal,
			};

			if( !form.ReloadImage() )
			{
				form.Dispose();
				return CommandResult.Fail( "Invalid image" );
			}

			form.Show();
			return CommandResult.Success( string.Empty );
		}
	}
}
