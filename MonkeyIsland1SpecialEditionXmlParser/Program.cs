using System;
using System.Linq;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser
{
	class Program
	{
		[STAThread]
		static void Main( string[] args )
		{
			var room28 = Formats.Rooms.Parser.Parse( @"K:\SVN\svn.code.sf.net\mi1sexmlparser\MonkeyIsland1SpecialEditionXmlParser\bin\Debug\Data\Rooms\28_bar.room.xml" );
			var room10 = Formats.Rooms.Parser.Parse( @"K:\SVN\svn.code.sf.net\mi1sexmlparser\MonkeyIsland1SpecialEditionXmlParser\bin\Debug\Data\Rooms\10_logo.room.xml" );
			var room12 = Formats.Rooms.Parser.Parse( @"K:\SVN\svn.code.sf.net\mi1sexmlparser\MonkeyIsland1SpecialEditionXmlParser\bin\Debug\Data\Rooms\12_monkey-he.room.xml" );
			var room33 = Formats.Rooms.Parser.Parse( @"K:\SVN\svn.code.sf.net\mi1sexmlparser\MonkeyIsland1SpecialEditionXmlParser\bin\Debug\Data\Rooms\33_dock.room.xml" );
			var room88 = Formats.Rooms.Parser.Parse( @"K:\SVN\svn.code.sf.net\mi1sexmlparser\MonkeyIsland1SpecialEditionXmlParser\bin\Debug\Data\Rooms\88_fighting.room.xml" );

			var fileNames = System.IO.Directory.GetFiles( @"K:\SVN\svn.code.sf.net\mi1sexmlparser\MonkeyIsland1SpecialEditionXmlParser\bin\Debug\Data\Rooms" );
			foreach( var fileName in fileNames )
			{
				var room = Formats.Rooms.Parser.Parse( fileName ) as Formats.Rooms.Entities.Room;
				if( room.Unknown4GroupList.Any( u4g => u4g.Unknown4List.Any( u4 => u4.Unknown4_1Address != 0 && u4.Unknown4_2Address != 0 ) ) )
				{
				}
			}


			//Application.EnableVisualStyles();

			//using( var mainForm = new MainForm() )
			//{
			//    Application.Run( mainForm );
			//}
		}
	}
}
