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

namespace MonkeyIsland1SpecialEditionXmlParser.UI
{
	public partial class RoomForm : Form
	{
		private static readonly List<RoomForm> instances = new List<RoomForm>();
		private readonly Dictionary<string, Image?> textureCache = new Dictionary<string, Image?>();
		private bool populatingGroupList;

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
			var placements = Renderer.ResolvePlacements( this.Room, classicRoom?.GetObjectsById(), Renderer.DefaultHdScale );

			this.roomPreviewControl.Background = background;
			this.roomPreviewControl.Sprites.Clear();
			this.roomPreviewControl.Sprites.AddRange(
				placements.Select( p => new RoomPreviewControlSprite( p, this.LoadTexture( p.Sprite.TextureFileName ) ) )
			);
			this.roomPreviewControl.RefreshContent();

			this.UpdateWarningLabel( classicData, classicRoom, placements.Count );
			this.PopulateGroupList();
		}

		private void UpdateWarningLabel( MonkeyIsland1SpecialEditionXmlParser.Formats.Scumm.Entities.ClassicData? classicData, MonkeyIsland1SpecialEditionXmlParser.Formats.Scumm.Entities.ClassicRoom? classicRoom, int placementCount )
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
