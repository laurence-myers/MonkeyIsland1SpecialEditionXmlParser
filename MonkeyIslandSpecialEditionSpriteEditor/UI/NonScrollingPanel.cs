using System.Drawing;
using System.Windows.Forms;

namespace MonkeyIslandSpecialEditionSpriteEditor.UI
{
	/// <summary>
	/// An AutoScroll panel that keeps its scroll position when a child control receives focus,
	/// instead of snapping the child's top-left corner into view. Used to host the oversized
	/// atlas/preview canvases, which grab focus on click so they can handle keyboard and wheel input.
	/// </summary>
	public class NonScrollingPanel : Panel
	{
		protected override Point ScrollToControl( Control activeControl )
		{
			// return the current scroll offset so focusing a child does not move the view
			return this.DisplayRectangle.Location;
		}
	}
}
