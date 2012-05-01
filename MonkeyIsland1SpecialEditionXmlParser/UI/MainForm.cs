using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	public partial class MainForm : System.Windows.Forms.Form
	{
		public static MainForm Instance
		{
			get;
			private set;
		}

		public MainForm()
		{
			MainForm.Instance = this;
			this.InitializeComponent();

			if( !UserSettings.Instance.DontShowQuickStart )
			{
				this.Paint += this.OpenQuickStartDialog;
			}
		}

		private void OpenQuickStartDialog( object sender, EventArgs args )
		{
			this.Paint -= this.OpenQuickStartDialog;
			Command.OpenQuickStartDialog.Execute();
		}

		private void OpenFileWithDialog( object sender, EventArgs e )
		{
			Command.OpenFileWithDialog.Execute();
		}

		private void ExitApplication( object sender, EventArgs e )
		{
			Application.Exit();
		}

		private void NavigateToForumThread( object sender, EventArgs e )
		{
			Process.Start( "http://www.lucasforums.com/showthread.php?p=2809988#post2809988" );
		}

		private void ShowAboutForm( object sender, EventArgs e )
		{
			using( var aboutForm = new AboutBox() )
			{
				aboutForm.ShowDialog( this );
			}
		}

		private void UpdateRecentMenuItems( object sender, EventArgs args )
		{
			this.recentToolStripMenuItem.DropDownItems.Clear();
			if( UserSettings.Instance.RecentLPAKFileNames != null )
			{
				foreach( var lpakFileName in UserSettings.Instance.RecentLPAKFileNames )
				{
					var item = new ToolStripMenuItem()
					{
						Text = lpakFileName,
						Tag = "recent",
					};
					item.Click += delegate
					{
						Command.OpenFile.OpenFileName = item.Text;
						Command.OpenFile.Execute();
					};
					this.recentToolStripMenuItem.DropDownItems.Add( item );
				}
			}
			else
			{
				this.recentToolStripMenuItem.DropDownItems.Add( "dummy" );
			}
		}
	}
}
