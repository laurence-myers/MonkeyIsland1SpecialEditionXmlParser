using System.IO;

namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	public static class StandardXsltFiles
	{
		public static string Cleanup
		{
			get
			{
				var fileName = StandardXsltFiles.GetPath( "Cleanup" );
				return fileName;
			}
		}

		private static string GetPath( string name )
		{
			var directory = Helper.GetExecutingAssemblyDirectory();
			var xsltDirectory = Path.Combine( directory, "XSLT" );
			var xsltFileName = Path.Combine( xsltDirectory, name + ".xslt" );
			return xsltFileName;
		}

		public static bool Contains( string xsltFileName )
		{
			return StandardXsltFiles.Cleanup == xsltFileName;
		}
	}
}
