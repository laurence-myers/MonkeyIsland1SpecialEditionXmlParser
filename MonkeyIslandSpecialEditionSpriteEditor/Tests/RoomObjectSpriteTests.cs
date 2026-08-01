using System.IO;
using System.Xml.Serialization;
using MonkeyIslandSpecialEditionSpriteEditor.Formats;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class RoomObjectSpriteTests
	{
		[Test]
		public void AtlasSpriteProperties_ForwardToTheRectFields()
		{
			// Arrange
			var sprite = new RoomObjectSprite( textureFileNameAddress: 0, x: 292, y: 0, width: 279, height: 535 );

			// Act: edit through the IAtlasSprite view the atlas control uses
			IAtlasSprite atlasSprite = sprite;
			atlasSprite.TextureX = 300;
			atlasSprite.TextureHeight = 500;

			// Assert: one set of values, two names
			Assert.That( sprite.X, Is.EqualTo( 300 ) );
			Assert.That( sprite.Y, Is.EqualTo( 0 ) );
			Assert.That( sprite.Width, Is.EqualTo( 279 ) );
			Assert.That( sprite.Height, Is.EqualTo( 500 ) );
			Assert.That( atlasSprite.TextureY, Is.EqualTo( 0 ) );
			Assert.That( atlasSprite.TextureWidth, Is.EqualTo( 279 ) );
		}

		[Test]
		public void XmlSerialization_KeepsTheOriginalElementNames()
		{
			// Arrange: the IAtlasSprite forwarding properties must not change the XML export
			// shape, which serializes X/Y/Width/Height
			var sprite = new RoomObjectSprite( textureFileNameAddress: 0, x: 292, y: 7, width: 279, height: 535 );

			// Act
			var serializer = new XmlSerializer( typeof( RoomObjectSprite ) );
			string xml;
			using( var writer = new StringWriter() )
			{
				serializer.Serialize( writer, sprite );
				xml = writer.ToString();
			}

			// Assert
			Assert.That( xml, Does.Contain( "<X>292</X>" ) );
			Assert.That( xml, Does.Contain( "<Height>535</Height>" ) );
			Assert.That( xml, Does.Not.Contain( "TextureX" ) );
			Assert.That( xml, Does.Not.Contain( "TextureHeight" ) );
		}
	}
}
