using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;
using NUnit.Framework;
using ScummParser = MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Parser;

namespace Tests
{
	[TestFixture]
	public class ScummPatchTests
	{
		[Test]
		public void BuildAndApply_RoundTripsTheEdit()
		{
			var pristine = BuildResourceFile(
				( 10, new[] { MakeBox( 0, 0, 10, 20, 0, 0, 255 ), MakeBox( 10, 0, 30, 40, 1, 0, 200 ) } ),
				( 20, new[] { MakeBox( 5, 5, 15, 25, 2, 0, 100 ) } ) );
			var edited = BuildResourceFile(
				( 10, new[] { MakeBox( 1, 2, 11, 22, 3, 0, 128 ), MakeBox( 10, 0, 30, 40, 1, 0, 200 ) } ),
				( 20, new[] { MakeBox( 5, 5, 15, 25, 2, 0, 100 ) } ) );

			var patch = ClassicPatcher.BuildPatch( pristine, edited, new ClassicPatchInfo { Name = "test" }, "TestVariant", "1.0" );
			Assert.That( patch, Is.Not.Null );
			Assert.That( patch!.RoomEdits.Count, Is.EqualTo( 1 ), "only room 10 changed" );
			Assert.That( patch.RoomEdits[0].Number, Is.EqualTo( 10 ) );
			Assert.That( patch.RoomEdits[0].Boxes.Count, Is.EqualTo( 1 ), "only box 0 changed" );
			Assert.That( patch.RoomEdits[0].Boxes[0].Index, Is.EqualTo( 0 ) );

			// apply to a fresh pristine install
			var result = ClassicPatcher.Apply( pristine, pristine, patch );
			Assert.That( result.Ok, Is.True );
			Assert.That( result.Changed, Is.True );

			var rooms = ScummParser.ReadRoomsFromEncodedBytes( result.EncodedBytes! );
			var room10 = rooms.First( r => r.RoomNumber == 10 );
			Assert.That( room10.BoxList[0].CornerList[0], Is.EqualTo( new Point( 1, 2 ) ) );
			Assert.That( room10.BoxList[0].Mask, Is.EqualTo( 3 ) );
			Assert.That( room10.BoxList[0].Scale, Is.EqualTo( 128 ) );
			Assert.That( room10.BoxList[1].Mask, Is.EqualTo( 1 ), "unchanged box preserved" );
			var room20 = rooms.First( r => r.RoomNumber == 20 );
			Assert.That( room20.BoxList[0].Mask, Is.EqualTo( 2 ), "other room untouched" );
		}

		[Test]
		public void Apply_IsIdempotent()
		{
			var pristine = BuildResourceFile( ( 10, new[] { MakeBox( 0, 0, 10, 20, 0, 0, 255 ) } ) );
			var edited = BuildResourceFile( ( 10, new[] { MakeBox( 1, 1, 11, 21, 5, 0, 255 ) } ) );
			var patch = ClassicPatcher.BuildPatch( pristine, edited, new ClassicPatchInfo(), "V", "1.0" )!;

			var first = ClassicPatcher.Apply( pristine, pristine, patch );
			Assert.That( first.Changed, Is.True );

			// applying again over the already-patched data is a successful no-op
			var second = ClassicPatcher.Apply( pristine, first.EncodedBytes!, patch );
			Assert.That( second.Ok, Is.True );
			Assert.That( second.Changed, Is.False );
		}

		[Test]
		public void Apply_WrongGameVariant_Refuses()
		{
			var pristine = BuildResourceFile( ( 10, new[] { MakeBox( 0, 0, 10, 20, 0, 0, 255 ) } ) );
			var edited = BuildResourceFile( ( 10, new[] { MakeBox( 1, 1, 11, 21, 5, 0, 255 ) } ) );
			var patch = ClassicPatcher.BuildPatch( pristine, edited, new ClassicPatchInfo(), "V", "1.0" )!;

			// a different install (different box geometry) doesn't match the fingerprint
			var otherPristine = BuildResourceFile( ( 10, new[] { MakeBox( 9, 9, 19, 29, 7, 0, 111 ) } ) );
			var result = ClassicPatcher.Apply( otherPristine, otherPristine, patch );
			Assert.That( result.Ok, Is.False );
		}

		[Test]
		public void Apply_RoomChangedByAnotherEdit_Refuses()
		{
			var pristine = BuildResourceFile( ( 10, new[] { MakeBox( 0, 0, 10, 20, 0, 0, 255 ) } ) );
			var edited = BuildResourceFile( ( 10, new[] { MakeBox( 1, 1, 11, 21, 5, 0, 255 ) } ) );
			var patch = ClassicPatcher.BuildPatch( pristine, edited, new ClassicPatchInfo(), "V", "1.0" )!;

			// current data has room 10 changed to something that is neither pristine nor the patch result
			var foreign = BuildResourceFile( ( 10, new[] { MakeBox( 3, 3, 13, 23, 9, 0, 50 ) } ) );
			var result = ClassicPatcher.Apply( pristine, foreign, patch );
			Assert.That( result.Ok, Is.False );
		}

		[Test]
		public void Remove_RestoresPristine()
		{
			var pristine = BuildResourceFile( ( 10, new[] { MakeBox( 0, 0, 10, 20, 0, 0, 255 ) } ), ( 20, new[] { MakeBox( 5, 5, 15, 25, 2, 0, 100 ) } ) );
			var edited = BuildResourceFile( ( 10, new[] { MakeBox( 1, 1, 11, 21, 5, 0, 255 ) } ), ( 20, new[] { MakeBox( 5, 5, 15, 25, 2, 0, 100 ) } ) );
			var patch = ClassicPatcher.BuildPatch( pristine, edited, new ClassicPatchInfo(), "V", "1.0" )!;

			var applied = ClassicPatcher.Apply( pristine, pristine, patch );
			var removed = ClassicPatcher.Remove( pristine, applied.EncodedBytes!, patch );
			Assert.That( removed.Ok, Is.True );
			Assert.That( removed.Changed, Is.True );

			var rooms = ScummParser.ReadRoomsFromEncodedBytes( removed.EncodedBytes! );
			Assert.That( rooms.First( r => r.RoomNumber == 10 ).BoxList[0].CornerList[0], Is.EqualTo( new Point( 0, 0 ) ) );
			Assert.That( rooms.First( r => r.RoomNumber == 10 ).BoxList[0].Mask, Is.EqualTo( 0 ) );
		}

		[Test]
		public void Remove_WhenNotApplied_IsNoOp()
		{
			var pristine = BuildResourceFile( ( 10, new[] { MakeBox( 0, 0, 10, 20, 0, 0, 255 ) } ) );
			var edited = BuildResourceFile( ( 10, new[] { MakeBox( 1, 1, 11, 21, 5, 0, 255 ) } ) );
			var patch = ClassicPatcher.BuildPatch( pristine, edited, new ClassicPatchInfo(), "V", "1.0" )!;

			var removed = ClassicPatcher.Remove( pristine, pristine, patch );
			Assert.That( removed.Ok, Is.True );
			Assert.That( removed.Changed, Is.False );
		}

		[Test]
		public void BuildPatch_NoDifferences_ReturnsNull()
		{
			var pristine = BuildResourceFile( ( 10, new[] { MakeBox( 0, 0, 10, 20, 0, 0, 255 ) } ) );
			var patch = ClassicPatcher.BuildPatch( pristine, pristine, new ClassicPatchInfo(), "V", "1.0" );
			Assert.That( patch, Is.Null );
		}

		[Test]
		public void Patch_XmlRoundTrips()
		{
			var pristine = BuildResourceFile( ( 10, new[] { MakeBox( 0, 0, 10, 20, 0, 0, 255 ) } ) );
			var edited = BuildResourceFile( ( 10, new[] { MakeBox( 1, 2, 11, 22, 5, 128, 200 ) } ) );
			var patch = ClassicPatcher.BuildPatch( pristine, edited, new ClassicPatchInfo { Name = "n", Author = "a" }, "V", "1.0" )!;

			var path = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".xml" );
			try
			{
				MonkeyIslandSpecialEditionSpriteEditor.Helper.WriteObjectToFile( path, patch );
				var loaded = MonkeyIslandSpecialEditionSpriteEditor.Helper.ReadObjectFromFile<ClassicPatch>( path );
				Assert.That( loaded.Validate(), Is.Null );
				Assert.That( loaded.RoomEdits[0].Boxes[0].Mask, Is.EqualTo( 5 ) );
				Assert.That( loaded.RoomEdits[0].Boxes[0].Flags, Is.EqualTo( 128 ) );
				Assert.That( loaded.BaseFingerprints[0].Label, Is.EqualTo( "V" ) );

				// the loaded patch still applies
				var result = ClassicPatcher.Apply( pristine, pristine, loaded );
				Assert.That( result.Ok, Is.True );
				Assert.That( result.Changed, Is.True );
			}
			finally
			{
				if( File.Exists( path ) )
				{
					File.Delete( path );
				}
			}
		}

		//-------------------------------------------
		// synthetic resource-file builders (mirror ScummPackerTests)

		private static ClassicBox MakeBox( int ulx, int uly, int lrx, int lry, int mask, int flags, int scale )
		{
			var corners = new[]
			{
				new Point( ulx, uly ),
				new Point( lrx, uly ),
				new Point( lrx, lry ),
				new Point( ulx, lry ),
			};
			return new ClassicBox( corners, mask, flags, scale );
		}

		private static byte[] BuildResourceFile( params (int RoomNumber, ClassicBox[] Boxes)[] rooms )
		{
			var lflfs = rooms.Select( r => Block( "LFLF", Block( "ROOM", Block( "RMHD", RmhdPayload( 320, 200 ) ), Block( "BOXD", BoxdPayload( r.Boxes ) ) ) ) ).ToArray();

			var loffLength = 8 + 1 + rooms.Length * 5;
			var loffPayload = new List<byte> { (byte)rooms.Length };
			var position = 8 + loffLength;
			for( var index = 0; index < rooms.Length; index++ )
			{
				var roomOffset = position + 8;
				loffPayload.Add( (byte)rooms[index].RoomNumber );
				loffPayload.AddRange( BitConverter.GetBytes( (uint)roomOffset ) );
				position += lflfs[index].Length;
			}

			var loff = Block( "LOFF", loffPayload.ToArray() );
			var lecf = Block( "LECF", new[] { loff }.Concat( lflfs ).ToArray() );
			ScummParser.XorDecode( lecf, ScummParser.XorKey );
			return lecf;
		}

		private static byte[] BoxdPayload( ClassicBox[] boxes )
		{
			var bytes = new List<byte>();
			bytes.AddRange( BitConverter.GetBytes( (ushort)boxes.Length ) );
			foreach( var box in boxes )
			{
				foreach( var corner in box.CornerList )
				{
					bytes.AddRange( BitConverter.GetBytes( (short)corner.X ) );
					bytes.AddRange( BitConverter.GetBytes( (short)corner.Y ) );
				}
				bytes.Add( (byte)box.Mask );
				bytes.Add( (byte)box.Flags );
				bytes.AddRange( BitConverter.GetBytes( (ushort)box.Scale ) );
			}
			return bytes.ToArray();
		}

		private static byte[] RmhdPayload( int width, int height )
		{
			var bytes = new List<byte>();
			bytes.AddRange( BitConverter.GetBytes( (ushort)width ) );
			bytes.AddRange( BitConverter.GetBytes( (ushort)height ) );
			bytes.AddRange( BitConverter.GetBytes( (ushort)0 ) );
			return bytes.ToArray();
		}

		private static byte[] Block( string tag, params byte[][] payloads )
		{
			var payload = payloads.SelectMany( p => p ).ToArray();
			var size = payload.Length + 8;
			var bytes = new List<byte>();
			bytes.AddRange( Encoding.ASCII.GetBytes( tag ) );
			bytes.Add( (byte)( ( size >> 24 ) & 0xFF ) );
			bytes.Add( (byte)( ( size >> 16 ) & 0xFF ) );
			bytes.Add( (byte)( ( size >> 8 ) & 0xFF ) );
			bytes.Add( (byte)( size & 0xFF ) );
			bytes.AddRange( payload );
			return bytes.ToArray();
		}
	}
}
