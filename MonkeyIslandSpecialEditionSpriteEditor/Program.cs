using System;
using System.Threading;
using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.UI;

namespace MonkeyIslandSpecialEditionSpriteEditor
{
	class Program
	{
		[STAThread]
		static void Main( string[] args )
		{
			Application.EnableVisualStyles();
			Application.ThreadException += Program.HandleThreadException;

			using( var mainForm = MainForm.Instance )
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
