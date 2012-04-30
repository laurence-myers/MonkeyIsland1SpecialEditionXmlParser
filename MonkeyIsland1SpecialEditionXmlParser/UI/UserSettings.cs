using System.IO;

namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	public class UserSettings
	{
		private static string UserSettingsFileName
		{
			get
			{
				var directory = Helper.GetExecutingAssemblyDirectory();
				var fileName = Path.Combine( directory, "UserSettings.xml" );
				return fileName;
			}
		}

		private static UserSettings instance;
		public static UserSettings Instance
		{
			get
			{
				if( UserSettings.instance == null )
				{
					if( File.Exists( UserSettings.UserSettingsFileName ) )
					{
						UserSettings.instance = Helper.ReadObjectFromFile<UserSettings>( UserSettings.UserSettingsFileName );
					}
					else
					{
						UserSettings.instance = new UserSettings();
					}
				}
				return UserSettings.instance;
			}
		}

		public string[] RecentXsltFileNames
		{
			get;
			set;
		}

		public string[] RecentExportFileNames
		{
			get;
			set;
		}

		public string[] RecentLPAKFileNames
		{
			get;
			set;
		}

		public string[] RecentCostumeFileNames
		{
			get;
			set;
		}

		public string[] RecentImageExportDirectories
		{
			get;
			set;
		}

		public string[] RecentImageExportFilePrefixes
		{
			get;
			set;
		}

		public bool DontShowQuickStart
		{
			get;
			set;
		}

		public void Save()
		{
			Helper.WriteObjectToFile( UserSettings.UserSettingsFileName, this );
		}
	}
}
