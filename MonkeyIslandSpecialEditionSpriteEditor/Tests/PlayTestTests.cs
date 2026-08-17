using System;
using System.IO;
using System.Linq;
using MonkeyIslandSpecialEditionSpriteEditor.PlayTest;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class PlayTestTests
	{
		private string root = "";

		[SetUp]
		public void SetUp()
		{
			this.root = Path.Combine( Path.GetTempPath(), "mi1se-playtest-" + Guid.NewGuid().ToString( "N" ) );
			Directory.CreateDirectory( this.root );
		}

		[TearDown]
		public void TearDown()
		{
			if( Directory.Exists( this.root ) )
			{
				Directory.Delete( this.root, true );
			}
		}

		private string Touch( string relative, int bytes = 4 )
		{
			var full = Path.Combine( this.root, relative.Replace( '/', Path.DirectorySeparatorChar ) );
			Directory.CreateDirectory( Path.GetDirectoryName( full )! );
			File.WriteAllBytes( full, new byte[bytes] );
			return full;
		}

		[Test]
		public void ResolveExePath_FindsTheGameExeNextToThePak()
		{
			Assert.That( GameLauncher.ResolveExePath( this.root ), Is.Null );
			var exe = this.Touch( GameLauncher.ExeName );
			Assert.That( GameLauncher.ResolveExePath( this.root ), Is.EqualTo( exe ) );
			Assert.That( GameLauncher.ResolveExePath( null ), Is.Null );
		}

		[Test]
		public void SteamRunUrl_TargetsTheSpecialEditionAppId()
		{
			Assert.That( GameLauncher.SteamRunUrl, Is.EqualTo( "steam://rungameid/32360" ) );
		}

		[TestCase( "art/rooms/28_bar.room.xml", OverrideKind.Room )]
		[TestCase( @"art\costumes\24_leaders-skin.costume.xml", OverrideKind.Costume )]
		[TestCase( "art/rooms/images/28_bar/layer0_chunk_0_0.dxt", OverrideKind.Texture )]
		[TestCase( "art/rooms/images/28_bar/layer0_chunk_0_0.dds", OverrideKind.Texture )]
		[TestCase( "overrides/art/rooms/28_bar.room.xml", OverrideKind.OverrideXml )]
		[TestCase( "classic/en/monkey1.001", OverrideKind.Classic )]
		[TestCase( "art/rooms/28_bar.room.xml.bak", OverrideKind.Other )]
		public void Classify_JudgesTheKindFromThePath( string path, OverrideKind expected )
		{
			Assert.That( OverrideScanner.Classify( path ), Is.EqualTo( expected ) );
		}

		[Test]
		public void Scan_ListsLooseFilesUnderTheOverrideFolders_WithPakMembership()
		{
			this.Touch( "art/rooms/28_bar.room.xml" );
			this.Touch( "art/rooms/images/28_bar/new_sign.dxt", 16 );
			this.Touch( "overrides/art/rooms/28_bar.room.xml" );
			this.Touch( "audio/ignored.wav" ); // not an override folder
			this.Touch( "Monkey1.pak" );

			// the pak "knows" the room but not the brand-new texture
			var entries = OverrideScanner.Scan( this.root, rel => rel == "art/rooms/28_bar.room.xml" );

			Assert.That( entries.Select( e => e.RelativePath ), Is.EqualTo( new[]
			{
				"art/rooms/28_bar.room.xml",
				"art/rooms/images/28_bar/new_sign.dxt",
				"overrides/art/rooms/28_bar.room.xml",
			} ) );

			var room = entries[0];
			Assert.That( room.Kind, Is.EqualTo( OverrideKind.Room ) );
			Assert.That( room.InPak, Is.True );

			var texture = entries[1];
			Assert.That( texture.Kind, Is.EqualTo( OverrideKind.Texture ) );
			Assert.That( texture.InPak, Is.False, "a new texture is not a pak entry" );
			Assert.That( texture.Length, Is.EqualTo( 16 ) );

			// XML mirrors are never pak entries; membership is left unknown
			Assert.That( entries[2].Kind, Is.EqualTo( OverrideKind.OverrideXml ) );
			Assert.That( entries[2].InPak, Is.Null );
		}

		[Test]
		public void Scan_IsEmptyForAMissingOrBareFolder()
		{
			Assert.That( OverrideScanner.Scan( null, null ), Is.Empty );
			Assert.That( OverrideScanner.Scan( Path.Combine( this.root, "nope" ), null ), Is.Empty );
			Assert.That( OverrideScanner.Scan( this.root, null ), Is.Empty );
		}

		[Test]
		public void Revert_DeletesTheLooseFile()
		{
			var full = this.Touch( "art/rooms/28_bar.room.xml" );
			var entry = OverrideScanner.Scan( this.root, null ).Single();
			OverrideScanner.Revert( entry );
			Assert.That( File.Exists( full ), Is.False );
			// reverting twice is harmless
			Assert.DoesNotThrow( () => OverrideScanner.Revert( entry ) );
		}
	}
}
