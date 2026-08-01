using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class RoomRendererTests
	{
		[Test]
		public void ResolvePlacements_WithClassicMatch_ComputesScreenRect()
		{
			// Arrange: real-world values from room 28 (bar), object 315 (door)
			var sprite = MakeSprite( textureWidth: 269, textureHeight: 346, offsetX: -6.4f, offsetY: 2.4f, layer: 0 );
			var room = MakeRoom(
				new[] { MakeSpriteHeader( identifier: 315 ) },
				new[] { new SpriteGroup( new List<Sprite> { sprite } ) }
			);
			var classicObjects = new Dictionary<int, ClassicObject>
			{
				{ 315, new ClassicObject( objectId: 315, x: 32, y: 72, width: 40, height: 48, name: "door" ) },
			};

			// Act
			var placements = Renderer.ResolvePlacements( room, classicObjects, new RoomHdTransform( new SizeF( 6.0f, 7.2f ), new PointF( 10.0f, 20.0f ) ) );

			// Assert
			Assert.That( placements.Count, Is.EqualTo( 1 ) );
			var placement = placements[0];
			Assert.That( placement.HasClassicMatch, Is.True );
			Assert.That( placement.ScreenRect.X, Is.EqualTo( 10.0f + 32 * 6.0f - 6.4f ).Within( 0.001f ) );
			Assert.That( placement.ScreenRect.Y, Is.EqualTo( 20.0f + 72 * 7.2f + 2.4f ).Within( 0.001f ) );
			Assert.That( placement.ScreenRect.Width, Is.EqualTo( 269 ) );
			Assert.That( placement.ScreenRect.Height, Is.EqualTo( 346 ) );
		}

		[Test]
		public void ResolvePlacements_WithoutClassicMatch_FallsBackToOffsetOnly()
		{
			// Arrange
			var sprite = MakeSprite( textureWidth: 100, textureHeight: 50, offsetX: 12.5f, offsetY: 34.5f, layer: 0 );
			var room = MakeRoom(
				new[] { MakeSpriteHeader( identifier: 999 ) },
				new[] { new SpriteGroup( new List<Sprite> { sprite } ) }
			);
			var classicObjects = new Dictionary<int, ClassicObject>();

			// Act: also cover a null dictionary; the origin shift must not leak into
			// offset-only placements
			var transform = new RoomHdTransform( new SizeF( 6.0f, 7.2f ), new PointF( 267.7f, 0.0f ) );
			var placements = Renderer.ResolvePlacements( room, classicObjects, transform );
			var placementsWithNull = Renderer.ResolvePlacements( room, null, transform );

			// Assert: the sprite is flagged, not dropped
			Assert.That( placements.Count, Is.EqualTo( 1 ) );
			Assert.That( placements[0].HasClassicMatch, Is.False );
			Assert.That( placements[0].ScreenRect.X, Is.EqualTo( 12.5f ) );
			Assert.That( placements[0].ScreenRect.Y, Is.EqualTo( 34.5f ) );

			Assert.That( placementsWithNull.Count, Is.EqualTo( 1 ) );
			Assert.That( placementsWithNull[0].HasClassicMatch, Is.False );
		}

		[Test]
		public void ResolvePlacements_OrdersByLayer_AndKeepsGroupOrderWithinLayer()
		{
			// Arrange: two groups; layers 5, 0 and 0
			var room = MakeRoom(
				new[] { MakeSpriteHeader( identifier: 1 ), MakeSpriteHeader( identifier: 2 ) },
				new[]
				{
					new SpriteGroup( new List<Sprite>
					{
						MakeSprite( textureWidth: 1, textureHeight: 1, offsetX: 0, offsetY: 0, layer: 5 ),
						MakeSprite( textureWidth: 1, textureHeight: 1, offsetX: 0, offsetY: 0, layer: 0 ),
					} ),
					new SpriteGroup( new List<Sprite>
					{
						MakeSprite( textureWidth: 1, textureHeight: 1, offsetX: 0, offsetY: 0, layer: 0 ),
					} ),
				}
			);

			// Act
			var placements = Renderer.ResolvePlacements( room, null, new RoomHdTransform( Renderer.DefaultHdScale, PointF.Empty ) );

			// Assert: layer 0 sprites first (group 0 before group 1), then layer 5
			Assert.That( placements.Select( p => p.Sprite.Layer ).ToArray(), Is.EqualTo( new[] { 0, 0, 5 } ) );
			Assert.That( placements[0].GroupIndex, Is.EqualTo( 0 ) );
			Assert.That( placements[1].GroupIndex, Is.EqualTo( 1 ) );
		}

		[Test]
		public void GetHdTransform_ScrollingRoom_GivesAspectCorrectedScaleAndNoMargin()
		{
			// Arrange: a normal 144-line room (28 bar); its art is authored exactly as wide
			// as the classic view, so the origin is ~0 and the scale is the familiar
			// 7.2014 with X = Y / 1.2
			var barRoom = MakeRoom( new SpriteHeader[0], new SpriteGroup[0] );
			barRoom.Header = new Header { Width = 3841, Height = 1037 };
			var barClassic = new ClassicRoom( roomNumber: 28, width: 640, height: 144, objectList: new List<ClassicObject>() );

			// Act
			var transform = Renderer.GetHdTransform( barRoom, barClassic );

			// Assert
			var scaleY = 1037 / 144.0f;
			Assert.That( transform.Scale.Height, Is.EqualTo( scaleY ).Within( 0.0001f ) );
			Assert.That( transform.Scale.Width, Is.EqualTo( scaleY / 1.2f ).Within( 0.0001f ) );
			Assert.That( transform.Origin.X, Is.EqualTo( 0.0f ).Within( 0.5f ) );
			Assert.That( transform.Origin.Y, Is.EqualTo( 0.0f ).Within( 0.5f ) );
		}

		[Test]
		public void GetHdTransform_FullscreenRoom_CentersTheClassicViewInTheWidescreenArt()
		{
			// Arrange: a fullscreen 200-line room (4 monkey-3, the island map); the art
			// carries widescreen margins, the classic 4:3 view sits centered between them
			var mapRoom = MakeRoom( new SpriteHeader[0], new SpriteGroup[0] );
			mapRoom.Header = new Header { Width = 1918, Height = 1037 };
			var mapClassic = new ClassicRoom( roomNumber: 4, width: 320, height: 200, objectList: new List<ClassicObject>() );

			// Act
			var transform = Renderer.GetHdTransform( mapRoom, mapClassic );

			// Assert: Y is 1037/200 = 5.185, X is that over 1.2 = 4.3208, and the classic
			// view (320 * 4.3208 = 1382.7 wide) is centered with ~268 px margins
			var scaleY = 1037 / 200.0f;
			var scaleX = scaleY / 1.2f;
			Assert.That( transform.Scale.Height, Is.EqualTo( scaleY ).Within( 0.0001f ) );
			Assert.That( transform.Scale.Width, Is.EqualTo( scaleX ).Within( 0.0001f ) );
			Assert.That( transform.Origin.X, Is.EqualTo( ( 1918 - 320 * scaleX ) / 2.0f ).Within( 0.001f ) );
			Assert.That( transform.Origin.X, Is.EqualTo( 267.67f ).Within( 0.1f ) );
			Assert.That( transform.Origin.Y, Is.EqualTo( 0.0f ).Within( 0.001f ) );
		}

		[Test]
		public void GetHdTransform_WithoutUsableSizes_FallsBackToDefault()
		{
			// Arrange
			var room = MakeRoom( new SpriteHeader[0], new SpriteGroup[0] );
			room.Header = new Header { Width = 1920, Height = 1037 };
			var zeroSizeClassic = new ClassicRoom( roomNumber: 24, width: 0, height: 0, objectList: new List<ClassicObject>() );
			var zeroSizeRoom = MakeRoom( new SpriteHeader[0], new SpriteGroup[0] );
			var normalClassic = new ClassicRoom( roomNumber: 24, width: 320, height: 144, objectList: new List<ClassicObject>() );

			// Act + Assert
			Assert.That( Renderer.GetHdTransform( room, null ).Scale, Is.EqualTo( Renderer.DefaultHdScale ) );
			Assert.That( Renderer.GetHdTransform( room, null ).Origin, Is.EqualTo( PointF.Empty ) );
			Assert.That( Renderer.GetHdTransform( room, zeroSizeClassic ).Scale, Is.EqualTo( Renderer.DefaultHdScale ) );
			Assert.That( Renderer.GetHdTransform( zeroSizeRoom, normalClassic ).Scale, Is.EqualTo( Renderer.DefaultHdScale ) );
		}

		[Test]
		public void RenderBackground_CompositesStaticSpritesAtTheirPositions()
		{
			// Arrange: two 10x10 tiles side by side
			var staticSprite1 = new StaticSprite( index: 0, x: 0, y: 0, width: 10, height: 10, textureFileNameAddress: 0 )
			{
				TextureFileName = "red.dxt",
			};
			var staticSprite2 = new StaticSprite( index: 1, x: 10, y: 0, width: 10, height: 10, textureFileNameAddress: 0 )
			{
				TextureFileName = "blue.dxt",
			};
			var room = MakeRoom( new SpriteHeader[0], new SpriteGroup[0] );
			room.StaticSpriteHeaderList.Add( new StaticSpriteHeader( index: 0, identifier: 0, sourceWidth: 0, sourceHeight: 0, staticSpriteCount: 2, staticSpriteAddress: 0 ) );
			room.StaticSpriteList.Add( new List<StaticSprite> { staticSprite1, staticSprite2 } );

			var textures = new Dictionary<string, Image>
			{
				{ "red.dxt", MakeSolidBitmap( Color.Red ) },
				{ "blue.dxt", MakeSolidBitmap( Color.Blue ) },
			};

			// Act
			var background = Renderer.RenderBackground( room, fileName => fileName != null && textures.ContainsKey( fileName ) ? textures[fileName] : null );

			// Assert
			Assert.That( background, Is.Not.Null );
			Assert.That( background!.Width, Is.EqualTo( 20 ) );
			Assert.That( background.Height, Is.EqualTo( 10 ) );
			Assert.That( background.GetPixel( 5, 5 ).ToArgb(), Is.EqualTo( Color.Red.ToArgb() ) );
			Assert.That( background.GetPixel( 15, 5 ).ToArgb(), Is.EqualTo( Color.Blue.ToArgb() ) );
		}

		[Test]
		public void RenderBackground_PaddedChunkTexture_DrawsTheRectSizedRegionUnscaled()
		{
			// Arrange: a 10x10 placement rect backed by a 20x20 power-of-two padded
			// texture whose top-left 10x10 is red and whose padding is blue, the way the
			// game stores its chunk textures (e.g. room 41's right column: a 1024 px wide
			// texture placed at 896). The padding must never show; scaling the whole
			// texture into the rect would squeeze the blue half into view.
			var room = MakeRoom( new SpriteHeader[0], new SpriteGroup[0] );
			room.StaticSpriteHeaderList.Add( new StaticSpriteHeader( index: 0, identifier: 0, sourceWidth: 0, sourceHeight: 0, staticSpriteCount: 1, staticSpriteAddress: 0 ) );
			room.StaticSpriteList.Add( new List<StaticSprite>
			{
				new StaticSprite( index: 0, x: 0, y: 0, width: 10, height: 10, textureFileNameAddress: 0 ) { TextureFileName = "padded.dxt" },
			} );

			var texture = new Bitmap( 20, 20 );
			using( var graphics = Graphics.FromImage( texture ) )
			{
				graphics.Clear( Color.Blue );
				graphics.FillRectangle( Brushes.Red, 0, 0, 10, 10 );
			}

			// Act
			var background = Renderer.RenderBackground( room, fileName => texture );

			// Assert: every pixel of the rect comes from the texture's top-left region
			Assert.That( background, Is.Not.Null );
			Assert.That( background!.Width, Is.EqualTo( 10 ) );
			Assert.That( background.GetPixel( 1, 1 ).ToArgb(), Is.EqualTo( Color.Red.ToArgb() ) );
			Assert.That( background.GetPixel( 8, 8 ).ToArgb(), Is.EqualTo( Color.Red.ToArgb() ) );
		}

		[Test]
		public void RenderBackground_TextureSmallerThanRect_DrawsItUnscaledAndLeavesTheRestEmpty()
		{
			// Arrange: a 30x30 rect backed by only a 10x10 texture; it must not stretch
			var room = MakeRoom( new SpriteHeader[0], new SpriteGroup[0] );
			room.StaticSpriteHeaderList.Add( new StaticSpriteHeader( index: 0, identifier: 0, sourceWidth: 0, sourceHeight: 0, staticSpriteCount: 1, staticSpriteAddress: 0 ) );
			room.StaticSpriteList.Add( new List<StaticSprite>
			{
				new StaticSprite( index: 0, x: 0, y: 0, width: 30, height: 30, textureFileNameAddress: 0 ) { TextureFileName = "red.dxt" },
			} );

			// Act
			var background = Renderer.RenderBackground( room, fileName => MakeSolidBitmap( Color.Red ) );

			// Assert
			Assert.That( background, Is.Not.Null );
			Assert.That( background!.Width, Is.EqualTo( 30 ) );
			Assert.That( background.GetPixel( 5, 5 ).ToArgb(), Is.EqualTo( Color.Red.ToArgb() ) );
			Assert.That( background.GetPixel( 20, 20 ).A, Is.EqualTo( 0 ) );
		}

		[Test]
		public void RenderBackground_WithoutStaticSprites_ReturnsNull()
		{
			// Arrange
			var room = MakeRoom( new SpriteHeader[0], new SpriteGroup[0] );

			// Act
			var background = Renderer.RenderBackground( room, fileName => null );

			// Assert
			Assert.That( background, Is.Null );
		}

		[Test]
		public void RenderBackground_RendersOnlyTheFirstStaticLayer()
		{
			// Arrange: layer 0 red at 0,0; layer 1 (foreground) blue at 10,0
			var room = MakeRoomWithTwoStaticLayers();
			var textures = new Dictionary<string, Image>
			{
				{ "red.dxt", MakeSolidBitmap( Color.Red ) },
				{ "blue.dxt", MakeSolidBitmap( Color.Blue ) },
			};

			// Act
			var background = Renderer.RenderBackground( room, fileName => fileName != null && textures.ContainsKey( fileName ) ? textures[fileName] : null );

			// Assert: sized to the whole room, but only layer 0 is drawn
			Assert.That( background, Is.Not.Null );
			Assert.That( background!.Width, Is.EqualTo( 20 ) );
			Assert.That( background.GetPixel( 5, 5 ).ToArgb(), Is.EqualTo( Color.Red.ToArgb() ) );
			Assert.That( background.GetPixel( 15, 5 ).A, Is.EqualTo( 0 ) );
		}

		[Test]
		public void RenderForeground_RendersOnlyTheLayersAfterTheFirst()
		{
			// Arrange: layer 0 red at 0,0; layer 1 (foreground) blue at 10,0
			var room = MakeRoomWithTwoStaticLayers();
			var textures = new Dictionary<string, Image>
			{
				{ "red.dxt", MakeSolidBitmap( Color.Red ) },
				{ "blue.dxt", MakeSolidBitmap( Color.Blue ) },
			};

			// Act
			var foreground = Renderer.RenderForeground( room, fileName => fileName != null && textures.ContainsKey( fileName ) ? textures[fileName] : null );

			// Assert: same size as the background so the two overlay 1:1
			Assert.That( foreground, Is.Not.Null );
			Assert.That( foreground!.Width, Is.EqualTo( 20 ) );
			Assert.That( foreground.GetPixel( 15, 5 ).ToArgb(), Is.EqualTo( Color.Blue.ToArgb() ) );
			Assert.That( foreground.GetPixel( 5, 5 ).A, Is.EqualTo( 0 ) );
		}

		[Test]
		public void RenderForeground_WithoutForegroundLayers_ReturnsNull()
		{
			// Arrange: a single static layer only
			var room = MakeRoom( new SpriteHeader[0], new SpriteGroup[0] );
			room.StaticSpriteHeaderList.Add( new StaticSpriteHeader( index: 0, identifier: 0, sourceWidth: 0, sourceHeight: 0, staticSpriteCount: 1, staticSpriteAddress: 0 ) );
			room.StaticSpriteList.Add( new List<StaticSprite>
			{
				new StaticSprite( index: 0, x: 0, y: 0, width: 10, height: 10, textureFileNameAddress: 0 ) { TextureFileName = "red.dxt" },
			} );

			// Act
			var foreground = Renderer.RenderForeground( room, fileName => MakeSolidBitmap( Color.Red ) );

			// Assert
			Assert.That( foreground, Is.Null );
		}

		//-------------------------------------------
		// entity builders

		private static Room MakeRoom( SpriteHeader[] spriteHeaders, SpriteGroup[] spriteGroups )
		{
			return new Room(
				header: new Header(),
				staticSpriteHeaderList: new List<StaticSpriteHeader>(),
				spriteHeaderList: spriteHeaders.ToList(),
				unknown6HeaderList: new List<Unknown6Header>(),
				roomObjectHeaderList: new List<RoomObjectHeader>(),
				unknown5HeaderList: new List<Unknown5Header>(),
				staticSpriteList: new List<List<StaticSprite>>(),
				spriteGroupList: spriteGroups.ToList(),
				unknown6List: new List<Unknown6>(),
				roomObjectGroupList: new List<RoomObjectGroup>(),
				unknown5List: new List<Unknown5>()
			);
		}

		private static Room MakeRoomWithTwoStaticLayers()
		{
			var room = MakeRoom( new SpriteHeader[0], new SpriteGroup[0] );
			room.StaticSpriteHeaderList.Add( new StaticSpriteHeader( index: 0, identifier: 0, sourceWidth: 0, sourceHeight: 0, staticSpriteCount: 1, staticSpriteAddress: 0 ) );
			room.StaticSpriteHeaderList.Add( new StaticSpriteHeader( index: 1, identifier: 1, sourceWidth: 0, sourceHeight: 0, staticSpriteCount: 1, staticSpriteAddress: 0 ) );
			room.StaticSpriteList.Add( new List<StaticSprite>
			{
				new StaticSprite( index: 0, x: 0, y: 0, width: 10, height: 10, textureFileNameAddress: 0 ) { TextureFileName = "red.dxt" },
			} );
			room.StaticSpriteList.Add( new List<StaticSprite>
			{
				new StaticSprite( index: 0, x: 10, y: 0, width: 10, height: 10, textureFileNameAddress: 0 ) { TextureFileName = "blue.dxt" },
			} );
			return room;
		}

		private static SpriteHeader MakeSpriteHeader( int identifier )
		{
			return new SpriteHeader( index: 0, identifier: identifier, spriteCount: 0, spriteAddress: 0 );
		}

		private static Sprite MakeSprite( int textureWidth, int textureHeight, float offsetX, float offsetY, int layer )
		{
			return new Sprite(
				index: 0,
				textureFileNameAddress: 0,
				textureX: 0,
				textureY: 0,
				textureWidth: textureWidth,
				textureHeight: textureHeight,
				offsetX: offsetX,
				offsetY: offsetY,
				layer: layer
			);
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
