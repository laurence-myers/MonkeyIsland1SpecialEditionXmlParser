using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using MonkeyIslandSpecialEditionSpriteEditor.Commands;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class ImportNewTexturePngCommandTests
	{
		private string tempDir = null!;
		private LPAKFile lpakFile = null!;

		[SetUp]
		public void SetUp()
		{
			this.tempDir = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() );
			Directory.CreateDirectory( this.tempDir );
			this.lpakFile = new LPAKFile
			{
				FileNameOnDisk = Path.Combine( this.tempDir, "Monkey1.pak" ),
				PakFileNames = new[] { new PakFileName { FileName = "art/existing.dxt" } },
			};
		}

		[TearDown]
		public void TearDown()
		{
			if( Directory.Exists( this.tempDir ) )
			{
				Directory.Delete( this.tempDir, recursive: true );
			}
		}

		private string WritePng( int width, int height, bool withAlpha )
		{
			var path = Path.Combine( this.tempDir, Path.GetRandomFileName() + ".png" );
			using( var bitmap = new Bitmap( width, height, PixelFormat.Format32bppArgb ) )
			{
				using( var graphics = Graphics.FromImage( bitmap ) )
				{
					graphics.Clear( withAlpha ? Color.FromArgb( 128, 10, 20, 30 ) : Color.FromArgb( 255, 10, 20, 30 ) );
				}
				bitmap.Save( path, ImageFormat.Png );
			}
			return path;
		}

		[Test]
		public void Import_ValidOpaquePng_WritesLooseDxt1WithHeader()
		{
			var png = this.WritePng( 8, 8, withAlpha: false );

			var result = ImportNewTexturePngCommand.Import( this.lpakFile, "art/custom/new.dxt", png, "Auto", overwriteExisting: false );

			Assert.That( result.IsSuccess, Is.True, result.Error );
			var written = Path.Combine( this.tempDir, "art", "custom", "new.dxt" );
			Assert.That( File.Exists( written ), Is.True, "the loose .dxt should be written next to the pak" );
			var bytes = File.ReadAllBytes( written );
			Assert.That( Encoding.ASCII.GetString( bytes, 0, 4 ), Is.EqualTo( "DXT1" ), "an opaque PNG auto-detects DXT1" );
			Assert.That( BitConverter.ToInt32( bytes, 4 ), Is.EqualTo( 8 ), "width in the wrapper header" );
			Assert.That( BitConverter.ToInt32( bytes, 8 ), Is.EqualTo( 8 ), "height in the wrapper header" );
		}

		[Test]
		public void Import_TranslucentPng_AutoDetectsDxt5()
		{
			var png = this.WritePng( 8, 8, withAlpha: true );

			var result = ImportNewTexturePngCommand.Import( this.lpakFile, "art/custom/alpha.dxt", png, null, overwriteExisting: false );

			Assert.That( result.IsSuccess, Is.True, result.Error );
			var bytes = File.ReadAllBytes( Path.Combine( this.tempDir, "art", "custom", "alpha.dxt" ) );
			Assert.That( Encoding.ASCII.GetString( bytes, 0, 4 ), Is.EqualTo( "DXT5" ) );
		}

		[Test]
		public void Import_DimensionsNotMultipleOfFour_Fails()
		{
			var png = this.WritePng( 30, 30, withAlpha: false );

			var result = ImportNewTexturePngCommand.Import( this.lpakFile, "art/custom/bad.dxt", png, "Auto", overwriteExisting: false );

			Assert.That( result.IsSuccess, Is.False );
			Assert.That( result.Error, Does.Contain( "multiple of 4" ) );
		}

		[Test]
		public void ValidateResourcePath_ExactPakEntry_Fails()
		{
			var result = ImportNewTexturePngCommand.ValidateResourcePath( this.lpakFile, "art/existing.dxt", overwriteExisting: false );
			Assert.That( result.IsSuccess, Is.False );
		}

		[Test]
		public void ValidateResourcePath_CaseDifferingPakEntry_Fails()
		{
			var result = ImportNewTexturePngCommand.ValidateResourcePath( this.lpakFile, "ART/EXISTING.dxt", overwriteExisting: false );
			Assert.That( result.IsSuccess, Is.False, "a case-only difference would shadow the packed entry on a case-insensitive filesystem" );
		}

		[Test]
		public void ValidateResourcePath_ExistingLooseFile_RequiresOverwrite()
		{
			var loosePath = Path.Combine( this.tempDir, "art", "custom", "dup.dxt" );
			Directory.CreateDirectory( Path.GetDirectoryName( loosePath )! );
			File.WriteAllBytes( loosePath, new byte[16] );

			var without = ImportNewTexturePngCommand.ValidateResourcePath( this.lpakFile, "art/custom/dup.dxt", overwriteExisting: false );
			Assert.That( without.IsSuccess, Is.False, "an existing loose file needs an explicit overwrite" );

			var with = ImportNewTexturePngCommand.ValidateResourcePath( this.lpakFile, "art/custom/dup.dxt", overwriteExisting: true );
			Assert.That( with.IsSuccess, Is.True, with.Error );
		}

		[TestCase( "art/custom/bad.png" )]      // not a .dxt
		[TestCase( "../escape.dxt" )]           // traversal
		[TestCase( "art/../../escape.dxt" )]    // traversal
		[TestCase( "art/custom/nöng.dxt" )] // non-ascii (o-umlaut)
		[TestCase( "art\\custom\\back.dxt" )]   // backslashes
		public void ValidateResourcePath_InvalidPaths_Fail( string resourcePath )
		{
			var result = ImportNewTexturePngCommand.ValidateResourcePath( this.lpakFile, resourcePath, overwriteExisting: false );
			Assert.That( result.IsSuccess, Is.False );
		}

		[Test]
		public void ValidateResourcePath_RootedPath_Fails()
		{
			var result = ImportNewTexturePngCommand.ValidateResourcePath( this.lpakFile, "C:/art/custom/rooted.dxt", overwriteExisting: false );
			Assert.That( result.IsSuccess, Is.False );
		}

		[Test]
		public void ValidateResourcePath_ValidNewPath_Succeeds()
		{
			var result = ImportNewTexturePngCommand.ValidateResourcePath( this.lpakFile, "art/custom/fresh.dxt", overwriteExisting: false );
			Assert.That( result.IsSuccess, Is.True, result.Error );
		}
	}
}
