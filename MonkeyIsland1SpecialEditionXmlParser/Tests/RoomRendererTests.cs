using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Scumm.Entities;
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
			var placements = Renderer.ResolvePlacements( room, classicObjects, new SizeF( 6.0f, 7.2f ) );

			// Assert
			Assert.That( placements.Count, Is.EqualTo( 1 ) );
			var placement = placements[0];
			Assert.That( placement.HasClassicMatch, Is.True );
			Assert.That( placement.ScreenRect.X, Is.EqualTo( 32 * 6.0f - 6.4f ).Within( 0.001f ) );
			Assert.That( placement.ScreenRect.Y, Is.EqualTo( 72 * 7.2f + 2.4f ).Within( 0.001f ) );
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

			// Act: also cover a null dictionary
			var placements = Renderer.ResolvePlacements( room, classicObjects, new SizeF( 6.0f, 7.2f ) );
			var placementsWithNull = Renderer.ResolvePlacements( room, null, new SizeF( 6.0f, 7.2f ) );

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
			var placements = Renderer.ResolvePlacements( room, null, Renderer.DefaultHdScale );

			// Assert: layer 0 sprites first (group 0 before group 1), then layer 5
			Assert.That( placements.Select( p => p.Sprite.Layer ).ToArray(), Is.EqualTo( new[] { 0, 0, 5 } ) );
			Assert.That( placements[0].GroupIndex, Is.EqualTo( 0 ) );
			Assert.That( placements[1].GroupIndex, Is.EqualTo( 1 ) );
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
			room.StaticSpriteHeaderList.Add( new StaticSpriteHeader( index: 0, identifier: 0, unkn1: 0, unkn2: 0, staticSpriteCount: 2, staticSpriteAddress: 0 ) );
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
		public void RenderBackground_WithoutStaticSprites_ReturnsNull()
		{
			// Arrange
			var room = MakeRoom( new SpriteHeader[0], new SpriteGroup[0] );

			// Act
			var background = Renderer.RenderBackground( room, fileName => null );

			// Assert
			Assert.That( background, Is.Null );
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
				unknown4HeaderList: new List<Unknown4Header>(),
				unknown5HeaderList: new List<Unknown5Header>(),
				staticSpriteList: new List<List<StaticSprite>>(),
				spriteGroupList: spriteGroups.ToList(),
				unknown6List: new List<Unknown6>(),
				unknown4GroupList: new List<Unknown4Group>(),
				unknown5List: new List<Unknown5>()
			);
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
