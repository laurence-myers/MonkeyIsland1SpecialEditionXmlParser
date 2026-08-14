using System.Drawing;
using MonkeyIslandSpecialEditionSpriteEditor.UI;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class RoomPreviewControlSelectionCyclingTests
	{
		[Test]
		public void CycleSelectionAt_NothingSelected_PicksTheTopmostSprite()
		{
			using( var control = RoomPreviewControlHitTestTests.MakeControl() )
			using( var texture = RoomPreviewControlHitTestTests.MakeSolidTexture( 20, 10 ) )
			{
				var below = RoomPreviewControlHitTestTests.MakeSprite( texture, screenX: 100, screenY: 100, layer: 0 );
				var above = RoomPreviewControlHitTestTests.MakeSprite( texture, screenX: 100, screenY: 100, layer: 1 );
				control.Sprites.Add( below );
				control.Sprites.Add( above );

				Assert.That( control.CycleSelectionAt( new Point( 105, 105 ) ), Is.SameAs( above ) );
				Assert.That( control.SelectedSprite, Is.SameAs( above ) );
			}
		}

		[Test]
		public void CycleSelectionAt_RepeatedClicks_BurrowThroughTheStackAndWrap()
		{
			using( var control = RoomPreviewControlHitTestTests.MakeControl() )
			using( var texture = RoomPreviewControlHitTestTests.MakeSolidTexture( 20, 10 ) )
			{
				var below = RoomPreviewControlHitTestTests.MakeSprite( texture, screenX: 100, screenY: 100, layer: 0 );
				var above = RoomPreviewControlHitTestTests.MakeSprite( texture, screenX: 100, screenY: 100, layer: 1 );
				control.Sprites.Add( below );
				control.Sprites.Add( above );

				var point = new Point( 105, 105 );
				Assert.That( control.CycleSelectionAt( point ), Is.SameAs( above ) );
				Assert.That( control.CycleSelectionAt( point ), Is.SameAs( below ) );
				Assert.That( control.CycleSelectionAt( point ), Is.SameAs( above ), "wraps back to the top" );
			}
		}

		[Test]
		public void CycleSelectionAt_ReachesRoomObjectsUnderTheSprites()
		{
			using( var control = RoomPreviewControlHitTestTests.MakeControl() )
			using( var texture = RoomPreviewControlHitTestTests.MakeSolidTexture( 20, 10 ) )
			using( var overlayTexture = RoomPreviewControlHitTestTests.MakeSolidTexture( 20, 10 ) )
			{
				var sprite = RoomPreviewControlHitTestTests.MakeSprite( texture, screenX: 100, screenY: 100, layer: 0 );
				var overlay = RoomPreviewControlHitTestTests.MakeRoomObject( overlayTexture, offsetX: 100, offsetY: 100 );
				control.Sprites.Add( sprite );
				control.RoomObjects.Add( overlay );

				var point = new Point( 105, 105 );
				Assert.That( control.CycleSelectionAt( point ), Is.SameAs( sprite ) );

				Assert.That( control.CycleSelectionAt( point ), Is.SameAs( overlay ) );
				Assert.That( control.SelectedRoomObject, Is.SameAs( overlay ) );
				Assert.That( control.SelectedSprite, Is.Null );

				Assert.That( control.CycleSelectionAt( point ), Is.SameAs( sprite ), "wraps from the overlay back to the sprite" );
				Assert.That( control.SelectedSprite, Is.SameAs( sprite ) );
				Assert.That( control.SelectedRoomObject, Is.Null );
			}
		}

		[Test]
		public void CycleSelectionAt_SelectionMadeElsewhere_ContinuesFromIt()
		{
			using( var control = RoomPreviewControlHitTestTests.MakeControl() )
			using( var texture = RoomPreviewControlHitTestTests.MakeSolidTexture( 20, 10 ) )
			{
				var bottom = RoomPreviewControlHitTestTests.MakeSprite( texture, screenX: 100, screenY: 100, layer: 0 );
				var middle = RoomPreviewControlHitTestTests.MakeSprite( texture, screenX: 100, screenY: 100, layer: 1 );
				var top = RoomPreviewControlHitTestTests.MakeSprite( texture, screenX: 100, screenY: 100, layer: 2 );
				control.Sprites.Add( bottom );
				control.Sprites.Add( middle );
				control.Sprites.Add( top );

				// e.g. picked from the tree
				control.SelectedSprite = middle;

				Assert.That( control.CycleSelectionAt( new Point( 105, 105 ) ), Is.SameAs( bottom ) );
			}
		}

		[Test]
		public void CycleSelectionAt_MissesEverything_ClearsTheSelection()
		{
			using( var control = RoomPreviewControlHitTestTests.MakeControl() )
			using( var texture = RoomPreviewControlHitTestTests.MakeSolidTexture( 20, 10 ) )
			{
				var sprite = RoomPreviewControlHitTestTests.MakeSprite( texture, screenX: 100, screenY: 100, layer: 0 );
				control.Sprites.Add( sprite );
				control.SelectedSprite = sprite;

				Assert.That( control.CycleSelectionAt( new Point( 300, 300 ) ), Is.Null );
				Assert.That( control.SelectedSprite, Is.Null );
				Assert.That( control.SelectedRoomObject, Is.Null );
			}
		}

		[Test]
		public void CycleSelectionAt_SkipsTransparentPixelsOfTheSpriteAbove()
		{
			using( var control = RoomPreviewControlHitTestTests.MakeControl() )
			using( var opaqueTexture = RoomPreviewControlHitTestTests.MakeSolidTexture( 20, 10 ) )
			using( var halfTexture = RoomPreviewControlHitTestTests.MakeHalfTransparentTexture( 20, 10 ) )
			{
				var below = RoomPreviewControlHitTestTests.MakeSprite( opaqueTexture, screenX: 100, screenY: 100, layer: 0 );
				var above = RoomPreviewControlHitTestTests.MakeSprite( halfTexture, screenX: 100, screenY: 100, layer: 1 );
				control.Sprites.Add( below );
				control.Sprites.Add( above );

				// the top sprite is transparent here, so the first click already lands below
				var point = new Point( 115, 105 );
				Assert.That( control.CycleSelectionAt( point ), Is.SameAs( below ) );
				Assert.That( control.CycleSelectionAt( point ), Is.SameAs( below ), "the only hit stays selected" );
			}
		}
	}
}
