using System.IO;

namespace Tests
{
	/// <summary>
	/// Locates the real game data for integration tests; tests using it are skipped on
	/// machines without a Special Edition install.
	/// </summary>
	internal static class TestData
	{
		/// <summary>
		/// Looks for the real classic data (monkey1.001) next to a Special Edition install,
		/// extracting it from the pak when only the pak is present. Returns null when not
		/// found.
		/// </summary>
		public static string? FindRealDataFile()
		{
			var candidates = new[]
			{
				@"F:\Games\Steam\steamapps\common\The Secret of Monkey Island Special Edition\classic\en\monkey1.001",
				@"C:\Program Files (x86)\Steam\steamapps\common\The Secret of Monkey Island Special Edition\classic\en\monkey1.001",
			};
			foreach( var candidate in candidates )
			{
				if( File.Exists( candidate ) )
				{
					return candidate;
				}
			}

			// the retail install embeds the classic files in the pak; extract on the fly
			var pakCandidates = new[]
			{
				@"F:\Games\Steam\steamapps\common\The Secret of Monkey Island Special Edition\Monkey1.pak",
				@"C:\Program Files (x86)\Steam\steamapps\common\The Secret of Monkey Island Special Edition\Monkey1.pak",
			};
			foreach( var pakCandidate in pakCandidates )
			{
				if( !File.Exists( pakCandidate ) )
				{
					continue;
				}
				var lpak = MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK.Parser.Parse( pakCandidate );
				if( lpak == null )
				{
					continue;
				}
				lpak.FileNameOnDisk = pakCandidate;
				var index = MonkeyIslandSpecialEditionSpriteEditor.Helper.FindEntryIndex(
					lpak, name => name.EndsWith( ".001", System.StringComparison.OrdinalIgnoreCase ) );
				if( index < 0 )
				{
					continue;
				}
				var tempFileName = Path.Combine( Path.GetTempPath(), "mi1se-test-monkey1.001" );
				File.WriteAllBytes( tempFileName, MonkeyIslandSpecialEditionSpriteEditor.Helper.ReadEntryBytes( lpak, index ) );
				return tempFileName;
			}

			return null;
		}
	}
}
