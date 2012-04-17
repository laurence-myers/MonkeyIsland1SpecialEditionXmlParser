using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Xsl;
using MonkeyIsland1SpecialEditionXmlParser.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	public partial class MainForm : System.Windows.Forms.Form
	{
		private string costumeFileName;
		private Costume costume;
		private XmlExportDialog xmlExportDialog;

		public MainForm()
		{
			InitializeComponent();

			this.xmlExportDialog = new XmlExportDialog()
			{
				Text = "XML Export",
			};

			this.openFileDialog1.InitialDirectory
				= this.exportAsPngFilesDialog.InitialDirectory
				= this.xmlExportDialog.ExportFileDialog.InitialDirectory
				= this.xmlExportDialog.XsltFileDialog.InitialDirectory
				= Environment.CurrentDirectory
				;
			this.dataGridView1.CellContentClick += this.ExportAsPngFiles;
		}

		private void ShowOpenCostumeFileDialog( object sender, EventArgs e )
		{
			if( this.openFileDialog1.ShowDialog( this ) != DialogResult.OK )
			{
				return;
			}

			var costumeFileName = this.openFileDialog1.FileName;
			this.OpenCostumeFile( costumeFileName );
		}

		private void OpenCostumeFile( string costumeFileName )
		{
			this.costumeFileName = costumeFileName;
			this.costume = Parser.Parse( this.costumeFileName );
			if( this.costume == null )
			{
				MessageBox.Show( "Unable to parse costume file." );
				return;
			}

			SanityChecker.Check( this.costume );
			this.PooulateGridView();
			this.SetTitle();
			this.EnableExportOptions();

			UserSettings.Instance.RecentCostumeFileNames = Helper.UpdateRecentList( UserSettings.Instance.RecentCostumeFileNames, this.costumeFileName, 10 );
			UserSettings.Instance.Save();
		}

		private void EnableExportOptions()
		{
			this.exportToolStripMenuItem.Enabled = true;
			foreach( ToolStripMenuItem item in this.exportToolStripMenuItem.DropDownItems )
			{
				item.Enabled = true;
			}
		}

		private void PooulateGridView()
		{
			this.dataGridView1.DataSource = null;
			Application.DoEvents();

			foreach( var animation in this.costume.AnimationList )
			{
				var rowIndex = this.dataGridView1.Rows.Add();
				var row = this.dataGridView1.Rows[rowIndex];
				row.Cells["animationColumn"].Value = animation.Name;
				row.Cells["spritesColumn"].Value = animation.AnimationFrameList.Count;
				row.Cells["framesColumn"].Value = animation.AnimationFrameList.Sum( af => af.FrameCount );
				row.Cells["exportButtonColumn"].Value = "Export as PNG files";
			}
		}

		private void ExportAsPngFiles( object sender, DataGridViewCellEventArgs args )
		{
			if( args.RowIndex < 0 )
			{
				return;
			}

			var columnName = this.dataGridView1.Columns[args.ColumnIndex].Name;
			if( columnName != "exportButtonColumn" )
			{
				return;
			}

			if( this.exportAsPngFilesDialog.ShowDialog( this ) != DialogResult.OK )
			{
				return;
			}

			var exportFileName = this.exportAsPngFilesDialog.FileName;
			var animationName = this.dataGridView1.Rows[args.RowIndex].Cells["animationColumn"].Value as string;

			this.ExportAsPngFiles( exportFileName, animationName );
		}

		private void SetTitle()
		{
			this.Text = this.costumeFileName;
		}

		private void ExportAllAsPngFiles( object sender, EventArgs e )
		{
			if( this.exportAsPngFilesDialog.ShowDialog( this ) != DialogResult.OK )
			{
				return;
			}
			var exportFileName = this.exportAsPngFilesDialog.FileName;

			foreach( var animation in this.costume.AnimationList )
			{
				this.ExportAsPngFiles( exportFileName, animation.Name );
			}
		}

		private void ExportAsPngFiles( string fileName, string animationName )
		{
			Renderer.Render( this.costume, animationName, fileName );
		}

		private void ExitApplication( object sender, EventArgs e )
		{
			Application.Exit();
		}

		private void ExportAsXmlFile( object sender, EventArgs e )
		{
			this.xmlExportDialog.ExportFileDialog.FileName = this.costume.Header.Name + this.costume.Header.Identifier + ".xml";
			if( this.xmlExportDialog.ShowDialog( this ) != DialogResult.OK )
			{
				return;
			}
			var exportFileName = this.xmlExportDialog.ExportFileName;
			var xsltFileName = this.xmlExportDialog.XsltFileName;

			Helper.WriteObjectToFile( exportFileName, this.costume );
			UserSettings.Instance.RecentExportFileNames = UserSettings.Instance.RecentExportFileNames.UpdateRecentList( exportFileName, 10 );
			UserSettings.Instance.Save();

			var isXsltFileNameValid
				= !string.IsNullOrWhiteSpace( xsltFileName )
				&& File.Exists( xsltFileName )
				;
			if( isXsltFileNameValid )
			{
				var tempFileName = Path.GetTempFileName();
				var transform = new XslCompiledTransform();
				transform.Load( xsltFileName );
				transform.Transform( exportFileName, tempFileName );
				File.Copy( tempFileName, exportFileName, overwrite: true );
				File.Delete( tempFileName );

				if( !StandardXsltFiles.Contains( xsltFileName ) )
				{
					UserSettings.Instance.RecentXsltFileNames = UserSettings.Instance.RecentXsltFileNames.UpdateRecentList( xsltFileName, 10 );
					UserSettings.Instance.Save();
				}
			}
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
					item.Click += delegate { this.OpenCostumeFile( item.Text ); };
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
