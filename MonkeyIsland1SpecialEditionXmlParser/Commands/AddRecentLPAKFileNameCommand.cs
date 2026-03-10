using System.IO;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class AddRecentLPAKFileNameCommand( string recentFileName ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			if( string.IsNullOrWhiteSpace( recentFileName ) )
			{
				return CommandResult.Fail( "Invalid recent file name" );
			}
			if( !File.Exists( recentFileName ) )
			{
				return CommandResult.Fail( "Recent file does not exist" );
			}

			UserSettings.Instance.RecentLPAKFileNames = Helper.UpdateRecentList(
				UserSettings.Instance.RecentLPAKFileNames,
				recentFileName,
				10
				);
			UserSettings.Instance.Save();

			return CommandResult.Success("");
		}
	}
}
