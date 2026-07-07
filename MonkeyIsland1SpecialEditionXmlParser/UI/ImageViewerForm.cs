using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Commands;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;

namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	public partial class ImageViewerForm : Form
	{
		private LPAKFile LPAKFile
		{
			get;
		}

		private string ResourcePath
		{
			get;
		}

		public ImageViewerForm( LPAKFile lpakFile, string resourcePath )
		{
			this.LPAKFile = lpakFile;
			this.ResourcePath = resourcePath;
			this.InitializeComponent();
		}

		/// <summary>
		/// (Re)loads the image from the LPAK, preferring a loose override file, and shows
		/// whether an override is in effect. Returns false when the image cannot be loaded.
		/// </summary>
		public bool ReloadImage()
		{
			var bitmap = this.LPAKFile.LoadImage( this.ResourcePath ) as Bitmap;
			if( bitmap == null )
			{
				return false;
			}

			this.spriteSetPreviewControl.Sprites.Clear();
			this.spriteSetPreviewControl.Sprites.Add( new SpriteSetPreviewControlSprite( bitmap, layer: 0, name: this.ResourcePath ) );
			this.spriteSetPreviewControl.Invalidate();

			var hasOverride = Formats.LPAK.Parser.GetOverrideFilePath( this.LPAKFile.FileNameOnDisk, this.ResourcePath ) != null;
			this.label1.Text = string.Concat(
				this.ResourcePath, " - ", bitmap.Width, "x", bitmap.Height,
				hasOverride ? " (override)" : ""
			);
			return true;
		}

		private void ExportAsPng( object sender, EventArgs args )
		{
			var sprite = this.spriteSetPreviewControl.Sprites.FirstOrDefault();
			if( sprite == null )
			{
				return;
			}

			var defaultFileName = Path.GetFileNameWithoutExtension( this.ResourcePath.Replace( '/', '_' ) ) + ".png";
			new ExportToPngWithDialogCommand( sprite.Image, defaultFileName ).Execute();
		}

		private void ImportFromPng( object sender, EventArgs args )
		{
			using( var dialog = new OpenFileDialog() )
			{
				dialog.Filter = "PNG files (*.png)|*.png|All files (*.*)|*.*";
				dialog.Title = string.Concat( "Import PNG as ", this.ResourcePath );
				if( dialog.ShowDialog( this ) != DialogResult.OK )
				{
					return;
				}

				var result = new ImportTexturePngCommand( this.LPAKFile, this.ResourcePath, dialog.FileName ).Execute();
				if( !result.IsSuccess )
				{
					MessageBox.Show( this, result.Error, "Import texture", MessageBoxButtons.OK, MessageBoxIcon.Error );
					return;
				}
			}

			this.ReloadImage();
		}
	}
}
