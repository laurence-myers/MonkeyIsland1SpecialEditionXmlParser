using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.Commands;
using MonkeyIslandSpecialEditionSpriteEditor.Formats;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;
using Costume = MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Entities.Costume;
using CostumeRenderer = MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Renderer;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	/// <summary>
	/// Views and edits a room: the preview composites it the way the game does - background,
	/// object sprites at their resolved positions, the actors the classic scripts place, then
	/// the static foreground - while the atlas edits each sprite's texture rectangle and the
	/// numeric fields its offset, layer and the room's classic-to-HD scale.
	/// </summary>
	public partial class SpriteSheetEditorForm : Form
	{
		/// <summary>
		/// Matches SE costume pak entries; the number prefix is the classic costume number.
		/// </summary>
		private static readonly Regex costumeFileNameRegex = new Regex(
			@"^art/costumes/(\d+)_.*\.costume\.xml$", RegexOptions.IgnoreCase | RegexOptions.Compiled );

		private static readonly List<SpriteSheetEditorForm> instances = new List<SpriteSheetEditorForm>();
		private readonly Dictionary<string, Image?> textureCache = new Dictionary<string, Image?>();
		private readonly Dictionary<int, Costume?> costumeCache = new Dictionary<int, Costume?>();
		private ClassicData? classicData;
		private ClassicRoom? classicRoom;
		private Dictionary<int, ClassicObject>? classicObjects;
		private readonly HashSet<Sprite> hiddenSprites = new HashSet<Sprite>();
		private Sprite? selectedSprite;
		// the entity whose texture the "Change..." button retargets; equals selectedSprite when
		// an object sprite is selected, or a background / room-object entity from the tree
		private ITextureReference? selectedTextureTarget;
		// the two tree branches whose checkboxes are hidden (static sprites and room objects are
		// not toggled in the preview); kept so the hidden state can be re-applied after edits
		private TreeNode? backgroundTreeRoot;
		private TreeNode? roomObjectsTreeRoot;
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

			this.InitializeSpriteTreeContextMenu();
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

			// seed the scale editors with the room's own classic-to-HD scale (fullscreen
			// 200-line rooms like the island maps scale by 1037/200, not the usual 7.2,
			// and X is always Y over the 1.2 VGA pixel aspect)
			var hdTransform = Renderer.GetHdTransform( this.Room, this.classicRoom );
			this.suppressUiEvents = true;
			try
			{
				this.numericScaleX.Value = (decimal)hdTransform.Scale.Width;
				this.numericScaleY.Value = (decimal)hdTransform.Scale.Height;
			}
			finally
			{
				this.suppressUiEvents = false;
			}
			this.ApplyHdTransformToPreview();

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
			this.roomPreviewControl.Foreground = Renderer.RenderForeground( this.Room, this.LoadTexture );
			this.checkBoxForeground.Enabled = this.roomPreviewControl.Foreground != null;

			this.PopulateTextureCombo();
			this.PopulateSpriteTree();
			this.PopulateDiagnostics();
			this.BuildActorOverlays();
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

				this.backgroundTreeRoot = this.AddBackgroundNodes();

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

				this.roomObjectsTreeRoot = this.AddRoomObjectNodes();

				this.treeViewSprites.EndUpdate();
			}
			finally
			{
				this.suppressUiEvents = false;
			}

			// static sprites and room objects are not toggled in the preview, so their checkboxes
			// would do nothing; suppress them (after EndUpdate, when the node handles exist)
			this.ReapplyHiddenCheckBoxes();
		}

		private static string DescribeSprite( int spriteIndex, Sprite sprite )
		{
			return string.Concat( "Frame ", spriteIndex, ": ", sprite.TextureWidth, "x", sprite.TextureHeight, " L", sprite.Layer );
		}

		/// <summary>
		/// The last path segment of a texture name, for compact tree labels, or "(none)".
		/// </summary>
		private static string TextureTail( string? textureFileName )
		{
			if( string.IsNullOrEmpty( textureFileName ) )
			{
				return "(none)";
			}
			var slash = textureFileName!.LastIndexOf( '/' );
			return slash >= 0 ? textureFileName.Substring( slash + 1 ) : textureFileName;
		}

		/// <summary>
		/// Adds a "Background" root whose leaves are the room's static sprites, grouped by layer
		/// (layer 0 is the background, later layers are foreground overlays). Selecting a leaf lets
		/// the user retarget that static sprite's texture.
		/// </summary>
		private TreeNode? AddBackgroundNodes()
		{
			if( this.Room!.StaticSpriteList == null || this.Room.StaticSpriteList.All( layer => layer.Count == 0 ) )
			{
				return null;
			}

			var backgroundNode = new TreeNode { Text = "Background", Checked = true };

			for( var layerIndex = 0; layerIndex < this.Room.StaticSpriteList.Count; layerIndex++ )
			{
				var layer = this.Room.StaticSpriteList[layerIndex];
				if( layer.Count == 0 )
				{
					continue;
				}

				var layerNode = new TreeNode
				{
					Text = string.Concat( "Layer ", layerIndex, layerIndex == 0 ? " (background)" : " (foreground)" ),
					Checked = true,
				};

				for( var staticIndex = 0; staticIndex < layer.Count; staticIndex++ )
				{
					var staticSprite = layer[staticIndex];
					layerNode.Nodes.Add( new TreeNode
					{
						Text = string.Concat( "Static ", staticIndex, ": ", staticSprite.Width, "x", staticSprite.Height, " @ ", staticSprite.X, ";", staticSprite.Y, " - ", TextureTail( staticSprite.TextureFileName ) ),
						Tag = staticSprite,
						Checked = true,
					} );
				}

				backgroundNode.Nodes.Add( layerNode );
			}

			this.treeViewSprites.Nodes.Add( backgroundNode );
			return backgroundNode;
		}

		/// <summary>
		/// Adds a "Room objects" root whose leaves are the named room objects: a sprite-variant
		/// object is one leaf, an image-variant object is a node with one leaf per texture chunk.
		/// Selecting a leaf lets the user retarget that object's texture.
		/// </summary>
		private TreeNode? AddRoomObjectNodes()
		{
			if( this.Room!.RoomObjectGroupList == null || this.Room.RoomObjectGroupList.Count == 0 )
			{
				return null;
			}

			var rootNode = new TreeNode { Text = "Room objects", Checked = true };

			for( var groupIndex = 0; groupIndex < this.Room.RoomObjectGroupList.Count; groupIndex++ )
			{
				var group = this.Room.RoomObjectGroupList[groupIndex];
				var header = groupIndex < this.Room.RoomObjectHeaderList.Count ? this.Room.RoomObjectHeaderList[groupIndex] : null;
				var name = string.IsNullOrEmpty( header?.Name ) ? "?" : header!.Name!;

				var groupNode = new TreeNode
				{
					Text = string.Concat( name, " (", group.RoomObjectList.Count, ")" ),
					Checked = true,
				};

				foreach( var roomObject in group.RoomObjectList )
				{
					if( roomObject.Sprite != null )
					{
						groupNode.Nodes.Add( new TreeNode
						{
							Text = string.Concat( "[", name, "/", roomObject.Index, "] sprite ", roomObject.Sprite.Width, "x", roomObject.Sprite.Height, " - ", TextureTail( roomObject.Sprite.TextureFileName ) ),
							Tag = roomObject.Sprite,
							Checked = true,
						} );
					}
					else if( roomObject.Image != null )
					{
						var imageNode = new TreeNode
						{
							Text = string.Concat( "[", name, "/", roomObject.Index, "] image (", roomObject.Image.ChunkList.Count, " chunks)" ),
							Checked = true,
						};
						for( var chunkIndex = 0; chunkIndex < roomObject.Image.ChunkList.Count; chunkIndex++ )
						{
							var chunk = roomObject.Image.ChunkList[chunkIndex];
							imageNode.Nodes.Add( new TreeNode
							{
								Text = string.Concat( "Chunk ", chunkIndex, ": ", chunk.Width, "x", chunk.Height, " @ ", chunk.X, ";", chunk.Y, " - ", TextureTail( chunk.TextureFileName ) ),
								Tag = chunk,
								Checked = true,
							} );
						}
						groupNode.Nodes.Add( imageNode );
					}
				}

				rootNode.Nodes.Add( groupNode );
			}

			this.treeViewSprites.Nodes.Add( rootNode );
			return rootNode;
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

			// Named room objects carry additional placement data; surface them for verification
			for( var index = 0; index < this.Room.RoomObjectGroupList.Count; index++ )
			{
				var roomObjectGroup = this.Room.RoomObjectGroupList[index];
				var roomObjectHeader = index < this.Room.RoomObjectHeaderList.Count ? this.Room.RoomObjectHeaderList[index] : null;
				var name = roomObjectHeader?.Name ?? "?";
				foreach( var roomObject in roomObjectGroup.RoomObjectList )
				{
					this.listBoxDiagnostics.Items.Add( string.Concat( "RoomObject[", name, "/", roomObject.Index, "]: ", roomObject.OffsetX, "; ", roomObject.OffsetY ) );
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

			var placements = Renderer.ResolvePlacements( this.Room, this.classicObjects, this.GetHdTransform() );

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

		/// <summary>
		/// Builds the classic-to-HD transform from the editable scale fields. The origin is
		/// recomputed from the scale so the classic view stays centered in the room the way
		/// the game places it (fullscreen rooms have widescreen margins on both sides).
		/// </summary>
		private RoomHdTransform GetHdTransform()
		{
			var scale = new SizeF( (float)this.numericScaleX.Value, (float)this.numericScaleY.Value );
			var origin = PointF.Empty;
			if( this.Room != null && this.classicRoom != null && this.classicRoom.Width > 0 && this.classicRoom.Height > 0 )
			{
				origin = new PointF(
					( this.Room.Header.Width - this.classicRoom.Width * scale.Width ) / 2.0f,
					( this.Room.Header.Height - this.classicRoom.Height * scale.Height ) / 2.0f
				);
			}
			return new RoomHdTransform( scale, origin );
		}

		private void ApplyHdTransformToPreview()
		{
			var hdTransform = this.GetHdTransform();
			this.roomPreviewControl.HdScale = hdTransform.Scale;
			this.roomPreviewControl.HdOrigin = hdTransform.Origin;
			this.UpdateActorPositions();
		}

		//-------------------------------------------
		// actor overlays

		/// <summary>
		/// Builds a costume overlay for every actor placement the classic scripts make in
		/// this room. The first placement of each actor (the room's initial setup) starts
		/// visible; later placements (mostly cutscene positions) start hidden.
		/// </summary>
		private void BuildActorOverlays()
		{
			this.roomPreviewControl.Actors.Clear();

			if( this.classicRoom != null )
			{
				var visibleActors = new HashSet<int>();
				foreach( var placement in this.classicRoom.ActorPlacementList )
				{
					var overlay = this.BuildActorOverlay( placement );
					overlay.Visible = overlay.Image != null && visibleActors.Add( placement.ActorNumber );

					// the walkbox mask decides whether the game draws the actor in front of or
					// behind the foreground props (tables, counters)
					overlay.DrawAboveForeground = this.classicRoom.GetBoxMaskAt( placement.X, placement.Y ) == 0;
					this.roomPreviewControl.Actors.Add( overlay );
				}
			}

			this.UpdateActorPositions();
			this.PopulateActorList();
		}

		private RoomPreviewControlActor BuildActorOverlay( ClassicActorPlacement placement )
		{
			Bitmap? image = null;
			var origin = PointF.Empty;
			var costumeName = placement.CostumeId != null
				? string.Concat( "costume ", placement.CostumeId )
				: "costume unknown";

			if( placement.CostumeId != null )
			{
				var costume = this.LoadCostumeById( placement.CostumeId.Value );
				if( costume != null )
				{
					costumeName = costume.Header.Name;
					var classicCostume = this.classicData?.FindCostume( placement.CostumeId.Value );
					image = CostumeRenderer.RenderStandingActor( costume, placement.DirectionName, this.LoadTexture, classicCostume, out origin );
				}
			}

			var label = string.Concat(
				"actor ", placement.ActorNumber, ": ", costumeName,
				placement.CostumeInferred ? "?" : "",
				" (", placement.X, ",", placement.Y, ") ",
				placement.Source
			);
			return new RoomPreviewControlActor( placement, image, origin, label );
		}

		/// <summary>
		/// Places every actor overlay with the current classic-to-HD transform, so the actors
		/// keep following the object sprites as the HD scale is edited.
		/// </summary>
		private void UpdateActorPositions()
		{
			var hdTransform = this.GetHdTransform();
			foreach( var actor in this.roomPreviewControl.Actors )
			{
				if( actor.Image == null )
				{
					continue;
				}

				// the actor origin is its feet: classic position scaled to HD, lifted by the
				// elevation
				var placement = actor.Placement;
				var originX = hdTransform.Origin.X + placement.X * hdTransform.Scale.Width;
				var originY = hdTransform.Origin.Y + ( placement.Y - ( placement.Elevation ?? 0 ) ) * hdTransform.Scale.Height;
				actor.Position = new PointF( originX - actor.Origin.X, originY - actor.Origin.Y );
			}
		}

		/// <summary>
		/// Loads the SE costume whose file name prefix matches the classic costume number,
		/// caching the result (including misses).
		/// </summary>
		private Costume? LoadCostumeById( int costumeId )
		{
			Costume? costume;
			if( this.costumeCache.TryGetValue( costumeId, out costume ) )
			{
				return costume;
			}

			try
			{
				for( var index = 0; index < this.LPAKFile.PakFileNames.Length; index++ )
				{
					var fileName = this.LPAKFile.PakFileNames[index].FileName;
					if( fileName == null )
					{
						continue;
					}
					var match = SpriteSheetEditorForm.costumeFileNameRegex.Match( fileName );
					if( match.Success && int.Parse( match.Groups[1].Value ) == costumeId )
					{
						costume = this.LPAKFile.LoadCostume( index );
						break;
					}
				}
			}
			catch( Exception )
			{
				costume = null;
			}

			this.costumeCache[costumeId] = costume;
			return costume;
		}

		private void PopulateActorList()
		{
			// adding a pre-checked item raises ItemCheck, which would toggle the overlay back
			this.suppressUiEvents = true;
			try
			{
				this.checkedListBoxActors.Items.Clear();
				foreach( var actor in this.roomPreviewControl.Actors )
				{
					this.checkedListBoxActors.Items.Add( actor.Label, actor.Visible );
				}
			}
			finally
			{
				this.suppressUiEvents = false;
			}

			this.splitTree.Panel2Collapsed = this.roomPreviewControl.Actors.Count == 0;
		}

		private void HandleActorItemCheck( object sender, ItemCheckEventArgs args )
		{
			if( this.suppressUiEvents )
			{
				return;
			}

			if( args.Index >= 0 && args.Index < this.roomPreviewControl.Actors.Count )
			{
				this.roomPreviewControl.Actors[args.Index].Visible = args.NewValue == CheckState.Checked;
				this.roomPreviewControl.Invalidate();
			}
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
			this.selectedTextureTarget = sprite;

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

		/// <summary>
		/// Selects a non-sprite texture target (a static sprite, room object sprite or image chunk)
		/// from the tree. These entities draw the whole texture into a screen-space rectangle, so
		/// there is no atlas rect and the numeric editors stay disabled; only the Texture row applies.
		/// </summary>
		private void SetSelectedTextureTarget( ITextureReference? target )
		{
			if( this.selectedTextureTarget == target && this.selectedSprite == null )
			{
				return;
			}

			// clear any object-sprite selection first (this also nulls selectedTextureTarget and
			// disables the numeric editors), then track the new target on its own
			this.SetSelectedSprite( null );
			this.selectedTextureTarget = target;

			this.suppressUiEvents = true;
			try
			{
				this.atlasViewControl.Texture = this.LoadTexture( target?.TextureFileName );
				this.atlasViewControl.Sprites.Clear();
				this.atlasViewControl.SelectedSprite = null;
				this.atlasViewControl.RefreshContent();
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
			// Sprite implements ITextureReference too, so match it first: object sprites keep their
			// full atlas-rect editor, the other targets get only the Texture row.
			if( args.Node?.Tag is Sprite sprite )
			{
				this.SetSelectedSprite( sprite );
			}
			else if( args.Node?.Tag is ITextureReference target )
			{
				this.SetSelectedTextureTarget( target );
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
			// a keyboard toggle can re-show a hidden checkbox (its state image is rewritten), so
			// re-hide the non-functional branches
			this.ReapplyHiddenCheckBoxes();
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
		/// Unchecks every node except the selected one, its subtree and its ancestors, so the
		/// preview shows only the selected entity.
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
				// keep ancestors checked so the soloed node stays visible
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
			// re-checking nodes re-shows their state image, so re-hide the non-functional ones
			this.ReapplyHiddenCheckBoxes();
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
			this.ReapplyHiddenCheckBoxes();
		}

		private static void SetCheckedRecursive( TreeNode node, bool value )
		{
			node.Checked = value;
			foreach( TreeNode child in node.Nodes )
			{
				SetCheckedRecursive( child, value );
			}
		}

		//-------------------------------------------
		// hiding checkboxes on non-functional nodes

		// WinForms only exposes CheckBoxes tree-wide, so hiding the checkbox on individual nodes
		// (the static sprite and room object branches, whose checkboxes would do nothing) is done
		// through the Win32 tree-item state image: index 0 draws no checkbox.
		private const int TvifState = 0x8;
		private const int TvisStateImageMask = 0xF000;
		private const int TvmSetItem = 0x1100 + 63;

		[StructLayout( LayoutKind.Sequential, CharSet = CharSet.Auto )]
		private struct TvItem
		{
			public int mask;
			public IntPtr hItem;
			public int state;
			public int stateMask;
			public IntPtr lpszText;
			public int cchTextMax;
			public int iImage;
			public int iSelectedImage;
			public int cChildren;
			public IntPtr lParam;
		}

		[DllImport( "user32.dll", CharSet = CharSet.Auto )]
		private static extern IntPtr SendMessage( IntPtr hWnd, int msg, IntPtr wParam, ref TvItem lParam );

		private void ReapplyHiddenCheckBoxes()
		{
			if( this.backgroundTreeRoot != null )
			{
				HideCheckBoxesRecursive( this.treeViewSprites, this.backgroundTreeRoot );
			}
			if( this.roomObjectsTreeRoot != null )
			{
				HideCheckBoxesRecursive( this.treeViewSprites, this.roomObjectsTreeRoot );
			}
		}

		private static void HideCheckBoxesRecursive( TreeView treeView, TreeNode node )
		{
			var item = new TvItem
			{
				hItem = node.Handle,
				mask = TvifState,
				stateMask = TvisStateImageMask,
				state = 0,
			};
			SendMessage( treeView.Handle, TvmSetItem, IntPtr.Zero, ref item );

			foreach( TreeNode child in node.Nodes )
			{
				HideCheckBoxesRecursive( treeView, child );
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

				// the Texture row follows the texture target, which may be a non-sprite entity whose
				// screen-space rectangle keeps the numeric editors above disabled
				this.textBoxTextureName.Text = this.selectedTextureTarget?.TextureFileName ?? "";
				this.buttonChangeTexture.Enabled = this.selectedTextureTarget != null;
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
			this.ApplyHdTransformToPreview();
			this.RefreshPlacements();
		}

		private void HandleCalibrationCheckedChanged( object sender, EventArgs args )
		{
			this.ApplyHdTransformToPreview();
			this.roomPreviewControl.ShowCalibrationOverlay = this.checkBoxCalibration.Checked;
			this.roomPreviewControl.Invalidate();
		}

		private void HandleForegroundCheckedChanged( object sender, EventArgs args )
		{
			this.roomPreviewControl.ShowForeground = this.checkBoxForeground.Checked;
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
		// texture reassignment

		private void HandleChangeTextureClick( object sender, EventArgs args )
		{
			if( this.selectedTextureTarget == null )
			{
				return;
			}

			using( var dialog = new TexturePickerDialog( this.LPAKFile, this.GetAllTextureNames(), this.selectedTextureTarget.TextureFileName ) )
			{
				if( dialog.ShowDialog( this ) != DialogResult.OK || dialog.SelectedResourcePath == null )
				{
					return;
				}
				this.ApplyTextureToTarget( this.selectedTextureTarget, dialog.SelectedResourcePath );
			}
		}

		private void ApplyTextureToTarget( ITextureReference target, string resourcePath )
		{
			if( target.TextureFileName == resourcePath )
			{
				return;
			}

			target.TextureFileName = resourcePath;
			this.MarkDirty();

			// a brand-new texture may have been cached as a null miss before it existed on disk
			this.textureCache.Clear();

			if( target is StaticSprite )
			{
				// the background/foreground bitmaps bake in the static sprites, so rebuild them
				this.roomPreviewControl.Background = Renderer.RenderBackground( this.Room!, this.LoadTexture );
				this.roomPreviewControl.Foreground = Renderer.RenderForeground( this.Room!, this.LoadTexture );
				this.checkBoxForeground.Enabled = this.roomPreviewControl.Foreground != null;
			}
			else if( target is Sprite sprite )
			{
				// the object sprite may now belong to a texture the combo did not list
				this.PopulateTextureCombo();
				var comboIndex = this.comboBoxTextures.Items.IndexOf( resourcePath );
				if( comboIndex >= 0 )
				{
					this.comboBoxTextures.SelectedIndex = comboIndex;
				}
				this.UpdateAtlas();
				this.atlasViewControl.SelectedSprite = this.atlasViewControl.Sprites.Contains( sprite ) ? sprite : null;
				this.UpdateSelectedSpriteNodeText();
			}

			// object sprite placements resolve their own texture when re-rendered
			this.RefreshPlacements();

			if( !( target is Sprite ) )
			{
				// keep the atlas showing the newly assigned texture and refresh the tree label
				this.atlasViewControl.Texture = this.LoadTexture( resourcePath );
				this.atlasViewControl.RefreshContent();
				this.UpdateSelectedTargetNodeText( resourcePath );
			}

			this.UpdateNumericEditors();
		}

		/// <summary>
		/// Replaces the texture tail (the part after the last " - ") of the selected tree node; the
		/// background and room object leaf labels all end with the texture name.
		/// </summary>
		private void UpdateSelectedTargetNodeText( string resourcePath )
		{
			var node = this.treeViewSprites.SelectedNode;
			if( node == null )
			{
				return;
			}
			var dash = node.Text.LastIndexOf( " - ", StringComparison.Ordinal );
			if( dash >= 0 )
			{
				node.Text = string.Concat( node.Text.Substring( 0, dash + 3 ), TextureTail( resourcePath ) );
			}
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
			this.selectedTextureTarget = null;
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
			this.roomPreviewControl.Foreground = Renderer.RenderForeground( this.Room!, this.LoadTexture );
			this.UpdateAtlas();
			this.RefreshPlacements();
		}

		/// <summary>
		/// Every texture the room references: object sprites, static layers, and named
		/// room object sprites/image chunks.
		/// </summary>
		private string[] GetAllTextureNames()
		{
			if( this.Room == null )
			{
				return new string[0];
			}

			return this.Room.SpriteGroupList
				.SelectMany( g => g.SpriteList )
				.Select( s => s.TextureFileName )
				.Concat( this.Room.StaticSpriteList
					.SelectMany( l => l )
					.Select( s => s.TextureFileName ) )
				.Concat( this.Room.RoomObjectGroupList
					.SelectMany( g => g.RoomObjectList )
					.SelectMany( o => new[] { o.Sprite?.TextureFileName }
						.Concat( o.Image?.ChunkList.Select( c => c.TextureFileName ) ?? Enumerable.Empty<string?>() ) ) )
				.Where( n => !string.IsNullOrEmpty( n ) )
				.Select( n => n! )
				.Distinct()
				.ToArray();
		}

		private void ExportAllTexturesPng( object sender, EventArgs args )
		{
			if( this.Room == null )
			{
				return;
			}

			using( var dialog = new FolderBrowserDialog() )
			{
				dialog.Description = "Select the folder to export this room's textures into (subfolders mirror the resource paths).";
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
			if( this.Room == null )
			{
				return;
			}

			using( var dialog = new FolderBrowserDialog() )
			{
				dialog.Description = "Select the folder to import this room's texture PNGs from (subfolders must mirror the resource paths, as written by the export).";
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
			this.roomPreviewControl.Background = Renderer.RenderBackground( this.Room, this.LoadTexture );
			this.roomPreviewControl.Foreground = Renderer.RenderForeground( this.Room, this.LoadTexture );
			this.UpdateAtlas();
			this.RefreshPlacements();
		}

		//-------------------------------------------
		// room export

		private void ExportAsXml( object sender, EventArgs args )
		{
			if( this.Room == null )
			{
				return;
			}
			new ExportToXmlCommand( this.Room, string.Concat( this.Room.Header.Identifier, "_", this.Room.Header.Name, ".xml" ) ).Execute();
		}

		private void ExportAsPng( object sender, EventArgs args )
		{
			if( this.Room == null )
			{
				return;
			}
			new ExportRoomToPngWithDialogCommand( this.LPAKFile, this.Room ).Execute();
		}

		private void ExportAsMergedPng( object sender, EventArgs args )
		{
			if( this.Room == null )
			{
				return;
			}
			new ExportRoomToMergedPngWithDialogCommand( this.LPAKFile, this.Room ).Execute();
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
