using System.Collections.Generic;
using System.IO;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities;
using NUnit.Framework;
using RoomPacker = MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Packer;
using RoomParser = MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Parser;

namespace Tests
{
	[TestFixture]
	public class RoomPackerRoundTripTests
	{
		private static string FixtureDatPath =>
			Path.Combine( TestContext.CurrentContext.TestDirectory, "Fixtures", "028 - bar.dat" );

		/// <summary>
		/// The strongest regression test for the packer: parsing an untouched game room and
		/// writing it back must reproduce the original file byte-for-byte. This exercises
		/// 16-byte alignment, string de-duplication, section ordering, the empty header
		/// sections and every "unknown" blob at once - the exact things whose corruption
		/// made the game crash.
		/// </summary>
		[Test]
		public void WriteUnmodifiedRoom_IsByteIdenticalToOriginal()
		{
			var original = File.ReadAllBytes( FixtureDatPath );
			var tempDatPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".dat" );

			try
			{
				var room = RoomParser.ReadRoomFromBinaryFile( FixtureDatPath );
				RoomPacker.WriteRoomToBinaryFile( tempDatPath, room );

				var written = File.ReadAllBytes( tempDatPath );
				Assert.That( written, Is.EqualTo( original ), "Repacked room should be byte-identical to the original" );
			}
			finally
			{
				if( File.Exists( tempDatPath ) )
				{
					File.Delete( tempDatPath );
				}
			}
		}

		[Test]
		public void EditSpriteFields_WriteAndReadBack_PreservesEditedValues()
		{
			var tempDatPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".dat" );

			try
			{
				var room = RoomParser.ReadRoomFromBinaryFile( FixtureDatPath );
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

		/// <summary>
		/// The fixture's "Chandelier" object is drawn as a <see cref="RoomObjectSprite"/> whose
		/// texture is one of the room's objects atlases. The old parser read that texture
		/// pointer as a plain integer and the old packer wrote it back unrelocated, leaving a
		/// dangling pointer - a direct cause of the crash. Confirm it survives a round-trip.
		/// </summary>
		[Test]
		public void RoomObjectSpriteTexture_SurvivesRoundTrip()
		{
			var tempDatPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".dat" );

			try
			{
				var room = RoomParser.ReadRoomFromBinaryFile( FixtureDatPath );

				var originalSprite = FindFirstRoomObjectSprite( room );
				Assert.That( originalSprite, Is.Not.Null, "Fixture should contain a room object drawn as a sprite" );
				Assert.That( originalSprite!.TextureFileName, Is.Not.Null.And.Not.Empty );

				RoomPacker.WriteRoomToBinaryFile( tempDatPath, room );
				var roundTripped = RoomParser.ReadRoomFromBinaryFile( tempDatPath );

				var roundTrippedSprite = FindFirstRoomObjectSprite( roundTripped );
				Assert.That( roundTrippedSprite, Is.Not.Null );
				Assert.That( roundTrippedSprite!.TextureFileName, Is.EqualTo( originalSprite.TextureFileName ) );
				Assert.That( roundTrippedSprite.X, Is.EqualTo( originalSprite.X ) );
				Assert.That( roundTrippedSprite.Y, Is.EqualTo( originalSprite.Y ) );
				Assert.That( roundTrippedSprite.Width, Is.EqualTo( originalSprite.Width ) );
				Assert.That( roundTrippedSprite.Height, Is.EqualTo( originalSprite.Height ) );
			}
			finally
			{
				if( File.Exists( tempDatPath ) )
				{
					File.Delete( tempDatPath );
				}
			}
		}

		/// <summary>
		/// A room object drawn as a chunked <see cref="RoomObjectImage"/> (used by animated
		/// overlays like lava / water). The old parser dropped the chunk records entirely
		/// while leaving their stale count and pointer in the file. Build one from scratch,
		/// round-trip it, and confirm every chunk is preserved.
		/// </summary>
		[Test]
		public void RoomObjectImageChunks_SurviveRoundTrip()
		{
			var tempDatPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".dat" );

			try
			{
				var image = new RoomObjectImage( sourceWidth: 9600, sourceHeight: 2592, chunkAddress: 0 );
				image.ChunkList.Add( new RoomObjectImageChunk( index: 0, x: 0, y: 0, width: 1024, height: 1024, textureFileNameAddress: 0 )
				{
					TextureFileName = "art/rooms/images/test/extra_lava_f0_chunk_0_0.dxt"
				} );
				image.ChunkList.Add( new RoomObjectImageChunk( index: 1, x: 1024, y: 0, width: 512, height: 1024, textureFileNameAddress: 0 )
				{
					TextureFileName = "art/rooms/images/test/extra_lava_f0_chunk_1024_0.dxt"
				} );

				var imageObject = new RoomObject( index: 0, spriteAddress: 0, imageAddress: 0, offsetX: 1.5f, offsetY: -2.5f )
				{
					Image = image
				};

				var room = MakeMinimalRoom( "testimage",
					roomObjectHeaders: new[] { new RoomObjectHeader( nameAddress: 0, roomObjectCount: 1, roomObjectAddress: 0 ) { Name = "Lava" } },
					roomObjectGroups: new[] { new RoomObjectGroup( new List<RoomObject> { imageObject } ) } );

				RoomPacker.WriteRoomToBinaryFile( tempDatPath, room );
				var roundTripped = RoomParser.ReadRoomFromBinaryFile( tempDatPath );

				Assert.That( roundTripped.RoomObjectGroupList.Count, Is.EqualTo( 1 ) );
				var roundTrippedObject = roundTripped.RoomObjectGroupList[0].RoomObjectList[0];
				Assert.That( roundTrippedObject.Sprite, Is.Null );
				Assert.That( roundTrippedObject.Image, Is.Not.Null );
				Assert.That( roundTrippedObject.OffsetX, Is.EqualTo( 1.5f ) );
				Assert.That( roundTrippedObject.OffsetY, Is.EqualTo( -2.5f ) );

				var roundTrippedImage = roundTrippedObject.Image!;
				Assert.That( roundTrippedImage.SourceWidth, Is.EqualTo( 9600 ) );
				Assert.That( roundTrippedImage.SourceHeight, Is.EqualTo( 2592 ) );
				Assert.That( roundTrippedImage.ChunkList.Count, Is.EqualTo( 2 ), "Both chunks must survive" );
				Assert.That( roundTrippedImage.ChunkList[0].Width, Is.EqualTo( 1024 ) );
				Assert.That( roundTrippedImage.ChunkList[0].TextureFileName, Is.EqualTo( "art/rooms/images/test/extra_lava_f0_chunk_0_0.dxt" ) );
				Assert.That( roundTrippedImage.ChunkList[1].X, Is.EqualTo( 1024 ) );
				Assert.That( roundTrippedImage.ChunkList[1].TextureFileName, Is.EqualTo( "art/rooms/images/test/extra_lava_f0_chunk_1024_0.dxt" ) );

				Assert.That( roundTripped.RoomObjectHeaderList[0].Name, Is.EqualTo( "Lava" ), "Object name must survive" );
			}
			finally
			{
				if( File.Exists( tempDatPath ) )
				{
					File.Delete( tempDatPath );
				}
			}
		}

		/// <summary>
		/// A room object sprite frequently shares a texture with an ordinary sprite, so its
		/// pooled string sits earlier in the file and the stored offset is negative. Confirm
		/// a back-referencing (signed) texture offset round-trips correctly.
		/// </summary>
		[Test]
		public void RoomObjectSpriteSharedTexture_SurvivesRoundTrip()
		{
			var tempDatPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".dat" );

			try
			{
				const string sharedTexture = "art/rooms/images/test/objects_a0.dxt";

				var sprite = new Sprite( index: 0, textureFileNameAddress: 0, textureX: 10, textureY: 20, textureWidth: 30, textureHeight: 40, offsetX: 0f, offsetY: 0f, layer: 0 )
				{
					TextureFileName = sharedTexture
				};

				var objectSprite = new RoomObject( index: 0, spriteAddress: 0, imageAddress: 0, offsetX: 0f, offsetY: 0f )
				{
					Sprite = new RoomObjectSprite( textureFileNameAddress: 0, x: 5, y: 6, width: 7, height: 8 )
					{
						TextureFileName = sharedTexture
					}
				};

				var room = MakeMinimalRoom( "testshared",
					spriteHeaders: new[] { new SpriteHeader( index: 0, identifier: 42, spriteCount: 0, spriteAddress: 0 ) },
					spriteGroups: new[] { new SpriteGroup( new List<Sprite> { sprite } ) },
					roomObjectHeaders: new[] { new RoomObjectHeader( nameAddress: 0, roomObjectCount: 1, roomObjectAddress: 0 ) { Name = "Obj" } },
					roomObjectGroups: new[] { new RoomObjectGroup( new List<RoomObject> { objectSprite } ) } );

				RoomPacker.WriteRoomToBinaryFile( tempDatPath, room );
				var roundTripped = RoomParser.ReadRoomFromBinaryFile( tempDatPath );

				var roundTrippedSprite = roundTripped.RoomObjectGroupList[0].RoomObjectList[0].Sprite;
				Assert.That( roundTrippedSprite, Is.Not.Null );
				Assert.That( roundTrippedSprite!.TextureFileName, Is.EqualTo( sharedTexture ) );
				Assert.That( roundTrippedSprite.X, Is.EqualTo( 5 ) );
				Assert.That( roundTrippedSprite.Height, Is.EqualTo( 8 ) );

				// the ordinary sprite still references the same (shared) texture
				Assert.That( roundTripped.SpriteGroupList[0].SpriteList[0].TextureFileName, Is.EqualTo( sharedTexture ) );
				Assert.That( roundTripped.SpriteHeaderList[0].Identifier, Is.EqualTo( 42 ), "Sprite header identifier must survive" );
			}
			finally
			{
				if( File.Exists( tempDatPath ) )
				{
					File.Delete( tempDatPath );
				}
			}
		}

		/// <summary>
		/// Reassigning the texture of an object sprite, a background static sprite and (when the
		/// fixture has one) a room object sprite must survive a pack/reparse. The file is not
		/// byte-identical here - the shared string pool legitimately changes when names change -
		/// so this asserts the values, not the bytes.
		/// </summary>
		[Test]
		public void ChangeTextureReferences_WriteAndReadBack_PreservesNewNames()
		{
			var tempDatPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".dat" );

			try
			{
				var room = RoomParser.ReadRoomFromBinaryFile( FixtureDatPath );

				const string newObjectTexture = "art/custom/new-object.dxt";
				const string newBackgroundTexture = "art/custom/new-background.dxt";
				room.SpriteGroupList[0].SpriteList[0].TextureFileName = newObjectTexture;

				var staticSprite = FindFirstStaticSprite( room );
				Assert.That( staticSprite, Is.Not.Null, "the bar fixture should have a background static sprite" );
				staticSprite!.TextureFileName = newBackgroundTexture;

				var roomObjectSprite = FindFirstRoomObjectSprite( room );
				string? newRoomObjectTexture = null;
				if( roomObjectSprite != null )
				{
					newRoomObjectTexture = "art/custom/new-roomobject.dxt";
					roomObjectSprite.TextureFileName = newRoomObjectTexture;
				}

				RoomPacker.WriteRoomToBinaryFile( tempDatPath, room );
				var roundTripped = RoomParser.ReadRoomFromBinaryFile( tempDatPath );

				Assert.That( roundTripped.SpriteGroupList[0].SpriteList[0].TextureFileName, Is.EqualTo( newObjectTexture ) );
				Assert.That( FindFirstStaticSprite( roundTripped )!.TextureFileName, Is.EqualTo( newBackgroundTexture ) );
				if( newRoomObjectTexture != null )
				{
					Assert.That( FindFirstRoomObjectSprite( roundTripped )!.TextureFileName, Is.EqualTo( newRoomObjectTexture ) );
				}
			}
			finally
			{
				if( File.Exists( tempDatPath ) )
				{
					File.Delete( tempDatPath );
				}
			}
		}

		private static StaticSprite? FindFirstStaticSprite( Room room )
		{
			foreach( var layer in room.StaticSpriteList )
			{
				if( layer.Count > 0 )
				{
					return layer[0];
				}
			}
			return null;
		}

		private static RoomObjectSprite? FindFirstRoomObjectSprite( Room room )
		{
			foreach( var group in room.RoomObjectGroupList )
			{
				foreach( var roomObject in group.RoomObjectList )
				{
					if( roomObject.Sprite != null )
					{
						return roomObject.Sprite;
					}
				}
			}
			return null;
		}

		private static Room MakeMinimalRoom(
			string name,
			SpriteHeader[]? spriteHeaders = null,
			SpriteGroup[]? spriteGroups = null,
			RoomObjectHeader[]? roomObjectHeaders = null,
			RoomObjectGroup[]? roomObjectGroups = null )
		{
			return new Room(
				header: new Header { Name = name, Width = 100, Height = 50, Unknown9 = 1 },
				staticSpriteHeaderList: new List<StaticSpriteHeader>(),
				spriteHeaderList: new List<SpriteHeader>( spriteHeaders ?? new SpriteHeader[0] ),
				unknown6HeaderList: new List<Unknown6Header>(),
				roomObjectHeaderList: new List<RoomObjectHeader>( roomObjectHeaders ?? new RoomObjectHeader[0] ),
				unknown5HeaderList: new List<Unknown5Header>(),
				staticSpriteList: new List<List<StaticSprite>>(),
				spriteGroupList: new List<SpriteGroup>( spriteGroups ?? new SpriteGroup[0] ),
				unknown6List: new List<Unknown6>(),
				roomObjectGroupList: new List<RoomObjectGroup>( roomObjectGroups ?? new RoomObjectGroup[0] ),
				unknown5List: new List<Unknown5>()
			);
		}
	}
}
