using System;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;

namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	public partial class ShaderForm : Form
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

		public string LPAKFileName
		{
			get;
			set;

		}

		public ShaderForm()
		{
			this.InitializeComponent();
		}

		public void HandleFormLoad( object sender, EventArgs args )
		{
			var entry = this.LPAKFile.PakFileEntries[this.FileIndex];
			Helper.ReadBinaryFile( this.LPAKFileName, reader =>
			{
				reader.BaseStream.Position = entry.OffsetToStartOfData + this.LPAKFile.PakHeader.StartOfData;
				this.textBox1.Text = reader.ReadStringMonkeyNoPadding( entry.SizeOfData2 );
			} );
			this.textBox1.Select( 0, 0 );
		}
	}
}
