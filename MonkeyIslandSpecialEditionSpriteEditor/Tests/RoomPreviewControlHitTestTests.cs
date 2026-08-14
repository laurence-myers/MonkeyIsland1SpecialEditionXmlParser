using System.Drawing;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities;
using MonkeyIslandSpecialEditionSpriteEditor.UI;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class RoomPreviewControlHitTestTests
	{
		[Test]
		public void HitTest_OpaquePixel_ReturnsTheSprite()
		{
			using( var control = MakeControl() )
			using( var texture = MakeHalfTransparentTexture( 20, 10 ) )
			{
				var sprite = MakeSprite( texture, screenX: 100, screenY: 100, layer: 0 );
				control.Sprites.Add( sprite );

				// the left half of the texture is opaque
				Assert.That( control.HitTest( new Point( 105, 105 ) ), Is.SameAs( sprite ) );
			}
		}

		[Test]
		public void HitTest_TransparentPixel_FallsThroughToTheSpriteBelow()
		{
			using( var control = MakeControl() )
			using( var opaqueTexture = MakeSolidTexture( 20, 10 ) )
			using( var halfTexture = MakeHalfTransparentTexture( 20, 10 ) )
			{
				var below = MakeSprite( opaqueTexture, screenX: 100, screenY: 100, layer: 0 );
				var above = MakeSprite( halfTexture, screenX: 100, screenY: 100, layer: 1 );
				control.Sprites.Add( below );
				control.Sprites.Add( above );

				// the right half of the top texture is transparent, so the click lands below
				Assert.That( control.HitTest( new Point( 115, 105 ) ), Is.SameAs( below ) );
				Assert.That( control.HitTest( new Point( 105, 105 ) ), Is.SameAs( above ) );
			}
		}

		[Test]
		public void HitTest_TransparentPixelWithNothingBelow_ReturnsNull()
		{
			using( var control = MakeControl() )
			using( var halfTexture = MakeHalfTransparentTexture( 20, 10 ) )
			{
				control.Sprites.Add( MakeSprite( halfTexture, screenX: 100, screenY: 100, layer: 0 ) );

				Assert.That( control.HitTest( new Point( 115, 105 ) ), Is.Null );
			}
		}

		[Test]
		public void HitTest_HiddenSprite_IsSkipped()
		{
			using( var control = MakeControl() )
			using( var texture = MakeSolidTexture( 20, 10 ) )
			{
				var sprite = MakeSprite( texture, screenX: 100, screenY: 100, layer: 0 );
				sprite.Visible = false;
				control.Sprites.Add( sprite );

				Assert.That( control.HitTest( new Point( 105, 105 ) ), Is.Null );
			}
		}

		[Test]
		public void HitTestRoomObject_TransparentPixel_IsNotHit()
		{
			using( var control = MakeControl() )
			using( var halfTexture = MakeHalfTransparentTexture( 20, 10 ) )
			{
				var entry = MakeRoomObject( halfTexture, offsetX: 100, offsetY: 100 );
				control.RoomObjects.Add( entry );

				Assert.That( control.HitTestRoomObject( new Point( 105, 105 ) ), Is.SameAs( entry ) );
				Assert.That( control.HitTestRoomObject( new Point( 115, 105 ) ), Is.Null );
			}
		}

		[Test]
		public void HitTestRoomObject_MissingTexture_TakesNoClicks()
		{
			using( var control = MakeControl() )
			{
				var entry = MakeRoomObject( texture: null, offsetX: 100, offsetY: 100 );
				control.RoomObjects.Add( entry );

				// nothing is painted for a missing texture, so nothing is clickable
				Assert.That( control.HitTestRoomObject( new Point( 105, 105 ) ), Is.Null );
			}
		}

		//-------------------------------------------
		// helpers

		internal static RoomPreviewControl MakeControl()
		{
			return new RoomPreviewControl
			{
				Zoom = 1.0f,
			};
		}

		/// <summary>
		/// A texture whose left half is opaque and right half fully transparent.
		/// </summary>
		internal static Bitmap MakeHalfTransparentTexture( int width, int height )
		{
			var bitmap = new Bitmap( width, height );
			using( var graphics = Graphics.FromImage( bitmap ) )
			{
				graphics.Clear( Color.Transparent );
				using( var brush = new SolidBrush( Color.Red ) )
				{
					graphics.FillRectangle( brush, 0, 0, width / 2, height );
				}
			}
			return bitmap;
		}

		internal static Bitmap MakeSolidTexture( int width, int height )
		{
			var bitmap = new Bitmap( width, height );
			using( var graphics = Graphics.FromImage( bitmap ) )
			{
				graphics.Clear( Color.Blue );
			}
			return bitmap;
		}

		/// <summary>
		/// A preview sprite drawn 1:1 from the whole texture at the given screen position.
		/// </summary>
		internal static RoomPreviewControlSprite MakeSprite( Bitmap texture, float screenX, float screenY, int layer )
		{
			var sprite = new Sprite(
				index: 0,
				textureFileNameAddress: 0,
				textureX: 0,
				textureY: 0,
				textureWidth: texture.Width,
				textureHeight: texture.Height,
				offsetX: 0,
				offsetY: 0,
				layer: layer
			);
			var placement = new RoomSpritePlacement(
				groupIndex: 0,
				spriteHeader: new SpriteHeader( index: 0, identifier: 0, spriteCount: 1, spriteAddress: 0 ),
				sprite: sprite,
				screenRect: new RectangleF( screenX, screenY, texture.Width, texture.Height ),
				hasClassicMatch: true,
				classicObject: null
			);
			return new RoomPreviewControlSprite( placement, texture );
		}

		/// <summary>
		/// A room object overlay with a single draw of the whole texture at its offset.
		/// </summary>
		internal static RoomPreviewControlRoomObject MakeRoomObject( Bitmap? texture, float offsetX, float offsetY )
		{
			var entry = new RoomPreviewControlRoomObject(
				new RoomObject( index: 0, spriteAddress: 0, imageAddress: 0, offsetX: offsetX, offsetY: offsetY ),
				"test"
			);
			var size = texture == null ? new Size( 20, 10 ) : texture.Size;
			entry.Draws.Add( new RoomPreviewControlRoomObjectDraw(
				texture: texture,
				sourceRect: new RectangleF( 0, 0, size.Width, size.Height ),
				relativeRect: new RectangleF( 0, 0, size.Width, size.Height )
			) );
			return entry;
		}
	}
}
