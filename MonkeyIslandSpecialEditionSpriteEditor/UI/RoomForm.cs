using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.Commands;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;
using Costume = MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Entities.Costume;
using CostumeRenderer = MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Renderer;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	public partial class RoomForm : Form
	{
		/// <summary>
		/// Matches SE costume pak entries; the number prefix is the classic costume number.
		/// </summary>
		private static readonly Regex costumeFileNameRegex = new Regex(
			@"^art/costumes/(\d+)_.*\.costume\.xml$", RegexOptions.IgnoreCase | RegexOptions.Compiled );

		private static readonly List<RoomForm> instances = new List<RoomForm>();
		private readonly Dictionary<string, Image?> textureCache = new Dictionary<string, Image?>();
		private readonly Dictionary<int, Costume?> costumeCache = new Dictionary<int, Costume?>();
		private bool populatingGroupList;
		private bool populatingActorList;

		private Room? Room
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

		public static RoomForm[] Instances
		{
			get
			{
				return RoomForm.instances.ToArray();
			}
		}

		public RoomForm( int fileIndex, LPAKFile lpakFile, Form mdiParent, FormWindowState windowState )
		{
			this.FileIndex = fileIndex;
			this.LPAKFile = lpakFile;
			this.MdiParent = mdiParent;
			this.WindowState = windowState;

			RoomForm.instances.Add( this );
			this.FormClosed += delegate { RoomForm.instances.Remove( this ); };

			this.InitializeComponent();

			this.Load += delegate { this.RenderRoom(); };
		}

		private void RenderRoom()
		{
			this.Room = this.LPAKFile.LoadRoom( this.FileIndex );

			if( this.Room == null )
			{
				return;
			}

			this.label1.Text = string.Concat( "Room ", this.Room.Header.Identifier, " - ", this.Room.Header.Name );

			// composite the background from the static sprites
			var background = Renderer.RenderBackground( this.Room, this.LoadTexture );

			// resolve object sprite positions from the classic SCUMM data
			var classicData = ClassicDataLocator.GetOrLoad( this.LPAKFile, this.PromptForClassicDataFolder );
			var classicRoom = classicData?.FindRoom( this.Room.Header.Identifier );
			var hdTransform = Renderer.GetHdTransform( this.Room, classicRoom );
			var placements = Renderer.ResolvePlacements( this.Room, classicRoom?.GetObjectsById(), hdTransform );

			this.roomPreviewControl.HdScale = hdTransform.Scale;
			this.roomPreviewControl.HdOrigin = hdTransform.Origin;
			this.roomPreviewControl.Background = background;
			this.roomPreviewControl.Foreground = Renderer.RenderForeground( this.Room, this.LoadTexture );
			this.roomPreviewControl.Sprites.Clear();
			this.roomPreviewControl.Sprites.AddRange(
				placements.Select( p => new RoomPreviewControlSprite( p, this.LoadTexture( p.Sprite.TextureFileName ) ) )
			);
			this.BuildActorOverlays( classicRoom, classicData );
			this.roomPreviewControl.RefreshContent();

			this.UpdateWarningLabel( classicData, classicRoom, placements.Count );
			this.PopulateGroupList();
			this.PopulateActorList();
		}

		/// <summary>
		/// Builds a costume overlay for every actor placement the classic scripts make in
		/// this room. The first placement of each actor (the room's initial setup) starts
		/// visible; later placements (mostly cutscene positions) start hidden.
		/// </summary>
		private void BuildActorOverlays( ClassicRoom? classicRoom, ClassicData? classicData )
		{
			this.roomPreviewControl.Actors.Clear();
			if( classicRoom == null )
			{
				return;
			}

			var visibleActors = new HashSet<int>();
			foreach( var placement in classicRoom.ActorPlacementList )
			{
				var overlay = this.BuildActorOverlay( placement, classicData );
				overlay.Visible = overlay.Image != null && visibleActors.Add( placement.ActorNumber );

				// the walkbox mask decides whether the game draws the actor in front of or
				// behind the foreground props (tables, counters)
				overlay.DrawAboveForeground = classicRoom.GetBoxMaskAt( placement.X, placement.Y ) == 0;
				this.roomPreviewControl.Actors.Add( overlay );
			}
		}

		private RoomPreviewControlActor BuildActorOverlay( ClassicActorPlacement placement, ClassicData? classicData )
		{
			Bitmap? image = null;
			var position = PointF.Empty;
			var costumeName = placement.CostumeId != null
				? string.Concat( "costume ", placement.CostumeId )
				: "costume unknown";

			if( placement.CostumeId != null )
			{
				var costume = this.LoadCostumeById( placement.CostumeId.Value );
				if( costume != null )
				{
					costumeName = costume.Header.Name;
					var classicCostume = classicData?.FindCostume( placement.CostumeId.Value );
					PointF origin;
					image = CostumeRenderer.RenderStandingActor( costume, placement.DirectionName, this.LoadTexture, classicCostume, out origin );
					if( image != null )
					{
						// the actor origin is its feet: classic position scaled to HD,
						// lifted by the elevation
						var originX = this.roomPreviewControl.HdOrigin.X + placement.X * this.roomPreviewControl.HdScale.Width;
						var originY = this.roomPreviewControl.HdOrigin.Y + ( placement.Y - ( placement.Elevation ?? 0 ) ) * this.roomPreviewControl.HdScale.Height;
						position = new PointF( originX - origin.X, originY - origin.Y );
					}
				}
			}

			var label = string.Concat(
				"actor ", placement.ActorNumber, ": ", costumeName,
				placement.CostumeInferred ? "?" : "",
				" (", placement.X, ",", placement.Y, ") ",
				placement.Source
			);
			return new RoomPreviewControlActor( placement, image, position, label );
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
					var match = RoomForm.costumeFileNameRegex.Match( fileName );
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
			this.populatingActorList = true;
			try
			{
				this.checkedListBoxActors.Items.Clear();
				foreach( var actor in this.roomPreviewControl.Actors )
				{
					this.checkedListBoxActors.Items.Add( actor.Label, actor.Visible );
				}
				this.panelActors.Visible = this.roomPreviewControl.Actors.Count > 0;
			}
			finally
			{
				this.populatingActorList = false;
			}
		}

		private void HandleActorItemCheck( object sender, ItemCheckEventArgs args )
		{
			if( this.populatingActorList )
			{
				return;
			}

			if( args.Index >= 0 && args.Index < this.roomPreviewControl.Actors.Count )
			{
				this.roomPreviewControl.Actors[args.Index].Visible = args.NewValue == CheckState.Checked;
				this.roomPreviewControl.Invalidate();
			}
		}

		private void UpdateWarningLabel( MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities.ClassicData? classicData, MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities.ClassicRoom? classicRoom, int placementCount )
		{
			if( classicData == null )
			{
				this.warningLabel.Text = "Classic SCUMM data (monkey1.000/001) not found - object sprites are placed by their offset only.";
				this.warningLabel.Visible = true;
			}
			else if( classicRoom == null && placementCount > 0 )
			{
				this.warningLabel.Text = string.Concat( "No classic room ", this.Room!.Header.Identifier, " in ", classicData.Source, " - object sprites are placed by their offset only." );
				this.warningLabel.Visible = true;
			}
			else
			{
				this.warningLabel.Visible = false;
			}
		}

		private void PopulateGroupList()
		{
			this.populatingGroupList = true;
			try
			{
				this.checkedListBoxGroups.Items.Clear();
				if( this.Room == null )
				{
					return;
				}

				for( var index = 0; index < this.Room.SpriteHeaderList.Count; index++ )
				{
					var spriteHeader = this.Room.SpriteHeaderList[index];
					var placement = this.roomPreviewControl.Sprites.FirstOrDefault( s => s.Placement.GroupIndex == index );
					var classicObject = placement?.Placement.ClassicObject;
					var text = string.Concat(
						index, ": id=", spriteHeader.Identifier,
						classicObject?.Name is { Length: > 0 } ? " " + classicObject.Name : "",
						placement != null && !placement.Placement.HasClassicMatch ? " [unplaced]" : ""
					);
					this.checkedListBoxGroups.Items.Add( text, true );
				}
			}
			finally
			{
				this.populatingGroupList = false;
			}
		}

		private void HandleGroupItemCheck( object sender, ItemCheckEventArgs args )
		{
			if( this.populatingGroupList )
			{
				return;
			}

			var visible = args.NewValue == CheckState.Checked;
			foreach( var sprite in this.roomPreviewControl.Sprites )
			{
				if( sprite.Placement.GroupIndex == args.Index )
				{
					sprite.Visible = visible;
				}
			}
			this.roomPreviewControl.Invalidate();
		}

		private void ToggleCalibrationOverlay( object sender, EventArgs args )
		{
			this.roomPreviewControl.ShowCalibrationOverlay = this.calibrationOverlayToolStripMenuItem.Checked;
			this.roomPreviewControl.Invalidate();
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

		private void OpenSpriteSheetEditor( object sender, EventArgs args )
		{
			var fileName = this.LPAKFile.PakFileNames[this.FileIndex].FileName;
			new OpenSpriteSheetEditorCommand( this.LPAKFile, fileName, this.FileIndex ).Execute();
		}

		private void ExportAsXml( object sender, EventArgs args )
		{
			if( this.Room == null )
			{
				return;
			}
			new ExportToXmlCommand( this.Room, string.Concat( this.Room.Header.Identifier, "_", this.Room.Header.Name, ".xml" ) ).Execute();
		}

		private void ExportAsMergedPng( object sender, EventArgs args )
		{
			if( this.Room == null )
			{
				return;
			}
			new ExportRoomToMergedPngWithDialogCommand( this.LPAKFile, this.Room ).Execute();
		}

		private void ExportAsPng( object sender, EventArgs args )
		{
			if( this.Room == null )
			{
				return;
			}
			new ExportRoomToPngWithDialogCommand( this.LPAKFile, this.Room ).Execute();
		}
	}
}
