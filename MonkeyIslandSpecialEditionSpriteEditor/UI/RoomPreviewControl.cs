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
	/// Draws a room the way the game composites it: the static background, every object
	/// sprite at its resolved screen position ordered by layer, then the static foreground
	/// on top. Supports mouse wheel zoom, click selection with hit-testing and a calibration
	/// overlay that outlines the classic object rectangles the placements were derived from.
	/// </summary>
	public class RoomPreviewControl : Control
	{
		private readonly CanvasMouseNavigation mouseNavigation;
		private float zoom = 0.5f;
		private Bitmap? background;
		private Bitmap? foreground;
		private bool showForeground = true;
		private RoomPreviewControlSprite? selectedSprite;

		public event EventHandler? SelectedSpriteChanged;
		public event EventHandler? ZoomChanged;

		public RoomPreviewControl()
		{
			this.SetStyle( ControlStyles.AllPaintingInWmPaint, true );
			this.SetStyle( ControlStyles.UserPaint, true );
			this.SetStyle( ControlStyles.OptimizedDoubleBuffer, true );
			this.SetStyle( ControlStyles.Selectable, true );

			this.Sprites = new List<RoomPreviewControlSprite>();
			this.Actors = new List<RoomPreviewControlActor>();
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
		/// Recomputes the control size from the current content and repaints.
		/// </summary>
		public void RefreshContent()
		{
			this.UpdateContentSize();
			this.Invalidate();
		}

		/// <summary>
		/// Returns the topmost visible sprite at the given client point, or null.
		/// </summary>
		public RoomPreviewControlSprite? HitTest( Point clientPoint )
		{
			var roomPoint = new PointF( clientPoint.X / this.zoom, clientPoint.Y / this.zoom );

			// walk from the topmost drawn sprite down; skip sprites that aren't painted
			foreach( var sprite in this.GetDrawOrder().Reverse() )
			{
				if( sprite.Visible && sprite.Texture != null && sprite.Placement.ScreenRect.Contains( roomPoint ) )
				{
					return sprite;
				}
			}
			return null;
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
				this.SelectedSprite = this.HitTest( args.Location );
			}
		}

		protected override void OnMouseMove( MouseEventArgs args )
		{
			base.OnMouseMove( args );
			this.mouseNavigation.HandleMouseMove();
		}

		protected override void OnMouseUp( MouseEventArgs args )
		{
			base.OnMouseUp( args );
			this.mouseNavigation.HandleMouseUp( args );
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
