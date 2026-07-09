using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.Commands;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	public partial class ImageViewerForm : Form
	{
		// keeps the zoom bar usable when the texture is tiny or the MDI workspace is small
		private static readonly Size MinimumClientSize = new Size( 360, 200 );

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

			// the trackbar's value is the index of the zoom level, so it can only ever sit on one
			this.trackBarZoom.Minimum = 0;
			this.trackBarZoom.Maximum = SpriteSetPreviewControl.ZoomLevels.Length - 1;
			this.trackBarZoom.Value = this.spriteSetPreviewControl.ZoomLevelIndex;
			this.UpdateZoomLabel();

			this.trackBarZoom.ValueChanged += this.HandleZoomTrackBarChanged;
			this.spriteSetPreviewControl.ZoomChanged += this.HandlePreviewZoomChanged;
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
			this.spriteSetPreviewControl.RefreshContent();

			var hasOverride = Formats.LPAK.Parser.GetOverrideFilePath( this.LPAKFile.FileNameOnDisk, this.ResourcePath ) != null;
			this.label1.Text = string.Concat(
				this.ResourcePath, " - ", bitmap.Width, "x", bitmap.Height,
				hasOverride ? " (override)" : ""
			);
			return true;
		}

		protected override void OnLoad( EventArgs args )
		{
			base.OnLoad( args );

			// only now has the form been scaled to the current font/DPI, so the chrome around the
			// preview panel finally measures the size it will actually have on screen
			this.SizeToTexture();

			// the wheel only reaches the focused control; zoom without having to click first
			this.spriteSetPreviewControl.Select();
		}

		//-------------------------------------------
		// zoom

		private void HandleZoomTrackBarChanged( object? sender, EventArgs args )
		{
			this.spriteSetPreviewControl.ZoomLevelIndex = this.trackBarZoom.Value;
			this.UpdateZoomLabel();
		}

		private void HandlePreviewZoomChanged( object? sender, EventArgs args )
		{
			// setting the same value again raises nothing, so this cannot bounce back
			this.trackBarZoom.Value = this.spriteSetPreviewControl.ZoomLevelIndex;
			this.UpdateZoomLabel();
		}

		private void UpdateZoomLabel()
		{
			this.labelZoom.Text = string.Concat( (int)Math.Round( this.spriteSetPreviewControl.Zoom * 100 ), "%" );
		}

		//-------------------------------------------
		// sizing

		/// <summary>
		/// Opens the window showing the whole texture at 100% zoom, shrinking to the MDI workspace
		/// when the texture does not fit; the preview panel scrolls in that case.
		/// </summary>
		private void SizeToTexture()
		{
			var contentSize = this.spriteSetPreviewControl.GetContentSize();
			if( contentSize.IsEmpty )
			{
				return;
			}

			// the preview panel is the only docked-Fill control, so the rest of the client area is
			// chrome; measure the panel's outer size, which excludes any scrollbars showing now
			var chromeSize = new Size(
				this.ClientSize.Width - this.panelPreview.Width,
				this.ClientSize.Height - this.panelPreview.Height
			);
			var maximumClientSize = this.GetMaximumClientSize();
			this.ClientSize = new Size(
				Math.Min( contentSize.Width + chromeSize.Width, maximumClientSize.Width ),
				Math.Min( contentSize.Height + chromeSize.Height, maximumClientSize.Height )
			);
		}

		/// <summary>
		/// The largest client area this window can have without spilling out of the MDI workspace
		/// (or, when it has no MDI parent, off the screen).
		/// </summary>
		private Size GetMaximumClientSize()
		{
			var mdiClient = this.MdiParent?.Controls.OfType<MdiClient>().FirstOrDefault();
			var workspaceSize = mdiClient != null
				? mdiClient.ClientSize
				: Screen.FromControl( this ).WorkingArea.Size;

			// leave room for the window's own border and caption
			var borderSize = this.SizeFromClientSize( Size.Empty );
			return new Size(
				Math.Max( ImageViewerForm.MinimumClientSize.Width, workspaceSize.Width - borderSize.Width ),
				Math.Max( ImageViewerForm.MinimumClientSize.Height, workspaceSize.Height - borderSize.Height )
			);
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
