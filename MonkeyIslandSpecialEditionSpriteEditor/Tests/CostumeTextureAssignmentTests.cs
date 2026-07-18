using System.IO;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Entities;
using NUnit.Framework;
using CostumeParser = MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Parser;

namespace Tests
{
	[TestFixture]
	public class CostumeTextureAssignmentTests
	{
		private static Costume LoadGuybrush()
		{
			var path = Path.Combine( TestContext.CurrentContext.TestDirectory, "Fixtures", "001 - guybrush-skin.dat" );
			return CostumeParser.ReadCostumeFromBinaryFile( path );
		}

		[Test]
		public void GetOrAddTextureIndex_ExistingPath_ReturnsExistingIndexWithoutAppending()
		{
			var costume = LoadGuybrush();
			Assert.That( costume.TextureFileNameList.Count, Is.GreaterThan( 0 ), "fixture should list textures" );

			var existingPath = costume.TextureFileNameList[0].Path;
			var originalCount = costume.TextureFileNameList.Count;

			var index = TextureAssignment.GetOrAddTextureIndex( costume, existingPath );

			Assert.That( index, Is.EqualTo( 0 ) );
			Assert.That( costume.TextureFileNameList.Count, Is.EqualTo( originalCount ), "an existing path must not append" );
		}

		[Test]
		public void GetOrAddTextureIndex_NewPath_AppendsEntryAndReturnsNewIndex()
		{
			var costume = LoadGuybrush();
			var originalCount = costume.TextureFileNameList.Count;
			const string newPath = "art/custom/test-skin.dxt";

			var index = TextureAssignment.GetOrAddTextureIndex( costume, newPath );

			Assert.That( index, Is.EqualTo( originalCount ), "the new entry lands at the end of the list" );
			Assert.That( costume.TextureFileNameList.Count, Is.EqualTo( originalCount + 1 ) );
			Assert.That( costume.TextureFileNameList[index].Path, Is.EqualTo( newPath ) );
			Assert.That( costume.TextureFileNameList[index].Index, Is.EqualTo( index ), "the appended entry records its own list position" );
		}

		[Test]
		public void GetOrAddTextureIndex_DuplicatePaths_ReturnsFirstOccurrence()
		{
			var costume = LoadGuybrush();
			var duplicatePath = costume.TextureFileNameList[0].Path;
			// force a second entry with the same path further down the list
			costume.TextureFileNameList.Add( new TextureFileName( duplicatePath, costume.TextureFileNameList.Count ) );

			var index = TextureAssignment.GetOrAddTextureIndex( costume, duplicatePath );

			Assert.That( index, Is.EqualTo( 0 ), "the first occurrence wins" );
		}

		[Test]
		public void GetOrAddTextureIndex_CaseDifferingPath_AppendsSeparateEntry()
		{
			var costume = LoadGuybrush();
			var originalCount = costume.TextureFileNameList.Count;
			var existingPath = costume.TextureFileNameList[0].Path;
			var upperCasePath = existingPath.ToUpperInvariant();
			Assume.That( upperCasePath, Is.Not.EqualTo( existingPath ), "fixture path must have letters to change case" );

			var index = TextureAssignment.GetOrAddTextureIndex( costume, upperCasePath );

			Assert.That( index, Is.EqualTo( originalCount ), "a case-differing path is treated as new (matching the case-sensitive pak lookup)" );
			Assert.That( costume.TextureFileNameList.Count, Is.EqualTo( originalCount + 1 ) );
		}
	}
}
