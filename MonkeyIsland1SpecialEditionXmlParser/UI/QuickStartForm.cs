using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	public partial class QuickStartForm : Form
	{
		public QuickStartForm()
		{
			this.InitializeComponent();
			this.Load += this.PopulateRecentList;
		}

		private void PopulateRecentList( object sender, EventArgs args )
		{
			this.treeView1.Nodes.Clear();
			if( UserSettings.Instance.RecentLPAKFileNames != null )
			{
				var nodes = UserSettings.Instance.RecentLPAKFileNames
					.Where( fileName => File.Exists( fileName ) )
					.Select( fileName => new TreeNode( fileName ) )
					.ToArray()
					;
				this.treeView1.Nodes.AddRange( nodes );
				this.treeView1.Enabled = true;
				this.openRecentFileButton.Enabled = true;
			}
		}

		private void ShowOpenFileDialog( object sender, EventArgs e )
		{
			this.Enabled = false;
			Application.DoEvents();
			this.Close();
			Command.OpenFileWithDialog.Execute();
		}

		private void OpenRecentFile( object sender, TreeNodeMouseClickEventArgs args )
		{
			if( args == null )
			{
				return;
			}

			var fileName = args.Node.Text;
			this.OpenRecentFile( fileName );
		}

		private void OpenRecentFile( object sender, EventArgs args )
		{
			var selectedNode = this.treeView1.SelectedNode;
			if( selectedNode == null )
			{
				return;
			}

			var fileName = selectedNode.Text;
			this.OpenRecentFile( fileName );
		}

		private void OpenRecentFile( string fileName )
		{
			if( !File.Exists( fileName ) )
			{
				return;
			}

			this.Enabled = false;
			Application.DoEvents();
			this.Close();
			Command.OpenFile.OpenFileName = fileName;
			Command.OpenFile.Execute();
		}

		private void UpdateRecentButtonEnabled( object sender, TreeViewEventArgs args )
		{
			this.openRecentFileButton.Enabled = args.Node != null;
		}

		private void CloseIfEscPressed( object sender, KeyEventArgs args )
		{
			if( args.KeyCode == Keys.Escape )
			{
				this.Close();
			}
		}

		private void SetDontShowAtStartupFlag( object sender, EventArgs args )
		{
			UserSettings.Instance.DontShowQuickStart = true;
			UserSettings.Instance.Save();
			this.Close();
		}
	}
}
