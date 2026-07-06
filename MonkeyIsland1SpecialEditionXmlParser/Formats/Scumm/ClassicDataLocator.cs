using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Scumm.Entities;
using MonkeyIsland1SpecialEditionXmlParser.UI;

namespace MonkeyIsland1SpecialEditionXmlParser.Formats.Scumm
{
	/// <summary>
	/// Locates and loads the classic SCUMM data (monkey1.000 / monkey1.001) that ships with
	/// the Special Edition. The retail install embeds the files inside the pak itself
	/// (classic/en/monkey1.000|001); they may also exist as loose files in a "classic" folder.
	/// </summary>
	public static class ClassicDataLocator
	{
		private static readonly Dictionary<string, ClassicData> cache = new Dictionary<string, ClassicData>( StringComparer.OrdinalIgnoreCase );
		private static readonly HashSet<string> promptDeclined = new HashSet<string>( StringComparer.OrdinalIgnoreCase );

		/// <summary>
		/// The result of a successful search for loose files: the resource file (.001) and,
		/// if present, the matching index file (.000).
		/// </summary>
		public class ClassicDataFiles( string dataFileName, string? indexFileName )
		{
			public string DataFileName
			{
				get;
				set;
			} = dataFileName;

			public string? IndexFileName
			{
				get;
				set;
			} = indexFileName;

			/// <summary>
			/// Gets or sets the folder the user picked in the prompt, when that is where the
			/// files were found. Only persisted once the data actually parses.
			/// </summary>
			public string? PromptFolder
			{
				get;
				set;
			}
		}

		/// <summary>
		/// Loads the classic data for an opened LPAK, caching the result per LPAK path.
		/// Sources are tried in order: entries embedded in the pak itself, a loose "classic"
		/// folder near the pak, the folder remembered from a previous session, and finally
		/// the folder prompt (asked at most once per pak and session).
		/// </summary>
		/// <param name="lpakFile">The opened LPAK file.</param>
		/// <param name="folderPrompt">Optional callback that asks the user for a folder; return null to cancel.</param>
		/// <returns>The loaded classic data, or null if it could not be found.</returns>
		public static ClassicData? GetOrLoad( LPAKFile lpakFile, Func<string?>? folderPrompt )
		{
			var key = lpakFile.FileNameOnDisk ?? "";
			ClassicData cached;
			if( ClassicDataLocator.cache.TryGetValue( key, out cached ) )
			{
				return cached;
			}

			if( ClassicDataLocator.promptDeclined.Contains( key ) )
			{
				folderPrompt = null;
			}

			// track whether the user actually saw the prompt, so a plain "not found"
			// (or a parse failure) doesn't suppress the prompt forever
			var promptInvoked = false;
			Func<string?>? trackingPrompt = folderPrompt == null
				? null
				: () =>
				{
					promptInvoked = true;
					return folderPrompt();
				};

			var data = Load( lpakFile, trackingPrompt );
			if( data != null )
			{
				ClassicDataLocator.cache[key] = data;
			}
			else if( promptInvoked )
			{
				// don't nag the user again for this pak in this session
				ClassicDataLocator.promptDeclined.Add( key );
			}
			return data;
		}

		/// <summary>
		/// Loads the classic data for an opened LPAK without caching.
		/// </summary>
		public static ClassicData? Load( LPAKFile lpakFile, Func<string?>? folderPrompt )
		{
			// 1. embedded in the pak itself (classic/en/monkey1.001)
			var embedded = LoadFromLpak( lpakFile );
			if( embedded != null )
			{
				return embedded;
			}

			// 2. loose files on disk
			var files = LocateFiles( lpakFile.FileNameOnDisk, folderPrompt );
			if( files != null )
			{
				var data = LoadFromFiles( files );

				// remember a prompted folder only once its data actually parsed,
				// otherwise a bad pick would shadow the prompt forever
				if( data != null && !string.IsNullOrWhiteSpace( files.PromptFolder ) )
				{
					UserSettings.Instance.ClassicDataFolder = files.PromptFolder;
					UserSettings.Instance.Save();
				}
				return data;
			}

			return null;
		}

		private static ClassicData? LoadFromLpak( LPAKFile lpakFile )
		{
			var dataIndex = lpakFile.FindEntryIndex( name => name.EndsWith( ".001", StringComparison.OrdinalIgnoreCase ) );
			if( dataIndex < 0 || lpakFile.PakFileEntries[dataIndex].IsCompressed != 0 )
			{
				return null;
			}

			try
			{
				var roomList = Parser.ReadRoomsFromEncodedBytes( lpakFile.ReadEntryBytes( dataIndex ) );
				if( roomList.Count == 0 )
				{
					return null;
				}

				var roomNames = new Dictionary<int, string>();
				var indexIndex = lpakFile.FindEntryIndex( name => name.EndsWith( ".000", StringComparison.OrdinalIgnoreCase ) );
				if( indexIndex >= 0 && lpakFile.PakFileEntries[indexIndex].IsCompressed == 0 )
				{
					roomNames = Parser.ReadRoomNamesFromEncodedBytes( lpakFile.ReadEntryBytes( indexIndex ) );
				}

				var data = new ClassicData(
					roomList: roomList,
					roomNames: roomNames,
					source: string.Concat( Path.GetFileName( lpakFile.FileNameOnDisk ), ":", lpakFile.PakFileNames[dataIndex].FileName )
				);
				ApplyRoomNames( data );
				return data;
			}
			catch( Exception )
			{
				return null;
			}
		}

		private static ClassicData? LoadFromFiles( ClassicDataFiles files )
		{
			try
			{
				var roomList = Parser.ReadRoomsFromDataFile( files.DataFileName );
				if( roomList.Count == 0 )
				{
					return null;
				}

				var roomNames = files.IndexFileName != null
					? Parser.ReadRoomNamesFromIndexFile( files.IndexFileName )
					: new Dictionary<int, string>();

				var data = new ClassicData(
					roomList: roomList,
					roomNames: roomNames,
					source: files.DataFileName
				);
				ApplyRoomNames( data );
				return data;
			}
			catch( Exception )
			{
				return null;
			}
		}

		private static void ApplyRoomNames( ClassicData data )
		{
			foreach( var room in data.RoomList )
			{
				string name;
				if( data.RoomNames.TryGetValue( room.RoomNumber, out name ) )
				{
					room.Name = name;
				}
			}
		}

		/// <summary>
		/// Tries to find loose classic data files. Looks for a "classic" folder near the LPAK
		/// first, then in the folder remembered from a previous session, and finally asks the
		/// caller to prompt the user (the chosen folder is remembered for next time).
		/// </summary>
		/// <param name="lpakFilePath">The path of the opened .paklang file, if any.</param>
		/// <param name="folderPrompt">Optional callback that asks the user for a folder; return null to cancel.</param>
		/// <returns>The located files, or null if nothing was found.</returns>
		public static ClassicDataFiles? LocateFiles( string? lpakFilePath, Func<string?>? folderPrompt )
		{
			// 1. a "classic" folder next to (or above) the opened LPAK
			var directory = string.IsNullOrWhiteSpace( lpakFilePath ) ? null : Path.GetDirectoryName( lpakFilePath );
			for( var level = 0; level < 4 && directory != null; level++ )
			{
				var classicDirectory = Path.Combine( directory, "classic" );
				if( Directory.Exists( classicDirectory ) )
				{
					var files = FindInFolder( classicDirectory );
					if( files != null )
					{
						return files;
					}
				}
				directory = Path.GetDirectoryName( directory );
			}

			// 2. the folder remembered from a previous session
			var rememberedFolder = UserSettings.Instance.ClassicDataFolder;
			if( !string.IsNullOrWhiteSpace( rememberedFolder ) && Directory.Exists( rememberedFolder ) )
			{
				var files = FindInFolder( rememberedFolder! );
				if( files != null )
				{
					return files;
				}
			}

			// 3. ask the user; the folder is only remembered after the data parses (see Load)
			if( folderPrompt != null )
			{
				var folder = folderPrompt();
				if( !string.IsNullOrWhiteSpace( folder ) && Directory.Exists( folder ) )
				{
					var files = FindInFolder( folder! );
					if( files != null )
					{
						files.PromptFolder = folder;
						return files;
					}
				}
			}

			return null;
		}

		private static ClassicDataFiles? FindInFolder( string folder )
		{
			string[] dataFileNames;
			try
			{
				dataFileNames = Directory.GetFiles( folder, "*.001", SearchOption.AllDirectories );
			}
			catch( Exception )
			{
				return null;
			}

			if( dataFileNames.Length == 0 )
			{
				return null;
			}

			var dataFileName
				= dataFileNames.FirstOrDefault( f => Path.GetFileName( f ).Equals( "monkey1.001", StringComparison.OrdinalIgnoreCase ) )
				?? dataFileNames[0];

			var indexFileName = Path.ChangeExtension( dataFileName, ".000" );
			return new ClassicDataFiles(
				dataFileName: dataFileName,
				indexFileName: File.Exists( indexFileName ) ? indexFileName : null
			);
		}
	}
}
