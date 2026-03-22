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
