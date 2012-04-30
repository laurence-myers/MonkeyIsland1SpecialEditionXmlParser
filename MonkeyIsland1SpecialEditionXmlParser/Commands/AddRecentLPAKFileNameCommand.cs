using System.IO;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	public class AddRecentLPAKFileNameCommand : BaseCommand
	{
		public string RecentFileName
		{
			get;
			set;
		}

		protected override bool InnerExecute()
		{
			if( string.IsNullOrWhiteSpace( this.RecentFileName ) )
			{
				return false;
			}
			if( !File.Exists( this.RecentFileName ) )
			{
				return false;
			}

			UserSettings.Instance.RecentLPAKFileNames = Helper.UpdateRecentList(
				UserSettings.Instance.RecentLPAKFileNames,
				this.RecentFileName,
				10
				);
			UserSettings.Instance.Save();

			return true;
		}
	}
}
