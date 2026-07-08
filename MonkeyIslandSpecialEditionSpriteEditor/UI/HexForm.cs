using System;
using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	public partial class HexForm : Form
	{
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

		public HexForm( int fileIndex, Form mdiParent, LPAKFile lpakFile, FormWindowState windowState )
		{
			this.FileIndex = fileIndex;
			this.MdiParent = mdiParent;
			this.LPAKFile = lpakFile;
			this.WindowState = windowState;
			
			this.InitializeComponent();
		}

		public void HandleFormLoad( object sender, EventArgs args )
		{
			var entry = this.LPAKFile.PakFileEntries[this.FileIndex];
			Helper.ReadBinaryFile( this.LPAKFile.FileNameOnDisk, reader =>
			{
				reader.BaseStream.Position = entry.OffsetToStartOfData + this.LPAKFile.PakHeader.StartOfData;
				this.textBox1.Text = reader.ReadBytesAsHexEditor( entry.SizeOfData2 );
			} );
			this.textBox1.Select( 0, 0 );
		}
	}
}
