using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.Commands;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Entities;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	/// <summary>
	/// Edits the sprites of a costume: texture rectangles on the spritesheet, and each
	/// sprite's position relative to the actor origin (ScreenX/ScreenY - the values whose
	/// mis-calibration makes costume sprites appear a few pixels off in game). The classic
	/// SCUMM costume provides a per-cel reference rectangle for calibration.
	/// </summary>
	public partial class CostumeSpriteSheetEditorForm : Form
	{
		private static readonly List<CostumeSpriteSheetEditorForm> instances = new List<CostumeSpriteSheetEditorForm>();
		private readonly Dictionary<string, Image?> textureCache = new Dictionary<string, Image?>();
		private ClassicData? classicData;
		private ClassicCostume? classicCostume;
		private readonly HashSet<SpriteGroup> hiddenGroups = new HashSet<SpriteGroup>();
		private readonly HashSet<Sprite> hiddenSprites = new HashSet<Sprite>();
		private Sprite? selectedSprite;
		private bool suppressUiEvents;
		private bool dirty;

		public Costume? Costume
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

		public static CostumeSpriteSheetEditorForm[] Instances
		{
			get
			{
				return CostumeSpriteSheetEditorForm.instances.ToArray();
			}
		}

		public CostumeSpriteSheetEditorForm( int fileIndex, LPAKFile lpakFile, Form mdiParent, FormWindowState windowState )
		{
			this.FileIndex = fileIndex;
			this.LPAKFile = lpakFile;
			this.MdiParent = mdiParent;
			this.WindowState = windowState;

			CostumeSpriteSheetEditorForm.instances.Add( this );
			this.FormClosed += delegate { CostumeSpriteSheetEditorForm.instances.Remove( this ); };

			this.InitializeComponent();

			this.Load += delegate { this.LoadCostume(); };
			this.FormClosing += this.ConfirmCloseWithUnsavedChanges;

			this.atlasViewControl.SelectedSpriteChanged += this.HandleAtlasSelectionChanged;
			this.atlasViewControl.SpriteRectChanged += this.HandleAtlasSpriteRectChanged;
			this.costumePreviewControl.SelectedSpriteChanged += this.HandlePreviewSelectionChanged;
			this.costumePreviewControl.KeyDown += this.HandlePreviewKeyDown;
			this.timerPlayback.Tick += this.HandlePlaybackTick;
		}

		private void LoadCostume()
		{
			this.Costume = this.LPAKFile.LoadCostume( this.FileIndex );
			if( this.Costume == null )
			{
				return;
			}

			this.dirty = false;
			this.UpdateTitle();

			this.classicData = ClassicDataLocator.GetOrLoad( this.LPAKFile, this.PromptForClassicDataFolder );
			this.classicCostume = this.classicData?.FindCostume( this.Costume.Header.Identifier );

			if( this.classicData == null )
			{
				this.warningLabel.Text = "Classic SCUMM data (monkey1.000/001) not found - no classic reference rectangles available.";
				this.warningLabel.Visible = true;
			}
			else if( this.classicCostume == null )
			{
				this.warningLabel.Text = string.Concat( "No classic costume ", this.Costume.Header.Identifier, " in ", this.classicData.Source, " - no classic reference rectangles available." );
				this.warningLabel.Visible = true;
			}
			else
			{
				this.warningLabel.Visible = false;
			}

			this.PopulateAnimationCombo();
			this.PopulateTextureCombo();
			this.PopulateSpriteTree();
			this.PopulateDiagnostics();
			this.UpdateStepRange();
			this.RefreshPlacements();
			this.UpdateNumericEditors();
		}

		//-------------------------------------------
		// population

		private void PopulateAnimationCombo()
		{
			this.suppressUiEvents = true;
			try
			{
				this.comboBoxAnimations.Items.Clear();
				foreach( var animation in this.Costume!.AnimationList )
				{
					this.comboBoxAnimations.Items.Add( animation.Name );
				}
			}
			finally
			{
				this.suppressUiEvents = false;
			}

			if( this.comboBoxAnimations.Items.Count > 0 )
			{
				this.comboBoxAnimations.SelectedIndex = 0;
			}
		}

		private void PopulateTextureCombo()
		{
			this.suppressUiEvents = true;
			try
			{
				this.comboBoxTextures.Items.Clear();
				foreach( var textureFileName in this.Costume!.TextureFileNameList )
				{
					this.comboBoxTextures.Items.Add( textureFileName.Path );
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

				foreach( var spriteGroup in this.Costume!.SpriteGroupList )
				{
					if( spriteGroup.SpriteList.Count == 0 )
					{
						continue;
					}

					var classicLimb = this.classicCostume?.FindLimb( spriteGroup.Identifier );
					var groupNode = new TreeNode
					{
						Text = string.Concat(
							"Group ", spriteGroup.Index, " - limb ", spriteGroup.Identifier,
							" (", spriteGroup.SpriteList.Count, " sprites",
							classicLimb != null ? ", " + classicLimb.CelList.Count + " classic cels" : "",
							")"
						),
						Tag = spriteGroup,
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
			return string.Concat(
				"Sprite ", spriteIndex, ": ", sprite.TextureWidth, "x", sprite.TextureHeight,
				" @ ", sprite.ScreenX.ToString( "0.##" ), "; ", sprite.ScreenY.ToString( "0.##" )
			);
		}

		private void PopulateDiagnostics()
		{
			this.listBoxDiagnostics.Items.Clear();

			this.listBoxDiagnostics.Items.Add(
				this.classicData == null
				? "Classic data: not found"
				: string.Concat( "Classic data: ", this.classicData.Source )
			);

			if( this.classicCostume != null )
			{
				this.listBoxDiagnostics.Items.Add( string.Concat(
					"Classic costume ", this.classicCostume.CostumeId,
					" (room ", this.classicCostume.RoomNumber, "): format 0x", this.classicCostume.Format.ToString( "X2" ),
					this.classicCostume.Mirror ? " mirrored" : "",
					", ", this.classicCostume.LimbList.Count, " limbs"
				) );

				foreach( var spriteGroup in this.Costume!.SpriteGroupList )
				{
					if( spriteGroup.SpriteList.Count == 0 )
					{
						continue;
					}
					var classicLimb = this.classicCostume.FindLimb( spriteGroup.Identifier );
					if( classicLimb == null )
					{
						this.listBoxDiagnostics.Items.Add( string.Concat( "Group ", spriteGroup.Index, " (limb ", spriteGroup.Identifier, "): no classic limb" ) );
					}
					else if( classicLimb.CelList.Count != spriteGroup.SpriteList.Count )
					{
						this.listBoxDiagnostics.Items.Add( string.Concat(
							"Group ", spriteGroup.Index, " (limb ", spriteGroup.Identifier, "): ",
							spriteGroup.SpriteList.Count, " sprites vs ", classicLimb.CelList.Count, " classic cels"
						) );
					}
				}
			}

			if( this.Costume!.PathPointList is { Count: > 0 } )
			{
				this.listBoxDiagnostics.Items.Add( string.Concat( "Path points: ", this.Costume.PathPointList.Count, " (", this.Costume.Header.PathPointTypeCount, " types)" ) );
			}

			var soundNames = this.Costume.AnimationList
				.SelectMany( a => a.AnimationFrameList )
				.SelectMany( t => t.FrameList ?? new List<Frame>() )
				.Where( f => !string.IsNullOrEmpty( f.SoundName ) )
				.Select( f => f.SoundName! )
				.Distinct()
				.ToArray();
			if( soundNames.Length > 0 )
			{
				this.listBoxDiagnostics.Items.Add( string.Concat( "Sounds: ", string.Join( ", ", soundNames ) ) );
			}
		}

		//-------------------------------------------
		// placement refresh

		private Animation? SelectedAnimation
		{
			get
			{
				var index = this.comboBoxAnimations.SelectedIndex;
				return index >= 0 && index < ( this.Costume?.AnimationList.Count ?? 0 )
					? this.Costume!.AnimationList[index]
					: null;
			}
		}

		private void RefreshPlacements()
		{
			if( this.Costume == null )
			{
				return;
			}

			var animation = this.SelectedAnimation;

			// the frame numeric is 1-based for display; the renderer counts from 0
			var step = Math.Max( 0, (int)this.numericStep.Value - 1 );
			var placements = animation == null
				? new List<CostumeSpritePlacement>()
				: Renderer.ResolveFramePlacements( this.Costume, animation, step, this.classicCostume );

			this.costumePreviewControl.Sprites.Clear();
			foreach( var placement in placements )
			{
				var textureFileName = this.GetTextureFileName( placement.Sprite.TextureNumber );
				var previewSprite = new CostumePreviewControlSprite( placement, this.LoadTexture( textureFileName ) )
				{
					Visible = this.IsPlacementVisible( placement ),
				};
				this.costumePreviewControl.Sprites.Add( previewSprite );
			}

			// keep the current selection pointing at the same sprite entity
			this.suppressUiEvents = true;
			try
			{
				this.costumePreviewControl.SelectedSprite = this.selectedSprite == null
					? null
					: this.costumePreviewControl.Sprites.FirstOrDefault( s => s.Placement.Sprite == this.selectedSprite );
			}
			finally
			{
				this.suppressUiEvents = false;
			}

			this.costumePreviewControl.RefreshContent();
		}

		private void UpdateStepRange()
		{
			var animation = this.SelectedAnimation;
			var stepCount = animation == null ? 0 : Renderer.GetStepCount( animation );
			this.suppressUiEvents = true;
			try
			{
				// frames are shown 1-based: "1 of 5" to "5 of 5"
				this.numericStep.Minimum = stepCount > 0 ? 1 : 0;
				this.numericStep.Maximum = Math.Max( stepCount > 0 ? 1 : 0, stepCount );
				if( this.numericStep.Value < this.numericStep.Minimum || this.numericStep.Value > this.numericStep.Maximum )
				{
					this.numericStep.Value = this.numericStep.Minimum;
				}
				this.labelStepCount.Text = string.Concat( "of ", stepCount );
			}
			finally
			{
				this.suppressUiEvents = false;
			}

			this.UpdateAnimationBounds();
		}

		/// <summary>
		/// Fixes the preview canvas to the union of every step of the selected animation,
		/// so the actor origin stays stationary while stepping through frames.
		/// </summary>
		private void UpdateAnimationBounds()
		{
			var animation = this.SelectedAnimation;
			if( this.Costume == null || animation == null )
			{
				this.costumePreviewControl.FixedBounds = null;
				return;
			}

			RectangleF? bounds = null;
			var stepCount = Renderer.GetStepCount( animation );
			for( var step = 0; step < stepCount; step++ )
			{
				foreach( var placement in Renderer.ResolveFramePlacements( this.Costume, animation, step, this.classicCostume ) )
				{
					bounds = bounds == null ? placement.ScreenRect : RectangleF.Union( bounds.Value, placement.ScreenRect );
					if( placement.ClassicCel != null )
					{
						bounds = RectangleF.Union( bounds.Value, Renderer.GetClassicScreenRect( placement.ClassicCel, placement.Flipped, Renderer.DefaultHdScale ) );
					}
				}
			}
			this.costumePreviewControl.FixedBounds = bounds;
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
				var textureFileName = sprite == null ? null : this.GetTextureFileName( sprite.TextureNumber );
				if( textureFileName != null && (string?)this.comboBoxTextures.SelectedItem != textureFileName )
				{
					var index = this.comboBoxTextures.Items.IndexOf( textureFileName );
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
				this.costumePreviewControl.SelectedSprite = sprite == null
					? null
					: this.costumePreviewControl.Sprites.FirstOrDefault( s => s.Placement.Sprite == sprite );

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
				// checking a group node toggles all of its sprites
				if( args.Node.Tag is SpriteGroup )
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
			this.hiddenGroups.Clear();
			this.hiddenSprites.Clear();
			foreach( TreeNode groupNode in this.treeViewSprites.Nodes )
			{
				var spriteGroup = groupNode.Tag as SpriteGroup;
				if( spriteGroup != null && !groupNode.Checked )
				{
					this.hiddenGroups.Add( spriteGroup );
				}
				foreach( TreeNode spriteNode in groupNode.Nodes )
				{
					var sprite = spriteNode.Tag as Sprite;
					if( sprite != null && !spriteNode.Checked )
					{
						this.hiddenSprites.Add( sprite );
					}
				}
			}

			foreach( var previewSprite in this.costumePreviewControl.Sprites )
			{
				previewSprite.Visible = this.IsPlacementVisible( previewSprite.Placement );
			}
			this.costumePreviewControl.Invalidate();
		}

		private bool IsPlacementVisible( CostumeSpritePlacement placement )
		{
			return !this.hiddenGroups.Contains( placement.SpriteGroup )
				&& !this.hiddenSprites.Contains( placement.Sprite );
		}

		private void HandleAtlasSelectionChanged( object? sender, EventArgs args )
		{
			if( this.suppressUiEvents )
			{
				return;
			}
			this.SetSelectedSprite( this.atlasViewControl.SelectedSprite as Sprite );
		}

		private void HandlePreviewSelectionChanged( object? sender, EventArgs args )
		{
			if( this.suppressUiEvents )
			{
				return;
			}
			this.SetSelectedSprite( this.costumePreviewControl.SelectedSprite?.Placement.Sprite );
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

			// a left facing preview shows the sprite mirrored; flip the horizontal nudge so
			// the sprite follows the arrow key on screen
			var previewSprite = this.costumePreviewControl.SelectedSprite;
			if( previewSprite != null && previewSprite.Placement.Flipped )
			{
				deltaX = -deltaX;
			}

			args.Handled = true;
			this.selectedSprite.ScreenX += deltaX;
			this.selectedSprite.ScreenY += deltaY;
			this.MarkDirty();
			this.UpdateNumericEditors();
			this.UpdateSelectedSpriteNodeText();
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
				this.numericScreenX.Enabled = enabled;
				this.numericScreenY.Enabled = enabled;
				this.numericMoveX.Enabled = enabled;
				this.numericMoveY.Enabled = enabled;
				this.buttonAlignToClassic.Enabled = enabled && this.FindClassicCel( sprite ) != null;

				if( sprite != null )
				{
					this.numericTextureX.Value = Clamp( sprite.TextureX, this.numericTextureX );
					this.numericTextureY.Value = Clamp( sprite.TextureY, this.numericTextureY );
					this.numericTextureWidth.Value = Clamp( sprite.TextureWidth, this.numericTextureWidth );
					this.numericTextureHeight.Value = Clamp( sprite.TextureHeight, this.numericTextureHeight );
					this.numericScreenX.Value = Clamp( (decimal)sprite.ScreenX, this.numericScreenX );
					this.numericScreenY.Value = Clamp( (decimal)sprite.ScreenY, this.numericScreenY );
					this.numericMoveX.Value = Clamp( (decimal)sprite.MoveX, this.numericMoveX );
					this.numericMoveY.Value = Clamp( (decimal)sprite.MoveY, this.numericMoveY );
				}

				this.UpdateClassicDeltaLabel();
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
			else if( sender == this.numericScreenX )
			{
				this.selectedSprite.ScreenX = (float)this.numericScreenX.Value;
			}
			else if( sender == this.numericScreenY )
			{
				this.selectedSprite.ScreenY = (float)this.numericScreenY.Value;
			}
			else if( sender == this.numericMoveX )
			{
				this.selectedSprite.MoveX = (float)this.numericMoveX.Value;
			}
			else if( sender == this.numericMoveY )
			{
				this.selectedSprite.MoveY = (float)this.numericMoveY.Value;
			}

			this.MarkDirty();
			this.UpdateSelectedSpriteNodeText();
			this.UpdateClassicDeltaLabel();
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

		//-------------------------------------------
		// classic alignment

		/// <summary>
		/// Finds the classic cel matching a sprite by its index within its group.
		/// </summary>
		private ClassicCel? FindClassicCel( Sprite? sprite )
		{
			if( sprite == null || this.classicCostume == null )
			{
				return null;
			}

			foreach( var spriteGroup in this.Costume!.SpriteGroupList )
			{
				var spriteIndex = spriteGroup.SpriteList.IndexOf( sprite );
				if( spriteIndex < 0 )
				{
					continue;
				}
				var classicLimb = this.classicCostume.FindLimb( spriteGroup.Identifier );
				return classicLimb != null && spriteIndex < classicLimb.CelList.Count
					? classicLimb.CelList[spriteIndex]
					: null;
			}
			return null;
		}

		private void UpdateClassicDeltaLabel()
		{
			var cel = this.FindClassicCel( this.selectedSprite );
			if( cel == null || this.selectedSprite == null )
			{
				this.labelClassicDelta.Text = "Classic: n/a";
				return;
			}

			var scale = Renderer.DefaultHdScale;
			var deltaX = this.selectedSprite.ScreenX - cel.RelX * scale.Width;
			var deltaY = this.selectedSprite.ScreenY - cel.RelY * scale.Height;
			this.labelClassicDelta.Text = string.Concat(
				"Classic: ", cel.Width, "x", cel.Height, " @ ", cel.RelX, "; ", cel.RelY,
				"  delta ", deltaX.ToString( "+0.##;-0.##;0" ), "; ", deltaY.ToString( "+0.##;-0.##;0" ), " px"
			);
		}

		private void AlignToClassic( object sender, EventArgs args )
		{
			var sprite = this.selectedSprite;
			var cel = this.FindClassicCel( sprite );
			if( sprite == null || cel == null )
			{
				return;
			}

			var scale = Renderer.DefaultHdScale;
			sprite.ScreenX = cel.RelX * scale.Width;
			sprite.ScreenY = cel.RelY * scale.Height;

			this.MarkDirty();
			this.UpdateNumericEditors();
			this.UpdateSelectedSpriteNodeText();
			this.RefreshPlacements();
		}

		//-------------------------------------------
		// animation playback

		private void HandleAnimationSelected( object sender, EventArgs args )
		{
			if( this.suppressUiEvents )
			{
				return;
			}
			this.UpdateStepRange();
			this.RefreshPlacements();
		}

		private void HandleStepChanged( object sender, EventArgs args )
		{
			if( this.suppressUiEvents )
			{
				return;
			}
			this.RefreshPlacements();
		}

		private void HandlePlayCheckedChanged( object sender, EventArgs args )
		{
			this.timerPlayback.Enabled = this.checkBoxPlay.Checked;
		}

		private void HandlePlaybackTick( object? sender, EventArgs args )
		{
			if( this.numericStep.Maximum <= this.numericStep.Minimum )
			{
				return;
			}
			this.suppressUiEvents = true;
			try
			{
				this.numericStep.Value = this.numericStep.Value >= this.numericStep.Maximum
					? this.numericStep.Minimum
					: this.numericStep.Value + 1;
			}
			finally
			{
				this.suppressUiEvents = false;
			}
			this.RefreshPlacements();
		}

		private void HandleCalibrationCheckedChanged( object sender, EventArgs args )
		{
			this.costumePreviewControl.ShowClassicOverlay = this.checkBoxCalibration.Checked;
			this.costumePreviewControl.Invalidate();
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
			var textureNumber = this.comboBoxTextures.SelectedIndex;
			this.atlasViewControl.Texture = this.LoadTexture( textureName );
			this.atlasViewControl.Sprites.Clear();
			this.atlasViewControl.Sprites.AddRange(
				this.Costume!.SpriteGroupList
					.SelectMany( g => g.SpriteList )
					.Where( s => s.TextureNumber == textureNumber )
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
			var result = new SaveCostumeOverrideCommand( this.LPAKFile, resourcePath, this.Costume ).Execute();
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
					"Discard all unsaved changes and reload the costume?",
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
			this.hiddenGroups.Clear();
			this.hiddenSprites.Clear();
			this.LoadCostume();
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
			this.UpdateAtlas();
			this.RefreshPlacements();
		}

		private string[] GetAllTextureNames()
		{
			return this.Costume == null
				? new string[0]
				: this.Costume.TextureFileNameList
					.Select( t => t.Path )
					.Where( p => !string.IsNullOrEmpty( p ) )
					.Distinct()
					.ToArray();
		}

		private void ExportAllTexturesPng( object sender, EventArgs args )
		{
			if( this.Costume == null )
			{
				return;
			}

			using( var dialog = new FolderBrowserDialog() )
			{
				dialog.Description = "Select the folder to export this costume's textures into (subfolders mirror the resource paths).";
				if( dialog.ShowDialog( this ) != DialogResult.OK )
				{
					return;
				}

				Cursor.Current = Cursors.WaitCursor;
				try
				{
					var result = new BatchExportTexturesPngCommand( this.LPAKFile, dialog.SelectedPath, this.GetAllTextureNames() ).Execute();
					if( !result.IsSuccess )
					{
						MessageBox.Show( this, result.Error, "Export all textures", MessageBoxButtons.OK, MessageBoxIcon.Warning );
					}
				}
				finally
				{
					Cursor.Current = Cursors.Default;
				}
			}
		}

		private void ImportAllTexturesPng( object sender, EventArgs args )
		{
			if( this.Costume == null )
			{
				return;
			}

			using( var dialog = new FolderBrowserDialog() )
			{
				dialog.Description = "Select the folder to import this costume's texture PNGs from (subfolders must mirror the resource paths, as written by the export).";
				if( dialog.ShowDialog( this ) != DialogResult.OK )
				{
					return;
				}

				Cursor.Current = Cursors.WaitCursor;
				try
				{
					var result = new BatchImportTexturesPngCommand( this.LPAKFile, dialog.SelectedPath, this.GetAllTextureNames() ).Execute();
					if( !result.IsSuccess )
					{
						MessageBox.Show( this, result.Error, "Import all textures", MessageBoxButtons.OK, MessageBoxIcon.Warning );
					}
				}
				finally
				{
					Cursor.Current = Cursors.Default;
				}
			}

			// some textures may have imported even when others failed, so always reload
			this.textureCache.Clear();
			this.UpdateAtlas();
			this.RefreshPlacements();
		}

		//-------------------------------------------
		// helpers

		private string? GetTextureFileName( int textureNumber )
		{
			return textureNumber >= 0 && textureNumber < ( this.Costume?.TextureFileNameList.Count ?? 0 )
				? this.Costume!.TextureFileNameList[textureNumber].Path
				: null;
		}

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
			if( this.Costume == null )
			{
				return;
			}
			this.label1.Text = string.Concat(
				"Costume Spritesheet Editor - Costume ", this.Costume.Header.Identifier, " - ", this.Costume.Header.Name,
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
