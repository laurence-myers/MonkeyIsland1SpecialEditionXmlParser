using System;
using System.Diagnostics;
using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.Commands;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	public partial class MainForm : System.Windows.Forms.Form, IDisposable
	{
		private static MainForm? _instance;

		public static MainForm Instance
		{
			get
			{
				MainForm._instance ??= new MainForm();
				return MainForm._instance;
			}
			private set
			{
				MainForm._instance = value;
			}
		}

		private MainForm()
		{
			this.InitializeComponent();

			if( !UserSettings.Instance.DontShowQuickStart )
			{
				this.Paint += this.OpenQuickStartDialog;
			}
		}

		public new void Dispose()
		{
			base.Dispose();
			MainForm._instance = null;
		}

		private void OpenQuickStartDialog( object sender, EventArgs args )
		{
			this.Paint -= this.OpenQuickStartDialog;
			new OpenQuickStartDialogCommand().Execute();
		}

		private void OpenFileWithDialog( object sender, EventArgs e )
		{
			new OpenFileWithDialogCommand().Execute();
		}

		private void ExitApplication( object sender, EventArgs e )
		{
			Application.Exit();
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
						new OpenFileCommand( item.Text ).Execute();
					};
					this.recentToolStripMenuItem.DropDownItems.Add( item );
				}
			}
			else
			{
				this.recentToolStripMenuItem.DropDownItems.Add( "dummy" );
			}
		}

		public void SetStatusText( string text )
		{
			this.toolStripStatusLabel.Text = text;
		}
	}
}
