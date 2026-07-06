using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using MonkeyIsland1SpecialEditionXmlParser;
using NUnit.Framework;
using CostumeParser = MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Parser;
using RoomParser = MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Parser;

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

				RemoveAddressValues( expectedDoc );
				RemoveAddressValues( actualDoc );

				Assert.That( actualDoc.ToString(), Is.EqualTo( expectedDoc.ToString() ), "XML content should match (ignoring Address values)" );
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

				RemoveAddressValues( expectedDoc );
				RemoveAddressValues( actualDoc );

				Assert.That( actualDoc.ToString(), Is.EqualTo( expectedDoc.ToString() ), "XML content should match after round-trip" );
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
				(Action<string, object>)( ( path, obj ) => MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Packer.WriteCostumeToBinaryFile( path, (MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities.Costume)obj ) )
			);
			yield return new TestCaseData(
				"028 - bar",
				(Func<string, object>)( MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Parser.ReadRoomFromBinaryFile ),
				(Action<string, object>)( ( path, obj ) => MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Packer.WriteRoomToBinaryFile( path, (MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities.Room)obj ) )
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

				RemoveAddressValues( originalDoc );
				RemoveAddressValues( roundTripDoc );

				File.Delete( originalXmlPath );
				File.Delete( roundTripXmlPath );

				Assert.That( roundTripDoc.ToString(), Is.EqualTo( originalDoc.ToString() ), "Round-tripped entity should match original entity" );
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
				(Action<string, object>)( ( path, obj ) => MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Packer.WriteCostumeToBinaryFile( path, (MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities.Costume)obj ) )
			);
			yield return new TestCaseData(
				"028 - bar",
				(Func<string, object>)( MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Parser.ReadRoomFromXmlFile ),
				(Func<string, object>)( MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Parser.ReadRoomFromBinaryFile ),
				(Action<string, object>)( ( path, obj ) => MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Packer.WriteRoomToBinaryFile( path, (MonkeyIsland1SpecialEditionXmlParser.Formats.Rooms.Entities.Room)obj ) )
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

				RemoveAddressValues( originalDoc );
				RemoveAddressValues( roundTripDoc );

				File.Delete( originalXmlPath );
				File.Delete( roundTripXmlPath );

				Assert.That( roundTripDoc.ToString(), Is.EqualTo( originalDoc.ToString() ), "Round-tripped entity from XML should match original entity" );
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

		private void RemoveAddressValues( XDocument doc )
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
	}
}
