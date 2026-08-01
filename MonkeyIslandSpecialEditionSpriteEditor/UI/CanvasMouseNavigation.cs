using System;
using System.Drawing;
using System.Windows.Forms;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	/// <summary>
	/// Shared mouse navigation for the oversized canvas controls (the atlas view and the
	/// previews). Those controls size themselves to their zoomed content and are hosted in
	/// an AutoScroll panel, so holding the middle mouse button pans by moving the panel's
	/// scroll position, and zooming keeps the content point under the cursor stationary by
	/// re-scrolling after the canvas resized.
	/// </summary>
	public sealed class CanvasMouseNavigation( Control canvas )
	{
		private readonly Control canvas = canvas;
		private bool panning;
		private Point panStartScreen;
		private Point panStartScroll;
		private Cursor? cursorBeforePan;

		private ScrollableControl? ScrollPanel
		{
			get
			{
				return this.canvas.Parent as ScrollableControl;
			}
		}

		/// <summary>
		/// Starts a pan when the middle mouse button went down; returns whether it did.
		/// </summary>
		public bool HandleMouseDown( MouseEventArgs args )
		{
			var scrollPanel = this.ScrollPanel;
			if( args.Button != MouseButtons.Middle || scrollPanel == null )
			{
				return false;
			}

			// track the pan in screen coordinates: the canvas itself moves while scrolling
			this.panning = true;
			this.panStartScreen = Control.MousePosition;
			this.panStartScroll = new Point( -scrollPanel.AutoScrollPosition.X, -scrollPanel.AutoScrollPosition.Y );
			this.cursorBeforePan = this.canvas.Cursor;
			this.canvas.Cursor = Cursors.SizeAll;
			return true;
		}

		/// <summary>
		/// Scrolls the hosting panel while a pan is active; returns whether one is.
		/// </summary>
		public bool HandleMouseMove()
		{
			if( !this.panning )
			{
				return false;
			}

			var scrollPanel = this.ScrollPanel;
			if( scrollPanel != null )
			{
				// drag the content along with the mouse; the setter takes a positive offset
				// and clamps itself to the scroll range
				var mouse = Control.MousePosition;
				scrollPanel.AutoScrollPosition = new Point(
					this.panStartScroll.X - ( mouse.X - this.panStartScreen.X ),
					this.panStartScroll.Y - ( mouse.Y - this.panStartScreen.Y )
				);
			}
			return true;
		}

		/// <summary>
		/// Ends a pan when the middle mouse button came up; returns whether one was active.
		/// </summary>
		public bool HandleMouseUp( MouseEventArgs args )
		{
			if( args.Button != MouseButtons.Middle || !this.panning )
			{
				return false;
			}
			this.panning = false;
			this.canvas.Cursor = this.cursorBeforePan ?? Cursors.Default;
			return true;
		}

		/// <summary>
		/// Applies a zoom change while keeping the content point under <paramref name="anchor"/>
		/// (in canvas client coordinates) stationary on screen. <paramref name="applyZoom"/>
		/// performs the actual zoom, resizing the canvas, and returns the new zoom factor.
		/// </summary>
		public void ZoomAtAnchor( Point anchor, float oldZoom, Func<float> applyZoom )
		{
			// where the anchor sits in the scroll panel, and which content pixel is under it;
			// the canvas's own origin moves as the panel scrolls, so read it before resizing
			var scrollPanel = this.ScrollPanel;
			var anchorInPanel = new Point( anchor.X + this.canvas.Left, anchor.Y + this.canvas.Top );
			var contentX = anchor.X / oldZoom;
			var contentY = anchor.Y / oldZoom;

			var newZoom = applyZoom();
			if( scrollPanel == null || newZoom == oldZoom )
			{
				return;
			}

			// put that same content pixel back under the anchor; the setter takes a positive
			// offset and clamps itself to the scroll range the resized canvas just established
			scrollPanel.AutoScrollPosition = new Point(
				(int)Math.Round( contentX * newZoom - anchorInPanel.X ),
				(int)Math.Round( contentY * newZoom - anchorInPanel.Y )
			);
		}
	}
}
