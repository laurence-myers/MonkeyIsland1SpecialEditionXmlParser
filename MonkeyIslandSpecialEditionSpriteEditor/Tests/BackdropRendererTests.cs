using System.Collections.Generic;
using System.Drawing;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class BackdropRendererTests
	{
		[Test]
		public void Render_ActorInFrontOfForeground_CompositesForegroundIntoBelowAndLeavesNothingAbove()
		{
			// Arrange: background red and foreground blue fully overlapping; the actor stands on
			// a mask-0 box, so the game (and the room preview) draw it in front of the foreground
			var room = MakeRoomWithOverlappingLayers();
			var classicRoom = MakeClassicRoomWithBoxMask( 0 );
			var placement = MakePlacement( x: 100, y: 100, elevation: null );
			var textures = MakeRedBlueTextures();

			// Act
			var backdrop = BackdropRenderer.Render( room, classicRoom, placement, name => Lookup( textures, name ) );

			// Assert: foreground was baked under the actor (Below shows blue), nothing sits above
			Assert.That( backdrop.Above, Is.Null );
			Assert.That( backdrop.Below, Is.Not.Null );
			Assert.That( backdrop.Below!.GetPixel( 5, 5 ).ToArgb(), Is.EqualTo( Color.Blue.ToArgb() ) );
		}

		[Test]
		public void Render_ActorBehindForeground_KeepsForegroundAboveAndBackgroundOnlyBelow()
		{
			// Arrange: same room, but the actor stands on a masked box, so it draws behind the
			// foreground (e.g. the storekeeper behind his counter)
			var room = MakeRoomWithOverlappingLayers();
			var classicRoom = MakeClassicRoomWithBoxMask( 1 );
			var placement = MakePlacement( x: 100, y: 100, elevation: null );
			var textures = MakeRedBlueTextures();

			// Act
			var backdrop = BackdropRenderer.Render( room, classicRoom, placement, name => Lookup( textures, name ) );

			// Assert: Below is background only (red), the foreground is handed back to draw on top
			Assert.That( backdrop.Below, Is.Not.Null );
			Assert.That( backdrop.Below!.GetPixel( 5, 5 ).ToArgb(), Is.EqualTo( Color.Red.ToArgb() ) );
			Assert.That( backdrop.Above, Is.Not.Null );
			Assert.That( backdrop.Above!.GetPixel( 5, 5 ).ToArgb(), Is.EqualTo( Color.Blue.ToArgb() ) );
		}

		[Test]
		public void Render_AnchorsOriginAtThePlacementFeetLiftedByElevation()
		{
			// Arrange: a bar-like 144-line room; the actor origin is its feet, in HD pixels
			var room = MakeRoomWithOverlappingLayers();
			room.Header = new Header { Width = 3841, Height = 1037 };
			var classicRoom = MakeClassicRoomWithBoxMask( 0 );
			classicRoom.Width = 640;
			classicRoom.Height = 144;
			var placement = MakePlacement( x: 100, y: 120, elevation: 5 );
			var textures = MakeRedBlueTextures();

			var transform = Renderer.GetHdTransform( room, classicRoom );

			// Act
			var backdrop = BackdropRenderer.Render( room, classicRoom, placement, name => Lookup( textures, name ) );

			// Assert: origin = transform origin + (x, y - elevation) * scale
			Assert.That( backdrop.ActorOriginHd.X, Is.EqualTo( transform.Origin.X + 100 * transform.Scale.Width ).Within( 0.001f ) );
			Assert.That( backdrop.ActorOriginHd.Y, Is.EqualTo( transform.Origin.Y + ( 120 - 5 ) * transform.Scale.Height ).Within( 0.001f ) );

			// the room scale is reported so the costume editor can size the actor to it
			Assert.That( backdrop.Scale, Is.EqualTo( transform.Scale ) );
		}

		[Test]
		public void Render_RoomWithoutStaticSprites_GivesNoImageryButStillAnchorsTheOrigin()
		{
			// Arrange
			var room = MakeRoom();
			var classicRoom = MakeClassicRoomWithBoxMask( 0 );
			var placement = MakePlacement( x: 10, y: 20, elevation: null );

			// Act
			var backdrop = BackdropRenderer.Render( room, classicRoom, placement, name => null );

			// Assert
			Assert.That( backdrop.Below, Is.Null );
			Assert.That( backdrop.Above, Is.Null );
			Assert.That( backdrop.ActorOriginHd.X, Is.EqualTo( 10 * Renderer.DefaultHdScale.Width ).Within( 0.001f ) );
		}

		//-------------------------------------------
		// builders

		private static Room MakeRoom()
		{
			return new Room(
				header: new Header(),
				staticSpriteHeaderList: new List<StaticSpriteHeader>(),
				spriteHeaderList: new List<SpriteHeader>(),
				unknown6HeaderList: new List<Unknown6Header>(),
				roomObjectHeaderList: new List<RoomObjectHeader>(),
				unknown5HeaderList: new List<Unknown5Header>(),
				staticSpriteList: new List<List<StaticSprite>>(),
				spriteGroupList: new List<SpriteGroup>(),
				unknown6List: new List<Unknown6>(),
				roomObjectGroupList: new List<RoomObjectGroup>(),
				unknown5List: new List<Unknown5>()
			);
		}

		/// <summary>
		/// A room whose background (layer 0) and foreground (layer 1) are both a 10x10 tile at
		/// the origin, so a pixel there tells the two layers apart after compositing.
		/// </summary>
		private static Room MakeRoomWithOverlappingLayers()
		{
			var room = MakeRoom();
			room.StaticSpriteHeaderList.Add( new StaticSpriteHeader( index: 0, identifier: 0, sourceWidth: 0, sourceHeight: 0, staticSpriteCount: 1, staticSpriteAddress: 0 ) );
			room.StaticSpriteHeaderList.Add( new StaticSpriteHeader( index: 1, identifier: 1, sourceWidth: 0, sourceHeight: 0, staticSpriteCount: 1, staticSpriteAddress: 0 ) );
			room.StaticSpriteList.Add( new List<StaticSprite>
			{
				new StaticSprite( index: 0, x: 0, y: 0, width: 10, height: 10, textureFileNameAddress: 0 ) { TextureFileName = "red.dxt" },
			} );
			room.StaticSpriteList.Add( new List<StaticSprite>
			{
				new StaticSprite( index: 0, x: 0, y: 0, width: 10, height: 10, textureFileNameAddress: 0 ) { TextureFileName = "blue.dxt" },
			} );
			return room;
		}

		private static ClassicRoom MakeClassicRoomWithBoxMask( int mask )
		{
			var classicRoom = new ClassicRoom( roomNumber: 28, width: 320, height: 144, objectList: new List<ClassicObject>() );
			classicRoom.BoxList.Add( new ClassicBox(
				cornerList: new[] { new Point( 0, 0 ), new Point( 640, 0 ), new Point( 640, 200 ), new Point( 0, 200 ) },
				mask: mask,
				flags: 0,
				scale: 255
			) );
			return classicRoom;
		}

		private static ClassicActorPlacement MakePlacement( int x, int y, int? elevation )
		{
			return new ClassicActorPlacement(
				actorNumber: 1,
				x: x,
				y: y,
				costumeId: 24,
				costumeInferred: false,
				direction: 1,
				elevation: elevation,
				source: "test"
			);
		}

		private static Dictionary<string, Image> MakeRedBlueTextures()
		{
			return new Dictionary<string, Image>
			{
				{ "red.dxt", MakeSolidBitmap( Color.Red ) },
				{ "blue.dxt", MakeSolidBitmap( Color.Blue ) },
			};
		}

		private static Image? Lookup( Dictionary<string, Image> textures, string? name )
		{
			return name != null && textures.ContainsKey( name ) ? textures[name] : null;
		}

		private static Bitmap MakeSolidBitmap( Color color )
		{
			var bitmap = new Bitmap( 10, 10 );
			using( var graphics = Graphics.FromImage( bitmap ) )
			{
				graphics.Clear( color );
			}
			return bitmap;
		}
	}
}
