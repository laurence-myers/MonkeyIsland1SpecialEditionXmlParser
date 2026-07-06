using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Commands;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Scumm;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Scumm.Entities;

namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	public partial class SpriteSheetEditorForm : Form
	{
		private static readonly List<SpriteSheetEditorForm> instances = new List<SpriteSheetEditorForm>();
		private readonly Dictionary<string, Image?> textureCache = new Dictionary<string, Image?>();
		private ClassicData? classicData;
		private ClassicRoom? classicRoom;
		private Dictionary<int, ClassicObject>? classicObjects;
		private readonly HashSet<Sprite> hiddenSprites = new HashSet<Sprite>();
		private Sprite? selectedSprite;
		private bool suppressUiEvents;
		private bool dirty;

		public Room? Room
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

		public static SpriteSheetEditorForm[] Instances
		{
			get
			{
				return SpriteSheetEditorForm.instances.ToArray();
			}
		}

		public SpriteSheetEditorForm( int fileIndex, LPAKFile lpakFile, Form mdiParent, FormWindowState windowState )
		{
			this.FileIndex = fileIndex;
			this.LPAKFile = lpakFile;
			this.MdiParent = mdiParent;
			this.WindowState = windowState;

			SpriteSheetEditorForm.instances.Add( this );
			this.FormClosed += delegate { SpriteSheetEditorForm.instances.Remove( this ); };

			this.InitializeComponent();

			this.Load += delegate { this.LoadRoom(); };
			this.FormClosing += this.ConfirmCloseWithUnsavedChanges;

			this.atlasViewControl.SelectedSpriteChanged += this.HandleAtlasSelectionChanged;
			this.atlasViewControl.SpriteRectChanged += this.HandleAtlasSpriteRectChanged;
			this.roomPreviewControl.SelectedSpriteChanged += this.HandlePreviewSelectionChanged;
			this.roomPreviewControl.KeyDown += this.HandlePreviewKeyDown;
		}

		private void LoadRoom()
		{
			this.Room = this.LPAKFile.LoadRoom( this.FileIndex );
			if( this.Room == null )
			{
				return;
			}

			this.dirty = false;
			this.UpdateTitle();

			this.classicData = ClassicDataLocator.GetOrLoad( this.LPAKFile, this.PromptForClassicDataFolder );
			this.classicRoom = this.classicData?.FindRoom( this.Room.Header.Identifier );
			this.classicObjects = this.classicRoom?.GetObjectsById();

			if( this.classicData == null )
			{
				this.warningLabel.Text = "Classic SCUMM data (monkey1.000/001) not found - object sprites are placed by their offset only.";
				this.warningLabel.Visible = true;
			}
			else if( this.classicRoom == null )
			{
				this.warningLabel.Text = string.Concat( "No classic room ", this.Room.Header.Identifier, " in ", this.classicData.Source, " - object sprites are placed by their offset only." );
				this.warningLabel.Visible = true;
			}
			else
			{
				this.warningLabel.Visible = false;
			}

			this.roomPreviewControl.Background = Renderer.RenderBackground( this.Room, this.LoadTexture );

			this.PopulateTextureCombo();
			this.PopulateSpriteTree();
			this.PopulateDiagnostics();
			this.RefreshPlacements();
			this.UpdateNumericEditors();
		}

		//-------------------------------------------
		// population

		private void PopulateTextureCombo()
		{
			this.suppressUiEvents = true;
			try
			{
				this.comboBoxTextures.Items.Clear();
				var textureNames = this.Room!.SpriteGroupList
					.SelectMany( g => g.SpriteList )
					.Select( s => s.TextureFileName )
					.Where( n => !string.IsNullOrEmpty( n ) )
					.Distinct()
					.OrderBy( n => n )
					.ToArray();
				foreach( var textureName in textureNames )
				{
					this.comboBoxTextures.Items.Add( textureName! );
				}
			}
			finally
			{
				this.suppressUiEvents = false;
			}

			if( this.comboBoxTextures.Items.Count > 0 )
			{
				this.comboBoxTextures.SelectedIndex = 0;
			}
		}

		private void PopulateSpriteTree()
		{
			this.suppressUiEvents = true;
			try
			{
				this.treeViewSprites.BeginUpdate();
				this.treeViewSprites.Nodes.Clear();

				for( var groupIndex = 0; groupIndex < this.Room!.SpriteHeaderList.Count && groupIndex < this.Room.SpriteGroupList.Count; groupIndex++ )
				{
					var spriteHeader = this.Room.SpriteHeaderList[groupIndex];
					var spriteGroup = this.Room.SpriteGroupList[groupIndex];

					ClassicObject? classicObject = null;
					if( this.classicObjects != null )
					{
						ClassicObject found;
						if( this.classicObjects.TryGetValue( spriteHeader.Identifier, out found ) )
						{
							classicObject = found;
						}
					}

					var groupNode = new TreeNode
					{
						Text = string.Concat(
							"Group ", groupIndex, " - id=", spriteHeader.Identifier,
							classicObject?.Name is { Length: > 0 } ? " " + classicObject.Name : "",
							classicObject == null ? " [unplaced]" : ""
						),
						Tag = groupIndex,
						Checked = true,
					};

					for( var spriteIndex = 0; spriteIndex < spriteGroup.SpriteList.Count; spriteIndex++ )
					{
						var sprite = spriteGroup.SpriteList[spriteIndex];
						var spriteNode = new TreeNode
						{
							Text = DescribeSprite( spriteIndex, sprite ),
							Tag = sprite,
							Checked = true,
						};
						groupNode.Nodes.Add( spriteNode );
					}

					this.treeViewSprites.Nodes.Add( groupNode );
				}

				this.treeViewSprites.EndUpdate();
			}
			finally
			{
				this.suppressUiEvents = false;
			}
		}

		private static string DescribeSprite( int spriteIndex, Sprite sprite )
		{
			return string.Concat( "Frame ", spriteIndex, ": ", sprite.TextureWidth, "x", sprite.TextureHeight, " L", sprite.Layer );
		}

		private void PopulateDiagnostics()
		{
			this.listBoxDiagnostics.Items.Clear();

			this.listBoxDiagnostics.Items.Add(
				this.classicData == null
				? "Classic data: not found"
				: string.Concat( "Classic data: ", this.classicData.Source )
			);

			if( this.classicRoom != null )
			{
				this.listBoxDiagnostics.Items.Add( string.Concat(
					"Classic room ", this.classicRoom.RoomNumber,
					this.classicRoom.Name is { Length: > 0 } ? " '" + this.classicRoom.Name + "'" : "",
					": ", this.classicRoom.Width, "x", this.classicRoom.Height,
					", ", this.classicRoom.ObjectList.Count, " objects"
				) );
			}

			var groupCount = Math.Min( this.Room!.SpriteHeaderList.Count, this.Room.SpriteGroupList.Count );
			var unmatchedCount = 0;
			for( var groupIndex = 0; groupIndex < groupCount; groupIndex++ )
			{
				var identifier = this.Room.SpriteHeaderList[groupIndex].Identifier;
				if( this.classicObjects == null || !this.classicObjects.ContainsKey( identifier ) )
				{
					unmatchedCount++;
					this.listBoxDiagnostics.Items.Add( string.Concat( "Unmatched group ", groupIndex, ": id=", identifier, " (offset-only placement)" ) );
				}
			}
			this.listBoxDiagnostics.Items.Add( string.Concat( groupCount - unmatchedCount, "/", groupCount, " sprite groups matched to classic objects" ) );

			// Unknown4 entities may hold additional placement data; surface them for verification
			for( var index = 0; index < this.Room.Unknown4GroupList.Count; index++ )
			{
				var unknown4Group = this.Room.Unknown4GroupList[index];
				foreach( var unknown4 in unknown4Group.Unknown4List )
				{
					this.listBoxDiagnostics.Items.Add( string.Concat( "Unknown4[", index, "/", unknown4.Index, "]: ", unknown4.Unkn3, "; ", unknown4.Unkn4 ) );
				}
			}
		}

		//-------------------------------------------
		// placement refresh

		private void RefreshPlacements()
		{
			if( this.Room == null )
			{
				return;
			}

			var placements = Renderer.ResolvePlacements( this.Room, this.classicObjects, this.GetHdScale() );

			this.roomPreviewControl.Sprites.Clear();
			foreach( var placement in placements )
			{
				var previewSprite = new RoomPreviewControlSprite( placement, this.LoadTexture( placement.Sprite.TextureFileName ) )
				{
					Visible = !this.hiddenSprites.Contains( placement.Sprite ),
				};
				this.roomPreviewControl.Sprites.Add( previewSprite );
			}

			// keep the current selection pointing at the same sprite entity
			this.suppressUiEvents = true;
			try
			{
				this.roomPreviewControl.SelectedSprite = this.selectedSprite == null
					? null
					: this.roomPreviewControl.Sprites.FirstOrDefault( s => s.Placement.Sprite == this.selectedSprite );
			}
			finally
			{
				this.suppressUiEvents = false;
			}

			this.roomPreviewControl.RefreshContent();
		}

		private SizeF GetHdScale()
		{
			return new SizeF( (float)this.numericScaleX.Value, (float)this.numericScaleY.Value );
		}

		//-------------------------------------------
		// selection synchronization

		private void SetSelectedSprite( Sprite? sprite )
		{
			if( this.selectedSprite == sprite )
			{
				return;
			}
			this.selectedSprite = sprite;

			this.suppressUiEvents = true;
			try
			{
				// atlas: switch to the sprite's texture and select it
				if( sprite != null && sprite.TextureFileName != null && (string?)this.comboBoxTextures.SelectedItem != sprite.TextureFileName )
				{
					var index = this.comboBoxTextures.Items.IndexOf( sprite.TextureFileName );
					if( index >= 0 )
					{
						this.comboBoxTextures.SelectedIndex = index;
						this.UpdateAtlas();
					}
				}

				// never select a sprite against a texture it doesn't belong to
				this.atlasViewControl.SelectedSprite
					= sprite != null && this.atlasViewControl.Sprites.Contains( sprite )
					? sprite
					: null;

				// preview
				this.roomPreviewControl.SelectedSprite = sprite == null
					? null
					: this.roomPreviewControl.Sprites.FirstOrDefault( s => s.Placement.Sprite == sprite );

				// tree
				var spriteNode = sprite == null ? null : this.FindSpriteNode( sprite );
				if( spriteNode != null && this.treeViewSprites.SelectedNode != spriteNode )
				{
					this.treeViewSprites.SelectedNode = spriteNode;
					spriteNode.EnsureVisible();
				}
			}
			finally
			{
				this.suppressUiEvents = false;
			}

			this.UpdateNumericEditors();
		}

		private TreeNode? FindSpriteNode( Sprite sprite )
		{
			foreach( TreeNode groupNode in this.treeViewSprites.Nodes )
			{
				foreach( TreeNode spriteNode in groupNode.Nodes )
				{
					if( spriteNode.Tag == sprite )
					{
						return spriteNode;
					}
				}
			}
			return null;
		}

		private void HandleTreeAfterSelect( object sender, TreeViewEventArgs args )
		{
			if( this.suppressUiEvents )
			{
				return;
			}
			var sprite = args.Node?.Tag as Sprite;
			if( sprite != null )
			{
				this.SetSelectedSprite( sprite );
			}
		}

		private void HandleTreeAfterCheck( object sender, TreeViewEventArgs args )
		{
			if( this.suppressUiEvents || args.Node == null )
			{
				return;
			}

			this.suppressUiEvents = true;
			try
			{
				// checking a group node toggles all of its frames
				if( args.Node.Tag is int )
				{
					foreach( TreeNode spriteNode in args.Node.Nodes )
					{
						spriteNode.Checked = args.Node.Checked;
					}
				}
			}
			finally
			{
				this.suppressUiEvents = false;
			}

			this.SyncVisibilityFromTree();
		}

		private void SyncVisibilityFromTree()
		{
			this.hiddenSprites.Clear();
			foreach( TreeNode groupNode in this.treeViewSprites.Nodes )
			{
				foreach( TreeNode spriteNode in groupNode.Nodes )
				{
					var sprite = spriteNode.Tag as Sprite;
					if( sprite != null && !spriteNode.Checked )
					{
						this.hiddenSprites.Add( sprite );
					}
				}
			}

			foreach( var previewSprite in this.roomPreviewControl.Sprites )
			{
				previewSprite.Visible = !this.hiddenSprites.Contains( previewSprite.Placement.Sprite );
			}
			this.roomPreviewControl.Invalidate();
		}

		private void HandleAtlasSelectionChanged( object? sender, EventArgs args )
		{
			if( this.suppressUiEvents )
			{
				return;
			}
			this.SetSelectedSprite( this.atlasViewControl.SelectedSprite );
		}

		private void HandlePreviewSelectionChanged( object? sender, EventArgs args )
		{
			if( this.suppressUiEvents )
			{
				return;
			}
			this.SetSelectedSprite( this.roomPreviewControl.SelectedSprite?.Placement.Sprite );
		}

		//-------------------------------------------
		// editing

		private void HandleAtlasSpriteRectChanged( object? sender, EventArgs args )
		{
			this.MarkDirty();
			this.UpdateNumericEditors();
			this.UpdateSelectedSpriteNodeText();
			this.RefreshPlacements();
		}

		private void HandlePreviewKeyDown( object? sender, KeyEventArgs args )
		{
			if( this.selectedSprite == null )
			{
				return;
			}

			var step = args.Shift ? 10.0f : 1.0f;
			var deltaX = 0.0f;
			var deltaY = 0.0f;
			switch( args.KeyCode )
			{
				case Keys.Left:
					deltaX = -step;
					break;
				case Keys.Right:
					deltaX = step;
					break;
				case Keys.Up:
					deltaY = -step;
					break;
				case Keys.Down:
					deltaY = step;
					break;
				default:
					return;
			}

			args.Handled = true;
			this.selectedSprite.OffsetX += deltaX;
			this.selectedSprite.OffsetY += deltaY;
			this.MarkDirty();
			this.UpdateNumericEditors();
			this.RefreshPlacements();
		}

		private void UpdateNumericEditors()
		{
			this.suppressUiEvents = true;
			try
			{
				var sprite = this.selectedSprite;
				var enabled = sprite != null;
				this.numericTextureX.Enabled = enabled;
				this.numericTextureY.Enabled = enabled;
				this.numericTextureWidth.Enabled = enabled;
				this.numericTextureHeight.Enabled = enabled;
				this.numericOffsetX.Enabled = enabled;
				this.numericOffsetY.Enabled = enabled;
				this.numericLayer.Enabled = enabled;

				if( sprite != null )
				{
					this.numericTextureX.Value = Clamp( sprite.TextureX, this.numericTextureX );
					this.numericTextureY.Value = Clamp( sprite.TextureY, this.numericTextureY );
					this.numericTextureWidth.Value = Clamp( sprite.TextureWidth, this.numericTextureWidth );
					this.numericTextureHeight.Value = Clamp( sprite.TextureHeight, this.numericTextureHeight );
					this.numericOffsetX.Value = Clamp( (decimal)sprite.OffsetX, this.numericOffsetX );
					this.numericOffsetY.Value = Clamp( (decimal)sprite.OffsetY, this.numericOffsetY );
					this.numericLayer.Value = Clamp( sprite.Layer, this.numericLayer );
				}
			}
			finally
			{
				this.suppressUiEvents = false;
			}
		}

		private static decimal Clamp( decimal value, NumericUpDown numeric )
		{
			return Math.Max( numeric.Minimum, Math.Min( numeric.Maximum, value ) );
		}

		private void HandleNumericValueChanged( object sender, EventArgs args )
		{
			if( this.suppressUiEvents || this.selectedSprite == null )
			{
				return;
			}

			// only write the edited field back; writing all of them would silently apply
			// the display clamp of an out-of-range field the user never touched
			if( sender == this.numericTextureX )
			{
				this.selectedSprite.TextureX = (int)this.numericTextureX.Value;
			}
			else if( sender == this.numericTextureY )
			{
				this.selectedSprite.TextureY = (int)this.numericTextureY.Value;
			}
			else if( sender == this.numericTextureWidth )
			{
				this.selectedSprite.TextureWidth = (int)this.numericTextureWidth.Value;
			}
			else if( sender == this.numericTextureHeight )
			{
				this.selectedSprite.TextureHeight = (int)this.numericTextureHeight.Value;
			}
			else if( sender == this.numericOffsetX )
			{
				this.selectedSprite.OffsetX = (float)this.numericOffsetX.Value;
			}
			else if( sender == this.numericOffsetY )
			{
				this.selectedSprite.OffsetY = (float)this.numericOffsetY.Value;
			}
			else if( sender == this.numericLayer )
			{
				this.selectedSprite.Layer = (int)this.numericLayer.Value;
			}

			this.MarkDirty();
			this.UpdateSelectedSpriteNodeText();
			this.atlasViewControl.Invalidate();
			this.RefreshPlacements();
		}

		private void UpdateSelectedSpriteNodeText()
		{
			if( this.selectedSprite == null )
			{
				return;
			}
			var spriteNode = this.FindSpriteNode( this.selectedSprite );
			if( spriteNode != null )
			{
				spriteNode.Text = DescribeSprite( spriteNode.Index, this.selectedSprite );
			}
		}

		private void HandleScaleChanged( object sender, EventArgs args )
		{
			if( this.suppressUiEvents )
			{
				return;
			}
			this.roomPreviewControl.HdScale = this.GetHdScale();
			this.RefreshPlacements();
		}

		private void HandleCalibrationCheckedChanged( object sender, EventArgs args )
		{
			this.roomPreviewControl.HdScale = this.GetHdScale();
			this.roomPreviewControl.ShowCalibrationOverlay = this.checkBoxCalibration.Checked;
			this.roomPreviewControl.Invalidate();
		}

		private void HandleTextureSelected( object sender, EventArgs args )
		{
			if( this.suppressUiEvents )
			{
				return;
			}
			this.UpdateAtlas();
		}

		private void UpdateAtlas()
		{
			var textureName = this.comboBoxTextures.SelectedItem as string;
			this.atlasViewControl.Texture = this.LoadTexture( textureName );
			this.atlasViewControl.Sprites.Clear();
			this.atlasViewControl.Sprites.AddRange(
				this.Room!.SpriteGroupList
					.SelectMany( g => g.SpriteList )
					.Where( s => s.TextureFileName == textureName )
			);
			if( this.atlasViewControl.SelectedSprite != null && !this.atlasViewControl.Sprites.Contains( this.atlasViewControl.SelectedSprite ) )
			{
				this.atlasViewControl.SelectedSprite = null;
			}
			this.atlasViewControl.RefreshContent();
		}

		//-------------------------------------------
		// saving

		private void SaveOverride( object sender, EventArgs args )
		{
			this.TrySaveOverride();
		}

		private bool TrySaveOverride()
		{
			var resourcePath = this.LPAKFile.PakFileNames[this.FileIndex].FileName;
			var result = new SaveRoomOverrideCommand( this.LPAKFile, resourcePath, this.Room ).Execute();
			if( result.IsSuccess )
			{
				this.dirty = false;
				this.UpdateTitle();
				return true;
			}

			MessageBox.Show( this, result.Error, "Save override", MessageBoxButtons.OK, MessageBoxIcon.Error );
			return false;
		}

		private void RevertChanges( object sender, EventArgs args )
		{
			if( this.dirty )
			{
				var answer = MessageBox.Show(
					this,
					"Discard all unsaved changes and reload the room?",
					"Revert changes",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question
				);
				if( answer != DialogResult.Yes )
				{
					return;
				}
			}

			this.selectedSprite = null;
			this.hiddenSprites.Clear();
			this.LoadRoom();
		}

		private void ConfirmCloseWithUnsavedChanges( object? sender, FormClosingEventArgs args )
		{
			if( !this.dirty )
			{
				return;
			}

			var answer = MessageBox.Show(
				this,
				"Save the changed sprite data as an override before closing?",
				"Unsaved changes",
				MessageBoxButtons.YesNoCancel,
				MessageBoxIcon.Question
			);
			if( answer == DialogResult.Cancel )
			{
				args.Cancel = true;
			}
			else if( answer == DialogResult.Yes && !this.TrySaveOverride() )
			{
				args.Cancel = true;
			}
		}

		//-------------------------------------------
		// texture export / import

		private void ExportTexturePng( object sender, EventArgs args )
		{
			var textureName = this.comboBoxTextures.SelectedItem as string;
			if( textureName == null )
			{
				return;
			}

			var bitmap = this.LoadTexture( textureName ) as Bitmap;
			if( bitmap == null )
			{
				MessageBox.Show( this, "The texture could not be loaded.", "Export texture", MessageBoxButtons.OK, MessageBoxIcon.Error );
				return;
			}

			using( var dialog = new SaveFileDialog() )
			{
				dialog.Filter = "PNG files (*.png)|*.png|All files (*.*)|*.*";
				dialog.Title = "Export texture as PNG";
				dialog.FileName = System.IO.Path.GetFileNameWithoutExtension( textureName.Replace( '/', '_' ) ) + ".png";
				if( dialog.ShowDialog( this ) != DialogResult.OK )
				{
					return;
				}
				new ExportToPngCommand( bitmap, dialog.FileName ).Execute();
			}
		}

		private void ImportTexturePng( object sender, EventArgs args )
		{
			var textureName = this.comboBoxTextures.SelectedItem as string;
			if( textureName == null )
			{
				return;
			}

			using( var dialog = new OpenFileDialog() )
			{
				dialog.Filter = "PNG files (*.png)|*.png|All files (*.*)|*.*";
				dialog.Title = string.Concat( "Import PNG as ", textureName );
				if( dialog.ShowDialog( this ) != DialogResult.OK )
				{
					return;
				}

				var result = new ImportTexturePngCommand( this.LPAKFile, textureName, dialog.FileName ).Execute();
				if( !result.IsSuccess )
				{
					MessageBox.Show( this, result.Error, "Import texture", MessageBoxButtons.OK, MessageBoxIcon.Error );
					return;
				}
			}

			// reload everything that may show the replaced texture
			this.textureCache.Clear();
			this.roomPreviewControl.Background = Renderer.RenderBackground( this.Room!, this.LoadTexture );
			this.UpdateAtlas();
			this.RefreshPlacements();
		}

		//-------------------------------------------
		// helpers

		private void MarkDirty()
		{
			if( !this.dirty )
			{
				this.dirty = true;
				this.UpdateTitle();
			}
		}

		private void UpdateTitle()
		{
			if( this.Room == null )
			{
				return;
			}
			this.label1.Text = string.Concat(
				"Spritesheet Editor - Room ", this.Room.Header.Identifier, " - ", this.Room.Header.Name,
				this.dirty ? " (modified)" : ""
			);
		}

		private string? PromptForClassicDataFolder()
		{
			using( var dialog = new FolderBrowserDialog() )
			{
				dialog.Description = "Classic SCUMM data (monkey1.000 / monkey1.001) was not found automatically. Select the folder containing it (e.g. the game's 'classic' folder).";
				return dialog.ShowDialog( this ) == DialogResult.OK ? dialog.SelectedPath : null;
			}
		}

		private Image? LoadTexture( string? fileName )
		{
			if( string.IsNullOrEmpty( fileName ) )
			{
				return null;
			}

			Image? image;
			if( this.textureCache.TryGetValue( fileName!, out image ) )
			{
				return image;
			}

			try
			{
				image = this.LPAKFile.LoadImage( fileName );
			}
			catch( Exception )
			{
				image = null;
			}
			this.textureCache[fileName!] = image;
			return image;
		}
	}
}
