using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.Formats;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	/// <summary>
	/// Shows a spritesheet texture with the source rectangle of every sprite drawn on top.
	/// Sprites can be selected by clicking, and the selected rectangle can be moved by
	/// dragging or nudged with the arrow keys (hold Shift for steps of 10).
	/// </summary>
	public class AtlasViewControl : Control
	{
		private readonly CanvasMouseNavigation mouseNavigation;
		private Image? texture;
		private IAtlasSprite? selectedSprite;
		private float zoom = 0.5f;
		private bool dragging;
		private Point dragStart;
		private Point dragStartRectLocation;

		/// <summary>
		/// Raised when the user selects a sprite by clicking.
		/// </summary>
		public event EventHandler? SelectedSpriteChanged;

		/// <summary>
		/// Raised after the selected sprite's rectangle was moved by dragging or arrow keys.
		/// </summary>
		public event EventHandler? SpriteRectChanged;

		public AtlasViewControl()
		{
			this.SetStyle( ControlStyles.AllPaintingInWmPaint, true );
			this.SetStyle( ControlStyles.UserPaint, true );
			this.SetStyle( ControlStyles.OptimizedDoubleBuffer, true );
			this.SetStyle( ControlStyles.Selectable, true );

			this.Sprites = new List<IAtlasSprite>();
			this.mouseNavigation = new CanvasMouseNavigation( this );
		}

		/// <summary>
		/// Gets or sets the spritesheet texture to show.
		/// </summary>
		public Image? Texture
		{
			get
			{
				return this.texture;
			}
			set
			{
				this.texture = value;
				this.UpdateContentSize();
				this.Invalidate();
			}
		}

		/// <summary>
		/// Gets the sprites whose source rectangles are drawn over the texture.
		/// </summary>
		public List<IAtlasSprite> Sprites
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets or sets the selected sprite; its rectangle is highlighted and can be moved.
		/// </summary>
		public IAtlasSprite? SelectedSprite
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
		/// Repaints and resizes the control after the sprite list or texture changed.
		/// </summary>
		public void RefreshContent()
		{
			this.UpdateContentSize();
			this.Invalidate();
		}

		protected override void OnMouseDown( MouseEventArgs args )
		{
			base.OnMouseDown( args );
			this.Focus();
			if( this.mouseNavigation.HandleMouseDown( args ) )
			{
				return;
			}
			if( args.Button != MouseButtons.Left )
			{
				return;
			}

			var atlasPoint = new Point( (int)( args.X / this.zoom ), (int)( args.Y / this.zoom ) );

			// keep the current selection when clicking inside it, so it can be dragged even
			// when overlapped by other rectangles
			var hit = this.selectedSprite != null && GetRect( this.selectedSprite ).Contains( atlasPoint )
				? this.selectedSprite
				: this.HitTest( atlasPoint );

			this.SelectedSprite = hit;

			if( hit != null )
			{
				this.dragging = true;
				this.dragStart = atlasPoint;
				this.dragStartRectLocation = new Point( hit.TextureX, hit.TextureY );
			}
		}

		protected override void OnMouseMove( MouseEventArgs args )
		{
			base.OnMouseMove( args );
			if( this.mouseNavigation.HandleMouseMove() )
			{
				return;
			}
			if( !this.dragging || this.selectedSprite == null )
			{
				return;
			}

			var atlasPoint = new Point( (int)( args.X / this.zoom ), (int)( args.Y / this.zoom ) );
			var newX = this.dragStartRectLocation.X + ( atlasPoint.X - this.dragStart.X );
			var newY = this.dragStartRectLocation.Y + ( atlasPoint.Y - this.dragStart.Y );
			this.MoveSelectedRect( newX, newY );
		}

		protected override void OnMouseUp( MouseEventArgs args )
		{
			base.OnMouseUp( args );
			if( this.mouseNavigation.HandleMouseUp( args ) )
			{
				return;
			}
			this.dragging = false;
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

		protected override void OnKeyDown( KeyEventArgs args )
		{
			base.OnKeyDown( args );
			if( this.selectedSprite == null )
			{
				return;
			}

			var step = args.Shift ? 10 : 1;
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
					return;
			}

			args.Handled = true;
			this.MoveSelectedRect( this.selectedSprite.TextureX + deltaX, this.selectedSprite.TextureY + deltaY );
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

			if( this.texture != null )
			{
				var destRect = new RectangleF( 0, 0, this.texture.Width * this.zoom, this.texture.Height * this.zoom );
				var srcRect = new RectangleF( 0, 0, this.texture.Width, this.texture.Height );
				graphics.DrawImage( this.texture, destRect, srcRect, GraphicsUnit.Pixel );
			}

			using( var pen = new Pen( Color.FromArgb( 160, Color.Yellow ) ) )
			{
				foreach( var sprite in this.Sprites )
				{
					if( sprite == this.selectedSprite )
					{
						continue;
					}
					var rect = GetRect( sprite );
					graphics.DrawRectangle( pen, rect.X * this.zoom, rect.Y * this.zoom, rect.Width * this.zoom, rect.Height * this.zoom );
				}
			}

			if( this.selectedSprite != null )
			{
				var rect = GetRect( this.selectedSprite );
				using( var pen = new Pen( Color.Red, 2 ) )
				{
					graphics.DrawRectangle( pen, rect.X * this.zoom, rect.Y * this.zoom, rect.Width * this.zoom, rect.Height * this.zoom );
				}
			}
		}

		protected override void OnPaintBackground( PaintEventArgs args )
		{
			// everything is painted in OnPaint over a transparency grid
		}

		private static Rectangle GetRect( IAtlasSprite sprite )
		{
			return new Rectangle( sprite.TextureX, sprite.TextureY, sprite.TextureWidth, sprite.TextureHeight );
		}

		private IAtlasSprite? HitTest( Point atlasPoint )
		{
			// prefer the smallest rectangle under the cursor so overlapped sprites stay reachable
			return this.Sprites
				.Where( s => GetRect( s ).Contains( atlasPoint ) )
				.OrderBy( s => s.TextureWidth * s.TextureHeight )
				.FirstOrDefault();
		}

		private void MoveSelectedRect( int newX, int newY )
		{
			if( this.selectedSprite == null )
			{
				return;
			}

			// clamp to the texture when one is loaded
			if( this.texture != null )
			{
				newX = Math.Max( 0, Math.Min( this.texture.Width - this.selectedSprite.TextureWidth, newX ) );
				newY = Math.Max( 0, Math.Min( this.texture.Height - this.selectedSprite.TextureHeight, newY ) );
			}

			if( newX == this.selectedSprite.TextureX && newY == this.selectedSprite.TextureY )
			{
				return;
			}

			this.selectedSprite.TextureX = newX;
			this.selectedSprite.TextureY = newY;
			this.Invalidate();
			this.SpriteRectChanged?.Invoke( this, EventArgs.Empty );
		}

		private void UpdateContentSize()
		{
			var width = this.texture?.Width ?? 0;
			var height = this.texture?.Height ?? 0;
			var newSize = new Size(
				Math.Max( 50, (int)( width * this.zoom ) ),
				Math.Max( 50, (int)( height * this.zoom ) )
			);
			if( this.Size != newSize )
			{
				this.Size = newSize;
			}
		}
	}
}
