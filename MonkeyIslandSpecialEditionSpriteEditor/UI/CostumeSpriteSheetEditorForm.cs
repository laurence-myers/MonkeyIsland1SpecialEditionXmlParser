using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
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
		/// <summary>
		/// Matches SE room pak entries; the number prefix of the file name is the classic room
		/// number. Anchored at a slash (or start) so a language-prefixed entry still matches.
		/// </summary>
		private static readonly Regex roomFileNameRegex = new Regex(
			@"(?:^|/)(\d+)_[^/]*\.room\.xml$", RegexOptions.IgnoreCase | RegexOptions.Compiled );

		private static readonly List<CostumeSpriteSheetEditorForm> instances = new List<CostumeSpriteSheetEditorForm>();
		private readonly Dictionary<string, Image?> textureCache = new Dictionary<string, Image?>();
		private ClassicData? classicData;
		private ClassicCostume? classicCostume;
		private readonly HashSet<SpriteGroup> hiddenGroups = new HashSet<SpriteGroup>();
		private readonly HashSet<Sprite> hiddenSprites = new HashSet<Sprite>();
		private Sprite? selectedSprite;
		private bool suppressUiEvents;
		private bool dirty;
		// play-test loop: debounces the "auto-write overrides on edit" writes (see ScheduleAutoWrite)
		private Timer? autoWriteTimer;
		// the room backdrop bitmaps this form owns and must dispose; the preview only borrows them
		private Bitmap? backdropBelow;
		private Bitmap? backdropAbove;
		// a room number + placement to select as the backdrop once the costume has loaded, set when
		// the room editor opens this form to place a specific actor
		private int? pendingBackdropRoomNumber;
		private int pendingBackdropPlacementIndex;
		// the three copy/paste slots: atlas rect position, atlas rect size, screen position;
		// each pair copies both of its numbers at once so animation cels line up exactly
		private Point? copiedTextureXY;
		private Size? copiedTextureSize;
		private PointF? copiedScreen;
		private readonly UndoStack undoStack = new UndoStack();

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
			this.FormClosed += delegate
			{
				CostumeSpriteSheetEditorForm.instances.Remove( this );
				this.ClearBackdrop();
				this.autoWriteTimer?.Stop();
				this.autoWriteTimer?.Dispose();
			};

			this.InitializeComponent();

			this.Load += delegate { this.LoadCostume(); };
			this.FormClosing += this.ConfirmCloseWithUnsavedChanges;

			this.atlasViewControl.SelectedSpriteChanged += this.HandleAtlasSelectionChanged;
			this.atlasViewControl.SpriteRectChanged += this.HandleAtlasSpriteRectChanged;
			this.costumePreviewControl.SelectedSpriteChanged += this.HandlePreviewSelectionChanged;
			this.costumePreviewControl.KeyDown += this.HandlePreviewKeyDown;
			this.timerPlayback.Tick += this.HandlePlaybackTick;
			this.undoStack.StateChanged += delegate { this.HandleUndoStackChanged(); };

			this.InitializeSpriteTreeContextMenu();
		}

		private void LoadCostume()
		{
			this.Costume = this.LPAKFile.LoadCostume( this.FileIndex );
			if( this.Costume == null )
			{
				return;
			}

			// a fresh (re)load starts a new undo history and a clean document
			this.undoStack.Clear();
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

			// drop any backdrop from a previous load before the room list is rebuilt
			this.ClearBackdrop();

			this.PopulateRoomBackdropCombo();

			this.PopulateAnimationCombo();
			this.PopulateTextureCombo();
			this.PopulateSpriteTree();
			this.PopulateDiagnostics();
			this.UpdateStepRange();
			this.RefreshPlacements();
			this.UpdateNumericEditors();

			this.ApplyPendingBackdrop();
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
					// the group's sprites cover cels FirstSpriteIdentifier onward; flag a
					// mismatch only when that window disagrees with the classic cel table
					else if( classicLimb.CelList.Count != spriteGroup.FirstSpriteIdentifier + spriteGroup.SpriteList.Count )
					{
						this.listBoxDiagnostics.Items.Add( string.Concat(
							"Group ", spriteGroup.Index, " (limb ", spriteGroup.Identifier, "): ",
							spriteGroup.SpriteList.Count, " sprites",
							spriteGroup.FirstSpriteIdentifier != 0 ? " from cel " + spriteGroup.FirstSpriteIdentifier : "",
							" vs ", classicLimb.CelList.Count, " classic cels"
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
					// match the preview's draw rect, including the room backdrop's scale
					var actorScale = this.costumePreviewControl.ActorScale;
					var drawRect = ScaleAboutOrigin( placement.ScreenRect, actorScale );
					bounds = bounds == null ? drawRect : RectangleF.Union( bounds.Value, drawRect );
					if( placement.ClassicCel != null )
					{
						var classicRect = ScaleAboutOrigin( Renderer.GetClassicScreenRect( placement.ClassicCel, placement.Flipped, Renderer.DefaultHdScale ), actorScale );
						bounds = RectangleF.Union( bounds.Value, classicRect );
					}
				}
			}
			this.costumePreviewControl.FixedBounds = bounds;
		}

		/// <summary>
		/// Scales a canvas rectangle about the actor origin, matching the preview's ActorScale.
		/// </summary>
		private static RectangleF ScaleAboutOrigin( RectangleF rect, float scale )
		{
			return scale == 1.0f
				? rect
				: new RectangleF( rect.X * scale, rect.Y * scale, rect.Width * scale, rect.Height * scale );
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

			// a new selection ends the current coalescing run, so edits on the new sprite start
			// their own undo step
			this.undoStack.BreakCoalescing();

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
				// checking a node toggles its whole subtree
				foreach( TreeNode child in args.Node.Nodes )
				{
					SetCheckedRecursive( child, args.Node.Checked );
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

		//-------------------------------------------
		// solo / show all

		private void InitializeSpriteTreeContextMenu()
		{
			var menu = new ContextMenuStrip();
			var soloItem = new ToolStripMenuItem( "Solo (hide the rest)" );
			soloItem.Click += delegate { this.SoloSelectedNode(); };
			var showAllItem = new ToolStripMenuItem( "Show all" );
			showAllItem.Click += delegate { this.ShowAllNodes(); };
			menu.Items.Add( soloItem );
			menu.Items.Add( showAllItem );
			menu.Opening += delegate { soloItem.Enabled = this.treeViewSprites.SelectedNode != null; };

			this.treeViewSprites.ContextMenuStrip = menu;
			// a right-click does not move the tree selection on its own, so do it here to solo the
			// node the user actually clicked
			this.treeViewSprites.NodeMouseClick += this.HandleTreeNodeMouseClick;
		}

		private void HandleTreeNodeMouseClick( object? sender, TreeNodeMouseClickEventArgs args )
		{
			if( args.Button == MouseButtons.Right )
			{
				this.treeViewSprites.SelectedNode = args.Node;
			}
		}

		/// <summary>
		/// Unchecks every node except the selected one, its subtree and its ancestors (a costume
		/// sprite is hidden when its group is unchecked), so the preview shows only that entity.
		/// </summary>
		private void SoloSelectedNode()
		{
			var node = this.treeViewSprites.SelectedNode;
			if( node == null )
			{
				return;
			}

			this.suppressUiEvents = true;
			try
			{
				this.treeViewSprites.BeginUpdate();
				foreach( TreeNode root in this.treeViewSprites.Nodes )
				{
					SetCheckedRecursive( root, false );
				}
				SetCheckedRecursive( node, true );
				for( var ancestor = node.Parent; ancestor != null; ancestor = ancestor.Parent )
				{
					ancestor.Checked = true;
				}
				this.treeViewSprites.EndUpdate();
			}
			finally
			{
				this.suppressUiEvents = false;
			}

			this.SyncVisibilityFromTree();
		}

		private void ShowAllNodes()
		{
			this.suppressUiEvents = true;
			try
			{
				this.treeViewSprites.BeginUpdate();
				foreach( TreeNode root in this.treeViewSprites.Nodes )
				{
					SetCheckedRecursive( root, true );
				}
				this.treeViewSprites.EndUpdate();
			}
			finally
			{
				this.suppressUiEvents = false;
			}

			this.SyncVisibilityFromTree();
		}

		private static void SetCheckedRecursive( TreeNode node, bool value )
		{
			node.Checked = value;
			foreach( TreeNode child in node.Nodes )
			{
				SetCheckedRecursive( child, value );
			}
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

		private void HandleAtlasSpriteRectChanged( object? sender, SpriteRectChangedEventArgs args )
		{
			if( args.Sprite is Sprite sprite )
			{
				var oldLocation = args.OldLocation;
				var newLocation = args.NewLocation;
				this.RecordEdit(
					this.SpriteEdit( sprite, "AtlasRect", "texture rectangle",
						() => { sprite.TextureX = oldLocation.X; sprite.TextureY = oldLocation.Y; },
						() => { sprite.TextureX = newLocation.X; sprite.TextureY = newLocation.Y; } ),
					// a drag fires many moves; coalesce them into one undo step
					forceCoalesce: args.Dragging );
			}
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
			var sprite = this.selectedSprite;
			var oldX = sprite.ScreenX;
			var oldY = sprite.ScreenY;
			sprite.ScreenX += deltaX;
			sprite.ScreenY += deltaY;
			var newX = sprite.ScreenX;
			var newY = sprite.ScreenY;
			this.RecordEdit( this.SpriteEdit( sprite, "ScreenXY", "screen position",
				() => { sprite.ScreenX = oldX; sprite.ScreenY = oldY; },
				() => { sprite.ScreenX = newX; sprite.ScreenY = newY; } ) );
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

				this.buttonCopyTextureXY.Enabled = enabled;
				this.buttonPasteTextureXY.Enabled = enabled && this.copiedTextureXY != null;
				this.buttonCopyTextureSize.Enabled = enabled;
				this.buttonPasteTextureSize.Enabled = enabled && this.copiedTextureSize != null;
				this.buttonCopyScreen.Enabled = enabled;
				this.buttonPasteScreen.Enabled = enabled && this.copiedScreen != null;

				// costume sprites reference a texture by index; -1 shows as "(none)"
				this.textBoxTextureName.Text = sprite == null ? "" : ( this.GetTextureFileName( sprite.TextureNumber ) ?? "(none)" );
				this.buttonChangeTexture.Enabled = enabled;
				this.buttonClearTexture.Enabled = enabled;

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

			// only write the edited field back (writing all of them would silently apply the
			// display clamp of an out-of-range field the user never touched), and record an
			// undoable edit that captures that field's old and new value
			var sprite = this.selectedSprite;
			if( sender == this.numericTextureX )
			{
				var oldValue = sprite.TextureX;
				var newValue = (int)this.numericTextureX.Value;
				sprite.TextureX = newValue;
				this.RecordEdit( this.SpriteEdit( sprite, "TextureX", "texture X", () => sprite.TextureX = oldValue, () => sprite.TextureX = newValue ) );
			}
			else if( sender == this.numericTextureY )
			{
				var oldValue = sprite.TextureY;
				var newValue = (int)this.numericTextureY.Value;
				sprite.TextureY = newValue;
				this.RecordEdit( this.SpriteEdit( sprite, "TextureY", "texture Y", () => sprite.TextureY = oldValue, () => sprite.TextureY = newValue ) );
			}
			else if( sender == this.numericTextureWidth )
			{
				var oldValue = sprite.TextureWidth;
				var newValue = (int)this.numericTextureWidth.Value;
				sprite.TextureWidth = newValue;
				this.RecordEdit( this.SpriteEdit( sprite, "TextureWidth", "texture width", () => sprite.TextureWidth = oldValue, () => sprite.TextureWidth = newValue ) );
			}
			else if( sender == this.numericTextureHeight )
			{
				var oldValue = sprite.TextureHeight;
				var newValue = (int)this.numericTextureHeight.Value;
				sprite.TextureHeight = newValue;
				this.RecordEdit( this.SpriteEdit( sprite, "TextureHeight", "texture height", () => sprite.TextureHeight = oldValue, () => sprite.TextureHeight = newValue ) );
			}
			else if( sender == this.numericScreenX )
			{
				var oldValue = sprite.ScreenX;
				var newValue = (float)this.numericScreenX.Value;
				sprite.ScreenX = newValue;
				this.RecordEdit( this.SpriteEdit( sprite, "ScreenX", "screen X", () => sprite.ScreenX = oldValue, () => sprite.ScreenX = newValue ) );
			}
			else if( sender == this.numericScreenY )
			{
				var oldValue = sprite.ScreenY;
				var newValue = (float)this.numericScreenY.Value;
				sprite.ScreenY = newValue;
				this.RecordEdit( this.SpriteEdit( sprite, "ScreenY", "screen Y", () => sprite.ScreenY = oldValue, () => sprite.ScreenY = newValue ) );
			}
			else if( sender == this.numericMoveX )
			{
				var oldValue = sprite.MoveX;
				var newValue = (float)this.numericMoveX.Value;
				sprite.MoveX = newValue;
				this.RecordEdit( this.SpriteEdit( sprite, "MoveX", "move X", () => sprite.MoveX = oldValue, () => sprite.MoveX = newValue ) );
			}
			else if( sender == this.numericMoveY )
			{
				var oldValue = sprite.MoveY;
				var newValue = (float)this.numericMoveY.Value;
				sprite.MoveY = newValue;
				this.RecordEdit( this.SpriteEdit( sprite, "MoveY", "move Y", () => sprite.MoveY = oldValue, () => sprite.MoveY = newValue ) );
			}

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
		// copy/paste of field pairs (each pair copies both numbers at once, so the cels
		// of an animation can be sized and placed identically)

		/// <summary>
		/// Runs the refreshes a paste needs after writing a pair of fields (the same work
		/// <see cref="HandleNumericValueChanged"/> does). Each paste handler records its own
		/// undoable edit before calling this.
		/// </summary>
		private void RefreshAfterPaste()
		{
			this.UpdateNumericEditors();
			this.UpdateSelectedSpriteNodeText();
			this.atlasViewControl.Invalidate();
			this.RefreshPlacements();
		}

		private void HandleCopyTextureXYClick( object sender, EventArgs args )
		{
			if( this.selectedSprite == null )
			{
				return;
			}
			this.copiedTextureXY = new Point( this.selectedSprite.TextureX, this.selectedSprite.TextureY );

			// enables the Paste button
			this.UpdateNumericEditors();
		}

		private void HandlePasteTextureXYClick( object sender, EventArgs args )
		{
			if( this.selectedSprite == null || this.copiedTextureXY == null )
			{
				return;
			}
			var sprite = this.selectedSprite;
			int oldX = sprite.TextureX, oldY = sprite.TextureY;
			int newX = this.copiedTextureXY.Value.X, newY = this.copiedTextureXY.Value.Y;
			sprite.TextureX = newX;
			sprite.TextureY = newY;
			this.RecordEdit( this.SpriteEdit( sprite, "PasteTextureXY", "paste texture position",
				() => { sprite.TextureX = oldX; sprite.TextureY = oldY; },
				() => { sprite.TextureX = newX; sprite.TextureY = newY; } ) );
			this.RefreshAfterPaste();
		}

		private void HandleCopyTextureSizeClick( object sender, EventArgs args )
		{
			if( this.selectedSprite == null )
			{
				return;
			}
			this.copiedTextureSize = new Size( this.selectedSprite.TextureWidth, this.selectedSprite.TextureHeight );
			this.UpdateNumericEditors();
		}

		private void HandlePasteTextureSizeClick( object sender, EventArgs args )
		{
			if( this.selectedSprite == null || this.copiedTextureSize == null )
			{
				return;
			}
			var sprite = this.selectedSprite;
			int oldW = sprite.TextureWidth, oldH = sprite.TextureHeight;
			int newW = this.copiedTextureSize.Value.Width, newH = this.copiedTextureSize.Value.Height;
			sprite.TextureWidth = newW;
			sprite.TextureHeight = newH;
			this.RecordEdit( this.SpriteEdit( sprite, "PasteTextureSize", "paste texture size",
				() => { sprite.TextureWidth = oldW; sprite.TextureHeight = oldH; },
				() => { sprite.TextureWidth = newW; sprite.TextureHeight = newH; } ) );
			this.RefreshAfterPaste();
		}

		private void HandleCopyScreenClick( object sender, EventArgs args )
		{
			if( this.selectedSprite == null )
			{
				return;
			}
			this.copiedScreen = new PointF( this.selectedSprite.ScreenX, this.selectedSprite.ScreenY );
			this.UpdateNumericEditors();
		}

		private void HandlePasteScreenClick( object sender, EventArgs args )
		{
			if( this.selectedSprite == null || this.copiedScreen == null )
			{
				return;
			}
			var sprite = this.selectedSprite;
			float oldX = sprite.ScreenX, oldY = sprite.ScreenY;
			float newX = this.copiedScreen.Value.X, newY = this.copiedScreen.Value.Y;
			sprite.ScreenX = newX;
			sprite.ScreenY = newY;
			this.RecordEdit( this.SpriteEdit( sprite, "PasteScreen", "paste screen position",
				() => { sprite.ScreenX = oldX; sprite.ScreenY = oldY; },
				() => { sprite.ScreenX = newX; sprite.ScreenY = newY; } ) );
			this.RefreshAfterPaste();
		}

		//-------------------------------------------
		// classic alignment

		/// <summary>
		/// Finds the classic cel matching a sprite: the cel index is the sprite's raw
		/// identifier (its group index plus the group's FirstSpriteIdentifier), matching how
		/// the animation frames reference both.
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
				return Renderer.FindCel( classicLimb, spriteIndex + spriteGroup.FirstSpriteIdentifier );
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
			float oldX = sprite.ScreenX, oldY = sprite.ScreenY;
			float newX = cel.RelX * scale.Width, newY = cel.RelY * scale.Height;
			sprite.ScreenX = newX;
			sprite.ScreenY = newY;
			this.RecordEdit( this.SpriteEdit( sprite, "AlignToClassic", "align to classic",
				() => { sprite.ScreenX = oldX; sprite.ScreenY = oldY; },
				() => { sprite.ScreenX = newX; sprite.ScreenY = newY; } ) );
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

		//-------------------------------------------
		// game placement and room backdrop

		/// <summary>
		/// One entry in the room backdrop combo: a classic actor placement of this costume, or
		/// the "(no room backdrop)" sentinel whose <see cref="Placement"/> is null.
		/// </summary>
		private sealed class RoomBackdropOption( int roomNumber, ClassicActorPlacement? placement, string label )
		{
			public int RoomNumber
			{
				get;
			} = roomNumber;

			public ClassicActorPlacement? Placement
			{
				get;
			} = placement;

			public string Label
			{
				get;
			} = label;

			public override string ToString()
			{
				return this.Label;
			}
		}

		private void HandleGamePlacementCheckedChanged( object sender, EventArgs args )
		{
			if( this.suppressUiEvents )
			{
				return;
			}

			// sprites always draw at their Screen X/Y (the placement the engine honors);
			// the checkbox only controls the room-backdrop context, so turning it off
			// drops the backdrop (selecting a room turns it back on)
			if( !this.checkBoxGamePlacement.Checked && ( this.backdropBelow != null || this.backdropAbove != null ) )
			{
				this.suppressUiEvents = true;
				try
				{
					this.comboBoxRoomBackdrop.SelectedIndex = 0;
				}
				finally
				{
					this.suppressUiEvents = false;
				}
				this.ClearBackdrop();
			}

			this.UpdateAnimationBounds();
			this.costumePreviewControl.RefreshContent();
		}

		/// <summary>
		/// Lists every room the classic scripts place this costume's actor in, so the costume can
		/// be shown against that room's background.
		/// </summary>
		private void PopulateRoomBackdropCombo()
		{
			this.suppressUiEvents = true;
			try
			{
				this.comboBoxRoomBackdrop.Items.Clear();
				this.comboBoxRoomBackdrop.Items.Add( new RoomBackdropOption( -1, null, "(no room backdrop)" ) );

				if( this.classicData != null && this.Costume != null )
				{
					foreach( var room in this.classicData.RoomList )
					{
						foreach( var placement in room.ActorPlacementList )
						{
							if( placement.CostumeId != this.Costume.Header.Identifier )
							{
								continue;
							}
							var label = string.Concat(
								room.RoomNumber, " - ", room.Name ?? "?",
								": actor ", placement.ActorNumber,
								" @ (", placement.X, ",", placement.Y, ") ", placement.Source
							);
							this.comboBoxRoomBackdrop.Items.Add( new RoomBackdropOption( room.RoomNumber, placement, label ) );
						}
					}
				}

				this.comboBoxRoomBackdrop.SelectedIndex = 0;
			}
			finally
			{
				this.suppressUiEvents = false;
			}

			this.comboBoxRoomBackdrop.Enabled = this.comboBoxRoomBackdrop.Items.Count > 1;
			this.labelRoomBackdrop.Enabled = this.comboBoxRoomBackdrop.Enabled;
		}

		private void HandleRoomBackdropSelected( object sender, EventArgs args )
		{
			if( this.suppressUiEvents )
			{
				return;
			}
			this.ApplySelectedRoomBackdrop();
		}

		private void ApplySelectedRoomBackdrop()
		{
			var option = this.comboBoxRoomBackdrop.SelectedItem as RoomBackdropOption;
			if( option?.Placement == null )
			{
				this.ClearBackdrop();
				this.costumePreviewControl.RefreshContent();
				return;
			}

			if( !this.SetRoomBackdrop( option.RoomNumber, option.Placement ) )
			{
				return;
			}

			// reflect the backdrop context in the Game placement checkbox
			this.suppressUiEvents = true;
			try
			{
				this.checkBoxGamePlacement.Checked = true;

				// face the actor the way the placement does
				var animation = Renderer.FindStandingAnimation( this.Costume!, option.Placement.DirectionName );
				if( animation != null )
				{
					var animationIndex = this.comboBoxAnimations.Items.IndexOf( animation.Name );
					if( animationIndex >= 0 )
					{
						this.comboBoxAnimations.SelectedIndex = animationIndex;
					}
				}
			}
			finally
			{
				this.suppressUiEvents = false;
			}

			this.UpdateStepRange();
			this.RefreshPlacements();
			this.UpdateNumericEditors();
		}

		/// <summary>
		/// Renders the room's imagery behind the costume, with the actor's feet on the placement.
		/// Returns false (and leaves no backdrop) when the SE room cannot be resolved or loaded.
		/// </summary>
		private bool SetRoomBackdrop( int roomNumber, ClassicActorPlacement placement )
		{
			var roomIndex = this.FindRoomFileIndex( roomNumber );
			Formats.Rooms.Entities.Room? room = null;
			if( roomIndex >= 0 )
			{
				try
				{
					room = this.LPAKFile.LoadRoom( roomIndex );
				}
				catch( Exception )
				{
					room = null;
				}
			}

			if( room == null )
			{
				this.ClearBackdrop();
				this.warningLabel.Text = string.Concat( "SE room ", roomNumber, " could not be loaded - no room backdrop shown." );
				this.warningLabel.Visible = true;
				this.costumePreviewControl.RefreshContent();
				return false;
			}

			var classicRoom = this.classicData?.FindRoom( roomNumber );

			// load the room's textures through a throwaway cache so a room's dozen large chunk
			// bitmaps do not linger in this costume form's texture cache for its whole lifetime
			var roomTextures = new Dictionary<string, Image?>();
			Func<string?, Image?> roomTextureLoader = fileName =>
			{
				if( string.IsNullOrEmpty( fileName ) )
				{
					return null;
				}
				Image? image;
				if( roomTextures.TryGetValue( fileName!, out image ) )
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
				roomTextures[fileName!] = image;
				return image;
			};

			Formats.Rooms.RoomBackdrop backdrop;
			try
			{
				backdrop = Formats.Rooms.BackdropRenderer.Render( room, classicRoom, placement, roomTextureLoader );
			}
			finally
			{
				foreach( var texture in roomTextures.Values )
				{
					texture?.Dispose();
				}
			}

			this.ClearBackdrop();
			this.backdropBelow = backdrop.Below;
			this.backdropAbove = backdrop.Above;
			this.costumePreviewControl.BackdropBelow = backdrop.Below;
			this.costumePreviewControl.BackdropAbove = backdrop.Above;
			this.costumePreviewControl.BackdropOffset = new PointF( -backdrop.ActorOriginHd.X, -backdrop.ActorOriginHd.Y );

			// the costume's sprites are authored at the costume scale; scale them to the room's
			// scale so the actor sits at the room's own size (a no-op for the usual 144-line
			// rooms, which share that scale; smaller for fullscreen map/title rooms)
			this.costumePreviewControl.ActorScale = backdrop.Scale.Height > 0
				? backdrop.Scale.Height / Renderer.DefaultHdScale.Height
				: 1.0f;
			return true;
		}

		private int FindRoomFileIndex( int roomNumber )
		{
			for( var index = 0; index < this.LPAKFile.PakFileNames.Length; index++ )
			{
				var fileName = this.LPAKFile.PakFileNames[index].FileName;
				if( fileName == null )
				{
					continue;
				}
				var match = CostumeSpriteSheetEditorForm.roomFileNameRegex.Match( fileName );
				if( match.Success && int.Parse( match.Groups[1].Value ) == roomNumber )
				{
					return index;
				}
			}
			return -1;
		}

		private void ClearBackdrop()
		{
			this.costumePreviewControl.BackdropBelow = null;
			this.costumePreviewControl.BackdropAbove = null;
			this.costumePreviewControl.ActorScale = 1.0f;
			this.backdropBelow?.Dispose();
			this.backdropAbove?.Dispose();
			this.backdropBelow = null;
			this.backdropAbove = null;
		}

		/// <summary>
		/// Selects the room backdrop for a specific classic actor placement, once the costume has
		/// loaded. Called when the room editor opens this form to place an actor against its room.
		/// </summary>
		public void SelectRoomBackdrop( int roomNumber, int placementIndex )
		{
			this.pendingBackdropRoomNumber = roomNumber;
			this.pendingBackdropPlacementIndex = placementIndex;
			if( this.Costume != null )
			{
				this.ApplyPendingBackdrop();
			}
		}

		private void ApplyPendingBackdrop()
		{
			if( this.pendingBackdropRoomNumber == null )
			{
				return;
			}
			var roomNumber = this.pendingBackdropRoomNumber.Value;
			var placementIndex = this.pendingBackdropPlacementIndex;
			this.pendingBackdropRoomNumber = null;

			var classicRoom = this.classicData?.FindRoom( roomNumber );
			if( classicRoom == null || placementIndex < 0 || placementIndex >= classicRoom.ActorPlacementList.Count )
			{
				return;
			}

			var target = classicRoom.ActorPlacementList[placementIndex];
			for( var index = 0; index < this.comboBoxRoomBackdrop.Items.Count; index++ )
			{
				if( this.comboBoxRoomBackdrop.Items[index] is RoomBackdropOption option && option.Placement == target )
				{
					this.comboBoxRoomBackdrop.SelectedIndex = index;
					return;
				}
			}
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
		// texture reassignment

		private void HandleChangeTextureClick( object sender, EventArgs args )
		{
			if( this.selectedSprite == null )
			{
				return;
			}

			using( var dialog = new TexturePickerDialog( this.LPAKFile, this.GetAllTextureNames(), this.GetTextureFileName( this.selectedSprite.TextureNumber ) ) )
			{
				if( dialog.ShowDialog( this ) != DialogResult.OK || dialog.SelectedResourcePath == null )
				{
					return;
				}

				// a change-texture is a discrete structural edit (it may append a texture entry);
				// never let it coalesce with another, or a merged undo could fail to drop both
				// appended entries
				this.undoStack.BreakCoalescing();

				// reuse the costume's existing texture entry, or append one for a texture it does
				// not list yet, then point the sprite at that index
				var sprite = this.selectedSprite;
				var oldTextureNumber = sprite.TextureNumber;
				var oldCount = this.Costume!.TextureFileNameList.Count;
				var index = TextureAssignment.GetOrAddTextureIndex( this.Costume, dialog.SelectedResourcePath );
				var addedEntry = this.Costume.TextureFileNameList.Count > oldCount
					? this.Costume.TextureFileNameList[this.Costume.TextureFileNameList.Count - 1]
					: null;
				sprite.TextureNumber = index;

				// undo restores the old index and drops the entry only if this edit appended it
				this.RecordEdit( new UndoableEdit
				{
					Owner = sprite,
					Key = "ChangeTexture",
					Description = "change texture",
					Undo = () =>
					{
						sprite.TextureNumber = oldTextureNumber;
						if( addedEntry != null
							&& this.Costume!.TextureFileNameList.Count > 0
							&& ReferenceEquals( this.Costume.TextureFileNameList[this.Costume.TextureFileNameList.Count - 1], addedEntry ) )
						{
							this.Costume.TextureFileNameList.RemoveAt( this.Costume.TextureFileNameList.Count - 1 );
						}
					},
					Redo = () =>
					{
						if( addedEntry != null && !this.Costume!.TextureFileNameList.Contains( addedEntry ) )
						{
							this.Costume.TextureFileNameList.Add( addedEntry );
						}
						sprite.TextureNumber = index;
					},
					Select = () => this.SetSelectedSprite( sprite ),
					ExtraRefresh = this.RefreshAfterTextureChange,
				} );

				this.RefreshAfterTextureChange();
				this.RefreshPlacements();
				this.UpdateNumericEditors();
			}
		}

		/// <summary>
		/// Rebuilds the texture combo and atlas after the selected sprite's texture number (or
		/// the costume's texture list) changed, selecting the sprite's texture. Used by the change
		/// texture edit both immediately and on undo/redo.
		/// </summary>
		private void RefreshAfterTextureChange()
		{
			// a brand-new texture may have been cached as a null miss before it existed on disk
			this.textureCache.Clear();
			this.PopulateTextureCombo();
			this.suppressUiEvents = true;
			try
			{
				var textureNumber = this.selectedSprite?.TextureNumber ?? -1;
				if( textureNumber >= 0 && textureNumber < this.comboBoxTextures.Items.Count )
				{
					this.comboBoxTextures.SelectedIndex = textureNumber;
				}
			}
			finally
			{
				this.suppressUiEvents = false;
			}
			this.UpdateAtlas();
			if( this.selectedSprite != null )
			{
				this.atlasViewControl.SelectedSprite = this.atlasViewControl.Sprites.Contains( this.selectedSprite ) ? this.selectedSprite : null;
			}
		}

		private void HandleClearTextureClick( object sender, EventArgs args )
		{
			if( this.selectedSprite == null )
			{
				return;
			}

			// a texture change is a discrete edit; keep it its own undo step
			this.undoStack.BreakCoalescing();

			// -1 is a valid "no texture" sprite; the packer ignores it and the sanity check allows it
			var sprite = this.selectedSprite;
			var oldNumber = sprite.TextureNumber;
			sprite.TextureNumber = -1;
			this.RecordEdit( new UndoableEdit
			{
				Owner = sprite,
				Key = "ClearTexture",
				Description = "clear texture",
				Undo = () => sprite.TextureNumber = oldNumber,
				Redo = () => sprite.TextureNumber = -1,
				Select = () => this.SetSelectedSprite( sprite ),
				ExtraRefresh = () => this.UpdateAtlas(),
			} );
			this.UpdateAtlas();
			this.RefreshPlacements();
			this.UpdateNumericEditors();
		}

		//-------------------------------------------
		// saving

		private void SaveOverride( object sender, EventArgs args )
		{
			this.TrySaveOverride();
		}

		/// <summary>The pak resource path this editor edits, e.g. "art/costumes/24_leaders-skin.costume.xml".</summary>
		public string? ResourcePath => this.LPAKFile.PakFileNames[this.FileIndex].FileName;

		/// <summary>Whether the costume has edits not yet written as an override.</summary>
		public bool IsDirty => this.dirty;

		/// <summary>
		/// Writes the override if the costume is dirty (a no-op returning true otherwise). Silent: a
		/// failure is reported through the return value and the status bar, not a dialog - for the
		/// play-test loop, which writes every open editor in one go.
		/// </summary>
		public bool SaveOverrideIfDirty( bool silent )
		{
			return !this.dirty || this.TrySaveOverride( silent );
		}

		private bool TrySaveOverride( bool silent = false )
		{
			var result = new SaveCostumeOverrideCommand( this.LPAKFile, this.ResourcePath, this.Costume ).Execute();
			if( result.IsSuccess )
			{
				this.undoStack.MarkSaved();
				this.NotifyRoomEditorsCostumeChanged();
				return true;
			}

			if( !silent )
			{
				MessageBox.Show( this, result.Error, "Save override", MessageBoxButtons.OK, MessageBoxIcon.Error );
			}

			return false;
		}

		/// <summary>
		/// Debounces the auto-write: a burst of arrow-key nudges becomes one override write shortly
		/// after the last one, so the game never reads a half-written run and the disk isn't hammered.
		/// </summary>
		private void ScheduleAutoWrite()
		{
			if( this.autoWriteTimer == null )
			{
				this.autoWriteTimer = new Timer { Interval = 500 };
				this.autoWriteTimer.Tick += delegate
				{
					this.autoWriteTimer!.Stop();
					if( this.dirty && !this.IsDisposed )
					{
						this.TrySaveOverride( silent: true );
					}
				};
			}

			this.autoWriteTimer.Stop();
			this.autoWriteTimer.Start();
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
			this.NotifyRoomEditorsCostumeChanged();
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
			this.NotifyRoomEditorsCostumeChanged();
		}

		//-------------------------------------------
		// helpers

		private string? GetTextureFileName( int textureNumber )
		{
			return textureNumber >= 0 && textureNumber < ( this.Costume?.TextureFileNameList.Count ?? 0 )
				? this.Costume!.TextureFileNameList[textureNumber].Path
				: null;
		}

		//-------------------------------------------
		// undo / redo

		/// <summary>
		/// Builds an undoable edit for a single sprite, re-selecting that sprite on undo/redo so
		/// the property fields and preview show the change.
		/// </summary>
		private UndoableEdit SpriteEdit( Sprite sprite, string key, string description, Action undo, Action redo )
		{
			return new UndoableEdit
			{
				Owner = sprite,
				Key = key,
				Description = description,
				Undo = undo,
				Redo = redo,
				Select = () => this.SetSelectedSprite( sprite ),
			};
		}

		private void RecordEdit( UndoableEdit edit, bool forceCoalesce = false )
		{
			this.undoStack.Record( edit, forceCoalesce );
		}

		private void HandleUndoClick( object? sender, EventArgs args )
		{
			this.undoStack.Undo();
			this.RefreshAfterUndoRedo();
		}

		private void HandleRedoClick( object? sender, EventArgs args )
		{
			this.undoStack.Redo();
			this.RefreshAfterUndoRedo();
		}

		/// <summary>
		/// Refreshes the whole editor after an undo or redo. The edit's Select already re-selected
		/// the affected sprite; UpdateNumericEditors is called unconditionally because Select is a
		/// no-op when that sprite was already selected, and a stale spinner value would otherwise
		/// be written straight back on the next edit.
		/// </summary>
		private void RefreshAfterUndoRedo()
		{
			this.UpdateNumericEditors();
			this.UpdateSelectedSpriteNodeText();
			this.UpdateClassicDeltaLabel();
			this.atlasViewControl.Invalidate();
			this.RefreshPlacements();
		}

		private void HandleUndoStackChanged()
		{
			this.dirty = !this.undoStack.IsAtSavedPosition;
			this.UpdateTitle();

			// play-test loop: with auto-write on, every edit lands on disk (debounced) so the game
			// picks it up the next time the room is entered - no explicit save needed
			if( this.dirty && UserSettings.Instance.AutoWriteOverrides )
			{
				this.ScheduleAutoWrite();
			}

			this.undoToolStripMenuItem.Enabled = this.undoStack.CanUndo;
			this.undoToolStripMenuItem.Text = this.undoStack.CanUndo
				? string.Concat( "&Undo ", this.undoStack.UndoDescription )
				: "&Undo";
			this.redoToolStripMenuItem.Enabled = this.undoStack.CanRedo;
			this.redoToolStripMenuItem.Text = this.undoStack.CanRedo
				? string.Concat( "&Redo ", this.undoStack.RedoDescription )
				: "&Redo";
		}

		/// <summary>
		/// Tells any open room editors to rebuild this costume's actor overlays, so a save or
		/// texture import here shows in their room previews without a manual reload.
		/// </summary>
		private void NotifyRoomEditorsCostumeChanged()
		{
			if( this.Costume == null )
			{
				return;
			}
			foreach( var roomForm in SpriteSheetEditorForm.Instances )
			{
				roomForm.InvalidateCostume( this.Costume.Header.Identifier );
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
