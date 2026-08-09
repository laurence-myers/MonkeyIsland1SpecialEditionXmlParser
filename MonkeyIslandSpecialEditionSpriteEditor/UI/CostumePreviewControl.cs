using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	/// <summary>
	/// Shows one composited step of a costume animation: every track's sprite drawn relative
	/// to the actor origin (marked with a crosshair). Sprites can be selected by clicking;
	/// an optional overlay draws the classic SCUMM cel rectangles for calibration.
	/// </summary>
	public class CostumePreviewControl : Control
	{
		/// <summary>
		/// Extra canvas space around the sprites so small costumes stay clickable and the
		/// origin is never on the very edge.
		/// </summary>
		private const int CanvasMargin = 32;

		private readonly CanvasMouseNavigation mouseNavigation;
		private float zoom = 1.0f;
		private CostumePreviewControlSprite? selectedSprite;
		private RectangleF contentBounds = new RectangleF( -100, -200, 200, 232 );

		/// <summary>
		/// Raised when the user selects a sprite by clicking.
		/// </summary>
		public event EventHandler? SelectedSpriteChanged;

		public CostumePreviewControl()
		{
			this.SetStyle( ControlStyles.AllPaintingInWmPaint, true );
			this.SetStyle( ControlStyles.UserPaint, true );
			this.SetStyle( ControlStyles.OptimizedDoubleBuffer, true );
			this.SetStyle( ControlStyles.Selectable, true );

			this.Sprites = new List<CostumePreviewControlSprite>();
			this.mouseNavigation = new CanvasMouseNavigation( this );
		}

		/// <summary>
		/// Gets the sprites of the current animation step, in draw order.
		/// </summary>
		public List<CostumePreviewControlSprite> Sprites
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets or sets the classic-to-HD scale used by the calibration overlay.
		/// </summary>
		public SizeF HdScale
		{
			get;
			set;
		} = Renderer.DefaultHdScale;

		/// <summary>
		/// Gets or sets whether classic cel rectangles are drawn over the sprites.
		/// </summary>
		public bool ShowClassicOverlay
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets whether sprites are drawn where the game shows them - anchored
		/// bottom-center to their classic cel (see
		/// <see cref="Renderer.GetAnchoredScreenRect"/>) - rather than at their raw ScreenX/ScreenY.
		/// Sprites with no matching classic cel are unaffected (the anchored rect falls back to
		/// the raw rect). This matches the room preview and the in-game placement, so a Screen X/Y
		/// edit that the engine ignores does not appear to move the sprite here either.
		/// </summary>
		public bool AnchorToClassic
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a room image drawn under the sprites, positioned by
		/// <see cref="BackdropOffset"/>. Lets the costume be placed against the room it appears
		/// in. Null hides it.
		/// </summary>
		public Image? BackdropBelow
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a room image drawn over the sprites (the static foreground props the
		/// actor stands behind, e.g. a counter). Null when the actor draws in front of the
		/// foreground.
		/// </summary>
		public Image? BackdropAbove
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the backdrop's top-left corner in canvas coordinates (relative to the
		/// actor origin), so the actor's feet land on its classic placement in the room.
		/// </summary>
		public PointF BackdropOffset
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a uniform scale applied to the drawn sprites around the actor origin.
		/// 1.0 draws them at the costume's own scale (the usual case, and what the room preview
		/// shows). A backdrop from a fullscreen room - whose art is smaller than the costume
		/// scale - sets this below 1 so the actor sits at the room's size rather than oversized.
		/// The feet stay on the origin because the scale is about it.
		/// </summary>
		public float ActorScale
		{
			get;
			set;
		} = 1.0f;

		/// <summary>
		/// Gets or sets a canvas area (relative to the actor origin) the control always
		/// covers, typically the union of every step of the current animation. This keeps
		/// the origin stationary while stepping through frames; without it the canvas would
		/// re-fit each frame and the animation would appear to jump around. The current
		/// frame's sprites still extend the canvas when they fall outside this area.
		/// </summary>
		public RectangleF? FixedBounds
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the selected sprite; it is outlined in red.
		/// </summary>
		public CostumePreviewControlSprite? SelectedSprite
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
		/// Gets or sets the zoom factor.
		/// </summary>
		public float Zoom
		{
			get
			{
				return this.zoom;
			}
			set
			{
				var clamped = Math.Max( 0.1f, Math.Min( 8.0f, value ) );
				if( clamped == this.zoom )
				{
					return;
				}
				this.zoom = clamped;
				this.UpdateContentSize();
				this.Invalidate();
			}
		}

		/// <summary>
		/// Recomputes the canvas bounds and repaints after the sprite list changed.
		/// </summary>
		public void RefreshContent()
		{
			// the canvas covers the fixed area, all sprites, their classic references and
			// the origin
			var bounds = new RectangleF( -CanvasMargin, -CanvasMargin, CanvasMargin * 2, CanvasMargin * 2 );
			if( this.FixedBounds != null )
			{
				bounds = RectangleF.Union( bounds, this.FixedBounds.Value );
			}
			foreach( var sprite in this.Sprites )
			{
				bounds = RectangleF.Union( bounds, this.GetDrawRect( sprite ) );
				if( sprite.Placement.ClassicCel != null )
				{
					bounds = RectangleF.Union( bounds, this.ApplyActorScale( Renderer.GetClassicScreenRect( sprite.Placement.ClassicCel, sprite.Placement.Flipped, this.HdScale ) ) );
				}
			}

			// the backdrop (when shown) usually dwarfs the sprites, so the canvas grows to the
			// room and the hosting panel scrolls over it
			var backdrop = this.BackdropBelow ?? this.BackdropAbove;
			if( backdrop != null )
			{
				bounds = RectangleF.Union( bounds, new RectangleF( this.BackdropOffset, backdrop.Size ) );
			}

			bounds.Inflate( CanvasMargin, CanvasMargin );
			this.contentBounds = bounds;

			this.UpdateContentSize();
			this.Invalidate();
		}

		/// <summary>
		/// Returns the topmost visible sprite under the point, or null.
		/// </summary>
		public CostumePreviewControlSprite? HitTest( Point clientPoint )
		{
			var canvasPoint = new PointF(
				clientPoint.X / this.zoom + this.contentBounds.X,
				clientPoint.Y / this.zoom + this.contentBounds.Y
			);

			// sprites later in the list draw on top, so search backwards
			for( var index = this.Sprites.Count - 1; index >= 0; index-- )
			{
				var sprite = this.Sprites[index];
				if( sprite.Visible && this.GetDrawRect( sprite ).Contains( canvasPoint ) )
				{
					return sprite;
				}
			}
			return null;
		}

		/// <summary>
		/// Returns the rectangle a sprite is drawn at, in canvas coordinates: its game-anchored
		/// position when <see cref="AnchorToClassic"/> is set, otherwise its raw ScreenX/ScreenY
		/// rectangle.
		/// </summary>
		private RectangleF GetDrawRect( CostumePreviewControlSprite sprite )
		{
			var rect = this.AnchorToClassic
				? Renderer.GetAnchoredScreenRect( sprite.Placement, this.HdScale )
				: sprite.Placement.ScreenRect;
			return this.ApplyActorScale( rect );
		}

		/// <summary>
		/// Scales a canvas rectangle about the actor origin by <see cref="ActorScale"/>.
		/// </summary>
		private RectangleF ApplyActorScale( RectangleF rect )
		{
			return this.ActorScale == 1.0f
				? rect
				: new RectangleF( rect.X * this.ActorScale, rect.Y * this.ActorScale, rect.Width * this.ActorScale, rect.Height * this.ActorScale );
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

		protected override bool IsInputKey( Keys keyData )
		{
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

		protected override void OnPaint( PaintEventArgs args )
		{
			base.OnPaint( args );
			var graphics = args.Graphics;

			graphics.ClearWithTransparencyGrid();
			graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			graphics.PixelOffsetMode = PixelOffsetMode.Half;

			this.PaintBackdrop( graphics, this.BackdropBelow );

			foreach( var sprite in this.Sprites )
			{
				if( !sprite.Visible || sprite.Texture == null )
				{
					continue;
				}

				var destRect = this.ToClient( this.GetDrawRect( sprite ) );
				var sourceRect = sprite.SourceRect;
				if( sprite.Placement.Flipped )
				{
					// draw mirrored by swapping the destination corners
					var destPoints = new[]
					{
						new PointF( destRect.Right, destRect.Top ),
						new PointF( destRect.Left, destRect.Top ),
						new PointF( destRect.Right, destRect.Bottom ),
					};
					graphics.DrawImage( sprite.Texture, destPoints, sourceRect, GraphicsUnit.Pixel );
				}
				else
				{
					graphics.DrawImage( sprite.Texture, destRect, sourceRect, GraphicsUnit.Pixel );
				}
			}

			this.PaintBackdrop( graphics, this.BackdropAbove );

			if( this.ShowClassicOverlay )
			{
				this.PaintClassicOverlay( graphics );
			}

			this.PaintOrigin( graphics );

			if( this.selectedSprite != null && this.selectedSprite.Visible )
			{
				var rect = this.ToClient( this.GetDrawRect( this.selectedSprite ) );
				using( var pen = new Pen( Color.Red, 2 ) )
				{
					graphics.DrawRectangle( pen, rect.X, rect.Y, rect.Width, rect.Height );
				}
			}
		}

		private void PaintBackdrop( Graphics graphics, Image? backdrop )
		{
			if( backdrop == null )
			{
				return;
			}
			var topLeft = this.ToClient( this.BackdropOffset );
			var destRect = new RectangleF( topLeft.X, topLeft.Y, backdrop.Width * this.zoom, backdrop.Height * this.zoom );
			var sourceRect = new RectangleF( 0, 0, backdrop.Width, backdrop.Height );
			graphics.DrawImage( backdrop, destRect, sourceRect, GraphicsUnit.Pixel );
		}

		protected override void OnPaintBackground( PaintEventArgs args )
		{
			// everything is painted in OnPaint over a transparency grid
		}

		private void PaintClassicOverlay( Graphics graphics )
		{
			using( var pen = new Pen( Color.FromArgb( 200, Color.Cyan ) ) )
			{
				pen.DashStyle = DashStyle.Dash;
				foreach( var sprite in this.Sprites )
				{
					if( !sprite.Visible || sprite.Placement.ClassicCel == null )
					{
						continue;
					}
					var rect = this.ToClient( this.ApplyActorScale( Renderer.GetClassicScreenRect( sprite.Placement.ClassicCel, sprite.Placement.Flipped, this.HdScale ) ) );
					graphics.DrawRectangle( pen, rect.X, rect.Y, rect.Width, rect.Height );
				}
			}
		}

		private void PaintOrigin( Graphics graphics )
		{
			var origin = this.ToClient( new PointF( 0, 0 ) );
			using( var pen = new Pen( Color.FromArgb( 220, Color.OrangeRed ) ) )
			{
				graphics.DrawLine( pen, origin.X - 12, origin.Y, origin.X + 12, origin.Y );
				graphics.DrawLine( pen, origin.X, origin.Y - 12, origin.X, origin.Y + 12 );
			}
		}

		private PointF ToClient( PointF canvasPoint )
		{
			return new PointF(
				( canvasPoint.X - this.contentBounds.X ) * this.zoom,
				( canvasPoint.Y - this.contentBounds.Y ) * this.zoom
			);
		}

		private RectangleF ToClient( RectangleF canvasRect )
		{
			return new RectangleF(
				( canvasRect.X - this.contentBounds.X ) * this.zoom,
				( canvasRect.Y - this.contentBounds.Y ) * this.zoom,
				canvasRect.Width * this.zoom,
				canvasRect.Height * this.zoom
			);
		}

		private void UpdateContentSize()
		{
			var newSize = new Size(
				Math.Max( 50, (int)( this.contentBounds.Width * this.zoom ) ),
				Math.Max( 50, (int)( this.contentBounds.Height * this.zoom ) )
			);
			if( this.Size != newSize )
			{
				this.Size = newSize;
			}
		}
	}
}
