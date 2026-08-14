using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	/// <summary>
	/// Draws a room the way the game composites it: the static background, the room object
	/// overlays, every object sprite at its resolved screen position ordered by layer, then
	/// the static foreground on top. Supports mouse wheel zoom, click selection with
	/// hit-testing and a calibration overlay that outlines the classic object rectangles
	/// the placements were derived from.
	/// </summary>
	public class RoomPreviewControl : Control
	{
		private readonly CanvasMouseNavigation mouseNavigation;
		private float zoom = 0.5f;
		private Bitmap? background;
		private Bitmap? foreground;
		private bool showForeground = true;
		private RoomPreviewControlSprite? selectedSprite;
		private RoomPreviewControlRoomObject? selectedRoomObject;

		private bool showWalkBoxes;
		private RoomPreviewControlWalkBox? selectedWalkBox;
		private const int WalkBoxHandlePixels = 6;
		private WalkBoxDragKind walkBoxDragKind = WalkBoxDragKind.None;
		private readonly List<(RoomPreviewControlWalkBox Box, int Corner, Point Original)> walkBoxDragCorners = new List<(RoomPreviewControlWalkBox, int, Point)>();
		private Point walkBoxDragStartClassic;
		private bool walkBoxDragMoved;

		private enum WalkBoxDragKind
		{
			None,
			Corner,
			Box,
		}

		public event EventHandler? SelectedSpriteChanged;
		public event EventHandler? SelectedRoomObjectChanged;
		public event EventHandler? SelectedWalkBoxChanged;
		public event EventHandler<WalkBoxEditEventArgs>? WalkBoxEdited;
		public event EventHandler? ZoomChanged;

		public RoomPreviewControl()
		{
			this.SetStyle( ControlStyles.AllPaintingInWmPaint, true );
			this.SetStyle( ControlStyles.UserPaint, true );
			this.SetStyle( ControlStyles.OptimizedDoubleBuffer, true );
			this.SetStyle( ControlStyles.Selectable, true );

			this.Sprites = new List<RoomPreviewControlSprite>();
			this.RoomObjects = new List<RoomPreviewControlRoomObject>();
			this.Actors = new List<RoomPreviewControlActor>();
			this.WalkBoxes = new List<RoomPreviewControlWalkBox>();
			this.HdScale = Renderer.DefaultHdScale;
			this.mouseNavigation = new CanvasMouseNavigation( this );
		}

		/// <summary>
		/// Gets or sets the composited background image, drawn below all sprites.
		/// </summary>
		public Bitmap? Background
		{
			get
			{
				return this.background;
			}
			set
			{
				this.background = value;
				this.UpdateContentSize();
				this.Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets the composited foreground image, drawn above all sprites the way the
		/// game draws its foreground static layers.
		/// </summary>
		public Bitmap? Foreground
		{
			get
			{
				return this.foreground;
			}
			set
			{
				this.foreground = value;
				this.Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether the foreground image is drawn. Hiding it helps editing sprites
		/// that the game partially covers.
		/// </summary>
		public bool ShowForeground
		{
			get
			{
				return this.showForeground;
			}
			set
			{
				this.showForeground = value;
				this.Invalidate();
			}
		}

		/// <summary>
		/// Gets the sprites to draw. Call <see cref="RefreshContent"/> after changing the list
		/// or any sprite's placement.
		/// </summary>
		public List<RoomPreviewControlSprite> Sprites
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the room object overlays, drawn between the background and the object
		/// sprites (where the game shows its Water and cover overlays). Call
		/// <see cref="RefreshContent"/> after changing the list.
		/// </summary>
		public List<RoomPreviewControlRoomObject> RoomObjects
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the costume actors to draw, the way the game shows them: above the object
		/// sprites but below the static foreground. Call <see cref="RefreshContent"/> after
		/// changing the list.
		/// </summary>
		public List<RoomPreviewControlActor> Actors
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets or sets the zoom factor applied to the whole room.
		/// </summary>
		public float Zoom
		{
			get
			{
				return this.zoom;
			}
			set
			{
				var clamped = Math.Max( 0.05f, Math.Min( 4.0f, value ) );
				if( clamped == this.zoom )
				{
					return;
				}
				this.zoom = clamped;
				this.UpdateContentSize();
				this.Invalidate();
				this.ZoomChanged?.Invoke( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Gets or sets whether the calibration overlay is drawn: a grid every 8 classic pixels
		/// and the classic object rectangle of every visible sprite.
		/// </summary>
		public bool ShowCalibrationOverlay
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the classic-to-HD scale used by the calibration overlay.
		/// </summary>
		public SizeF HdScale
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the HD position of the classic point (0, 0), used by the calibration
		/// overlay. Non-zero for fullscreen rooms whose art has widescreen margins around the
		/// centered classic view.
		/// </summary>
		public PointF HdOrigin
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the selected sprite; it is outlined and reported by hit-testing.
		/// </summary>
		public RoomPreviewControlSprite? SelectedSprite
		{
			get
			{
				return this.selectedSprite;
			}
			set
			{
				if( this.selectedSprite == value )
				{
					return;
				}
				this.selectedSprite = value;
				this.Invalidate();
				this.SelectedSpriteChanged?.Invoke( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Gets or sets the selected room object overlay; it is outlined (even while
		/// hidden, so an unchecked overlay can still be located and nudged).
		/// </summary>
		public RoomPreviewControlRoomObject? SelectedRoomObject
		{
			get
			{
				return this.selectedRoomObject;
			}
			set
			{
				if( this.selectedRoomObject == value )
				{
					return;
				}
				this.selectedRoomObject = value;
				this.Invalidate();
				this.SelectedRoomObjectChanged?.Invoke( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Gets the walkbox overlays. Call <see cref="RefreshContent"/> (or Invalidate) after
		/// changing the list.
		/// </summary>
		public List<RoomPreviewControlWalkBox> WalkBoxes
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets or sets whether the walkbox overlay is drawn and editable.
		/// </summary>
		public bool ShowWalkBoxes
		{
			get
			{
				return this.showWalkBoxes;
			}
			set
			{
				this.showWalkBoxes = value;
				this.Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets the selected walkbox; it is outlined and shows draggable corner handles.
		/// </summary>
		public RoomPreviewControlWalkBox? SelectedWalkBox
		{
			get
			{
				return this.selectedWalkBox;
			}
			set
			{
				if( this.selectedWalkBox == value )
				{
					return;
				}
				this.selectedWalkBox = value;
				this.Invalidate();
				this.SelectedWalkBoxChanged?.Invoke( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Recomputes the control size from the current content and repaints.
		/// </summary>
		public void RefreshContent()
		{
			this.UpdateContentSize();
			this.Invalidate();
		}

		/// <summary>
		/// The minimum texture alpha for a pixel to take a click. Keeps faint antialiased
		/// fringes from swallowing clicks meant for the art below.
		/// </summary>
		private const int HitTestMinimumAlpha = 16;

		/// <summary>
		/// Returns the topmost visible sprite at the given client point, or null. Hits test
		/// against the texture's alpha, not the bounding rectangle: a large, mostly
		/// transparent sprite (room 30's railing re-paint) does not swallow clicks meant
		/// for the art visible through it.
		/// </summary>
		public RoomPreviewControlSprite? HitTest( Point clientPoint )
		{
			var roomPoint = new PointF( clientPoint.X / this.zoom, clientPoint.Y / this.zoom );

			// walk from the topmost drawn sprite down; skip sprites that aren't painted
			foreach( var sprite in this.GetDrawOrder().Reverse() )
			{
				if( HitsSprite( sprite, roomPoint ) )
				{
					return sprite;
				}
			}
			return null;
		}

		/// <summary>
		/// Returns the topmost visible room object overlay at the given client point, or
		/// null. Like sprites, the overlay's draws are hit-tested against their texture
		/// alpha, so only painted pixels take the click.
		/// </summary>
		public RoomPreviewControlRoomObject? HitTestRoomObject( Point clientPoint )
		{
			var roomPoint = new PointF( clientPoint.X / this.zoom, clientPoint.Y / this.zoom );

			for( var index = this.RoomObjects.Count - 1; index >= 0; index-- )
			{
				var roomObject = this.RoomObjects[index];
				if( HitsRoomObject( roomObject, roomPoint ) )
				{
					return roomObject;
				}
			}
			return null;
		}

		/// <summary>
		/// Selects the item under the point the way repeated clicks burrow through a stack:
		/// the topmost hit when the current selection is not in the stack, otherwise the hit
		/// below the current selection, wrapping back to the top. The stack is the draw
		/// order top-down - sprites first, then the room object overlays under them - so
		/// items completely covered by other items stay reachable by clicking again.
		/// Nothing under the point clears the selection. Returns the selected item, or null.
		/// </summary>
		public object? CycleSelectionAt( Point clientPoint )
		{
			var stack = this.BuildHitStack( clientPoint );
			if( stack.Count == 0 )
			{
				this.SelectedRoomObject = null;
				this.SelectedSprite = null;
				return null;
			}

			var current = this.selectedSprite != null ? (object)this.selectedSprite : this.selectedRoomObject;
			var currentIndex = current == null ? -1 : stack.IndexOf( current );
			var next = stack[currentIndex < 0 ? 0 : ( currentIndex + 1 ) % stack.Count];

			if( next is RoomPreviewControlSprite sprite )
			{
				this.SelectedRoomObject = null;
				this.SelectedSprite = sprite;
			}
			else
			{
				this.SelectedSprite = null;
				this.SelectedRoomObject = (RoomPreviewControlRoomObject)next;
			}
			this.SelectedWalkBox = null;
			return next;
		}

		/// <summary>
		/// Collects every item whose painted pixels lie under the client point, topmost
		/// first: sprites in reverse draw order, then room object overlays.
		/// </summary>
		private List<object> BuildHitStack( Point clientPoint )
		{
			var roomPoint = new PointF( clientPoint.X / this.zoom, clientPoint.Y / this.zoom );

			var stack = new List<object>();
			foreach( var sprite in this.GetDrawOrder().Reverse() )
			{
				if( HitsSprite( sprite, roomPoint ) )
				{
					stack.Add( sprite );
				}
			}
			for( var index = this.RoomObjects.Count - 1; index >= 0; index-- )
			{
				if( HitsRoomObject( this.RoomObjects[index], roomPoint ) )
				{
					stack.Add( this.RoomObjects[index] );
				}
			}
			return stack;
		}

		private static bool HitsSprite( RoomPreviewControlSprite sprite, PointF roomPoint )
		{
			return sprite.Visible
				&& sprite.Texture != null
				&& sprite.Placement.ScreenRect.Contains( roomPoint )
				&& IsOpaqueAt( sprite.Texture, sprite.SourceRect, sprite.Placement.ScreenRect, roomPoint );
		}

		private static bool HitsRoomObject( RoomPreviewControlRoomObject roomObject, PointF roomPoint )
		{
			if( !roomObject.Visible )
			{
				return false;
			}
			foreach( var draw in roomObject.Draws )
			{
				if( !draw.Visible || draw.Texture == null )
				{
					continue;
				}
				var destRect = draw.RelativeRect;
				destRect.Offset( roomObject.RoomObject.OffsetX, roomObject.RoomObject.OffsetY );
				if( destRect.Contains( roomPoint ) && IsOpaqueAt( draw.Texture, draw.SourceRect, destRect, roomPoint ) )
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Samples the texture pixel a room point lands on and reports whether it is opaque
		/// enough to take a click. A texture that cannot be sampled (not a bitmap) counts
		/// as opaque, falling back to the plain rectangle test.
		/// </summary>
		private static bool IsOpaqueAt( Image texture, RectangleF sourceRect, RectangleF destRect, PointF roomPoint )
		{
			if( texture is not Bitmap bitmap || destRect.Width <= 0 || destRect.Height <= 0 )
			{
				return true;
			}

			var textureX = (int)( sourceRect.X + ( roomPoint.X - destRect.X ) * sourceRect.Width / destRect.Width );
			var textureY = (int)( sourceRect.Y + ( roomPoint.Y - destRect.Y ) * sourceRect.Height / destRect.Height );
			if( textureX < 0 || textureY < 0 || textureX >= bitmap.Width || textureY >= bitmap.Height )
			{
				// power-of-two padded textures can be smaller than the placement rect; the
				// out-of-texture region is never painted, so it takes no clicks
				return false;
			}
			return bitmap.GetPixel( textureX, textureY ).A >= HitTestMinimumAlpha;
		}

		protected override void OnMouseDown( MouseEventArgs args )
		{
			base.OnMouseDown( args );
			this.Focus();
			if( this.mouseNavigation.HandleMouseDown( args ) )
			{
				return;
			}
			if( args.Button == MouseButtons.Left )
			{
				// the walkbox overlay claims clicks near a box edge or the selected box's corners;
				// interior clicks fall through so sprites under the boxes stay selectable
				if( this.showWalkBoxes && this.HandleWalkBoxMouseDown( args ) )
				{
					return;
				}

				this.CycleSelectionAt( args.Location );
			}
		}

		protected override void OnMouseMove( MouseEventArgs args )
		{
			base.OnMouseMove( args );
			if( this.mouseNavigation.HandleMouseMove() )
			{
				return;
			}
			if( this.walkBoxDragKind != WalkBoxDragKind.None )
			{
				this.UpdateWalkBoxDrag( args );
			}
		}

		protected override void OnMouseUp( MouseEventArgs args )
		{
			base.OnMouseUp( args );
			if( this.mouseNavigation.HandleMouseUp( args ) )
			{
				return;
			}
			if( this.walkBoxDragKind != WalkBoxDragKind.None )
			{
				this.EndWalkBoxDrag();
			}
		}

		protected override void OnMouseWheel( MouseEventArgs args )
		{
			base.OnMouseWheel( args );

			// keep the wheel from also scrolling the hosting AutoScroll panel
			if( args is HandledMouseEventArgs handledArgs )
			{
				handledArgs.Handled = true;
			}

			this.mouseNavigation.ZoomAtAnchor( args.Location, this.zoom, () =>
			{
				this.Zoom = args.Delta > 0 ? this.zoom * 1.25f : this.zoom / 1.25f;
				return this.zoom;
			} );
		}

		protected override void OnPaint( PaintEventArgs args )
		{
			base.OnPaint( args );
			var graphics = args.Graphics;

			graphics.ClearWithTransparencyGrid();

			// fast, crisp scaling; the content is large and repainted often
			graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			graphics.PixelOffsetMode = PixelOffsetMode.Half;

			if( this.background != null )
			{
				var destRect = new RectangleF( 0, 0, this.background.Width * this.zoom, this.background.Height * this.zoom );
				var srcRect = new RectangleF( 0, 0, this.background.Width, this.background.Height );
				graphics.DrawImage( this.background, destRect, srcRect, GraphicsUnit.Pixel );
			}

			// room object overlays sit between the background and the object sprites (the
			// kitchen's Water lies behind the plank); their real order is per-name engine
			// logic, so this is the closest static approximation
			foreach( var roomObject in this.RoomObjects )
			{
				if( !roomObject.Visible )
				{
					continue;
				}
				foreach( var draw in roomObject.Draws )
				{
					if( !draw.Visible || draw.Texture == null )
					{
						continue;
					}
					var destRect = new RectangleF(
						( roomObject.RoomObject.OffsetX + draw.RelativeRect.X ) * this.zoom,
						( roomObject.RoomObject.OffsetY + draw.RelativeRect.Y ) * this.zoom,
						draw.RelativeRect.Width * this.zoom,
						draw.RelativeRect.Height * this.zoom
					);
					graphics.DrawImage( draw.Texture, destRect, draw.SourceRect, GraphicsUnit.Pixel );
				}
			}

			foreach( var sprite in this.GetDrawOrder() )
			{
				if( !sprite.Visible || sprite.Texture == null )
				{
					continue;
				}

				var screenRect = sprite.Placement.ScreenRect;
				var destRect = new RectangleF(
					screenRect.X * this.zoom,
					screenRect.Y * this.zoom,
					screenRect.Width * this.zoom,
					screenRect.Height * this.zoom
				);
				graphics.DrawImage( sprite.Texture, destRect, (RectangleF)sprite.SourceRect, GraphicsUnit.Pixel );
			}

			// actors on masked walkboxes go behind the foreground props, the others in front
			// (that is where their art expects the props: the pirate leaders' hands rest on
			// their table)
			this.PaintActors( graphics, drawAboveForeground: false );

			if( this.foreground != null && this.showForeground )
			{
				var destRect = new RectangleF( 0, 0, this.foreground.Width * this.zoom, this.foreground.Height * this.zoom );
				var srcRect = new RectangleF( 0, 0, this.foreground.Width, this.foreground.Height );
				graphics.DrawImage( this.foreground, destRect, srcRect, GraphicsUnit.Pixel );
			}

			this.PaintActors( graphics, drawAboveForeground: true );

			if( this.showWalkBoxes )
			{
				this.PaintWalkBoxes( graphics );
			}

			if( this.ShowCalibrationOverlay )
			{
				this.PaintCalibrationOverlay( graphics );
			}

			if( this.selectedSprite != null )
			{
				var screenRect = this.selectedSprite.Placement.ScreenRect;
				using( var pen = new Pen( Color.Red, 2 ) )
				{
					graphics.DrawRectangle(
						pen,
						screenRect.X * this.zoom,
						screenRect.Y * this.zoom,
						screenRect.Width * this.zoom,
						screenRect.Height * this.zoom
					);
				}
			}

			if( this.selectedRoomObject != null )
			{
				var screenRect = this.selectedRoomObject.ScreenRect;
				using( var pen = new Pen( Color.Red, 2 ) )
				{
					graphics.DrawRectangle(
						pen,
						screenRect.X * this.zoom,
						screenRect.Y * this.zoom,
						screenRect.Width * this.zoom,
						screenRect.Height * this.zoom
					);
				}
			}
		}

		protected override void OnPaintBackground( PaintEventArgs args )
		{
			// everything is painted in OnPaint over a transparency grid
		}

		private void PaintActors( Graphics graphics, bool drawAboveForeground )
		{
			foreach( var actor in this.Actors )
			{
				if( !actor.Visible || actor.Image == null || actor.DrawAboveForeground != drawAboveForeground )
				{
					continue;
				}

				var screenRect = actor.ScreenRect;
				var destRect = new RectangleF(
					screenRect.X * this.zoom,
					screenRect.Y * this.zoom,
					screenRect.Width * this.zoom,
					screenRect.Height * this.zoom
				);
				var srcRect = new RectangleF( 0, 0, actor.Image.Width, actor.Image.Height );
				graphics.DrawImage( actor.Image, destRect, srcRect, GraphicsUnit.Pixel );
			}
		}

		protected override bool IsInputKey( Keys keyData )
		{
			// let arrow keys reach KeyDown handlers (used for nudging in the editor)
			switch( keyData & Keys.KeyCode )
			{
				case Keys.Up:
				case Keys.Down:
				case Keys.Left:
				case Keys.Right:
					return true;
				default:
					return base.IsInputKey( keyData );
			}
		}

		private IEnumerable<RoomPreviewControlSprite> GetDrawOrder()
		{
			return this.Sprites.OrderBy( s => s.Placement.Sprite.Layer );
		}

		//-------------------------------------------
		// walkbox overlay

		private PointF ClassicToClient( Point classic )
		{
			return new PointF(
				( this.HdOrigin.X + classic.X * this.HdScale.Width ) * this.zoom,
				( this.HdOrigin.Y + classic.Y * this.HdScale.Height ) * this.zoom
			);
		}

		private Point ClientToClassic( Point client )
		{
			var scaleX = this.HdScale.Width == 0 ? 1 : this.HdScale.Width;
			var scaleY = this.HdScale.Height == 0 ? 1 : this.HdScale.Height;
			var classicX = ( client.X / this.zoom - this.HdOrigin.X ) / scaleX;
			var classicY = ( client.Y / this.zoom - this.HdOrigin.Y ) / scaleY;
			return new Point( (int)Math.Round( classicX ), (int)Math.Round( classicY ) );
		}

		private bool HandleWalkBoxMouseDown( MouseEventArgs args )
		{
			// a corner handle of the selected box starts a corner drag
			if( this.selectedWalkBox != null )
			{
				var corner = this.FindCornerHandle( this.selectedWalkBox, args.Location );
				if( corner >= 0 )
				{
					this.ClearSpriteAndRoomObjectSelection();
					this.BeginCornerDrag( this.selectedWalkBox, corner, detach: ( Control.ModifierKeys & Keys.Control ) != 0, args.Location );
					return true;
				}
			}

			// clicking near a box edge selects it (and arms a whole-box drag)
			var edgeBox = this.HitTestWalkBoxEdge( args.Location );
			if( edgeBox != null )
			{
				this.ClearSpriteAndRoomObjectSelection();
				this.SelectedWalkBox = edgeBox;
				this.BeginBoxDrag( edgeBox, args.Location );
				return true;
			}

			// clicking inside the already-selected box moves the whole box
			if( this.selectedWalkBox != null )
			{
				var classic = this.ClientToClassic( args.Location );
				if( this.selectedWalkBox.Box.Contains( classic.X, classic.Y ) )
				{
					this.ClearSpriteAndRoomObjectSelection();
					this.BeginBoxDrag( this.selectedWalkBox, args.Location );
					return true;
				}
			}

			return false;
		}

		// a walkbox and a sprite/room object are never selected at once, so the arrow keys and
		// the property editors act on exactly one thing
		private void ClearSpriteAndRoomObjectSelection()
		{
			this.SelectedSprite = null;
			this.SelectedRoomObject = null;
		}

		private int FindCornerHandle( RoomPreviewControlWalkBox box, Point clientPoint )
		{
			for( var corner = 0; corner < box.Box.CornerList.Length; corner++ )
			{
				var handle = this.ClassicToClient( box.Box.CornerList[corner] );
				var deltaX = handle.X - clientPoint.X;
				var deltaY = handle.Y - clientPoint.Y;
				if( deltaX * deltaX + deltaY * deltaY <= WalkBoxHandlePixels * WalkBoxHandlePixels )
				{
					return corner;
				}
			}
			return -1;
		}

		private RoomPreviewControlWalkBox? HitTestWalkBoxEdge( Point clientPoint )
		{
			var classic = this.ClientToClassic( clientPoint );

			// a client-pixel threshold in classic units; the smaller (X) scale is the more
			// forgiving, so use it
			var scale = Math.Min( this.HdScale.Width, this.HdScale.Height );
			var thresholdClassic = scale > 0 ? WalkBoxHandlePixels / ( this.zoom * scale ) : 0;

			// later boxes draw on top, so the last within the threshold wins
			RoomPreviewControlWalkBox? best = null;
			foreach( var walkBox in this.WalkBoxes )
			{
				if( walkBox.Box.DistanceToEdge( classic.X, classic.Y ) <= thresholdClassic )
				{
					best = walkBox;
				}
			}
			return best;
		}

		private void BeginCornerDrag( RoomPreviewControlWalkBox box, int corner, bool detach, Point clientPoint )
		{
			this.walkBoxDragKind = WalkBoxDragKind.Corner;
			this.walkBoxDragStartClassic = this.ClientToClassic( clientPoint );
			this.walkBoxDragMoved = false;
			this.walkBoxDragCorners.Clear();

			var grabbed = box.Box.CornerList[corner];
			if( detach )
			{
				this.walkBoxDragCorners.Add( ( box, corner, grabbed ) );
			}
			else
			{
				// move every coincident corner across all boxes together, so shared edges stay sealed
				foreach( var walkBox in this.WalkBoxes )
				{
					for( var index = 0; index < walkBox.Box.CornerList.Length; index++ )
					{
						if( walkBox.Box.CornerList[index] == grabbed )
						{
							this.walkBoxDragCorners.Add( ( walkBox, index, walkBox.Box.CornerList[index] ) );
						}
					}
				}
			}
		}

		private void BeginBoxDrag( RoomPreviewControlWalkBox box, Point clientPoint )
		{
			this.walkBoxDragKind = WalkBoxDragKind.Box;
			this.walkBoxDragStartClassic = this.ClientToClassic( clientPoint );
			this.walkBoxDragMoved = false;
			this.walkBoxDragCorners.Clear();
			for( var corner = 0; corner < box.Box.CornerList.Length; corner++ )
			{
				this.walkBoxDragCorners.Add( ( box, corner, box.Box.CornerList[corner] ) );
			}
		}

		private void UpdateWalkBoxDrag( MouseEventArgs args )
		{
			var classic = this.ClientToClassic( args.Location );
			var deltaX = classic.X - this.walkBoxDragStartClassic.X;
			var deltaY = classic.Y - this.walkBoxDragStartClassic.Y;
			if( deltaX == 0 && deltaY == 0 && !this.walkBoxDragMoved )
			{
				return;
			}

			foreach( var (box, corner, original) in this.walkBoxDragCorners )
			{
				box.Box.CornerList[corner] = new Point( original.X + deltaX, original.Y + deltaY );
			}
			if( deltaX != 0 || deltaY != 0 )
			{
				this.walkBoxDragMoved = true;
			}
			this.Invalidate();
		}

		private void EndWalkBoxDrag()
		{
			var changes = new List<WalkBoxCornerChange>();
			if( this.walkBoxDragMoved )
			{
				foreach( var (box, corner, original) in this.walkBoxDragCorners )
				{
					var current = box.Box.CornerList[corner];
					if( current != original )
					{
						changes.Add( new WalkBoxCornerChange( box.Index, corner, original, current ) );
					}
				}
			}

			this.walkBoxDragKind = WalkBoxDragKind.None;
			this.walkBoxDragCorners.Clear();
			this.walkBoxDragMoved = false;

			if( changes.Count > 0 )
			{
				this.WalkBoxEdited?.Invoke( this, new WalkBoxEditEventArgs( changes ) );
			}
		}

		private void PaintWalkBoxes( Graphics graphics )
		{
			using( var walkableFill = new SolidBrush( Color.FromArgb( 55, Color.LimeGreen ) ) )
			using( var blockedFill = new SolidBrush( Color.FromArgb( 55, Color.Red ) ) )
			using( var walkablePen = new Pen( Color.FromArgb( 210, Color.LimeGreen ) ) )
			using( var blockedPen = new Pen( Color.FromArgb( 210, Color.Red ) ) { DashStyle = DashStyle.Dash } )
			using( var labelBrush = new SolidBrush( Color.White ) )
			using( var labelBack = new SolidBrush( Color.FromArgb( 140, Color.Black ) ) )
			{
				foreach( var walkBox in this.WalkBoxes )
				{
					var points = walkBox.Box.CornerList.Select( this.ClassicToClient ).ToArray();
					var walkable = walkBox.Box.IsWalkable;
					graphics.FillPolygon( walkable ? walkableFill : blockedFill, points );
					graphics.DrawPolygon( walkable ? walkablePen : blockedPen, points );

					// a small label at the centroid: index and mask
					var centroidX = points.Average( p => p.X );
					var centroidY = points.Average( p => p.Y );
					var label = string.Concat( walkBox.Index, " m", walkBox.Box.Mask );
					var size = graphics.MeasureString( label, this.Font );
					graphics.FillRectangle( labelBack, centroidX - 1, centroidY - 1, size.Width + 2, size.Height + 2 );
					graphics.DrawString( label, this.Font, labelBrush, centroidX, centroidY );
				}

				if( this.selectedWalkBox != null && this.WalkBoxes.Contains( this.selectedWalkBox ) )
				{
					var points = this.selectedWalkBox.Box.CornerList.Select( this.ClassicToClient ).ToArray();
					using( var selectedPen = new Pen( Color.Yellow, 2 ) )
					{
						graphics.DrawPolygon( selectedPen, points );
					}
					using( var handleBrush = new SolidBrush( Color.Yellow ) )
					{
						foreach( var point in points )
						{
							graphics.FillRectangle( handleBrush, point.X - WalkBoxHandlePixels / 2.0f, point.Y - WalkBoxHandlePixels / 2.0f, WalkBoxHandlePixels, WalkBoxHandlePixels );
						}
					}
				}
			}
		}

		private void UpdateContentSize()
		{
			var width = this.background?.Width ?? 0;
			var height = this.background?.Height ?? 0;

			foreach( var sprite in this.Sprites )
			{
				var screenRect = sprite.Placement.ScreenRect;
				width = Math.Max( width, (int)Math.Ceiling( screenRect.Right ) );
				height = Math.Max( height, (int)Math.Ceiling( screenRect.Bottom ) );
			}

			foreach( var roomObject in this.RoomObjects )
			{
				if( !roomObject.Visible )
				{
					continue;
				}
				var screenRect = roomObject.ScreenRect;
				width = Math.Max( width, (int)Math.Ceiling( screenRect.Right ) );
				height = Math.Max( height, (int)Math.Ceiling( screenRect.Bottom ) );
			}

			foreach( var actor in this.Actors )
			{
				var screenRect = actor.ScreenRect;
				width = Math.Max( width, (int)Math.Ceiling( screenRect.Right ) );
				height = Math.Max( height, (int)Math.Ceiling( screenRect.Bottom ) );
			}

			var newSize = new Size(
				Math.Max( 50, (int)( width * this.zoom ) ),
				Math.Max( 50, (int)( height * this.zoom ) )
			);
			if( this.Size != newSize )
			{
				this.Size = newSize;
			}
		}

		private void PaintCalibrationOverlay( Graphics graphics )
		{
			var width = this.Width;
			var height = this.Height;

			// grid every 8 classic pixels (one SCUMM strip), anchored to the classic origin
			using( var gridPen = new Pen( Color.FromArgb( 80, Color.White ) ) )
			{
				var stepX = 8 * this.HdScale.Width * this.zoom;
				var stepY = 8 * this.HdScale.Height * this.zoom;
				if( stepX >= 4 )
				{
					var originX = this.HdOrigin.X * this.zoom;
					for( var x = originX - stepX * (float)Math.Floor( originX / stepX ); x < width; x += stepX )
					{
						graphics.DrawLine( gridPen, x, 0, x, height );
					}
				}
				if( stepY >= 4 )
				{
					var originY = this.HdOrigin.Y * this.zoom;
					for( var y = originY - stepY * (float)Math.Floor( originY / stepY ); y < height; y += stepY )
					{
						graphics.DrawLine( gridPen, 0, y, width, y );
					}
				}
			}

			// classic object rectangles of the visible sprites
			using( var classicPen = new Pen( Color.Cyan ) )
			{
				foreach( var sprite in this.Sprites )
				{
					var classicObject = sprite.Placement.ClassicObject;
					if( !sprite.Visible || classicObject == null )
					{
						continue;
					}

					graphics.DrawRectangle(
						classicPen,
						( this.HdOrigin.X + classicObject.X * this.HdScale.Width ) * this.zoom,
						( this.HdOrigin.Y + classicObject.Y * this.HdScale.Height ) * this.zoom,
						classicObject.Width * this.HdScale.Width * this.zoom,
						classicObject.Height * this.HdScale.Height * this.zoom
					);
				}
			}
		}
	}
}
