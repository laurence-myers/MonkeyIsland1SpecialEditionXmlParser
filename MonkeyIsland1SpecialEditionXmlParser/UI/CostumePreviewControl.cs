using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes;

namespace MonkeyIsland1SpecialEditionXmlParser.UI
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
			// the canvas covers all sprites, their classic references and the origin
			var bounds = new RectangleF( -CanvasMargin, -CanvasMargin, CanvasMargin * 2, CanvasMargin * 2 );
			foreach( var sprite in this.Sprites )
			{
				bounds = RectangleF.Union( bounds, sprite.Placement.ScreenRect );
				if( sprite.Placement.ClassicCel != null )
				{
					bounds = RectangleF.Union( bounds, Renderer.GetClassicScreenRect( sprite.Placement.ClassicCel, sprite.Placement.Flipped, this.HdScale ) );
				}
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
				if( sprite.Visible && sprite.Placement.ScreenRect.Contains( canvasPoint ) )
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
			if( args.Button == MouseButtons.Left )
			{
				this.SelectedSprite = this.HitTest( args.Location );
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

			this.Zoom = args.Delta > 0 ? this.zoom * 1.25f : this.zoom / 1.25f;
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

			foreach( var sprite in this.Sprites )
			{
				if( !sprite.Visible || sprite.Texture == null )
				{
					continue;
				}

				var destRect = this.ToClient( sprite.Placement.ScreenRect );
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

			if( this.ShowClassicOverlay )
			{
				this.PaintClassicOverlay( graphics );
			}

			this.PaintOrigin( graphics );

			if( this.selectedSprite != null && this.selectedSprite.Visible )
			{
				var rect = this.ToClient( this.selectedSprite.Placement.ScreenRect );
				using( var pen = new Pen( Color.Red, 2 ) )
				{
					graphics.DrawRectangle( pen, rect.X, rect.Y, rect.Width, rect.Height );
				}
			}
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
					var rect = this.ToClient( Renderer.GetClassicScreenRect( sprite.Placement.ClassicCel, sprite.Placement.Flipped, this.HdScale ) );
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
