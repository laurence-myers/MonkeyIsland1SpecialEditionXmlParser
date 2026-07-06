using System.IO;
using System.Linq;
using NUnit.Framework;
using CostumePacker = MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Packer;
using CostumeParser = MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Parser;
using CostumeSanityChecker = MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.SanityChecker;

namespace Tests
{
	[TestFixture]
	public class CostumePackerRoundTripTests
	{
		private static string FixturePath( string fixtureName )
		{
			return Path.Combine( TestContext.CurrentContext.TestDirectory, "Fixtures", fixtureName + ".dat" );
		}

		/// <summary>
		/// The strongest regression test for the packer: parsing an untouched game costume and
		/// writing it back must reproduce the original file byte-for-byte. The four fixtures
		/// cover the format's corner cases:
		/// - guybrush: path points, and a trailing empty track whose (flushed) padding the
		///   file ends with,
		/// - stan: a shared sound name string, an empty sprite group, and a file that ends
		///   in the middle of a string's padding,
		/// - seaweed: a non-zero FirstSpriteIdentifier, empty frame lists sharing the next
		///   list's address, and a path point address stored despite a zero count,
		/// - f-pirate1: a path point list whose size is an exact multiple of 16 (which the
		///   old parser mis-padded), plus several sound names.
		/// (The full round-trip was verified against all 122 costumes in the retail pak.)
		/// </summary>
		[TestCase( "001 - guybrush-skin" )]
		[TestCase( "021 - stan-skin" )]
		[TestCase( "064 - seaweed-skin" )]
		[TestCase( "087 - f-pirate1-skin" )]
		public void WriteUnmodifiedCostume_IsByteIdenticalToOriginal( string fixtureName )
		{
			var fixturePath = FixturePath( fixtureName );
			var original = File.ReadAllBytes( fixturePath );
			var tempDatPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".dat" );

			try
			{
				var costume = CostumeParser.ReadCostumeFromBinaryFile( fixturePath );
				CostumePacker.WriteCostumeToBinaryFile( tempDatPath, costume );

				var written = File.ReadAllBytes( tempDatPath );
				Assert.That( written, Is.EqualTo( original ), "Repacked costume should be byte-identical to the original" );
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
				var costume = CostumeParser.ReadCostumeFromBinaryFile( FixturePath( "001 - guybrush-skin" ) );
				Assert.That( costume.SpriteGroupList.Count, Is.GreaterThan( 0 ), "Fixture should contain sprite groups" );

				// Act: the edits a spritesheet editor user would make
				var sprite = costume.SpriteGroupList[0].SpriteList[0];
				var editedTextureX = sprite.TextureX + 3;
				var editedScreenX = sprite.ScreenX + 5.5f;
				var editedScreenY = sprite.ScreenY - 2.25f;
				sprite.TextureX = editedTextureX;
				sprite.ScreenX = editedScreenX;
				sprite.ScreenY = editedScreenY;

				CostumePacker.WriteCostumeToBinaryFile( tempDatPath, costume );

				var roundTrippedCostume = CostumeParser.ReadCostumeFromBinaryFile( tempDatPath );
				CostumeSanityChecker.Check( roundTrippedCostume );

				// Assert
				var roundTrippedSprite = roundTrippedCostume.SpriteGroupList[0].SpriteList[0];
				Assert.That( roundTrippedSprite.TextureX, Is.EqualTo( editedTextureX ) );
				Assert.That( roundTrippedSprite.ScreenX, Is.EqualTo( editedScreenX ) );
				Assert.That( roundTrippedSprite.ScreenY, Is.EqualTo( editedScreenY ) );

				// the rest of the costume survives untouched
				Assert.That( roundTrippedCostume.Header.Identifier, Is.EqualTo( costume.Header.Identifier ) );
				Assert.That( roundTrippedCostume.Header.Name, Is.EqualTo( costume.Header.Name ) );
				Assert.That( roundTrippedCostume.AnimationList.Count, Is.EqualTo( costume.AnimationList.Count ) );
				Assert.That( roundTrippedCostume.SpriteGroupList.Count, Is.EqualTo( costume.SpriteGroupList.Count ) );
				Assert.That( roundTrippedCostume.PathPointList.Count, Is.EqualTo( costume.PathPointList.Count ) );
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
		/// Frames can carry a "play sound" command whose sound name is stored as a shared
		/// string at the end of the file. The old parser dropped these entirely; make sure
		/// they are read and survive a round-trip.
		/// </summary>
		[Test]
		public void SoundNames_AreParsedAndSurviveRoundTrip()
		{
			var costume = CostumeParser.ReadCostumeFromBinaryFile( FixturePath( "021 - stan-skin" ) );

			var soundFrames = costume.AnimationList
				.SelectMany( a => a.AnimationFrameList )
				.SelectMany( t => t.FrameList! )
				.Where( f => f.SoundName != null )
				.ToArray();

			Assert.That( soundFrames, Is.Not.Empty, "stan should reference a sound" );
			Assert.That( soundFrames.Select( f => f.SoundName ).Distinct().Single(), Is.EqualTo( "211_Stan_Punched" ) );
			Assert.That( soundFrames.All( f => f.Command == MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities.Frame.PlaySoundCommand ), Is.True );
		}

		/// <summary>
		/// Frame sprite identifiers are relative to the group's FirstSpriteIdentifier;
		/// seaweed's single group starts at 5, so resolving must subtract the base.
		/// </summary>
		[Test]
		public void FirstSpriteIdentifier_ResolvesFrameSprites()
		{
			var costume = CostumeParser.ReadCostumeFromBinaryFile( FixturePath( "064 - seaweed-skin" ) );

			var spriteGroup = costume.SpriteGroupList.Single();
			Assert.That( spriteGroup.FirstSpriteIdentifier, Is.EqualTo( 5 ) );
			Assert.That( spriteGroup.SpriteList.Count, Is.EqualTo( 18 ) );

			var initFront = costume.AnimationList.Single( a => a.Name == "InitFront" );
			var frames = initFront.AnimationFrameList.Single().FrameList!;
			Assert.That( frames.Count, Is.EqualTo( 36 ) );
			Assert.That( frames[0].SpriteIdentifier, Is.EqualTo( 5 ), "first frame shows the group's first sprite" );
			Assert.That( spriteGroup.ResolveSprite( frames[0] ), Is.SameAs( spriteGroup.SpriteList[0] ) );
			Assert.That( frames[frames.Count - 1].SpriteIdentifier, Is.EqualTo( 22 ) );
			Assert.That( spriteGroup.ResolveSprite( frames[frames.Count - 1] ), Is.SameAs( spriteGroup.SpriteList[17] ) );
		}
	}
}
