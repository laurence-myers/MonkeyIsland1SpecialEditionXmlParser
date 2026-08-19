using System;
using System.IO;
using System.Windows.Forms;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	public partial class XmlExportDialog : System.Windows.Forms.Form
	{
		public string ExportFileName
		{
			get
			{
				return this.exportFileNameTextBox.Text;
			}
			set
			{
				this.exportFileNameTextBox.Text = value;
				this.ExportFileDialog.FileName = Path.GetFileName( value );
			}
		}

		public XmlExportDialog()
		{
			this.InitializeComponent();
			this.FormClosing += this.UpdateRecentLists;
		}

		private void UpdateRecentLists( object sender, FormClosingEventArgs args )
		{
			if( this.DialogResult != System.Windows.Forms.DialogResult.OK )
			{
				return;
			}

			UserSettings.Instance.RecentExportFileNames = UserSettings.Instance.RecentExportFileNames.UpdateRecentList( this.ExportFileName, 10 );
			UserSettings.Instance.Save();
		}

		private void SelectExportFileName( object sender, EventArgs e )
		{
			if( this.ExportFileDialog.ShowDialog( this ) != DialogResult.OK )
			{
				return;
			}
			this.exportFileNameTextBox.Text = this.ExportFileDialog.FileName;
		}

		private void ExportFileNameChanged( object sender, EventArgs e )
		{
			try
			{
				this.exportButton.Enabled
					= !string.IsNullOrWhiteSpace( this.exportFileNameTextBox.Text )
					&& Directory.Exists( Path.GetPathRoot( this.exportFileNameTextBox.Text ) )
					;
			}
			catch
			{
				this.exportButton.Enabled = false;
			}
		}

		private void ShowExportFileNameList( object sender, EventArgs e )
		{
			var items = new ToolStripItem[this.exportFileNameContextMenuStrip.Items.Count];
			this.exportFileNameContextMenuStrip.Items.CopyTo( items, 0 );
			foreach( var item in items )
			{
				if( "recent".Equals( item.Tag ) )
				{
					exportFileNameContextMenuStrip.Items.Remove( item );
				}
			}
			if( UserSettings.Instance.RecentExportFileNames != null )
			{
				foreach( var exportFileName in UserSettings.Instance.RecentExportFileNames )
				{
					var item = new ToolStripMenuItem()
					{
						Text = exportFileName,
						Tag = "recent",
					};
					item.Click += delegate { this.exportFileNameTextBox.Text = item.Text; };
					this.exportFileNameContextMenuStrip.Items.Add( item );
				}
			}

			this.exportFileNameContextMenuStrip.Show( Cursor.Position );
		}
	}
}
