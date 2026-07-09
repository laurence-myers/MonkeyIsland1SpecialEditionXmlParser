using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	/// <summary>
	/// Draws a set of sprites stacked at the origin, scaled by <see cref="Zoom"/>. The control
	/// sizes itself to the zoomed content, so hosting it in an AutoScroll panel scrolls the view.
	/// Zooming snaps to <see cref="ZoomLevels"/> and keeps the content under the mouse in place.
	/// </summary>
	public class SpriteSetPreviewControl : Control
	{
		/// <summary>
		/// The zoom factors the control snaps to, smallest first.
		/// </summary>
		public static readonly float[] ZoomLevels = { 0.25f, 0.5f, 0.75f, 1.0f, 1.5f, 2.0f };

		/// <summary>
		/// The index into <see cref="ZoomLevels"/> of the 100% zoom the control starts at.
		/// </summary>
		public const int DefaultZoomLevelIndex = 3;

		// shown when there is nothing to draw, so the control still needs a usable size
		private const int EmptyContentSize = 50;

		private int zoomLevelIndex = SpriteSetPreviewControl.DefaultZoomLevelIndex;

		/// <summary>
		/// Raised after the zoom level changed, however it was changed.
		/// </summary>
		public event EventHandler? ZoomChanged;

		public List<SpriteSetPreviewControlSprite> Sprites
		{
			get;
			private set;
		}

		public SpriteSetPreviewControl()
		{
			this.SetStyle( ControlStyles.AllPaintingInWmPaint, true );
			this.SetStyle( ControlStyles.UserPaint, true );
			this.SetStyle( ControlStyles.OptimizedDoubleBuffer, true );
			this.SetStyle( ControlStyles.Selectable, true );

			this.Sprites = new List<SpriteSetPreviewControlSprite>();
		}

		/// <summary>
		/// Gets or sets the current zoom level as an index into <see cref="ZoomLevels"/>. Setting
		/// it zooms around the centre of the visible area; out-of-range values are clamped.
		/// </summary>
		public int ZoomLevelIndex
		{
			get
			{
				return this.zoomLevelIndex;
			}
			set
			{
				this.SetZoomLevelIndex( value, this.GetViewportCentre() );
			}
		}

		/// <summary>
		/// Gets the current zoom factor, where 1.0 is the texture's own resolution.
		/// </summary>
		public float Zoom
		{
			get
			{
				return SpriteSetPreviewControl.ZoomLevels[this.zoomLevelIndex];
			}
		}

		/// <summary>
		/// Gets the unzoomed size of the drawn content. The sprites are all stacked at the origin,
		/// so this is the largest of them.
		/// </summary>
		public Size GetContentSize()
		{
			var sprites = this.Sprites;
			if( sprites == null || sprites.Count == 0 )
			{
				return Size.Empty;
			}
			return new Size( sprites.Max( s => s.Image.Width ), sprites.Max( s => s.Image.Height ) );
		}

		/// <summary>
		/// Repaints and resizes the control after the sprite list changed.
		/// </summary>
		public void RefreshContent()
		{
			this.UpdateContentSize();
			this.Invalidate();
		}

		/// <summary>
		/// Snaps to the zoom level at <paramref name="index"/> (clamped to <see cref="ZoomLevels"/>),
		/// keeping the content pixel under <paramref name="anchor"/> under it afterwards.
		/// <paramref name="anchor"/> is in this control's client coordinates.
		/// </summary>
		public void SetZoomLevelIndex( int index, Point anchor )
		{
			var clamped = Math.Max( 0, Math.Min( SpriteSetPreviewControl.ZoomLevels.Length - 1, index ) );
			if( clamped == this.zoomLevelIndex )
			{
				return;
			}

			var oldZoom = this.Zoom;
			this.zoomLevelIndex = clamped;
			var newZoom = this.Zoom;

			// where the anchor sits in the scroll panel, and which content pixel is under it;
			// the control's own origin moves as the panel scrolls, so read it before resizing
			var scrollPanel = this.Parent as ScrollableControl;
			var anchorInPanel = new Point( anchor.X + this.Left, anchor.Y + this.Top );
			var contentX = anchor.X / oldZoom;
			var contentY = anchor.Y / oldZoom;

			this.UpdateContentSize();
			this.Invalidate();

			// put that same content pixel back under the anchor; the setter takes a positive
			// offset and clamps itself to the scroll range the resized control just established
			if( scrollPanel != null )
			{
				scrollPanel.AutoScrollPosition = new Point(
					(int)Math.Round( contentX * newZoom - anchorInPanel.X ),
					(int)Math.Round( contentY * newZoom - anchorInPanel.Y )
				);
			}

			this.ZoomChanged?.Invoke( this, EventArgs.Empty );
		}

		protected override void OnMouseDown( MouseEventArgs args )
		{
			base.OnMouseDown( args );

			// the wheel only reaches the focused control
			this.Focus();
		}

		protected override void OnMouseWheel( MouseEventArgs args )
		{
			base.OnMouseWheel( args );

			// keep the wheel from also scrolling the hosting AutoScroll panel
			if( args is HandledMouseEventArgs handledArgs )
			{
				handledArgs.Handled = true;
			}

			if( args.Delta == 0 )
			{
				return;
			}

			this.SetZoomLevelIndex( this.zoomLevelIndex + ( args.Delta > 0 ? 1 : -1 ), args.Location );
		}

		protected override void OnPaint( PaintEventArgs args )
		{
			base.OnPaint( args );
			var graphics = args.Graphics;

			graphics.ClearWithTransparencyGrid();

			var sprites = this.Sprites;
			if( sprites == null || sprites.Count == 0 )
			{
				graphics.DrawString( "NO SPRITES", this.Font, Brushes.Red, 0, 0 );
				return;
			}

			// fast, crisp scaling; textures are pixel art and the content is repainted while scrolling
			graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			graphics.PixelOffsetMode = PixelOffsetMode.Half;

			var zoom = this.Zoom;
			foreach( var sprite in sprites.OrderBy( s => s.Layer ) )
			{
				var destRect = new RectangleF( 0, 0, sprite.Image.Width * zoom, sprite.Image.Height * zoom );
				var srcRect = new RectangleF( 0, 0, sprite.Image.Width, sprite.Image.Height );
				graphics.DrawImage( sprite.Image, destRect, srcRect, GraphicsUnit.Pixel );
			}
		}

		protected override void OnPaintBackground( PaintEventArgs args )
		{
			// everything is painted in OnPaint over a transparency grid
		}

		/// <summary>
		/// The centre of the visible part of the control, in the control's own coordinates. Used as
		/// the anchor when the zoom is changed by something other than the mouse.
		/// </summary>
		private Point GetViewportCentre()
		{
			var scrollPanel = this.Parent as ScrollableControl;
			if( scrollPanel == null )
			{
				return new Point( this.Width / 2, this.Height / 2 );
			}

			return new Point(
				scrollPanel.ClientSize.Width / 2 - this.Left,
				scrollPanel.ClientSize.Height / 2 - this.Top
			);
		}

		private void UpdateContentSize()
		{
			var contentSize = this.GetContentSize();
			var zoom = this.Zoom;
			var newSize = new Size(
				Math.Max( SpriteSetPreviewControl.EmptyContentSize, (int)Math.Ceiling( contentSize.Width * zoom ) ),
				Math.Max( SpriteSetPreviewControl.EmptyContentSize, (int)Math.Ceiling( contentSize.Height * zoom ) )
			);
			if( this.Size != newSize )
			{
				this.Size = newSize;
			}
		}
	}
}
