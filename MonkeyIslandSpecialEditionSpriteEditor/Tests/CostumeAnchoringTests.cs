using System.Drawing;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Entities;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;
using NUnit.Framework;
using CostumeRenderer = MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Renderer;

namespace Tests
{
	/// <summary>
	/// The game anchors each SE costume sprite bottom-center to the classic cel it
	/// replaces; a sprite's own ScreenX/ScreenY only matter when no classic cel is known.
	/// Verified against the retail data: costumes like the bar's pirate leaders carry
	/// ScreenY values ~15 classic pixels below their classic cels, yet sit at the classic
	/// position in-game.
	/// </summary>
	[TestFixture]
	public class CostumeAnchoringTests
	{
		private static readonly SizeF HdScale = new SizeF( 6.0f, 7.2f );

		private static CostumeSpritePlacement BuildPlacement( RectangleF screenRect, bool flipped, ClassicCel? classicCel )
		{
			var sprite = new Sprite(
				textureNumber: 0,
				textureX: 0,
				textureY: 0,
				textureWidth: (int)screenRect.Width,
				textureHeight: (int)screenRect.Height,
				screenX: screenRect.X,
				screenY: screenRect.Y,
				moveX: 0,
				moveY: 0,
				pathPointIndex: -1
			);
			var group = new SpriteGroup(
				spriteList: new System.Collections.Generic.List<Sprite> { sprite },
				identifier: 0,
				index: 0,
				firstSpriteIdentifier: 0
			);
			return new CostumeSpritePlacement(
				trackIndex: 0,
				spriteGroup: group,
				sprite: sprite,
				spriteIndex: 0,
				screenRect: screenRect,
				flipped: flipped,
				classicCel: classicCel
			);
		}

		[Test]
		public void GetAnchoredScreenRect_WithoutClassicCel_ReturnsTheSpriteRect()
		{
			var screenRect = new RectangleF( -50, -300, 100, 310 );
			var placement = BuildPlacement( screenRect, flipped: false, classicCel: null );

			var anchored = CostumeRenderer.GetAnchoredScreenRect( placement, HdScale );

			Assert.That( anchored, Is.EqualTo( screenRect ) );
		}

		[Test]
		public void GetAnchoredScreenRect_WithClassicCel_AnchorsBottomCenterToTheClassicRect()
		{
			// a classic cel 35x52 at relX 4, relY -57: bottom edge at -5, center x at 21.5
			var cel = new ClassicCel( index: 0, width: 35, height: 52, relX: 4, relY: -57, moveX: 0, moveY: 0 );

			// the SE sprite carries offsets ~15 classic pixels lower (like the pirate
			// leaders); its own rect must be ignored except for its size
			var screenRect = new RectangleF( -38.0f, -248.9f, 228, 322 );
			var placement = BuildPlacement( screenRect, flipped: false, classicCel: cel );

			var anchored = CostumeRenderer.GetAnchoredScreenRect( placement, HdScale );

			var classicRect = CostumeRenderer.GetClassicScreenRect( cel, flipped: false, hdScale: HdScale );
			Assert.That( anchored.Width, Is.EqualTo( screenRect.Width ) );
			Assert.That( anchored.Height, Is.EqualTo( screenRect.Height ) );
			Assert.That( anchored.Bottom, Is.EqualTo( classicRect.Bottom ).Within( 0.01f ), "bottom edges must coincide" );
			Assert.That( anchored.Left + anchored.Width / 2, Is.EqualTo( classicRect.Left + classicRect.Width / 2 ).Within( 0.01f ), "horizontal centers must coincide" );
		}

		[Test]
		public void GetAnchoredScreenRect_Flipped_UsesTheMirroredClassicRect()
		{
			var cel = new ClassicCel( index: 0, width: 20, height: 40, relX: 10, relY: -40, moveX: 0, moveY: 0 );
			var screenRect = new RectangleF( -200, -300, 130, 290 );
			var placement = BuildPlacement( screenRect, flipped: true, classicCel: cel );

			var anchored = CostumeRenderer.GetAnchoredScreenRect( placement, HdScale );

			var classicRect = CostumeRenderer.GetClassicScreenRect( cel, flipped: true, hdScale: HdScale );
			Assert.That( anchored.Bottom, Is.EqualTo( classicRect.Bottom ).Within( 0.01f ) );
			Assert.That( anchored.Left + anchored.Width / 2, Is.EqualTo( classicRect.Left + classicRect.Width / 2 ).Within( 0.01f ) );
		}
	}
}
