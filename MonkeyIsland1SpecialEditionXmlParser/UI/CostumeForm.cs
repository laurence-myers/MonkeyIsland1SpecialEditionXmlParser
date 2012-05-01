using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Xsl;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;

namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	public partial class CostumeForm : Form
	{
		private static readonly List<CostumeForm> instances = new List<CostumeForm>();

		public Costume Costume
		{
			get;
			set;
		}

		public LPAKFile LPAKFile
		{
			get;
			set;
		}

		public int FileIndex
		{
			get;
			set;
		}

		public static CostumeForm[] Instances
		{
			get
			{
				return CostumeForm.instances.ToArray();
			}
		}

		public CostumeForm()
		{
			CostumeForm.instances.Add( this );
			this.FormClosed += delegate { CostumeForm.instances.Remove( this ); };
			
			this.InitializeComponent();

			this.Load += delegate { this.PopulateGridView(); };
			this.dataGridView1.CellContentClick += this.ExportAsPngFiles;
		}

		private void PopulateGridView()
		{
			if( this.LPAKFile == null )
			{
				return;
			}

			var fileEntry = this.LPAKFile.PakFileEntries[this.FileIndex];
			Helper.ReadBinaryFile( this.LPAKFile.FileNameOnDisk, reader =>
			{
				reader.BaseStream.Position = fileEntry.OffsetToStartOfData + this.LPAKFile.PakHeader.StartOfData;
				this.Costume = MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Parser.ReadCostume( reader );
			} );

			this.dataGridView1.DataSource = null;
			Application.DoEvents();

			foreach( var animation in this.Costume.AnimationList )
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

			Command.OpenImageExportDialog.FilePrefix = string.Concat( this.Costume.Header.Name, this.Costume.Header.Identifier );
			if( !Command.OpenImageExportDialog.Execute() )
			{
				return;
			}

			var directory = Command.OpenImageExportDialog.Directory;
			var filePrefix = Command.OpenImageExportDialog.FilePrefix;
			var spritePadding = Command.OpenImageExportDialog.SpritePadding;
			var animationName = this.dataGridView1.Rows[args.RowIndex].Cells["animationColumn"].Value as string;

			this.ExportAsPngFiles( directory, filePrefix, animationName, spritePadding );
		}

		private void ExportAllAsPngFiles( object sender, EventArgs e )
		{
			Command.OpenImageExportDialog.FilePrefix = string.Concat( this.Costume.Header.Name, this.Costume.Header.Identifier );
			if( !Command.OpenImageExportDialog.Execute() )
			{
				return;
			}
			var directory = Command.OpenImageExportDialog.Directory;
			var filePrefix = Command.OpenImageExportDialog.FilePrefix;
			var spritePadding = Command.OpenImageExportDialog.SpritePadding;

			foreach( var animation in this.Costume.AnimationList )
			{
				this.ExportAsPngFiles( directory, filePrefix, animation.Name, spritePadding );
			}
		}

		private void ExportAsPngFiles( string directory, string filePrefix, string animationName, Padding spritePadding )
		{
			Renderer.Render( this.Costume, animationName, directory, filePrefix, spritePadding, this.LPAKFile );
		}

		private void ExportAsXmlFile( object sender, EventArgs args )
		{
			Command.ExportToXml.ObjectToExport = this.Costume;
			Command.ExportToXml.ExportFileName = string.Concat( this.Costume.Header.Identifier, "_", this.Costume.Header.Name, ".xml" );
			Command.ExportToXml.Execute();
		}
	}
}
