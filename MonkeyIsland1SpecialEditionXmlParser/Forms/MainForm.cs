using System;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using MonkeyIsland1SpecialEditionXmlParser.Entities;
using System.IO;

namespace MonkeyIsland1SpecialEditionXmlParser.Forms
{
	public partial class MainForm : Form
	{
		private string costumeFileName;
		private Costume costume;

		public MainForm()
		{
			InitializeComponent();

			this.openFileDialog1.InitialDirectory
				= this.exportAsPngFilesDialog.InitialDirectory
				= this.exportAsXmlFileDialog.InitialDirectory
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

			this.costumeFileName = this.openFileDialog1.FileName;
			this.OpenCostumeFile();
		}

		private void OpenCostumeFile()
		{
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
			if( this.exportAsXmlFileDialog.ShowDialog( this ) != DialogResult.OK )
			{
				return;
			}
			var exportFileName = this.exportAsXmlFileDialog.FileName;

			Stream stream = null;

			try
			{
				stream = File.Create( exportFileName );
				var serializer = new XmlSerializer( typeof( Costume ) );
				serializer.Serialize( stream, this.costume );
			}
			finally
			{
				if( stream != null )
				{
					stream.Close();
					stream.Dispose();
					stream = null;
				}
			}
		}
	}
}
