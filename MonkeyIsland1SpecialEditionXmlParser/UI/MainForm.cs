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

			// TODO
			//this.openFileDialog.InitialDirectory
			//    = this.xmlExportDialog.ExportFileDialog.InitialDirectory
			//    = this.xmlExportDialog.XsltFileDialog.InitialDirectory
			//    = this.imageExportDialog.FolderBrowserDialog.SelectedPath
			//    = Environment.CurrentDirectory
			//    ;
		}

		private void OpenFileWithDialog( object sender, EventArgs e )
		{
			Command.OpenFileWithDialog.Execute();
		}

		public void EnableExportOptions()
		{
			this.exportToolStripMenuItem.Enabled = true;
			foreach( ToolStripMenuItem item in this.exportToolStripMenuItem.DropDownItems )
			{
				item.Enabled = true;
			}
		}

		private void ExitApplication( object sender, EventArgs e )
		{
			Application.Exit();
		}

		private void NavigateToMISEExplorer( object sender, EventArgs e )
		{
			Process.Start( "http://quick.mixnmojo.com/software/monkey-island-explorer" );
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
			if( UserSettings.Instance.RecentCostumeFileNames != null )
			{
				foreach( var costumeFileName in UserSettings.Instance.RecentCostumeFileNames )
				{
					var item = new ToolStripMenuItem()
					{
						Text = costumeFileName,
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
