using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using MonkeyIslandSpecialEditionSpriteEditor;
using NUnit.Framework;
using CostumeParser = MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Parser;
using RoomParser = MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Parser;

namespace Tests
{
	[TestFixture]
	public class HelperTests
	{
		public static IEnumerable WriteObjectToXmlFile_WritesValidXml_TestCases()
		{
			yield return new TestCaseData(
				"001 - guybrush-skin",
				(Func<string, object>)( CostumeParser.ReadCostumeFromBinaryFile )
			);
			yield return new TestCaseData(
				"028 - bar",
				(Func<string, object>)( RoomParser.ReadRoomFromBinaryFile )
				);
		}
		
		[Test, TestCaseSource(nameof(HelperTests.WriteObjectToXmlFile_WritesValidXml_TestCases))]
		public void WriteObjectToXmlFile_WritesValidXml(string fixtureName, Func<string, object> parseFromBinary)
		{
			// Arrange
			var datFilePath = Path.Combine( TestContext.CurrentContext.TestDirectory, "Fixtures", fixtureName + ".dat" );
			var expectedXmlPath = Path.Combine( TestContext.CurrentContext.TestDirectory, "Fixtures", fixtureName + ".xml" );
			var tempFilePath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".xml" );

			try
			{
				// Act
				var entity = parseFromBinary( datFilePath );
				Helper.WriteObjectToFile( tempFilePath, entity );

				// Assert
				Assert.That( File.Exists( tempFilePath ), Is.True, "Temporary file should exist" );

				var expectedDoc = XDocument.Load( expectedXmlPath );
				var actualDoc = XDocument.Load( tempFilePath );

				Assert.That( ToComparableXml( actualDoc ), Is.EqualTo( ToComparableXml( expectedDoc ) ), "XML content should match (ignoring Address values)" );
			}
			finally
			{
				// Cleanup
				if( File.Exists( tempFilePath ) )
				{
					File.Delete( tempFilePath );
				}
			}
		}

		public static IEnumerable ReadObjectFromXmlFile_ReadsValidXml_TestCases()
		{
			yield return new TestCaseData(
				"001 - guybrush-skin",
				(Func<string, object>)( CostumeParser.ReadCostumeFromXmlFile )
				);
			yield return new TestCaseData(
				"028 - bar",
				(Func<string, object>)( RoomParser.ReadRoomFromXmlFile )
				);
		}
		
		[Test, TestCaseSource(nameof(HelperTests.ReadObjectFromXmlFile_ReadsValidXml_TestCases))]
		public void ReadObjectFromXmlFile_ReadsValidXml(string fixtureName, Func<string, object> parseFromXml)
		{
			// Arrange
			var xmlPath = Path.Combine( TestContext.CurrentContext.TestDirectory, "Fixtures", fixtureName + ".xml" );
			var tempFilePath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".xml" );

			try
			{
				// Act
				var entity = parseFromXml( xmlPath );
				Helper.WriteObjectToFile( tempFilePath, entity );

				// Assert
				Assert.That( File.Exists( tempFilePath ), Is.True, "Temporary file should exist" );

				var expectedDoc = XDocument.Load( xmlPath );
				var actualDoc = XDocument.Load( tempFilePath );

				Assert.That( ToComparableXml( actualDoc ), Is.EqualTo( ToComparableXml( expectedDoc ) ), "XML content should match after round-trip" );
			}
			finally
			{
				// Cleanup
				if( File.Exists( tempFilePath ) )
				{
					File.Delete( tempFilePath );
				}
			}
		}

		public static IEnumerable RoundTripFromBinary_TestCases()
		{
			yield return new TestCaseData(
				"001 - guybrush-skin",
				(Func<string, object>)( CostumeParser.ReadCostumeFromBinaryFile ),
				(Action<string, object>)( ( path, obj ) => MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Packer.WriteCostumeToBinaryFile( path, (MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Entities.Costume)obj ) )
			);
			yield return new TestCaseData(
				"028 - bar",
				(Func<string, object>)( MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Parser.ReadRoomFromBinaryFile ),
				(Action<string, object>)( ( path, obj ) => MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Packer.WriteRoomToBinaryFile( path, (MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities.Room)obj ) )
			);
		}

		[Test, TestCaseSource(nameof(HelperTests.RoundTripFromBinary_TestCases))]
		public void RoundTripFromBinary_ProducesSameValues(string fixtureName, Func<string, object> parseFromBinary, Action<string, object> writeToBinary)
		{
			// Arrange
			var datFilePath = Path.Combine( TestContext.CurrentContext.TestDirectory, "Fixtures", fixtureName + ".dat" );
			var tempDatPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".dat" );

			try
			{
				// Act
				var originalEntity = parseFromBinary( datFilePath );
				writeToBinary( tempDatPath, originalEntity );
				var roundTripEntity = parseFromBinary( tempDatPath );

				// Assert

				var originalXmlPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".xml" );
				var roundTripXmlPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".xml" );

				Helper.WriteObjectToFile( originalXmlPath, originalEntity );
				Helper.WriteObjectToFile( roundTripXmlPath, roundTripEntity );

				var originalDoc = XDocument.Load( originalXmlPath );
				var roundTripDoc = XDocument.Load( roundTripXmlPath );

				File.Delete( originalXmlPath );
				File.Delete( roundTripXmlPath );

				Assert.That( ToComparableXml( roundTripDoc ), Is.EqualTo( ToComparableXml( originalDoc ) ), "Round-tripped entity should match original entity" );
			}
			finally
			{
				if( File.Exists( tempDatPath ) )
				{
					File.Delete( tempDatPath );
				}
			}
		}

		public static IEnumerable RoundTripFromXml_TestCases()
		{
			yield return new TestCaseData(
				"001 - guybrush-skin",
				(Func<string, object>)( CostumeParser.ReadCostumeFromXmlFile ),
				(Func<string, object>)( CostumeParser.ReadCostumeFromBinaryFile ),
				(Action<string, object>)( ( path, obj ) => MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Packer.WriteCostumeToBinaryFile( path, (MonkeyIslandSpecialEditionSpriteEditor.Formats.Costumes.Entities.Costume)obj ) )
			);
			yield return new TestCaseData(
				"028 - bar",
				(Func<string, object>)( MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Parser.ReadRoomFromXmlFile ),
				(Func<string, object>)( MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Parser.ReadRoomFromBinaryFile ),
				(Action<string, object>)( ( path, obj ) => MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Packer.WriteRoomToBinaryFile( path, (MonkeyIslandSpecialEditionSpriteEditor.Formats.Rooms.Entities.Room)obj ) )
			);
		}

		[Test, TestCaseSource(nameof(HelperTests.RoundTripFromXml_TestCases))]
		public void RoundTripFromXml_ProducesSameValues(string fixtureName, Func<string, object> parseFromXml, Func<string, object> parseFromBinary, Action<string, object> writeToBinary)
		{
			// Arrange
			var xmlFilePath = Path.Combine( TestContext.CurrentContext.TestDirectory, "Fixtures", fixtureName + ".xml" );
			var tempDatPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".dat" );

			try
			{
				// Act
				var originalEntity = parseFromXml( xmlFilePath );
				writeToBinary( tempDatPath, originalEntity );
				var roundTripEntity = parseFromBinary( tempDatPath );

				// Assert
				var originalXmlPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".xml" );
				var roundTripXmlPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".xml" );

				Helper.WriteObjectToFile( originalXmlPath, originalEntity );
				Helper.WriteObjectToFile( roundTripXmlPath, roundTripEntity );

				var originalDoc = XDocument.Load( originalXmlPath );
				var roundTripDoc = XDocument.Load( roundTripXmlPath );

				File.Delete( originalXmlPath );
				File.Delete( roundTripXmlPath );

				Assert.That( ToComparableXml( roundTripDoc ), Is.EqualTo( ToComparableXml( originalDoc ) ), "Round-tripped entity from XML should match original entity" );
			}
			finally
			{
				if( File.Exists( tempDatPath ) )
				{
					File.Delete( tempDatPath );
				}
			}
		}

		[TestCase( "DXT1", 8 )]
		[TestCase( "DXT5", 16 )]
		public void DxtBytesFromImage_WritesValidHeaderAndBlockData( string fourCC, int blockSize )
		{
			// Arrange: 16x8 pixels = 4x2 blocks
			using( var bitmap = new System.Drawing.Bitmap( 16, 8 ) )
			{
				using( var graphics = System.Drawing.Graphics.FromImage( bitmap ) )
				{
					graphics.Clear( System.Drawing.Color.Red );
				}

				// Act
				var bytes = Helper.DxtBytesFromImage( bitmap, fourCC );

				// Assert: the game's 12 byte wrapper header
				Assert.That( System.Text.Encoding.ASCII.GetString( bytes, 0, 4 ), Is.EqualTo( fourCC ) );
				Assert.That( System.BitConverter.ToInt32( bytes, 4 ), Is.EqualTo( 16 ), "width" );
				Assert.That( System.BitConverter.ToInt32( bytes, 8 ), Is.EqualTo( 8 ), "height" );
				Assert.That( bytes.Length, Is.EqualTo( 12 + 4 * 2 * blockSize ), "block data size" );
			}
		}

		[Test]
		public void DxtBytesFromImage_SolidColor_EncodesMatchingEndpoints()
		{
			// Arrange: a solid color quantizes exactly to RGB565 endpoints
			using( var bitmap = new System.Drawing.Bitmap( 4, 4 ) )
			{
				using( var graphics = System.Drawing.Graphics.FromImage( bitmap ) )
				{
					graphics.Clear( System.Drawing.Color.FromArgb( 255, 255, 0, 0 ) );
				}

				// Act
				var bytes = Helper.DxtBytesFromImage( bitmap, "DXT1" );

				// Assert: both color endpoints of the single block should be pure red in 565
				var color0 = (ushort)( bytes[12] | ( bytes[13] << 8 ) );
				var color1 = (ushort)( bytes[14] | ( bytes[15] << 8 ) );
				Assert.That( color0, Is.EqualTo( 0xF800 ), "color0 should be pure red" );
				Assert.That( color1, Is.EqualTo( 0xF800 ), "color1 should be pure red" );
			}
		}

		[Test]
		public void DxtBytesFromImage_Dxt5_PreservesAlphaEndpoints()
		{
			// Arrange: a 4x4 block with alpha ranging between two values
			using( var bitmap = new System.Drawing.Bitmap( 4, 4 ) )
			{
				for( var y = 0; y < 4; y++ )
				{
					for( var x = 0; x < 4; x++ )
					{
						var alpha = y < 2 ? 32 : 224;
						bitmap.SetPixel( x, y, System.Drawing.Color.FromArgb( alpha, 0, 255, 0 ) );
					}
				}

				// Act
				var bytes = Helper.DxtBytesFromImage( bitmap, "DXT5" );

				// Assert: alpha0 = max, alpha1 = min (8 value palette mode)
				Assert.That( bytes[12], Is.EqualTo( 224 ), "alpha0" );
				Assert.That( bytes[13], Is.EqualTo( 32 ), "alpha1" );
			}
		}

		[Test]
		public void DxtBytesFromImage_UnknownFormat_Throws()
		{
			using( var bitmap = new System.Drawing.Bitmap( 4, 4 ) )
			{
				Assert.Throws<System.NotSupportedException>( () => Helper.DxtBytesFromImage( bitmap, "DXT3" ) );
			}
		}

		[Test]
		public void ImageFromDxtBytes_Dxt1_DecodesSolidColorsWithCorrectChannels()
		{
			// Arrange: four opaque solid 4x4 quadrants. Primary colors survive RGB565
			// quantization exactly, so a correct decode returns them exactly. A swapped
			// R/B channel would turn the red quadrant blue.
			using( var source = new System.Drawing.Bitmap( 8, 8 ) )
			{
				FillQuadrant( source, 0, 0, System.Drawing.Color.Red );
				FillQuadrant( source, 4, 0, System.Drawing.Color.Lime );
				FillQuadrant( source, 0, 4, System.Drawing.Color.Blue );
				FillQuadrant( source, 4, 4, System.Drawing.Color.White );

				var bytes = Helper.DxtBytesFromImage( source, "DXT1" );

				// Act
				using( var decoded = (System.Drawing.Bitmap)Helper.ImageFromDxtBytes( bytes ) )
				{
					// Assert
					Assert.That( decoded.Width, Is.EqualTo( 8 ) );
					Assert.That( decoded.Height, Is.EqualTo( 8 ) );
					Assert.That( decoded.GetPixel( 1, 1 ).ToArgb(), Is.EqualTo( System.Drawing.Color.Red.ToArgb() ), "red quadrant" );
					Assert.That( decoded.GetPixel( 5, 1 ).ToArgb(), Is.EqualTo( System.Drawing.Color.Lime.ToArgb() ), "green quadrant" );
					Assert.That( decoded.GetPixel( 1, 5 ).ToArgb(), Is.EqualTo( System.Drawing.Color.Blue.ToArgb() ), "blue quadrant" );
					Assert.That( decoded.GetPixel( 5, 5 ).ToArgb(), Is.EqualTo( System.Drawing.Color.White.ToArgb() ), "white quadrant" );
				}
			}
		}

		[Test]
		public void ImageFromDxtBytes_Dxt5_PreservesAlpha()
		{
			// Arrange: a uniform 4x4 block with alpha 128 (min == max, so the alpha decodes
			// exactly), color chosen away from 565 boundaries so it survives within tolerance.
			using( var source = new System.Drawing.Bitmap( 4, 4 ) )
			{
				FillQuadrant( source, 0, 0, System.Drawing.Color.FromArgb( 128, 10, 200, 90 ) );

				var bytes = Helper.DxtBytesFromImage( source, "DXT5" );

				// Act
				using( var decoded = (System.Drawing.Bitmap)Helper.ImageFromDxtBytes( bytes ) )
				{
					// Assert
					var pixel = decoded.GetPixel( 2, 2 );
					Assert.That( pixel.A, Is.EqualTo( 128 ), "alpha" );
					Assert.That( (int)pixel.R, Is.EqualTo( 10 ).Within( 12 ), "red" );
					Assert.That( (int)pixel.G, Is.EqualTo( 200 ).Within( 12 ), "green" );
					Assert.That( (int)pixel.B, Is.EqualTo( 90 ).Within( 12 ), "blue" );
				}
			}
		}

		[Test]
		public void ClearWithTransparencyGrid_PaintsTheWholeClipRegion_EvenWhenItStartsPastTheOrigin()
		{
			// Arrange: scrolling invalidates only the newly exposed part of the control, so
			// the clip region starts away from the origin (and not on a cell boundary)
			using( var bitmap = new System.Drawing.Bitmap( 60, 60 ) )
			{
				using( var graphics = System.Drawing.Graphics.FromImage( bitmap ) )
				{
					graphics.SetClip( new System.Drawing.Rectangle( 25, 35, 35, 25 ) );

					// Act
					graphics.ClearWithTransparencyGrid();
				}

				// Assert: every pixel inside the clip got one of the two grid colors
				var white = System.Drawing.Color.White.ToArgb();
				var gray = System.Drawing.Color.FromArgb( 255, 191, 191, 191 ).ToArgb();
				for( var y = 35; y < 60; y++ )
				{
					for( var x = 25; x < 60; x++ )
					{
						var pixel = bitmap.GetPixel( x, y ).ToArgb();
						Assert.That( pixel == white || pixel == gray, Is.True, $"pixel {x},{y} should be painted" );
					}
				}
			}
		}

		[Test]
		public void ClearWithTransparencyGrid_KeepsThePatternAnchoredToTheControlOrigin()
		{
			// Arrange: the same pixel must get the same color whether it was painted by a
			// full repaint or by a partial repaint after scrolling
			using( var full = new System.Drawing.Bitmap( 40, 40 ) )
			using( var partial = new System.Drawing.Bitmap( 40, 40 ) )
			{
				using( var graphics = System.Drawing.Graphics.FromImage( full ) )
				{
					graphics.ClearWithTransparencyGrid();
				}

				// Act
				using( var graphics = System.Drawing.Graphics.FromImage( partial ) )
				{
					graphics.SetClip( new System.Drawing.Rectangle( 15, 15, 25, 25 ) );
					graphics.ClearWithTransparencyGrid();
				}

				// Assert
				for( var y = 15; y < 40; y++ )
				{
					for( var x = 15; x < 40; x++ )
					{
						Assert.That( partial.GetPixel( x, y ).ToArgb(), Is.EqualTo( full.GetPixel( x, y ).ToArgb() ), $"pixel {x},{y}" );
					}
				}
			}
		}

		private static void FillQuadrant( System.Drawing.Bitmap bitmap, int x, int y, System.Drawing.Color color )
		{
			for( var yy = y; yy < y + 4; yy++ )
			{
				for( var xx = x; xx < x + 4; xx++ )
				{
					bitmap.SetPixel( xx, yy, color );
				}
			}
		}

		/// <summary>
		/// Renders a document for comparison: Address values are blanked out (they are file
		/// offsets, not content), and namespace declarations are sorted, because XmlSerializer
		/// does not guarantee the order in which it declares the xsd and xsi prefixes.
		/// </summary>
		private static string ToComparableXml( XDocument doc )
		{
			RemoveAddressValues( doc );
			SortNamespaceDeclarations( doc );
			return doc.ToString();
		}

		private static void RemoveAddressValues( XDocument doc )
		{
			var addressElements = doc.Descendants().Where( e =>
				e.Name.LocalName.EndsWith( "Address" ) ||
				System.Text.RegularExpressions.Regex.IsMatch( e.Name.LocalName, @"Address\d+$" )
			);
			foreach( var element in addressElements )
			{
				element.Value = string.Empty;
			}
		}

		private static void SortNamespaceDeclarations( XDocument doc )
		{
			// XDocument.Descendants() already includes the root element, which is where
			// XmlSerializer puts the declarations.
			foreach( var element in doc.Descendants() )
			{
				var attributes = element.Attributes().ToList();
				if( attributes.Count( a => a.IsNamespaceDeclaration ) < 2 )
				{
					continue;
				}

				// Keep the declarations ahead of the ordinary attributes, as the serializer writes
				// them, but in a stable order among themselves.
				var sorted = attributes
					.Where( a => a.IsNamespaceDeclaration )
					.OrderBy( a => a.Name.ToString(), StringComparer.Ordinal )
					.Concat( attributes.Where( a => !a.IsNamespaceDeclaration ) )
					.ToList();
				element.ReplaceAttributes( sorted );
			}
		}
	}
}
