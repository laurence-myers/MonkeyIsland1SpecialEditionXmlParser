using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using MonkeyIsland1SpecialEditionXmlParser;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes;
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

		[Test]
		public void WriteCostumeToBinaryFile_RoundTripFromBinary_ProducesSameValues()
		{
			// Arrange
			var datFilePath = Path.Combine( TestContext.CurrentContext.TestDirectory, "Fixtures", "001 - guybrush-skin.dat" );
			var tempDatPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".dat" );

			try
			{
				// Act
				var originalCostume = CostumeParser.ReadCostumeFromBinaryFile( datFilePath );
				Packer.WriteCostumeToBinaryFile( tempDatPath, originalCostume );
				var roundTripCostume = CostumeParser.ReadCostumeFromBinaryFile( tempDatPath );

				// Assert
				
				var originalXmlPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".xml" );
				var roundTripXmlPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".xml" );

				Helper.WriteObjectToFile( originalXmlPath, originalCostume );
				Helper.WriteObjectToFile( roundTripXmlPath, roundTripCostume );

				var originalDoc = XDocument.Load( originalXmlPath );
				var roundTripDoc = XDocument.Load( roundTripXmlPath );

				RemoveAddressValues( originalDoc );
				RemoveAddressValues( roundTripDoc );

				File.Delete( originalXmlPath );
				File.Delete( roundTripXmlPath );

				Assert.That( roundTripDoc.ToString(), Is.EqualTo( originalDoc.ToString() ), "Round-tripped costume should match original costume" );
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
		public void WriteCostumeToBinaryFile_RoundTripFromXml_ProducesSameValues()
		{
			// Arrange
			var xmlFilePath = Path.Combine( TestContext.CurrentContext.TestDirectory, "Fixtures", "001 - guybrush-skin.xml" );
			var tempDatPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".dat" );

			try
			{
				// Act
				var originalCostume = CostumeParser.ReadCostumeFromXmlFile( xmlFilePath );
				Packer.WriteCostumeToBinaryFile( tempDatPath, originalCostume );
				var roundTripCostume = CostumeParser.ReadCostumeFromBinaryFile( tempDatPath );

				// Assert
				var originalXmlPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".xml" );
				var roundTripXmlPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".xml" );

				Helper.WriteObjectToFile( originalXmlPath, originalCostume );
				Helper.WriteObjectToFile( roundTripXmlPath, roundTripCostume );

				var originalDoc = XDocument.Load( originalXmlPath );
				var roundTripDoc = XDocument.Load( roundTripXmlPath );

				RemoveAddressValues( originalDoc );
				RemoveAddressValues( roundTripDoc );

				File.Delete( originalXmlPath );
				File.Delete( roundTripXmlPath );

				Assert.That( roundTripDoc.ToString(), Is.EqualTo( originalDoc.ToString() ), "Round-tripped costume from XML should match original costume" );
			}
			finally
			{
				if( File.Exists( tempDatPath ) )
				{
					File.Delete( tempDatPath );
				}
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
