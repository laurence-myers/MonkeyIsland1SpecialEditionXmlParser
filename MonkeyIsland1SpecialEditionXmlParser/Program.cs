using System;
using System.Threading;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser
{
	class Program
	{
		[STAThread]
		static void Main( string[] args )
		{
			Application.EnableVisualStyles();
			Application.ThreadException += Program.HandleThreadException;

			using( var mainForm = new MainForm() )
			{
				Application.Run( mainForm );
			}
		}

		private static void HandleThreadException( object sender, ThreadExceptionEventArgs args )
		{
			using( var form = new ExceptionForm() )
			{
				form.SetException( args.Exception );
				form.ShowDialog();
			}
		}
	}
}
