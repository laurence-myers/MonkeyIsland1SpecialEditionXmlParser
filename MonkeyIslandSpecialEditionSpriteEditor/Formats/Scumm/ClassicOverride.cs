using System;
using System.IO;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm
{
	/// <summary>
	/// Shared file operations for writing a loose classic-data override beside the pak: locating
	/// the override path, atomically writing it, and extracting the .000 index sibling so a
	/// reloaded loose .001 override still resolves room names and costumes. The pak itself is
	/// never modified.
	/// </summary>
	internal static class ClassicOverride
	{
		/// <summary>
		/// The pak's resource (.001) entry index, or -1.
		/// </summary>
		public static int ResourceEntryIndex( LPAKFile lpakFile )
		{
			return lpakFile.FindEntryIndex( name => name.EndsWith( ".001", StringComparison.OrdinalIgnoreCase ) );
		}

		/// <summary>
		/// Whether the pak's .001 entry is stored compressed (the patch tools read raw entry
		/// bytes, so a compressed entry would decode to garbage; retail data is uncompressed).
		/// </summary>
		public static bool ResourceEntryIsCompressed( LPAKFile lpakFile, int dataIndex )
		{
			return dataIndex >= 0 && lpakFile.PakFileEntries[dataIndex].IsCompressed != 0;
		}

		/// <summary>
		/// The loose override path for the pak's .001 entry (pak directory + entry name), or null.
		/// </summary>
		public static string? ResourceOverridePath( LPAKFile lpakFile )
		{
			var index = ResourceEntryIndex( lpakFile );
			if( index < 0 )
			{
				return null;
			}
			var name = lpakFile.PakFileNames[index].FileName;
			var directory = string.IsNullOrWhiteSpace( lpakFile.FileNameOnDisk ) ? null : Path.GetDirectoryName( lpakFile.FileNameOnDisk );
			if( string.IsNullOrWhiteSpace( name ) || directory == null )
			{
				return null;
			}
			return Path.Combine( directory, name! );
		}

		/// <summary>
		/// Atomically writes bytes to a target path via a temp file and File.Replace (which never
		/// leaves the target missing on a failed swap), creating it with File.Move when absent.
		/// Throws on failure, after removing the temp file.
		/// </summary>
		public static void AtomicWrite( string targetPath, byte[] bytes )
		{
			var directory = Path.GetDirectoryName( targetPath );
			if( !string.IsNullOrWhiteSpace( directory ) && !Directory.Exists( directory ) )
			{
				Directory.CreateDirectory( directory );
			}

			var tempPath = targetPath + ".tmp";
			try
			{
				File.WriteAllBytes( tempPath, bytes );
				if( File.Exists( targetPath ) )
				{
					File.Replace( tempPath, targetPath, null );
				}
				else
				{
					File.Move( tempPath, targetPath );
				}
			}
			catch( Exception )
			{
				try
				{
					if( File.Exists( tempPath ) )
					{
						File.Delete( tempPath );
					}
				}
				catch( Exception )
				{
					// the temp file is inert; leaving it behind beats masking the real error
				}
				throw;
			}
		}

		/// <summary>
		/// Extracts the pak's .000 index next to a .001 override (once), so a reload of the loose
		/// override still finds room names and costumes. Returns an error message on failure, or
		/// null on success or when there is nothing to extract.
		/// </summary>
		public static string? ExtractIndexSibling( LPAKFile lpakFile, string pakDirectory )
		{
			var indexEntryIndex = lpakFile.FindEntryIndex( name => name.EndsWith( ".000", StringComparison.OrdinalIgnoreCase ) );
			if( indexEntryIndex < 0 )
			{
				return null;
			}
			var indexEntryName = lpakFile.PakFileNames[indexEntryIndex].FileName;
			if( string.IsNullOrWhiteSpace( indexEntryName ) )
			{
				return null;
			}

			var indexPath = Path.Combine( pakDirectory, indexEntryName! );
			if( File.Exists( indexPath ) )
			{
				return null;
			}

			try
			{
				AtomicWrite( indexPath, lpakFile.ReadEntryBytes( indexEntryIndex ) );
				return null;
			}
			catch( Exception exception )
			{
				return "Could not extract the classic index (.000) beside the override: " + exception.Message;
			}
		}
	}
}
