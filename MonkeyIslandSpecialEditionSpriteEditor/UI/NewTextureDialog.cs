
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.Commands;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	/// <summary>
	/// Imports a PNG as a brand-new game texture: the user picks the PNG, the resource path it
	/// will live at, and the DXT format. On success the new .dxt is written next to the pak and
	/// its resource path is returned so the caller can point a sprite at it.
	/// </summary>
	public partial class NewTextureDialog : Form
	{
		private readonly LPAKFile lpakFile;
		private string lastPrefilledPath = "";

		/// <summary>
		/// The resource path of the imported texture, valid once the dialog closes with OK.
		/// </summary>
		public string? ImportedResourcePath
		{
			get;
			private set;
		}

		public NewTextureDialog( LPAKFile lpakFile )
		{
			this.lpakFile = lpakFile;
			this.InitializeComponent();

			this.comboBoxFormat.Items.AddRange( new object[] { "Auto", "DXT1", "DXT5" } );
			this.comboBoxFormat.SelectedIndex = 0;

			this.buttonBrowse.Click += this.HandleBrowse;
			this.textBoxPngFile.TextChanged += this.HandlePngFileChanged;
			this.buttonImport.Click += this.HandleImport;
		}

		private void HandleBrowse( object? sender, EventArgs args )
		{
			using( var dialog = new OpenFileDialog() )
			{
				dialog.Filter = "PNG files (*.png)|*.png|All files (*.*)|*.*";
				dialog.Title = "Choose a PNG to import as a new texture";
				if( dialog.ShowDialog( this ) != DialogResult.OK )
				{
					return;
				}

				this.textBoxPngFile.Text = dialog.FileName;

				// prefill the resource path from the PNG name, unless the user has typed their own
				if( string.IsNullOrWhiteSpace( this.textBoxResourcePath.Text ) || this.textBoxResourcePath.Text == this.lastPrefilledPath )
				{
					var baseName = Path.GetFileNameWithoutExtension( dialog.FileName );
					this.lastPrefilledPath = "art/custom/" + baseName + ".dxt";
					this.textBoxResourcePath.Text = this.lastPrefilledPath;
				}
			}
		}

		private void HandlePngFileChanged( object? sender, EventArgs args )
		{
			var path = this.textBoxPngFile.Text.Trim();
			if( string.IsNullOrEmpty( path ) || !File.Exists( path ) )
			{
				this.labelPngInfo.Text = "";
				return;
			}

			try
			{
				// read through a stream so the file is not left locked while the dialog is open
				using( var stream = File.OpenRead( path ) )
				using( var image = Image.FromStream( stream ) )
				using( var bitmap = new Bitmap( image ) )
				{
					var detected = Helper.DetectDxtFourCC( bitmap );
					var aligned = bitmap.Width % 4 == 0 && bitmap.Height % 4 == 0;
					this.labelPngInfo.Text = string.Concat(
						bitmap.Width, "x", bitmap.Height, ", detected ", detected,
						aligned ? "" : " - dimensions must be multiples of 4"
					);
					this.labelPngInfo.ForeColor = aligned ? SystemColors.GrayText : Color.Firebrick;
				}
			}
			catch( Exception )
			{
				this.labelPngInfo.Text = "Not a readable image.";
				this.labelPngInfo.ForeColor = Color.Firebrick;
			}
		}

		private void HandleImport( object? sender, EventArgs args )
		{
			var resourcePath = this.textBoxResourcePath.Text.Trim();
			var pngFileName = this.textBoxPngFile.Text.Trim();
			var fourCC = this.comboBoxFormat.SelectedItem as string;

			var overwriteExisting = false;
			var existingOverride = Formats.LPAK.Parser.GetOverrideFilePath( this.lpakFile.FileNameOnDisk, resourcePath );
			if( existingOverride != null )
			{
				var answer = MessageBox.Show(
					this,
					"A loose texture file already exists at this path. Overwrite it?",
					"Import new texture",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Warning
				);
				if( answer != DialogResult.Yes )
				{
					return;
				}
				overwriteExisting = true;
			}

			var result = new ImportNewTexturePngCommand( this.lpakFile, resourcePath, pngFileName, fourCC, overwriteExisting ).Execute();
			if( !result.IsSuccess )
			{
				MessageBox.Show( this, result.Error, "Import new texture", MessageBoxButtons.OK, MessageBoxIcon.Error );
				return;
			}

			this.ImportedResourcePath = resourcePath;
			this.DialogResult = DialogResult.OK;
		}
	}
}
