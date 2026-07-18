
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	/// <summary>
	/// Lets the user pick a texture for a sprite, room background or room object: any .dxt in the
	/// LPAK (plus any override-only names the caller passes in), filtered by a search box and shown
	/// in a preview. The "Import PNG as new texture" button brings in a brand-new texture and
	/// returns it as the selection.
	/// </summary>
	public partial class TexturePickerDialog : Form
	{
		private readonly LPAKFile lpakFile;
		private readonly List<string> allTextureNames;
		private Bitmap? previewBitmap;

		/// <summary>
		/// The resource path the user chose, valid once the dialog closes with DialogResult.OK.
		/// </summary>
		public string? SelectedResourcePath
		{
			get;
			private set;
		}

		public TexturePickerDialog( LPAKFile lpakFile, IEnumerable<string>? additionalTextureNames, string? initialSelection )
		{
			this.lpakFile = lpakFile;
			this.InitializeComponent();

			var names = new HashSet<string>( StringComparer.Ordinal );
			foreach( var pakName in lpakFile.PakFileNames )
			{
				if( pakName.FileName != null && pakName.FileName.EndsWith( ".dxt", StringComparison.OrdinalIgnoreCase ) )
				{
					names.Add( pakName.FileName );
				}
			}
			if( additionalTextureNames != null )
			{
				foreach( var name in additionalTextureNames )
				{
					if( !string.IsNullOrEmpty( name ) )
					{
						names.Add( name );
					}
				}
			}
			this.allTextureNames = names.OrderBy( n => n, StringComparer.Ordinal ).ToList();

			this.PopulateList( initialSelection );

			this.textBoxFilter.TextChanged += this.HandleFilterChanged;
			this.listBoxTextures.SelectedIndexChanged += this.HandleSelectionChanged;
			this.listBoxTextures.DoubleClick += this.HandleListDoubleClick;
			this.buttonImportNew.Click += this.HandleImportNew;
			this.FormClosing += this.HandleFormClosing;

			this.UpdatePreview();
			this.buttonOK.Enabled = this.listBoxTextures.SelectedItem is string;
		}

		private void PopulateList( string? selection )
		{
			var filter = this.textBoxFilter.Text;
			this.listBoxTextures.BeginUpdate();
			try
			{
				this.listBoxTextures.Items.Clear();
				foreach( var name in this.allTextureNames )
				{
					if( string.IsNullOrEmpty( filter ) || name.IndexOf( filter, StringComparison.OrdinalIgnoreCase ) >= 0 )
					{
						this.listBoxTextures.Items.Add( name );
					}
				}
			}
			finally
			{
				this.listBoxTextures.EndUpdate();
			}

			if( selection != null )
			{
				var index = this.listBoxTextures.Items.IndexOf( selection );
				this.listBoxTextures.SelectedIndex = index >= 0 ? index : ( this.listBoxTextures.Items.Count > 0 ? 0 : -1 );
			}
			else if( this.listBoxTextures.Items.Count > 0 )
			{
				this.listBoxTextures.SelectedIndex = 0;
			}
		}

		private void HandleFilterChanged( object? sender, EventArgs args )
		{
			// keep the current selection visible when it still matches the new filter
			var current = this.listBoxTextures.SelectedItem as string;
			this.PopulateList( current );
		}

		private void HandleSelectionChanged( object? sender, EventArgs args )
		{
			this.UpdatePreview();
			this.buttonOK.Enabled = this.listBoxTextures.SelectedItem is string;
		}

		private void HandleListDoubleClick( object? sender, EventArgs args )
		{
			if( this.listBoxTextures.SelectedItem is string )
			{
				this.DialogResult = DialogResult.OK;
			}
		}

		private void UpdatePreview()
		{
			var name = this.listBoxTextures.SelectedItem as string;

			this.pictureBoxPreview.Image = null;
			this.previewBitmap?.Dispose();
			this.previewBitmap = null;

			if( name == null )
			{
				this.labelInfo.Text = "";
				return;
			}

			try
			{
				var bytes = this.lpakFile.ReadResourceBytes( name );
				if( bytes == null || bytes.Length < 12 )
				{
					this.labelInfo.Text = "Could not load texture.";
					return;
				}
				this.previewBitmap = Helper.ImageFromDxtBytes( bytes ) as Bitmap;
				this.pictureBoxPreview.Image = this.previewBitmap;

				var fourCC = Encoding.ASCII.GetString( bytes, 0, 4 );
				var hasOverride = Formats.LPAK.Parser.GetOverrideFilePath( this.lpakFile.FileNameOnDisk, name ) != null;
				this.labelInfo.Text = string.Concat(
					fourCC, " ",
					this.previewBitmap?.Width ?? BitConverter.ToInt32( bytes, 4 ), "x",
					this.previewBitmap?.Height ?? BitConverter.ToInt32( bytes, 8 ),
					hasOverride ? " (override)" : ""
				);
			}
			catch( Exception exception )
			{
				this.labelInfo.Text = "Could not load texture: " + exception.Message;
			}
		}

		private void HandleImportNew( object? sender, EventArgs args )
		{
			using( var dialog = new NewTextureDialog( this.lpakFile ) )
			{
				if( dialog.ShowDialog( this ) != DialogResult.OK || dialog.ImportedResourcePath == null )
				{
					return;
				}
				this.SelectedResourcePath = dialog.ImportedResourcePath;
			}
			this.DialogResult = DialogResult.OK;
		}

		private void HandleFormClosing( object? sender, FormClosingEventArgs args )
		{
			// OK/double-click leave SelectedResourcePath unset; fill it from the list. The
			// import-new path has already set it to the freshly created texture.
			if( this.DialogResult == DialogResult.OK && this.SelectedResourcePath == null )
			{
				this.SelectedResourcePath = this.listBoxTextures.SelectedItem as string;
			}
		}
	}
}
