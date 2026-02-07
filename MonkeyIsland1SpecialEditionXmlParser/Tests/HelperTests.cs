using System.IO;
using System.Linq;
using System.Xml.Linq;
using MonkeyIsland1SpecialEditionXmlParser;
using NUnit.Framework;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes;

namespace Tests
{
	[TestFixture]
	public class HelperTests
	{
		[Test]
		public void WriteObjectToFile_WritesValidXml()
		{
			// Arrange
			var datFilePath = Path.Combine( TestContext.CurrentContext.TestDirectory, "Fixtures", "001 - guybrush-skin.dat" );
			var expectedXmlPath = Path.Combine( TestContext.CurrentContext.TestDirectory, "Fixtures", "001 - guybrush-skin.xml" );
			var tempFilePath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".xml" );

			try
			{
				// Act
				var costume = Parser.ReadCostumeFromFile( datFilePath );
				Helper.WriteObjectToFile( tempFilePath, costume );

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
