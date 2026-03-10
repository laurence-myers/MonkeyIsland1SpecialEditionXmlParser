using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class OpenShaderFormCommand( LPAKFile lpakFile, string? fileName, int fileIndex ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			if( string.IsNullOrWhiteSpace( fileName ) )
			{
				return CommandResult.Fail( "Invalid file name" );
			}

			var form = new ShaderForm(
				fileIndex: fileIndex,
				mdiParent: MainForm.Instance,
				lpakFile: lpakFile,
				windowState: FormWindowState.Normal
			)
			{
				Text = fileName,	
			};
			form.Show();

			return CommandResult.Success(string.Empty);
		}
	}
}
