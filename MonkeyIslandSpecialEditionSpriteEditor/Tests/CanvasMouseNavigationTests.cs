using System.Drawing;
using System.Windows.Forms;
using MonkeyIslandSpecialEditionSpriteEditor.UI;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class CanvasMouseNavigationTests
	{
		[Test]
		public void ZoomAtAnchor_KeepsTheContentPointUnderTheAnchor()
		{
			// Arrange: a 1000x1000 canvas in a 200x200 AutoScroll panel, scrolled to (300,100)
			using( var panel = new Panel { AutoScroll = true, Size = new Size( 200, 200 ) } )
			{
				var canvas = new Control { Size = new Size( 1000, 1000 ), Location = new Point( 0, 0 ) };
				panel.Controls.Add( canvas );
				panel.CreateControl();
				panel.AutoScrollPosition = new Point( 300, 100 );
				Assert.That( canvas.Location, Is.EqualTo( new Point( -300, -100 ) ), "scroll setup" );

				var navigation = new CanvasMouseNavigation( canvas );

				// Act: zoom 1.0 -> 2.0 anchored at canvas client point (350,160), which sits at
				// panel point (50,60); the content pixel under it is (350,160)
				navigation.ZoomAtAnchor( new Point( 350, 160 ), 1.0f, () =>
				{
					canvas.Size = new Size( 2000, 2000 );
					return 2.0f;
				} );

				// Assert: that content pixel now maps to canvas (700,320) and must still sit at
				// panel point (50,60), so the scroll offset became (650,260)
				Assert.That( canvas.Location, Is.EqualTo( new Point( -650, -260 ) ) );
			}
		}

		[Test]
		public void ZoomAtAnchor_WithoutAScrollHost_StillAppliesTheZoom()
		{
			// Arrange: a parentless canvas (no ScrollableControl to adjust)
			var canvas = new Control { Size = new Size( 100, 100 ) };
			var applied = false;

			// Act
			var navigation = new CanvasMouseNavigation( canvas );
			navigation.ZoomAtAnchor( new Point( 10, 10 ), 1.0f, () =>
			{
				applied = true;
				return 2.0f;
			} );

			// Assert
			Assert.That( applied, Is.True );
		}
	}
}
