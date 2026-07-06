using MonkeyIsland1SpecialEditionXmlParser.Parsing;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK
{
	public static class Parser
	{
		public static LPAKFile? Parse( string fileName )
		{
			LPAKFile? file = null;
			Helper.ReadBinaryFile( fileName, reader =>
			{
				file = GenericReader.Read( reader, typeof( LPAKFile ), null, 0 ) as LPAKFile;
			} );
			file?.FileNameOnDisk = fileName;
			return file;
		}

		/// <summary>
		/// Checks if an override file exists for the given resource path and returns the file path if it does.
		/// </summary>
		/// <param name="lpakFilePath">The path to the LPAK file.</param>
		/// <param name="resourcePath">The resource path within the LPAK (e.g., "art\costumes\24_leaders-skin.costume.xml").</param>
		/// <returns>The full path to the override file if it exists; otherwise, null.</returns>
		public static string? GetOverrideFilePath( string lpakFilePath, string resourcePath )
		{
			if( string.IsNullOrWhiteSpace( lpakFilePath ) || string.IsNullOrWhiteSpace( resourcePath ) )
			{
				return null;
			}

			// Get the directory containing the LPAK file
			var lpakDirectory = System.IO.Path.GetDirectoryName( lpakFilePath );
			if( lpakDirectory == null )
			{
				return null;
			}

			// Construct the override file path
			var overrideFilePath = System.IO.Path.Combine( lpakDirectory, resourcePath );

			// Check if the file exists
			if( System.IO.File.Exists( overrideFilePath ) )
			{
				return overrideFilePath;
			}

			return null;
		}
	}
}
