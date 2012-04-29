using System;
using System.IO;
using System.Windows.Forms;

namespace MonkeyIsland1SpecialEditionXmlParser.UI
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

		public string XsltFileName
		{
			get
			{
				return this.xsltFileNameTextBox.Text;
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

			if( !string.IsNullOrEmpty( this.XsltFileName ) && File.Exists( this.XsltFileName ) && !StandardXsltFiles.Contains( this.XsltFileName ) )
			{
				UserSettings.Instance.RecentXsltFileNames = UserSettings.Instance.RecentXsltFileNames.UpdateRecentList( this.XsltFileName, 10 );
				UserSettings.Instance.Save();
			}
		}

		private void SelectExportFileName( object sender, EventArgs e )
		{
			if( this.ExportFileDialog.ShowDialog( this ) != DialogResult.OK )
			{
				return;
			}
			this.exportFileNameTextBox.Text = this.ExportFileDialog.FileName;
		}

		private void SelectXsltFileName( object sender, EventArgs e )
		{
			if( this.XsltFileDialog.ShowDialog( this ) != DialogResult.OK )
			{
				return;
			}
			this.xsltFileNameTextBox.Text = this.XsltFileDialog.FileName;
		}

		private void ExportFileNameChanged( object sender, EventArgs e )
		{
			try
			{
				var isExportFileNameValid
					= !string.IsNullOrWhiteSpace( this.exportFileNameTextBox.Text )
					&& Directory.Exists( Path.GetPathRoot( this.exportFileNameTextBox.Text ) )
					;
				var isXsltFileNameValid
					= string.IsNullOrEmpty( this.xsltFileNameTextBox.Text )
					|| File.Exists( this.xsltFileNameTextBox.Text )
					;
				this.exportButton.Enabled
					= isExportFileNameValid
					&& isXsltFileNameValid
					;
			}
			catch
			{
				this.exportButton.Enabled = false;
			}
		}

		private void ShowXsltList( object sender, EventArgs e )
		{
			var items = new ToolStripItem[this.xsltContextMenuStrip.Items.Count];
			this.xsltContextMenuStrip.Items.CopyTo( items, 0 );
			foreach( var item in items )
			{
				if( "recent".Equals( item.Tag ) )
				{
					xsltContextMenuStrip.Items.Remove( item );
				}
			}
			if( UserSettings.Instance.RecentXsltFileNames != null )
			{
				var breakerItem = new ToolStripSeparator()
				{
					Tag = "recent",
				};
				this.xsltContextMenuStrip.Items.Add( breakerItem );

				foreach( var xsltFileName in UserSettings.Instance.RecentXsltFileNames )
				{
					var item = new ToolStripMenuItem()
					{
						Text = xsltFileName,
						Tag = "recent",
					};
					item.Click += delegate { this.SetXsltFileNameToCustom( item.Text ); };
					this.xsltContextMenuStrip.Items.Add( item );
				}
			}

			this.xsltContextMenuStrip.Show( Cursor.Position );
		}

		private void SetXsltFileNameToCustom( string xsltFileName )
		{
			this.xsltFileNameTextBox.Text = xsltFileName;
		}

		private void SetXsltFileNameToCleanup( object sender, EventArgs e )
		{
			var xsltFileName = StandardXsltFiles.Cleanup;
			this.SetStandardXsltFileName( xsltFileName );
		}

		private void SetStandardXsltFileName( string xsltFileName )
		{
			if( !File.Exists( xsltFileName ) )
			{
				MessageBox.Show( string.Concat( "XSLT file was not found where it was expected to be.", Environment.NewLine, xsltFileName ) );
				return;
			}
			this.xsltFileNameTextBox.Text = xsltFileName;
		}

		private void XsltFileNameChanged( object sender, EventArgs args )
		{
			this.ExportFileNameChanged( sender, args );
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
