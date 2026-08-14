using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
		// the room object instance whose screen offset the numeric editors apply to; set for
		// sprite-variant leaves, image-variant instance nodes and chunk leaves alike
		private RoomObject? selectedRoomObject;
		// the entity whose texture the "Change..." button retargets; equals selectedSprite when
		// an object sprite is selected, or a background / room-object entity from the tree
		private ITextureReference? selectedTextureTarget;
		private TreeNode? roomObjectsTreeRoot;
		private bool suppressUiEvents;
		private bool dirty;
		// the three copy/paste slots: atlas rect position, atlas rect size, screen offset;
		// each pair copies both of its numbers at once so animation frames line up exactly
		private Point? copiedTextureXY;
		private Size? copiedTextureSize;
		private PointF? copiedOffsets;
		private readonly UndoStack undoStack = new UndoStack();

		// walkboxes are a separate edit domain: they save to the classic data file, not the SE
		// room override, so they carry their own undo stack and modified flag; Ctrl+Z/Y act on
		// whichever domain was edited most recently
		private readonly UndoStack walkBoxUndoStack = new UndoStack();
		private UndoStack lastActiveUndoStack = null!;
		private bool walkBoxesDirty;
		// an editable deep copy of the room's classic walkboxes, and a snapshot for the save-time
		// integrity check; the shared ClassicRoom.BoxList is only updated on a successful save
		private List<ClassicBox> workingBoxes = new List<ClassicBox>();
		private List<ClassicBox> originalBoxes = new List<ClassicBox>();

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
			this.roomPreviewControl.SelectedRoomObjectChanged += this.HandlePreviewRoomObjectSelectionChanged;
			this.roomPreviewControl.KeyDown += this.HandlePreviewKeyDown;
			this.roomPreviewControl.SelectedWalkBoxChanged += this.HandleWalkBoxSelectionChanged;
			this.roomPreviewControl.WalkBoxEdited += this.HandleWalkBoxEdited;
			this.undoStack.StateChanged += delegate { this.HandleUndoStackChanged(); };
			this.walkBoxUndoStack.StateChanged += delegate { this.HandleUndoStackChanged(); };
			this.lastActiveUndoStack = this.undoStack;

			this.InitializeSpriteTreeContextMenu();
			this.InitializeActorListContextMenu();
		}

		private void LoadRoom()
		{
			this.Room = this.LPAKFile.LoadRoom( this.FileIndex );
			if( this.Room == null )
			{
				return;
			}

			// re-read everything from disk on a (re)load, so a Revert or a costume/texture saved
			// since the last load is picked up rather than served stale from these caches
			this.costumeCache.Clear();
			this.textureCache.Clear();

			// a fresh (re)load starts a new undo history and a clean document
			this.undoStack.Clear();
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

			this.RebuildStaticLayers();

			this.PopulateTextureCombo();
			this.PopulateSpriteTree();
			this.PopulateDiagnostics();
			this.BuildActorOverlays();
			this.BuildWalkBoxes();
			this.RefreshPlacements();
			this.RefreshRoomObjectPreviews();
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
				this.treeViewBackground.BeginUpdate();
				this.treeViewBackground.Nodes.Clear();

				this.AddBackgroundNodes();

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

				this.treeViewBackground.EndUpdate();
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
		/// Fills the Backgrounds tab's tree with the room's static sprites, grouped by layer
		/// (layer 0 is the background, later layers are foreground overlays). Selecting a leaf
		/// lets the user retarget that static sprite's texture; there are no checkboxes because
		/// the static sprites are baked into the background/foreground bitmaps.
		/// </summary>
		private void AddBackgroundNodes()
		{
			if( this.Room!.StaticSpriteList == null || this.Room.StaticSpriteList.All( layer => layer.Count == 0 ) )
			{
				return;
			}

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
				};

				for( var staticIndex = 0; staticIndex < layer.Count; staticIndex++ )
				{
					var staticSprite = layer[staticIndex];
					layerNode.Nodes.Add( new TreeNode
					{
						Text = string.Concat( "Static ", staticIndex, ": ", staticSprite.Width, "x", staticSprite.Height, " @ ", staticSprite.X, ";", staticSprite.Y, " - ", TextureTail( staticSprite.TextureFileName ) ),
						Tag = staticSprite,
					} );
				}

				layerNode.Expand();
				this.treeViewBackground.Nodes.Add( layerNode );
			}
		}

		/// <summary>
		/// Adds a "Room objects" root whose leaves are the named room objects: a sprite-variant
		/// object is one leaf, an image-variant object is a node (tagged with the object, so
		/// selecting it edits the offset) with one leaf per texture chunk. Selecting a leaf lets
		/// the user retarget that texture and edit the owning object's screen offset. The
		/// checkboxes toggle the preview overlays and start unchecked: the game composes these
		/// with per-name logic (animation, scripted states) the preview can only approximate.
		/// </summary>
		private TreeNode? AddRoomObjectNodes()
		{
			if( this.Room!.RoomObjectGroupList == null || this.Room.RoomObjectGroupList.Count == 0 )
			{
				return null;
			}

			var rootNode = new TreeNode { Text = "Room objects", Checked = false };

			for( var groupIndex = 0; groupIndex < this.Room.RoomObjectGroupList.Count; groupIndex++ )
			{
				var group = this.Room.RoomObjectGroupList[groupIndex];
				var header = groupIndex < this.Room.RoomObjectHeaderList.Count ? this.Room.RoomObjectHeaderList[groupIndex] : null;
				var name = string.IsNullOrEmpty( header?.Name ) ? "?" : header!.Name!;

				var groupNode = new TreeNode
				{
					Text = string.Concat( name, " (", group.RoomObjectList.Count, ")" ),
					Checked = false,
				};

				foreach( var roomObject in group.RoomObjectList )
				{
					if( roomObject.Sprite != null )
					{
						groupNode.Nodes.Add( new TreeNode
						{
							Text = string.Concat( "[", name, "/", roomObject.Index, "] sprite ", roomObject.Sprite.Width, "x", roomObject.Sprite.Height, " - ", TextureTail( roomObject.Sprite.TextureFileName ) ),
							Tag = roomObject.Sprite,
							Checked = false,
						} );
					}
					else if( roomObject.Image != null )
					{
						var imageNode = new TreeNode
						{
							Text = string.Concat( "[", name, "/", roomObject.Index, "] image (", roomObject.Image.ChunkList.Count, " chunks)" ),
							Tag = roomObject,
							Checked = false,
						};
						for( var chunkIndex = 0; chunkIndex < roomObject.Image.ChunkList.Count; chunkIndex++ )
						{
							var chunk = roomObject.Image.ChunkList[chunkIndex];
							imageNode.Nodes.Add( new TreeNode
							{
								Text = string.Concat( "Chunk ", chunkIndex, ": ", chunk.Width, "x", chunk.Height, " @ ", chunk.X, ";", chunk.Y, " - ", TextureTail( chunk.TextureFileName ) ),
								Tag = chunk,
								Checked = false,
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

		/// <summary>
		/// Rebuilds the preview's static imagery from the room: the background (static layer 0)
		/// and one bitmap per foreground static layer. The preview interleaves the object
		/// sprites between these layers by the sprite's Layer, so an object state can paint over
		/// the foreground the way the game composites it.
		/// </summary>
		private void RebuildStaticLayers()
		{
			if( this.Room == null )
			{
				return;
			}

			this.roomPreviewControl.Background = Renderer.RenderBackground( this.Room, this.LoadTexture );

			var layerCount = Renderer.GetStaticLayerCount( this.Room );
			var foregroundLayers = new List<Bitmap?>();
			for( var layer = 1; layer < layerCount; layer++ )
			{
				foregroundLayers.Add( Renderer.RenderStaticLayer( this.Room, this.LoadTexture, layer ) );
			}
			this.roomPreviewControl.ForegroundLayers = foregroundLayers;
			this.checkBoxForeground.Enabled = this.roomPreviewControl.HasForeground;
		}

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
		/// Rebuilds the preview's room object overlays from the room data: one draw for a
		/// sprite-variant object, one per chunk (1:1, texture padding clipped the way the
		/// game shows it) for an image-variant one. The overlays sit between the background
		/// and the object sprites; visibility is re-applied from the tree checkboxes.
		/// </summary>
		private void RefreshRoomObjectPreviews()
		{
			this.roomPreviewControl.RoomObjects.Clear();
			if( this.Room?.RoomObjectGroupList != null )
			{
				for( var groupIndex = 0; groupIndex < this.Room.RoomObjectGroupList.Count; groupIndex++ )
				{
					var group = this.Room.RoomObjectGroupList[groupIndex];
					var header = groupIndex < this.Room.RoomObjectHeaderList.Count ? this.Room.RoomObjectHeaderList[groupIndex] : null;
					var name = string.IsNullOrEmpty( header?.Name ) ? "?" : header!.Name!;

					foreach( var roomObject in group.RoomObjectList )
					{
						var entry = new RoomPreviewControlRoomObject( roomObject, name );
						if( roomObject.Sprite != null )
						{
							var sprite = roomObject.Sprite;
							entry.Draws.Add( new RoomPreviewControlRoomObjectDraw(
								texture: this.LoadTexture( sprite.TextureFileName ),
								sourceRect: new RectangleF( sprite.X, sprite.Y, sprite.Width, sprite.Height ),
								relativeRect: new RectangleF( 0, 0, sprite.Width, sprite.Height )
							) );
						}
						else if( roomObject.Image != null )
						{
							foreach( var chunk in roomObject.Image.ChunkList )
							{
								var texture = this.LoadTexture( chunk.TextureFileName );

								// chunk textures are power-of-two padded; draw the rect-sized
								// region 1:1 (see Renderer.RenderStaticLayers)
								var width = texture == null ? chunk.Width : Math.Min( chunk.Width, texture.Width );
								var height = texture == null ? chunk.Height : Math.Min( chunk.Height, texture.Height );
								entry.Draws.Add( new RoomPreviewControlRoomObjectDraw(
									texture: texture,
									sourceRect: new RectangleF( 0, 0, width, height ),
									relativeRect: new RectangleF( chunk.X, chunk.Y, width, height )
								) );
							}
						}
						this.roomPreviewControl.RoomObjects.Add( entry );
					}
				}
			}

			// keep the selection pointing at the same entity across the rebuild
			this.suppressUiEvents = true;
			try
			{
				this.roomPreviewControl.SelectedRoomObject = this.FindRoomObjectPreviewEntry( this.selectedRoomObject );
			}
			finally
			{
				this.suppressUiEvents = false;
			}

			this.SyncVisibilityFromTree();
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

			// the actor images are composited at the costume scale (which matches the usual
			// 144-line rooms); scale them to this room's scale so they sit at its own size
			// rather than oversized in a fullscreen room (a no-op where the scales agree)
			var actorScale = CostumeRenderer.DefaultHdScale.Height > 0
				? hdTransform.Scale.Height / CostumeRenderer.DefaultHdScale.Height
				: 1.0f;

			foreach( var actor in this.roomPreviewControl.Actors )
			{
				if( actor.Image == null )
				{
					continue;
				}

				// the actor origin is its feet: classic position scaled to HD, lifted by the
				// elevation; the origin is scaled too so the feet stay on the placement
				var placement = actor.Placement;
				var originX = hdTransform.Origin.X + placement.X * hdTransform.Scale.Width;
				var originY = hdTransform.Origin.Y + ( placement.Y - ( placement.Elevation ?? 0 ) ) * hdTransform.Scale.Height;
				actor.Scale = actorScale;
				actor.Position = new PointF( originX - actor.Origin.X * actorScale, originY - actor.Origin.Y * actorScale );
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
				var index = this.FindCostumeFileIndex( costumeId );
				costume = index >= 0 ? this.LPAKFile.LoadCostume( index ) : null;
			}
			catch( Exception )
			{
				costume = null;
			}

			this.costumeCache[costumeId] = costume;
			return costume;
		}

		/// <summary>
		/// Finds the pak entry index of the SE costume whose file name prefix matches the classic
		/// costume number, or -1 when the pak has no such costume.
		/// </summary>
		private int FindCostumeFileIndex( int costumeId )
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
					return index;
				}
			}
			return -1;
		}

		/// <summary>
		/// Rebuilds the room's actor overlays for a costume that was just saved or re-textured in
		/// an open costume editor, so the room preview reflects the change without a full reload.
		/// Preserves the user's per-actor show/hide choices.
		/// </summary>
		public void InvalidateCostume( int costumeId )
		{
			if( this.Room == null )
			{
				return;
			}

			// forget the cached costume so it reloads (picking up its override), and drop the
			// texture cache since a re-imported costume texture may be cached under its name
			this.costumeCache.Remove( costumeId );
			this.textureCache.Clear();

			// remember which actors the user had shown or hidden, keyed by their placement
			var visibleByPlacement = new Dictionary<ClassicActorPlacement, bool>();
			foreach( var actor in this.roomPreviewControl.Actors )
			{
				visibleByPlacement[actor.Placement] = actor.Visible;
			}

			this.BuildActorOverlays();

			foreach( var actor in this.roomPreviewControl.Actors )
			{
				if( visibleByPlacement.TryGetValue( actor.Placement, out var wasVisible ) )
				{
					actor.Visible = actor.Image != null && wasVisible;
				}
			}
			this.PopulateActorList();
			this.roomPreviewControl.Invalidate();
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

			var actorCount = this.roomPreviewControl.Actors.Count;
			this.tabPageActors.Text = actorCount == 0 ? "Actors" : string.Concat( "Actors (", actorCount, ")" );
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

		/// <summary>
		/// Adds a right-click menu to the actor list for opening the costume editor with the room
		/// as its backdrop, so the actor can be placed pixel-accurately against this scene.
		/// </summary>
		private void InitializeActorListContextMenu()
		{
			var menu = new ContextMenuStrip();
			var openItem = new ToolStripMenuItem( "Open costume editor here" );
			openItem.Click += delegate { this.OpenCostumeEditorForSelectedActor(); };
			menu.Items.Add( openItem );
			menu.Opening += delegate
			{
				var placement = this.GetSelectedActorPlacement();
				openItem.Enabled = placement?.CostumeId != null
					&& this.FindCostumeFileIndex( placement.CostumeId.Value ) >= 0;
			};

			this.checkedListBoxActors.ContextMenuStrip = menu;

			// a right-click does not move the list selection on its own, so select the clicked
			// row (left-click still toggles the check thanks to CheckOnClick)
			this.checkedListBoxActors.MouseDown += delegate ( object? sender, MouseEventArgs args )
			{
				if( args.Button == MouseButtons.Right )
				{
					var index = this.checkedListBoxActors.IndexFromPoint( args.Location );
					if( index >= 0 )
					{
						this.checkedListBoxActors.SelectedIndex = index;
					}
				}
			};
		}

		private ClassicActorPlacement? GetSelectedActorPlacement()
		{
			var index = this.checkedListBoxActors.SelectedIndex;
			return index >= 0 && index < this.roomPreviewControl.Actors.Count
				? this.roomPreviewControl.Actors[index].Placement
				: null;
		}

		private void OpenCostumeEditorForSelectedActor()
		{
			var index = this.checkedListBoxActors.SelectedIndex;
			if( this.Room == null || index < 0 || index >= this.roomPreviewControl.Actors.Count )
			{
				return;
			}

			// the actor list is built one-to-one from the classic room's placement list, so the
			// list index is also the placement's index there
			var placement = this.roomPreviewControl.Actors[index].Placement;
			if( placement.CostumeId == null )
			{
				return;
			}

			var costumeFileIndex = this.FindCostumeFileIndex( placement.CostumeId.Value );
			if( costumeFileIndex < 0 )
			{
				MessageBox.Show(
					this,
					string.Concat( "No SE costume file found for costume ", placement.CostumeId.Value, "." ),
					"Open costume editor",
					MessageBoxButtons.OK,
					MessageBoxIcon.Information
				);
				return;
			}

			var fileName = this.LPAKFile.PakFileNames[costumeFileIndex].FileName;
			new OpenCostumeSpriteSheetEditorCommand(
				this.LPAKFile,
				fileName,
				costumeFileIndex,
				backdropRoomNumber: this.Room.Header.Identifier,
				backdropPlacementIndex: index
			).Execute();
		}

		//-------------------------------------------
		// selection synchronization

		private void SetSelectedSprite( Sprite? sprite )
		{
			if( this.selectedSprite == sprite )
			{
				return;
			}
			// a new selection ends the current coalescing run
			this.undoStack.BreakCoalescing();
			this.selectedSprite = sprite;
			this.selectedTextureTarget = sprite;

			// picking an object sprite ends any room object selection; clearing the sprite
			// (null) leaves it alone, so selecting a room object can first drop the sprite
			if( sprite != null )
			{
				this.selectedRoomObject = null;
			}

			this.suppressUiEvents = true;
			try
			{
				if( sprite != null )
				{
					this.roomPreviewControl.SelectedRoomObject = null;
				}
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
		/// from the tree. A room object sprite gets its atlas rectangle in the atlas view and the
		/// offset editors apply to its owning object; a static sprite only gets the Texture row.
		/// </summary>
		private void SetSelectedTextureTarget( ITextureReference? target )
		{
			if( this.selectedTextureTarget == target && this.selectedSprite == null )
			{
				return;
			}
			this.undoStack.BreakCoalescing();

			// clear any object-sprite selection first (this also nulls selectedTextureTarget and
			// disables the numeric editors), then track the new target on its own
			this.SetSelectedSprite( null );
			this.selectedTextureTarget = target;
			this.selectedRoomObject = target == null ? null : this.FindRoomObjectOwning( target );

			this.suppressUiEvents = true;
			try
			{
				this.atlasViewControl.Texture = this.LoadTexture( target?.TextureFileName );
				this.atlasViewControl.Sprites.Clear();

				// a sprite-variant room object edits its atlas rectangle like an object sprite
				if( target is RoomObjectSprite roomObjectSprite )
				{
					this.atlasViewControl.Sprites.Add( roomObjectSprite );
					this.atlasViewControl.SelectedSprite = roomObjectSprite;
				}
				else
				{
					this.atlasViewControl.SelectedSprite = null;
				}
				this.atlasViewControl.RefreshContent();

				this.roomPreviewControl.SelectedRoomObject = this.FindRoomObjectPreviewEntry( this.selectedRoomObject );
			}
			finally
			{
				this.suppressUiEvents = false;
			}

			this.UpdateNumericEditors();
		}

		/// <summary>
		/// Selects an image-variant room object from its instance node: the offset editors
		/// apply to the whole overlay while the Texture row stays with the individual chunks.
		/// </summary>
		private void SetSelectedRoomObjectInstance( RoomObject roomObject )
		{
			this.undoStack.BreakCoalescing();
			this.SetSelectedSprite( null );
			this.selectedTextureTarget = null;
			this.selectedRoomObject = roomObject;

			this.suppressUiEvents = true;
			try
			{
				this.atlasViewControl.Sprites.Clear();
				this.atlasViewControl.SelectedSprite = null;
				this.atlasViewControl.RefreshContent();
				this.roomPreviewControl.SelectedRoomObject = this.FindRoomObjectPreviewEntry( roomObject );
			}
			finally
			{
				this.suppressUiEvents = false;
			}

			this.UpdateNumericEditors();
		}

		/// <summary>
		/// Finds the room object whose sprite record or image chunk is the given target, so
		/// selecting either edits that object's screen offset.
		/// </summary>
		private RoomObject? FindRoomObjectOwning( ITextureReference target )
		{
			if( this.Room?.RoomObjectGroupList == null )
			{
				return null;
			}
			foreach( var group in this.Room.RoomObjectGroupList )
			{
				foreach( var roomObject in group.RoomObjectList )
				{
					if( roomObject.Sprite == target
						|| ( target is RoomObjectImageChunk chunk && roomObject.Image?.ChunkList.Contains( chunk ) == true ) )
					{
						return roomObject;
					}
				}
			}
			return null;
		}

		private RoomPreviewControlRoomObject? FindRoomObjectPreviewEntry( RoomObject? roomObject )
		{
			return roomObject == null
				? null
				: this.roomPreviewControl.RoomObjects.FirstOrDefault( entry => entry.RoomObject == roomObject );
		}

		private TreeNode? FindRoomObjectInstanceNode( RoomObject roomObject )
		{
			if( this.roomObjectsTreeRoot == null )
			{
				return null;
			}
			foreach( TreeNode groupNode in this.roomObjectsTreeRoot.Nodes )
			{
				foreach( TreeNode instanceNode in groupNode.Nodes )
				{
					if( instanceNode.Tag == roomObject || ( roomObject.Sprite != null && instanceNode.Tag == roomObject.Sprite ) )
					{
						return instanceNode;
					}
				}
			}
			return null;
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
			// Sprite implements ITextureReference too, so match it first: object sprites keep
			// their full atlas-rect editor. A room object instance node carries the object
			// itself (offset editing); the remaining targets get the Texture row.
			if( args.Node?.Tag is Sprite sprite )
			{
				this.SetSelectedSprite( sprite );
			}
			else if( args.Node?.Tag is RoomObject roomObject )
			{
				this.SetSelectedRoomObjectInstance( roomObject );
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

			// Defer the propagation out of the AfterCheck handler. Changing other nodes' Checked
			// state synchronously from inside AfterCheck desyncs the TreeView's internal checkbox
			// tracking - the symptom the user hit is a checkbox click that lands as a bare
			// selection and needs a second click. Running it once the click has settled avoids that.
			var node = args.Node;
			this.BeginInvoke( (Action)( () => this.PropagateCheckState( node ) ) );
		}

		/// <summary>
		/// Mirrors a node's new checkbox state through the tree in both directions: its whole
		/// subtree follows it (checking a group checks every frame under it), and every ancestor
		/// becomes checked exactly when at least one of its own children is checked. So checking
		/// the only frame in a group checks the group and the branch above it, and unchecking the
		/// last checked child clears them again.
		/// </summary>
		private void PropagateCheckState( TreeNode node )
		{
			// the tree may have been rebuilt (a reload) between the click and this deferred call
			if( node.TreeView == null )
			{
				return;
			}

			this.suppressUiEvents = true;
			try
			{
				this.treeViewSprites.BeginUpdate();
				TreeCheckPropagation.Apply( node );
				this.treeViewSprites.EndUpdate();
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

			// room object overlays follow their instance checkbox, image chunks their own
			if( this.roomObjectsTreeRoot != null )
			{
				foreach( TreeNode groupNode in this.roomObjectsTreeRoot.Nodes )
				{
					foreach( TreeNode instanceNode in groupNode.Nodes )
					{
						var entry = instanceNode.Tag is RoomObject roomObject
							? this.FindRoomObjectPreviewEntry( roomObject )
							: this.roomPreviewControl.RoomObjects.FirstOrDefault( e => e.RoomObject.Sprite != null && e.RoomObject.Sprite == instanceNode.Tag );
						if( entry == null )
						{
							continue;
						}

						entry.Visible = instanceNode.Checked;
						for( var chunkIndex = 0; chunkIndex < instanceNode.Nodes.Count && chunkIndex < entry.Draws.Count; chunkIndex++ )
						{
							entry.Draws[chunkIndex].Visible = instanceNode.Nodes[chunkIndex].Checked;
						}
					}
				}
			}

			// visibility changes the content bounds when overlays extend past the background
			this.roomPreviewControl.RefreshContent();
		}

		//-------------------------------------------
		// solo / show all

		private void InitializeSpriteTreeContextMenu()
		{
			var menu = new ContextMenuStrip();
			var soloItem = new ToolStripMenuItem( "Solo (hide the rest)" );
			soloItem.Click += delegate { this.SoloSelectedNode(); };
			var soloFrameItem = new ToolStripMenuItem( "Show only this frame in its group" );
			soloFrameItem.Click += delegate { this.SoloSelectedFrameInGroup(); };
			var showAllItem = new ToolStripMenuItem( "Show all" );
			showAllItem.Click += delegate { this.ShowAllNodes(); };
			var gameDefaultItem = new ToolStripMenuItem( "Game default view" );
			gameDefaultItem.Click += delegate { this.ShowGameDefaultView(); };
			menu.Items.Add( soloItem );
			menu.Items.Add( soloFrameItem );
			menu.Items.Add( showAllItem );
			menu.Items.Add( gameDefaultItem );
			menu.Opening += delegate
			{
				soloItem.Enabled = this.treeViewSprites.SelectedNode != null;
				soloFrameItem.Enabled = this.treeViewSprites.SelectedNode?.Tag is Sprite;
			};

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
		}

		/// <summary>
		/// Checks the selected frame and unchecks its group siblings, so the group shows one
		/// object state at a time the way the game does. Other groups are left alone.
		/// </summary>
		private void SoloSelectedFrameInGroup()
		{
			var node = this.treeViewSprites.SelectedNode;
			if( node?.Tag is not Sprite || node.Parent == null )
			{
				return;
			}

			this.suppressUiEvents = true;
			try
			{
				this.treeViewSprites.BeginUpdate();
				foreach( TreeNode sibling in node.Parent.Nodes )
				{
					sibling.Checked = sibling == node;
				}
				node.Parent.Checked = true;
				this.treeViewSprites.EndUpdate();
			}
			finally
			{
				this.suppressUiEvents = false;
			}

			this.SyncVisibilityFromTree();
		}

		/// <summary>
		/// Approximates the game's default composite: every object sprite frame hidden and
		/// every named room object overlay shown. The static background already paints each
		/// object's default state (room 30's safe is painted closed), the frames are the
		/// alternate states scripts switch to, and the overlays (like that safe's lever) are
		/// drawn by the game on top.
		/// </summary>
		private void ShowGameDefaultView()
		{
			this.suppressUiEvents = true;
			try
			{
				this.treeViewSprites.BeginUpdate();
				foreach( TreeNode root in this.treeViewSprites.Nodes )
				{
					SetCheckedRecursive( root, root == this.roomObjectsTreeRoot );
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
			TreeCheckPropagation.SetCheckedRecursive( node, value );
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
			var sprite = this.roomPreviewControl.SelectedSprite?.Placement.Sprite;
			this.SetSelectedSprite( sprite );
			if( sprite != null )
			{
				// reveal the tree selection the preview click just made
				this.tabControlRoom.SelectedTab = this.tabPageObjects;
			}
		}

		private void HandlePreviewRoomObjectSelectionChanged( object? sender, EventArgs args )
		{
			if( this.suppressUiEvents )
			{
				return;
			}

			var entry = this.roomPreviewControl.SelectedRoomObject;
			if( entry == null )
			{
				this.selectedRoomObject = null;
				this.UpdateNumericEditors();
				return;
			}

			// land on the same tree node a manual click would use, then run its selection
			var node = this.FindRoomObjectInstanceNode( entry.RoomObject );
			if( node != null )
			{
				this.suppressUiEvents = true;
				try
				{
					this.treeViewSprites.SelectedNode = node;
					node.EnsureVisible();
				}
				finally
				{
					this.suppressUiEvents = false;
				}
			}
			if( entry.RoomObject.Sprite != null )
			{
				this.SetSelectedTextureTarget( entry.RoomObject.Sprite );
			}
			else
			{
				this.SetSelectedRoomObjectInstance( entry.RoomObject );
			}
			// reveal the tree selection the preview click just made
			this.tabControlRoom.SelectedTab = this.tabPageObjects;
		}

		//-------------------------------------------
		// editing

		private void HandleAtlasSpriteRectChanged( object? sender, SpriteRectChangedEventArgs args )
		{
			var atlasSprite = args.Sprite;
			var oldLocation = args.OldLocation;
			var newLocation = args.NewLocation;
			this.RecordRoomEdit( atlasSprite, "AtlasRect", "texture rectangle",
				() => { atlasSprite.TextureX = oldLocation.X; atlasSprite.TextureY = oldLocation.Y; },
				() => { atlasSprite.TextureX = newLocation.X; atlasSprite.TextureY = newLocation.Y; },
				// a drag fires many moves; coalesce them into one undo step
				forceCoalesce: args.Dragging );

			this.UpdateNumericEditors();
			if( this.selectedSprite != null )
			{
				this.UpdateSelectedSpriteNodeText();
				this.RefreshPlacements();
			}
			else if( this.selectedRoomObject != null )
			{
				// the atlas rect belongs to a sprite-variant room object
				this.UpdateSelectedRoomObjectNodeText();
				this.RefreshRoomObjectPreviews();
			}
		}

		private void HandlePreviewKeyDown( object? sender, KeyEventArgs args )
		{
			// a selected walk box takes the arrow keys before sprites/room objects do
			if( this.checkBoxWalkBoxes.Checked && this.roomPreviewControl.SelectedWalkBox != null && this.HandleWalkBoxNudge( args ) )
			{
				return;
			}

			if( this.selectedSprite == null && this.selectedRoomObject == null )
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
			if( this.selectedSprite != null )
			{
				var sprite = this.selectedSprite;
				float oldX = sprite.OffsetX, oldY = sprite.OffsetY;
				sprite.OffsetX += deltaX;
				sprite.OffsetY += deltaY;
				float newX = sprite.OffsetX, newY = sprite.OffsetY;
				this.RecordRoomEdit( sprite, "Offset", "offset",
					() => { sprite.OffsetX = oldX; sprite.OffsetY = oldY; },
					() => { sprite.OffsetX = newX; sprite.OffsetY = newY; } );
				this.UpdateNumericEditors();
				this.RefreshPlacements();
			}
			else
			{
				var roomObject = this.selectedRoomObject!;
				float oldX = roomObject.OffsetX, oldY = roomObject.OffsetY;
				roomObject.OffsetX += deltaX;
				roomObject.OffsetY += deltaY;
				float newX = roomObject.OffsetX, newY = roomObject.OffsetY;
				this.RecordRoomEdit( roomObject, "Offset", "offset",
					() => { roomObject.OffsetX = oldX; roomObject.OffsetY = oldY; },
					() => { roomObject.OffsetX = newX; roomObject.OffsetY = newY; } );
				this.UpdateNumericEditors();
				this.roomPreviewControl.RefreshContent();
			}
		}

		private void UpdateNumericEditors()
		{
			this.suppressUiEvents = true;
			try
			{
				var sprite = this.selectedSprite;

				// a sprite-variant room object shares the atlas rect editors, and any room
				// object shares the offset editors (they hold its screen position)
				var atlasSprite = (IAtlasSprite?)sprite ?? this.selectedRoomObject?.Sprite;
				var atlasEnabled = atlasSprite != null;
				this.numericTextureX.Enabled = atlasEnabled;
				this.numericTextureY.Enabled = atlasEnabled;
				this.numericTextureWidth.Enabled = atlasEnabled;
				this.numericTextureHeight.Enabled = atlasEnabled;
				this.numericOffsetX.Enabled = sprite != null || this.selectedRoomObject != null;
				this.numericOffsetY.Enabled = this.numericOffsetX.Enabled;
				this.numericLayer.Enabled = sprite != null;

				if( atlasSprite != null )
				{
					this.numericTextureX.Value = Clamp( atlasSprite.TextureX, this.numericTextureX );
					this.numericTextureY.Value = Clamp( atlasSprite.TextureY, this.numericTextureY );
					this.numericTextureWidth.Value = Clamp( atlasSprite.TextureWidth, this.numericTextureWidth );
					this.numericTextureHeight.Value = Clamp( atlasSprite.TextureHeight, this.numericTextureHeight );
				}
				if( sprite != null )
				{
					this.numericOffsetX.Value = Clamp( (decimal)sprite.OffsetX, this.numericOffsetX );
					this.numericOffsetY.Value = Clamp( (decimal)sprite.OffsetY, this.numericOffsetY );
					this.numericLayer.Value = Clamp( sprite.Layer, this.numericLayer );
				}
				else if( this.selectedRoomObject != null )
				{
					this.numericOffsetX.Value = Clamp( (decimal)this.selectedRoomObject.OffsetX, this.numericOffsetX );
					this.numericOffsetY.Value = Clamp( (decimal)this.selectedRoomObject.OffsetY, this.numericOffsetY );
				}

				this.buttonCopyTextureXY.Enabled = atlasEnabled;
				this.buttonPasteTextureXY.Enabled = atlasEnabled && this.copiedTextureXY != null;
				this.buttonCopyTextureSize.Enabled = atlasEnabled;
				this.buttonPasteTextureSize.Enabled = atlasEnabled && this.copiedTextureSize != null;
				this.buttonCopyOffsets.Enabled = this.numericOffsetX.Enabled;
				this.buttonPasteOffsets.Enabled = this.numericOffsetX.Enabled && this.copiedOffsets != null;

				// the Texture row follows the texture target, which may be a non-sprite entity
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
			if( this.suppressUiEvents )
			{
				return;
			}

			if( this.selectedSprite != null )
			{
				// only write the edited field back (writing all of them would silently apply the
				// display clamp of an untouched out-of-range field), recording an undoable edit
				var sprite = this.selectedSprite;
				if( sender == this.numericTextureX )
				{
					int oldValue = sprite.TextureX, newValue = (int)this.numericTextureX.Value;
					sprite.TextureX = newValue;
					this.RecordRoomEdit( sprite, "TextureX", "texture X", () => sprite.TextureX = oldValue, () => sprite.TextureX = newValue );
				}
				else if( sender == this.numericTextureY )
				{
					int oldValue = sprite.TextureY, newValue = (int)this.numericTextureY.Value;
					sprite.TextureY = newValue;
					this.RecordRoomEdit( sprite, "TextureY", "texture Y", () => sprite.TextureY = oldValue, () => sprite.TextureY = newValue );
				}
				else if( sender == this.numericTextureWidth )
				{
					int oldValue = sprite.TextureWidth, newValue = (int)this.numericTextureWidth.Value;
					sprite.TextureWidth = newValue;
					this.RecordRoomEdit( sprite, "TextureWidth", "texture width", () => sprite.TextureWidth = oldValue, () => sprite.TextureWidth = newValue );
				}
				else if( sender == this.numericTextureHeight )
				{
					int oldValue = sprite.TextureHeight, newValue = (int)this.numericTextureHeight.Value;
					sprite.TextureHeight = newValue;
					this.RecordRoomEdit( sprite, "TextureHeight", "texture height", () => sprite.TextureHeight = oldValue, () => sprite.TextureHeight = newValue );
				}
				else if( sender == this.numericOffsetX )
				{
					float oldValue = sprite.OffsetX, newValue = (float)this.numericOffsetX.Value;
					sprite.OffsetX = newValue;
					this.RecordRoomEdit( sprite, "OffsetX", "offset X", () => sprite.OffsetX = oldValue, () => sprite.OffsetX = newValue );
				}
				else if( sender == this.numericOffsetY )
				{
					float oldValue = sprite.OffsetY, newValue = (float)this.numericOffsetY.Value;
					sprite.OffsetY = newValue;
					this.RecordRoomEdit( sprite, "OffsetY", "offset Y", () => sprite.OffsetY = oldValue, () => sprite.OffsetY = newValue );
				}
				else if( sender == this.numericLayer )
				{
					int oldValue = sprite.Layer, newValue = (int)this.numericLayer.Value;
					sprite.Layer = newValue;
					this.RecordRoomEdit( sprite, "Layer", "layer", () => sprite.Layer = oldValue, () => sprite.Layer = newValue );
				}

				this.UpdateSelectedSpriteNodeText();
				this.atlasViewControl.Invalidate();
				this.RefreshPlacements();
				return;
			}

			if( this.selectedRoomObject == null )
			{
				return;
			}

			// room object: the atlas rect editors write to its sprite record, the offset
			// editors to the overlay's screen position
			var roomObject = this.selectedRoomObject;
			var roomObjectSprite = roomObject.Sprite;
			if( sender == this.numericTextureX && roomObjectSprite != null )
			{
				int oldValue = roomObjectSprite.X, newValue = (int)this.numericTextureX.Value;
				roomObjectSprite.X = newValue;
				this.RecordRoomEdit( roomObjectSprite, "RoomObjectSpriteX", "texture X", () => roomObjectSprite.X = oldValue, () => roomObjectSprite.X = newValue );
			}
			else if( sender == this.numericTextureY && roomObjectSprite != null )
			{
				int oldValue = roomObjectSprite.Y, newValue = (int)this.numericTextureY.Value;
				roomObjectSprite.Y = newValue;
				this.RecordRoomEdit( roomObjectSprite, "RoomObjectSpriteY", "texture Y", () => roomObjectSprite.Y = oldValue, () => roomObjectSprite.Y = newValue );
			}
			else if( sender == this.numericTextureWidth && roomObjectSprite != null )
			{
				int oldValue = roomObjectSprite.Width, newValue = (int)this.numericTextureWidth.Value;
				roomObjectSprite.Width = newValue;
				this.RecordRoomEdit( roomObjectSprite, "RoomObjectSpriteWidth", "texture width", () => roomObjectSprite.Width = oldValue, () => roomObjectSprite.Width = newValue );
			}
			else if( sender == this.numericTextureHeight && roomObjectSprite != null )
			{
				int oldValue = roomObjectSprite.Height, newValue = (int)this.numericTextureHeight.Value;
				roomObjectSprite.Height = newValue;
				this.RecordRoomEdit( roomObjectSprite, "RoomObjectSpriteHeight", "texture height", () => roomObjectSprite.Height = oldValue, () => roomObjectSprite.Height = newValue );
			}
			else if( sender == this.numericOffsetX )
			{
				float oldValue = roomObject.OffsetX, newValue = (float)this.numericOffsetX.Value;
				roomObject.OffsetX = newValue;
				this.RecordRoomEdit( roomObject, "RoomObjectOffsetX", "offset X", () => roomObject.OffsetX = oldValue, () => roomObject.OffsetX = newValue );
			}
			else if( sender == this.numericOffsetY )
			{
				float oldValue = roomObject.OffsetY, newValue = (float)this.numericOffsetY.Value;
				roomObject.OffsetY = newValue;
				this.RecordRoomEdit( roomObject, "RoomObjectOffsetY", "offset Y", () => roomObject.OffsetY = oldValue, () => roomObject.OffsetY = newValue );
			}
			else
			{
				return;
			}

			this.atlasViewControl.Invalidate();
			if( sender == this.numericOffsetX || sender == this.numericOffsetY )
			{
				// draw positions derive from the entity's offset at paint time
				this.roomPreviewControl.RefreshContent();
			}
			else
			{
				this.UpdateSelectedRoomObjectNodeText();
				this.RefreshRoomObjectPreviews();
			}
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

		private void UpdateSelectedRoomObjectNodeText()
		{
			var roomObject = this.selectedRoomObject;
			if( roomObject?.Sprite == null )
			{
				return;
			}
			var node = this.FindRoomObjectInstanceNode( roomObject );
			if( node != null )
			{
				var name = this.FindRoomObjectPreviewEntry( roomObject )?.Name ?? "?";
				node.Text = string.Concat( "[", name, "/", roomObject.Index, "] sprite ", roomObject.Sprite.Width, "x", roomObject.Sprite.Height, " - ", TextureTail( roomObject.Sprite.TextureFileName ) );
			}
		}

		//-------------------------------------------
		// copy/paste of field pairs (each pair copies both numbers at once, so the frames
		// of an animation can be sized and placed identically)

		/// <summary>
		/// The atlas rect the Texture X/Y and Texture W/H pairs read and write: the selected
		/// object sprite, or the selected room object's sprite record.
		/// </summary>
		private IAtlasSprite? GetSelectedAtlasSprite()
		{
			return (IAtlasSprite?)this.selectedSprite ?? this.selectedRoomObject?.Sprite;
		}

		/// <summary>
		/// Refreshes everything that shows the selected atlas rect after both of a pair's
		/// fields were written (the same work <see cref="HandleNumericValueChanged"/> does).
		/// </summary>
		private void RefreshAfterAtlasRectEdit()
		{
			this.UpdateNumericEditors();
			this.atlasViewControl.Invalidate();
			if( this.selectedSprite != null )
			{
				this.UpdateSelectedSpriteNodeText();
				this.RefreshPlacements();
			}
			else if( this.selectedRoomObject != null )
			{
				this.UpdateSelectedRoomObjectNodeText();
				this.RefreshRoomObjectPreviews();
			}
		}

		private void HandleCopyTextureXYClick( object sender, EventArgs args )
		{
			var atlasSprite = this.GetSelectedAtlasSprite();
			if( atlasSprite == null )
			{
				return;
			}
			this.copiedTextureXY = new Point( atlasSprite.TextureX, atlasSprite.TextureY );

			// enables the Paste button
			this.UpdateNumericEditors();
		}

		private void HandlePasteTextureXYClick( object sender, EventArgs args )
		{
			var atlasSprite = this.GetSelectedAtlasSprite();
			if( atlasSprite == null || this.copiedTextureXY == null )
			{
				return;
			}
			int oldX = atlasSprite.TextureX, oldY = atlasSprite.TextureY;
			int newX = this.copiedTextureXY.Value.X, newY = this.copiedTextureXY.Value.Y;
			atlasSprite.TextureX = newX;
			atlasSprite.TextureY = newY;
			this.RecordRoomEdit( atlasSprite, "PasteTextureXY", "paste texture position",
				() => { atlasSprite.TextureX = oldX; atlasSprite.TextureY = oldY; },
				() => { atlasSprite.TextureX = newX; atlasSprite.TextureY = newY; } );
			this.RefreshAfterAtlasRectEdit();
		}

		private void HandleCopyTextureSizeClick( object sender, EventArgs args )
		{
			var atlasSprite = this.GetSelectedAtlasSprite();
			if( atlasSprite == null )
			{
				return;
			}
			this.copiedTextureSize = new Size( atlasSprite.TextureWidth, atlasSprite.TextureHeight );
			this.UpdateNumericEditors();
		}

		private void HandlePasteTextureSizeClick( object sender, EventArgs args )
		{
			var atlasSprite = this.GetSelectedAtlasSprite();
			if( atlasSprite == null || this.copiedTextureSize == null )
			{
				return;
			}
			int oldW = atlasSprite.TextureWidth, oldH = atlasSprite.TextureHeight;
			int newW = this.copiedTextureSize.Value.Width, newH = this.copiedTextureSize.Value.Height;
			atlasSprite.TextureWidth = newW;
			atlasSprite.TextureHeight = newH;
			this.RecordRoomEdit( atlasSprite, "PasteTextureSize", "paste texture size",
				() => { atlasSprite.TextureWidth = oldW; atlasSprite.TextureHeight = oldH; },
				() => { atlasSprite.TextureWidth = newW; atlasSprite.TextureHeight = newH; } );
			this.RefreshAfterAtlasRectEdit();
		}

		private void HandleCopyOffsetsClick( object sender, EventArgs args )
		{
			if( this.selectedSprite != null )
			{
				this.copiedOffsets = new PointF( this.selectedSprite.OffsetX, this.selectedSprite.OffsetY );
			}
			else if( this.selectedRoomObject != null )
			{
				this.copiedOffsets = new PointF( this.selectedRoomObject.OffsetX, this.selectedRoomObject.OffsetY );
			}
			else
			{
				return;
			}
			this.UpdateNumericEditors();
		}

		private void HandlePasteOffsetsClick( object sender, EventArgs args )
		{
			if( this.copiedOffsets == null )
			{
				return;
			}

			if( this.selectedSprite != null )
			{
				var sprite = this.selectedSprite;
				float oldX = sprite.OffsetX, oldY = sprite.OffsetY;
				float newX = this.copiedOffsets.Value.X, newY = this.copiedOffsets.Value.Y;
				sprite.OffsetX = newX;
				sprite.OffsetY = newY;
				this.RecordRoomEdit( sprite, "PasteOffsets", "paste offset",
					() => { sprite.OffsetX = oldX; sprite.OffsetY = oldY; },
					() => { sprite.OffsetX = newX; sprite.OffsetY = newY; } );
				this.UpdateNumericEditors();
				this.RefreshPlacements();
			}
			else if( this.selectedRoomObject != null )
			{
				var roomObject = this.selectedRoomObject;
				float oldX = roomObject.OffsetX, oldY = roomObject.OffsetY;
				float newX = this.copiedOffsets.Value.X, newY = this.copiedOffsets.Value.Y;
				roomObject.OffsetX = newX;
				roomObject.OffsetY = newY;
				this.RecordRoomEdit( roomObject, "PasteOffsets", "paste offset",
					() => { roomObject.OffsetX = oldX; roomObject.OffsetY = oldY; },
					() => { roomObject.OffsetX = newX; roomObject.OffsetY = newY; } );
				this.UpdateNumericEditors();
				this.roomPreviewControl.RefreshContent();
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
			var oldPath = target.TextureFileName;
			if( oldPath == resourcePath )
			{
				return;
			}

			// capture the tree node now: on undo/redo the selection may be elsewhere, and the
			// leaf label (background, room object chunk) has no lookup helper to find it again
			var node = this.treeViewBackground.SelectedNode?.Tag == target
				? this.treeViewBackground.SelectedNode
				: this.treeViewSprites.SelectedNode;
			target.TextureFileName = resourcePath;

			this.RecordEdit( new UndoableEdit
			{
				Owner = target,
				Key = "TextureTarget",
				Description = "change texture",
				Undo = () => target.TextureFileName = oldPath,
				Redo = () => target.TextureFileName = resourcePath,
				Select = () =>
				{
					if( node?.TreeView != null )
					{
						node.TreeView.SelectedNode = node;
					}
				},
				ExtraRefresh = () => this.RefreshAfterTextureTargetChange( target, node ),
			} );

			this.RefreshAfterTextureTargetChange( target, node );
			this.UpdateNumericEditors();
		}

		/// <summary>
		/// Rebuilds whatever shows a texture target after its texture file name changed - the
		/// baked background/foreground for a static sprite, the overlay draws for a room object,
		/// or the atlas and combo for an object sprite - and refreshes the given tree node's label.
		/// Used both when applying the change and on undo/redo.
		/// </summary>
		private void RefreshAfterTextureTargetChange( ITextureReference target, TreeNode? node )
		{
			// a brand-new texture may have been cached as a null miss before it existed on disk
			this.textureCache.Clear();

			if( target is StaticSprite )
			{
				// the background/foreground bitmaps bake in the static sprites, so rebuild them
				this.RebuildStaticLayers();
			}
			else if( target is RoomObjectSprite || target is RoomObjectImageChunk )
			{
				// the overlay draws cache their textures, so rebuild them
				this.RefreshRoomObjectPreviews();
			}
			else if( target is Sprite sprite )
			{
				// the object sprite may now belong to a texture the combo did not list
				this.PopulateTextureCombo();
				var comboIndex = this.comboBoxTextures.Items.IndexOf( target.TextureFileName );
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
				// keep the atlas showing the assigned texture and refresh the tree label
				this.atlasViewControl.Texture = this.LoadTexture( target.TextureFileName );
				this.atlasViewControl.RefreshContent();
				this.UpdateTargetNodeText( node, target.TextureFileName );
			}
		}

		/// <summary>
		/// Replaces the texture tail (the part after the last " - ") of a tree node; the background
		/// and room object leaf labels all end with the texture name.
		/// </summary>
		private void UpdateTargetNodeText( TreeNode? node, string? resourcePath )
		{
			if( node == null || resourcePath == null )
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
				this.undoStack.MarkSaved();
				return true;
			}

			MessageBox.Show( this, result.Error, "Save override", MessageBoxButtons.OK, MessageBoxIcon.Error );
			return false;
		}

		private void RevertChanges( object sender, EventArgs args )
		{
			if( this.dirty || this.walkBoxesDirty )
			{
				var answer = MessageBox.Show(
					this,
					"Discard all unsaved changes (including walk boxes) and reload the room?",
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
			this.selectedRoomObject = null;
			this.hiddenSprites.Clear();
			this.LoadRoom();
		}

		private void ConfirmCloseWithUnsavedChanges( object? sender, FormClosingEventArgs args )
		{
			if( this.dirty )
			{
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
					return;
				}
				if( answer == DialogResult.Yes && !this.TrySaveOverride() )
				{
					args.Cancel = true;
					return;
				}
			}

			if( this.walkBoxesDirty )
			{
				var answer = MessageBox.Show(
					this,
					"Save the changed walk boxes to the classic data file before closing?",
					"Unsaved walk boxes",
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Question
				);
				if( answer == DialogResult.Cancel )
				{
					args.Cancel = true;
					return;
				}
				if( answer == DialogResult.Yes && !this.TrySaveWalkBoxes() )
				{
					args.Cancel = true;
				}
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
			this.RebuildStaticLayers();
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
			this.RebuildStaticLayers();
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

		//-------------------------------------------
		// undo / redo

		private void RecordEdit( UndoableEdit edit, bool forceCoalesce = false )
		{
			this.lastActiveUndoStack = this.undoStack;
			this.undoStack.Record( edit, forceCoalesce );
		}

		/// <summary>
		/// Records an undoable edit that re-selects whatever is currently selected on undo/redo,
		/// so the property fields and preview show the change.
		/// </summary>
		private void RecordRoomEdit( object owner, string key, string description, Action undo, Action redo, bool forceCoalesce = false )
		{
			this.RecordEdit( new UndoableEdit
			{
				Owner = owner,
				Key = key,
				Description = description,
				Undo = undo,
				Redo = redo,
				Select = this.CaptureSelectionRestore(),
			}, forceCoalesce );
		}

		/// <summary>
		/// Captures the current selection as an action that restores it, so an undo/redo of an
		/// edit made now jumps back to the entity it changed.
		/// </summary>
		private Action CaptureSelectionRestore()
		{
			var sprite = this.selectedSprite;
			var target = this.selectedTextureTarget;
			var roomObject = this.selectedRoomObject;
			return () =>
			{
				if( sprite != null )
				{
					this.SetSelectedSprite( sprite );
				}
				else if( target != null )
				{
					this.SetSelectedTextureTarget( target );
				}
				else if( roomObject != null )
				{
					this.SetSelectedRoomObjectInstance( roomObject );
				}
			};
		}

		private UndoStack OtherStack( UndoStack stack )
		{
			return stack == this.undoStack ? this.walkBoxUndoStack : this.undoStack;
		}

		/// <summary>
		/// The stack Ctrl+Z acts on: the most recently edited domain, falling back to the other
		/// once the active one is exhausted so an earlier edit is never stranded.
		/// </summary>
		private UndoStack UndoTarget()
		{
			return this.lastActiveUndoStack.CanUndo ? this.lastActiveUndoStack : this.OtherStack( this.lastActiveUndoStack );
		}

		private UndoStack RedoTarget()
		{
			return this.lastActiveUndoStack.CanRedo ? this.lastActiveUndoStack : this.OtherStack( this.lastActiveUndoStack );
		}

		private void HandleUndoClick( object? sender, EventArgs args )
		{
			var target = this.UndoTarget();
			if( !target.CanUndo )
			{
				return;
			}
			this.lastActiveUndoStack = target;
			target.Undo();
			if( target == this.walkBoxUndoStack )
			{
				this.RefreshAfterWalkBoxUndoRedo();
			}
			else
			{
				this.RefreshAfterUndoRedo();
			}
		}

		private void HandleRedoClick( object? sender, EventArgs args )
		{
			var target = this.RedoTarget();
			if( !target.CanRedo )
			{
				return;
			}
			this.lastActiveUndoStack = target;
			target.Redo();
			if( target == this.walkBoxUndoStack )
			{
				this.RefreshAfterWalkBoxUndoRedo();
			}
			else
			{
				this.RefreshAfterUndoRedo();
			}
		}

		/// <summary>
		/// Refreshes the whole editor after an undo or redo. The edit's Select already re-selected
		/// the affected entity; UpdateNumericEditors is called unconditionally because Select is a
		/// no-op when that entity was already selected, and a stale spinner value would otherwise
		/// be written straight back on the next edit.
		/// </summary>
		private void RefreshAfterUndoRedo()
		{
			this.UpdateNumericEditors();
			this.UpdateSelectedSpriteNodeText();
			this.UpdateSelectedRoomObjectNodeText();
			this.atlasViewControl.Invalidate();
			this.RefreshPlacements();
			this.RefreshRoomObjectPreviews();
		}

		private void HandleUndoStackChanged()
		{
			this.dirty = !this.undoStack.IsAtSavedPosition;
			this.walkBoxesDirty = !this.walkBoxUndoStack.IsAtSavedPosition;
			this.UpdateTitle();
			if( this.buttonSaveWalkBoxes != null )
			{
				this.buttonSaveWalkBoxes.Enabled = this.walkBoxesDirty;
			}

			// the menu acts on whichever domain Ctrl+Z/Y would target
			var undoTarget = this.UndoTarget();
			this.undoToolStripMenuItem.Enabled = undoTarget.CanUndo;
			this.undoToolStripMenuItem.Text = undoTarget.CanUndo
				? string.Concat( "&Undo ", undoTarget.UndoDescription )
				: "&Undo";
			var redoTarget = this.RedoTarget();
			this.redoToolStripMenuItem.Enabled = redoTarget.CanRedo;
			this.redoToolStripMenuItem.Text = redoTarget.CanRedo
				? string.Concat( "&Redo ", redoTarget.RedoDescription )
				: "&Redo";
		}

		private void UpdateTitle()
		{
			if( this.Room == null )
			{
				return;
			}
			this.label1.Text = string.Concat(
				"Spritesheet Editor - Room ", this.Room.Header.Identifier, " - ", this.Room.Header.Name,
				this.dirty ? " (modified)" : "",
				this.walkBoxesDirty ? " (walk boxes modified)" : ""
			);
		}

		//-------------------------------------------
		// walk boxes (classic BOXD)

		/// <summary>
		/// Builds an editable working copy of the room's classic walkboxes and the overlay that
		/// draws them. The shared classic data is never touched until a successful save.
		/// </summary>
		private void BuildWalkBoxes()
		{
			var boxes = this.classicRoom?.BoxList ?? new List<ClassicBox>();
			this.workingBoxes = boxes.Select( box => box.Clone() ).ToList();
			this.originalBoxes = boxes.Select( box => box.Clone() ).ToList();

			this.roomPreviewControl.SelectedWalkBox = null;
			this.roomPreviewControl.WalkBoxes.Clear();
			for( var index = 0; index < this.workingBoxes.Count; index++ )
			{
				this.roomPreviewControl.WalkBoxes.Add( new RoomPreviewControlWalkBox( index, this.workingBoxes[index] ) );
			}

			this.walkBoxUndoStack.Clear();
			this.walkBoxesDirty = false;

			var haveBoxes = this.workingBoxes.Count > 0;
			this.suppressUiEvents = true;
			try
			{
				this.checkBoxWalkBoxes.Enabled = haveBoxes;
				if( !haveBoxes )
				{
					this.checkBoxWalkBoxes.Checked = false;
				}
			}
			finally
			{
				this.suppressUiEvents = false;
			}

			this.roomPreviewControl.ShowWalkBoxes = this.checkBoxWalkBoxes.Checked;
			this.PopulateWalkBoxList();
			this.UpdateWalkBoxEditors();
			this.roomPreviewControl.Invalidate();
		}

		private void PopulateWalkBoxList()
		{
			this.suppressUiEvents = true;
			try
			{
				this.listViewWalkBoxes.BeginUpdate();
				this.listViewWalkBoxes.Items.Clear();
				for( var index = 0; index < this.workingBoxes.Count; index++ )
				{
					var item = new ListViewItem( new[] { "", "", "", "" } );
					ApplyWalkBoxListValues( item, index, this.workingBoxes[index] );
					this.listViewWalkBoxes.Items.Add( item );
				}
				this.listViewWalkBoxes.EndUpdate();
			}
			finally
			{
				this.suppressUiEvents = false;
			}

			this.tabPageWalkBoxes.Text = this.workingBoxes.Count == 0
				? "Walk boxes"
				: string.Concat( "Walk boxes (", this.workingBoxes.Count, ")" );
		}

		private static void ApplyWalkBoxListValues( ListViewItem item, int index, ClassicBox box )
		{
			item.SubItems[0].Text = index.ToString();
			item.SubItems[1].Text = box.Mask.ToString();
			item.SubItems[2].Text = box.IsWalkable ? "yes" : "no";
			item.SubItems[3].Text = box.Scale.ToString();
		}

		/// <summary>
		/// Rewrites the list rows in place after a box attribute changed (an edit or an
		/// undo/redo); the row count never changes outside <see cref="BuildWalkBoxes"/>.
		/// </summary>
		private void RefreshWalkBoxListValues()
		{
			for( var index = 0; index < this.listViewWalkBoxes.Items.Count && index < this.workingBoxes.Count; index++ )
			{
				ApplyWalkBoxListValues( this.listViewWalkBoxes.Items[index], index, this.workingBoxes[index] );
			}
		}

		/// <summary>
		/// Selects the clicked walk box in the preview, turning the overlay on first so the
		/// selection is visible.
		/// </summary>
		private void HandleWalkBoxListSelectionChanged( object sender, EventArgs args )
		{
			if( this.suppressUiEvents )
			{
				return;
			}

			var index = this.listViewWalkBoxes.SelectedIndices.Count > 0 ? this.listViewWalkBoxes.SelectedIndices[0] : -1;
			var walkBox = index >= 0 && index < this.roomPreviewControl.WalkBoxes.Count
				? this.roomPreviewControl.WalkBoxes[index]
				: null;
			if( walkBox != null && this.checkBoxWalkBoxes.Enabled && !this.checkBoxWalkBoxes.Checked )
			{
				this.checkBoxWalkBoxes.Checked = true;
			}
			this.roomPreviewControl.SelectedWalkBox = walkBox;
			this.roomPreviewControl.Invalidate();
		}

		private void HandleWalkBoxesCheckedChanged( object sender, EventArgs args )
		{
			if( this.suppressUiEvents )
			{
				return;
			}
			var on = this.checkBoxWalkBoxes.Checked;
			this.roomPreviewControl.ShowWalkBoxes = on;
			if( !on )
			{
				// don't leave a hidden box selected, eating arrow keys
				this.roomPreviewControl.SelectedWalkBox = null;
			}
			this.roomPreviewControl.Invalidate();
		}

		private void HandleWalkBoxSelectionChanged( object? sender, EventArgs args )
		{
			this.walkBoxUndoStack.BreakCoalescing();
			this.UpdateWalkBoxEditors();
			if( this.roomPreviewControl.SelectedWalkBox != null )
			{
				// reveal the list selection the preview click just made
				this.tabControlRoom.SelectedTab = this.tabPageWalkBoxes;
			}
		}

		private void UpdateWalkBoxEditors()
		{
			var selectedWalkBox = this.roomPreviewControl.SelectedWalkBox;
			var box = selectedWalkBox?.Box;
			this.suppressUiEvents = true;
			try
			{
				var enabled = box != null;
				this.numericBoxMask.Enabled = enabled;
				this.checkBoxBoxWalkable.Enabled = enabled;
				this.numericBoxScale.Enabled = enabled;
				if( box != null )
				{
					this.numericBoxMask.Value = Clamp( box.Mask, this.numericBoxMask );
					this.checkBoxBoxWalkable.Checked = box.IsWalkable;
					this.numericBoxScale.Value = Clamp( box.Scale, this.numericBoxScale );
				}

				// keep the list selection in step with the preview selection
				var selectedIndex = selectedWalkBox?.Index ?? -1;
				if( selectedIndex >= 0 && selectedIndex < this.listViewWalkBoxes.Items.Count )
				{
					this.listViewWalkBoxes.Items[selectedIndex].Selected = true;
					this.listViewWalkBoxes.EnsureVisible( selectedIndex );
				}
				else
				{
					this.listViewWalkBoxes.SelectedItems.Clear();
				}
			}
			finally
			{
				this.suppressUiEvents = false;
			}
		}

		private void HandleWalkBoxAttributeChanged( object sender, EventArgs args )
		{
			if( this.suppressUiEvents )
			{
				return;
			}
			var walkBox = this.roomPreviewControl.SelectedWalkBox;
			if( walkBox == null )
			{
				return;
			}
			var box = walkBox.Box;

			if( sender == this.numericBoxMask )
			{
				int oldValue = box.Mask, newValue = (int)this.numericBoxMask.Value;
				if( oldValue == newValue )
				{
					return;
				}
				box.Mask = newValue;
				this.RecordWalkBoxEdit( box, "BoxMask", "walk box mask", () => box.Mask = oldValue, () => box.Mask = newValue );
			}
			else if( sender == this.checkBoxBoxWalkable )
			{
				int oldFlags = box.Flags;
				int newFlags = this.checkBoxBoxWalkable.Checked ? ( box.Flags & ~0x80 ) : ( box.Flags | 0x80 );
				if( oldFlags == newFlags )
				{
					return;
				}
				box.Flags = newFlags;
				this.RecordWalkBoxEdit( box, "BoxWalkable", "walk box walkable", () => box.Flags = oldFlags, () => box.Flags = newFlags );
			}
			else if( sender == this.numericBoxScale )
			{
				int oldValue = box.Scale, newValue = (int)this.numericBoxScale.Value;
				if( oldValue == newValue )
				{
					return;
				}
				box.Scale = newValue;
				this.RecordWalkBoxEdit( box, "BoxScale", "walk box scale", () => box.Scale = oldValue, () => box.Scale = newValue );
			}

			this.RefreshWalkBoxListValues();
			this.RecomputeActorLayering();
			this.roomPreviewControl.Invalidate();
		}

		private void HandleWalkBoxEdited( object? sender, WalkBoxEditEventArgs args )
		{
			var changes = args.Changes;

			// a completed drag is one undo step
			this.walkBoxUndoStack.BreakCoalescing();
			this.RecordWalkBoxEdit(
				owner: null,
				key: "WalkBoxDrag",
				description: "move walk box",
				undo: () =>
				{
					foreach( var change in changes )
					{
						this.SetWorkingCorner( change.BoxIndex, change.CornerIndex, change.OldPoint );
					}
				},
				redo: () =>
				{
					foreach( var change in changes )
					{
						this.SetWorkingCorner( change.BoxIndex, change.CornerIndex, change.NewPoint );
					}
				} );

			this.RecomputeActorLayering();
			this.roomPreviewControl.Invalidate();
		}

		private void SetWorkingCorner( int boxIndex, int cornerIndex, Point point )
		{
			if( boxIndex >= 0 && boxIndex < this.workingBoxes.Count )
			{
				this.workingBoxes[boxIndex].CornerList[cornerIndex] = point;
			}
		}

		/// <summary>
		/// Records a walkbox edit on the walkbox undo stack and makes that stack the target of
		/// Ctrl+Z/Y. Undo/redo re-enable the overlay and re-select the edited box.
		/// </summary>
		private void RecordWalkBoxEdit( object? owner, string key, string description, Action undo, Action redo )
		{
			var selectedIndex = this.roomPreviewControl.SelectedWalkBox?.Index;
			this.lastActiveUndoStack = this.walkBoxUndoStack;
			this.walkBoxUndoStack.Record( new UndoableEdit
			{
				Owner = owner,
				Key = key,
				Description = description,
				Undo = undo,
				Redo = redo,
				Select = () =>
				{
					if( !this.checkBoxWalkBoxes.Checked )
					{
						this.suppressUiEvents = true;
						try
						{
							this.checkBoxWalkBoxes.Checked = true;
						}
						finally
						{
							this.suppressUiEvents = false;
						}
						this.roomPreviewControl.ShowWalkBoxes = true;
					}
					if( selectedIndex != null && selectedIndex.Value >= 0 && selectedIndex.Value < this.roomPreviewControl.WalkBoxes.Count )
					{
						this.roomPreviewControl.SelectedWalkBox = this.roomPreviewControl.WalkBoxes[selectedIndex.Value];
					}
				},
			} );
		}

		private void RefreshAfterWalkBoxUndoRedo()
		{
			this.UpdateWalkBoxEditors();
			this.RefreshWalkBoxListValues();
			this.RecomputeActorLayering();
			this.roomPreviewControl.Invalidate();
		}

		/// <summary>
		/// The z-plane mask an actor at the point would get from the working (edited) boxes, so
		/// the actor overlay's draw order reflects mask edits before they are saved. Mirrors
		/// <see cref="ClassicRoom.GetBoxMaskAt"/> over the working copy.
		/// </summary>
		private int GetWorkingBoxMaskAt( int x, int y )
		{
			ClassicBox? best = null;
			var bestDistance = double.MaxValue;
			foreach( var box in this.workingBoxes )
			{
				if( !box.IsWalkable )
				{
					continue;
				}
				var distance = box.DistanceTo( x, y );
				if( distance < bestDistance )
				{
					bestDistance = distance;
					best = box;
				}
			}
			return best?.Mask ?? 0;
		}

		private void RecomputeActorLayering()
		{
			foreach( var actor in this.roomPreviewControl.Actors )
			{
				actor.DrawAboveForeground = this.GetWorkingBoxMaskAt( actor.Placement.X, actor.Placement.Y ) == 0;
			}
		}

		private void HandleSaveWalkBoxesClick( object sender, EventArgs args )
		{
			this.TrySaveWalkBoxes();
		}

		private bool TrySaveWalkBoxes()
		{
			if( this.Room == null || this.classicRoom == null || this.classicData == null )
			{
				return false;
			}

			var result = new SaveWalkBoxesCommand( this.LPAKFile, this.classicData, this.Room.Header.Identifier, this.workingBoxes, this.originalBoxes ).Execute();
			if( !result.IsSuccess )
			{
				MessageBox.Show( this, result.Error, "Save walk boxes", MessageBoxButtons.OK, MessageBoxIcon.Error );
				return false;
			}

			// the file now holds the working boxes: sync the shared classic room and the snapshot
			// so a further save verifies against the new on-disk state, and drop the cache so a
			// reopen re-reads the loose override
			this.classicRoom.BoxList = this.workingBoxes.Select( box => box.Clone() ).ToList();
			this.originalBoxes = this.workingBoxes.Select( box => box.Clone() ).ToList();
			this.walkBoxUndoStack.MarkSaved();
			ClassicDataLocator.Invalidate( this.LPAKFile.FileNameOnDisk );
			return true;
		}

		private bool HandleWalkBoxNudge( KeyEventArgs args )
		{
			var walkBox = this.roomPreviewControl.SelectedWalkBox;
			if( walkBox == null )
			{
				return false;
			}

			var step = args.Shift ? 8 : 1;
			var deltaX = 0;
			var deltaY = 0;
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
					return false;
			}

			args.Handled = true;
			var box = walkBox.Box;
			var index = walkBox.Index;
			var oldCorners = (Point[])box.CornerList.Clone();
			for( var corner = 0; corner < box.CornerList.Length; corner++ )
			{
				box.CornerList[corner] = new Point( box.CornerList[corner].X + deltaX, box.CornerList[corner].Y + deltaY );
			}
			var newCorners = (Point[])box.CornerList.Clone();
			this.RecordWalkBoxEdit( box, "BoxNudge", "nudge walk box",
				() => { for( var corner = 0; corner < 4; corner++ ) this.SetWorkingCorner( index, corner, oldCorners[corner] ); },
				() => { for( var corner = 0; corner < 4; corner++ ) this.SetWorkingCorner( index, corner, newCorners[corner] ); } );
			this.RecomputeActorLayering();
			this.roomPreviewControl.Invalidate();
			return true;
		}

		//-------------------------------------------
		// redistributable walk box patch

		private const string ClassicPatchFilter = "Walk box patch (*.mi1classicpatch.xml)|*.mi1classicpatch.xml|XML files (*.xml)|*.xml";

		private static string ToolVersion()
		{
			return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";
		}

		private void HandleExportClassicPatch( object sender, EventArgs args )
		{
			// the patch is built from the saved classic data, so offer to save pending edits first
			if( this.walkBoxesDirty )
			{
				var answer = MessageBox.Show(
					this,
					"Save your walk box changes before exporting? The patch is built from the saved classic data.",
					"Export walk box patch",
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Question
				);
				if( answer == DialogResult.Cancel )
				{
					return;
				}
				if( answer == DialogResult.Yes && !this.TrySaveWalkBoxes() )
				{
					return;
				}
			}

			using( var dialog = new SaveFileDialog() )
			{
				dialog.Filter = ClassicPatchFilter;
				dialog.Title = "Export walk box patch";
				dialog.FileName = string.Concat( string.IsNullOrWhiteSpace( this.Room?.Header.Name ) ? "walkboxes" : this.Room!.Header.Name, ".mi1classicpatch.xml" );
				if( dialog.ShowDialog( this ) != DialogResult.OK )
				{
					return;
				}

				var info = new ClassicPatchInfo { Name = this.Room?.Header.Name ?? "" };
				var label = System.IO.Path.GetFileNameWithoutExtension( this.LPAKFile.FileNameOnDisk ) ?? "local";
				var result = new ExportClassicPatchCommand( this.LPAKFile, dialog.FileName, info, label, ToolVersion() ).Execute();
				MessageBox.Show(
					this,
					result.IsSuccess ? result.Value : result.Error,
					"Export walk box patch",
					MessageBoxButtons.OK,
					result.IsSuccess ? MessageBoxIcon.Information : MessageBoxIcon.Error
				);
			}
		}

		private void HandleApplyClassicPatch( object sender, EventArgs args )
		{
			using( var dialog = new OpenFileDialog() )
			{
				dialog.Filter = ClassicPatchFilter;
				dialog.Title = "Apply walk box patch";
				if( dialog.ShowDialog( this ) != DialogResult.OK )
				{
					return;
				}
				var result = new ApplyClassicPatchCommand( this.LPAKFile, dialog.FileName ).Execute();
				MessageBox.Show(
					this,
					result.IsSuccess ? result.Value : result.Error,
					"Apply walk box patch",
					MessageBoxButtons.OK,
					result.IsSuccess ? MessageBoxIcon.Information : MessageBoxIcon.Error
				);
			}
		}

		private void HandleRemoveClassicPatch( object sender, EventArgs args )
		{
			using( var dialog = new OpenFileDialog() )
			{
				dialog.Filter = ClassicPatchFilter;
				dialog.Title = "Remove walk box patch";
				if( dialog.ShowDialog( this ) != DialogResult.OK )
				{
					return;
				}
				var result = new RemoveClassicPatchCommand( this.LPAKFile, dialog.FileName ).Execute();
				MessageBox.Show(
					this,
					result.IsSuccess ? result.Value : result.Error,
					"Remove walk box patch",
					MessageBoxButtons.OK,
					result.IsSuccess ? MessageBoxIcon.Information : MessageBoxIcon.Error
				);
			}
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
