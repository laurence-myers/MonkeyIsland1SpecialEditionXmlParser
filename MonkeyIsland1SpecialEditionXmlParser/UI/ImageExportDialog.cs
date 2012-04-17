using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	public partial class ImageExportDialog : Form
	{
		public Padding SpritePadding
		{
			get
			{
				return new Padding(
					int.Parse( this.paddingLeftTextBox.Text ),
					int.Parse( this.paddingTopTextBox.Text ),
					int.Parse( this.paddingRightTextBox.Text ),
					int.Parse( this.paddingBottomTextBox.Text )
					);
			}
		}

		public string Directory
		{
			get
			{
				return this.directoryTextBox.Text;
			}
		}

		public string FilePrefix
		{
			get
			{
				return this.filePrefixTextBox.Text;
			}
			set
			{
				this.filePrefixTextBox.Text = value;
			}
		}

		public ImageExportDialog()
		{
			this.InitializeComponent();
		}

		private void ShowDirectoryBrowser( object sender, EventArgs e )
		{
			if( this.FolderBrowserDialog.ShowDialog( this ) != DialogResult.OK )
			{
				return;
			}

			this.directoryTextBox.Text = this.FolderBrowserDialog.SelectedPath;
		}

		private void ShowDirectoryRecentList( object sender, EventArgs e )
		{
			this.directoryRecentContextMenuStrip.Items.Clear();
			if( UserSettings.Instance.RecentImageExportDirectories != null )
			{
				foreach( var directory in UserSettings.Instance.RecentImageExportDirectories )
				{
					var item = new ToolStripMenuItem()
					{
						Text = directory,
						Tag = "recent",
					};
					item.Click += delegate { this.directoryTextBox.Text = item.Text; };
					this.directoryRecentContextMenuStrip.Items.Add( item );
				}
			}
			this.directoryRecentContextMenuStrip.Show( Cursor.Position );
		}

		private void ShowFilePrefixRecentList( object sender, EventArgs e )
		{
			this.filePrefixRecentContextMenuStrip.Items.Clear();
			if( UserSettings.Instance.RecentImageExportFilePrefixes != null )
			{
				foreach( var filePrefix in UserSettings.Instance.RecentImageExportFilePrefixes )
				{
					var item = new ToolStripMenuItem()
					{
						Text = filePrefix,
						Tag = "recent",
					};
					item.Click += delegate { this.filePrefixTextBox.Text = item.Text; };
					this.filePrefixRecentContextMenuStrip.Items.Add( item );
				}
			}
			this.filePrefixRecentContextMenuStrip.Show( Cursor.Position );
		}

		private void HandleFilePrefixChanged( object sender, EventArgs e )
		{
			this.UpdateExportButtonEnabledState();
		}

		private void HandleDirectoryChanged( object sender, EventArgs e )
		{
			this.UpdateExportButtonEnabledState();
		}

		private void HandlePaddingChanged( object sender, EventArgs e )
		{
			this.UpdateExportButtonEnabledState();
		}

		private void UpdateExportButtonEnabledState()
		{
			try
			{
				var isDirectoryValid
					= !string.IsNullOrWhiteSpace( this.directoryTextBox.Text )
					&& System.IO.Directory.Exists( Path.GetPathRoot( this.directoryTextBox.Text ) )
					;
				var isFilePrefixValid
					= !string.IsNullOrWhiteSpace( this.filePrefixTextBox.Text )
					&& this.filePrefixTextBox.Text.IndexOfAny( Path.GetInvalidFileNameChars() ) == -1
					;
				var isValidPadding
					= !string.IsNullOrWhiteSpace( this.paddingLeftTextBox.Text )
					&& !string.IsNullOrWhiteSpace( this.paddingTopTextBox.Text )
					&& !string.IsNullOrWhiteSpace( this.paddingRightTextBox.Text )
					&& !string.IsNullOrWhiteSpace( this.paddingBottomTextBox.Text )
					&& this.paddingLeftTextBox.Text.All( c => char.IsNumber( c ) )
					&& this.paddingTopTextBox.Text.All( c => char.IsNumber( c ) )
					&& this.paddingRightTextBox.Text.All( c => char.IsNumber( c ) )
					&& this.paddingBottomTextBox.Text.All( c => char.IsNumber( c ) )
					;
				this.exportButton.Enabled
					= isDirectoryValid
					&& isFilePrefixValid
					&& isValidPadding
					;
			}
			catch
			{
				this.exportButton.Enabled = false;
			}
		}
	}
}
