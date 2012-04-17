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
		private ImageExportDialog imageExportDialog;

		public MainForm()
		{
			InitializeComponent();

			this.xmlExportDialog = new XmlExportDialog()
			{
				Text = "XML Export",
			};
			this.imageExportDialog = new ImageExportDialog()
			{
				Text = "Image Export",
			};

			this.openFileDialog1.InitialDirectory
				= this.xmlExportDialog.ExportFileDialog.InitialDirectory
				= this.xmlExportDialog.XsltFileDialog.InitialDirectory
				= this.imageExportDialog.FolderBrowserDialog.SelectedPath
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
			this.PopulateGridView();
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

		private void PopulateGridView()
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

			this.imageExportDialog.FilePrefix = string.Concat( this.costume.Header.Name, this.costume.Header.Identifier );
			if( this.imageExportDialog.ShowDialog( this ) != DialogResult.OK )
			{
				return;
			}

			var directory = this.imageExportDialog.Directory;
			var filePrefix = this.imageExportDialog.FilePrefix;
			var spritePadding = this.imageExportDialog.SpritePadding;
			var animationName = this.dataGridView1.Rows[args.RowIndex].Cells["animationColumn"].Value as string;

			this.ExportAsPngFiles( directory, filePrefix, animationName, spritePadding );
		}

		private void SetTitle()
		{
			this.Text = this.costumeFileName;
		}

		private void ExportAllAsPngFiles( object sender, EventArgs e )
		{
			this.imageExportDialog.FilePrefix = string.Concat( this.costume.Header.Name, this.costume.Header.Identifier );
			if( this.imageExportDialog.ShowDialog( this ) != DialogResult.OK )
			{
				return;
			}
			var directory = this.imageExportDialog.Directory;
			var filePrefix = this.imageExportDialog.FilePrefix;
			var spritePadding = this.imageExportDialog.SpritePadding;

			foreach( var animation in this.costume.AnimationList )
			{
				this.ExportAsPngFiles( directory, filePrefix, animation.Name, spritePadding );
			}

			UserSettings.Instance.RecentImageExportDirectories = UserSettings.Instance.RecentImageExportDirectories.UpdateRecentList( directory, 10 );
			UserSettings.Instance.RecentImageExportFilePrefixes = UserSettings.Instance.RecentImageExportFilePrefixes.UpdateRecentList( filePrefix, 10 );
			UserSettings.Instance.Save();
		}

		private void ExportAsPngFiles( string directory, string filePrefix, string animationName, Padding spritePadding )
		{
			Renderer.Render( this.costume, animationName, directory, filePrefix, spritePadding );
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
