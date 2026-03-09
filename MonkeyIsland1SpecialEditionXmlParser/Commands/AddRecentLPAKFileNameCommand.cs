using System.IO;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class AddRecentLPAKFileNameCommand( string recentFileName ) : BaseCommand
	{
		protected override bool InnerExecute()
		{
			if( string.IsNullOrWhiteSpace( recentFileName ) )
			{
				return false;
			}
			if( !File.Exists( recentFileName ) )
			{
				return false;
			}

			UserSettings.Instance.RecentLPAKFileNames = Helper.UpdateRecentList(
				UserSettings.Instance.RecentLPAKFileNames,
				recentFileName,
				10
				);
			UserSettings.Instance.Save();

			return true;
		}
	}
}
