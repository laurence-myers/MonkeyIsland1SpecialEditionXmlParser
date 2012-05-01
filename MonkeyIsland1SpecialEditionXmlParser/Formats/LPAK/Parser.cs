using MonkeyIsland1SpecialEditionXmlParser.Parsing;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK
{
	public static class Parser
	{
		public static LPAKFile Parse( string fileName )
		{
			LPAKFile file = null;
			Helper.ReadBinaryFile( fileName, reader =>
			{
				file = GenericReader.Read( reader, typeof( LPAKFile ) ) as LPAKFile;
			} );
			file.FileNameOnDisk = fileName;
			return file;
		}
	}
}
