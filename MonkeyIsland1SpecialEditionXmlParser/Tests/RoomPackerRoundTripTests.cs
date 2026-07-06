using System.IO;
using NUnit.Framework;
using RoomPacker = MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Packer;
using RoomParser = MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Parser;

namespace Tests
{
	[TestFixture]
	public class RoomPackerRoundTripTests
	{
		[Test]
		public void EditSpriteFields_WriteAndReadBack_PreservesEditedValues()
		{
			// Arrange
			var datFilePath = Path.Combine( TestContext.CurrentContext.TestDirectory, "Fixtures", "028 - bar.dat" );
			var tempDatPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".dat" );

			try
			{
				var room = RoomParser.ReadRoomFromBinaryFile( datFilePath );
				Assert.That( room.SpriteGroupList.Count, Is.GreaterThan( 0 ), "Fixture should contain sprite groups" );

				// Act: the edits a spritesheet editor user would make
				var sprite = room.SpriteGroupList[0].SpriteList[0];
				var editedTextureX = sprite.TextureX + 3;
				var editedOffsetX = sprite.OffsetX + 5.5f;
				var editedOffsetY = sprite.OffsetY - 2.25f;
				var editedLayer = sprite.Layer + 1;
				sprite.TextureX = editedTextureX;
				sprite.OffsetX = editedOffsetX;
				sprite.OffsetY = editedOffsetY;
				sprite.Layer = editedLayer;

				RoomPacker.WriteRoomToBinaryFile( tempDatPath, room );

				// reading back also runs SanityChecker.Check
				var roundTrippedRoom = RoomParser.ReadRoomFromBinaryFile( tempDatPath );

				// Assert
				var roundTrippedSprite = roundTrippedRoom.SpriteGroupList[0].SpriteList[0];
				Assert.That( roundTrippedSprite.TextureX, Is.EqualTo( editedTextureX ) );
				Assert.That( roundTrippedSprite.OffsetX, Is.EqualTo( editedOffsetX ) );
				Assert.That( roundTrippedSprite.OffsetY, Is.EqualTo( editedOffsetY ) );
				Assert.That( roundTrippedSprite.Layer, Is.EqualTo( editedLayer ) );

				// the rest of the room survives untouched
				Assert.That( roundTrippedRoom.Header.Identifier, Is.EqualTo( room.Header.Identifier ) );
				Assert.That( roundTrippedRoom.Header.Name, Is.EqualTo( room.Header.Name ) );
				Assert.That( roundTrippedRoom.SpriteGroupList.Count, Is.EqualTo( room.SpriteGroupList.Count ) );
				Assert.That( roundTrippedRoom.StaticSpriteList.Count, Is.EqualTo( room.StaticSpriteList.Count ) );
			}
			finally
			{
				if( File.Exists( tempDatPath ) )
				{
					File.Delete( tempDatPath );
				}
			}
		}
	}
}
