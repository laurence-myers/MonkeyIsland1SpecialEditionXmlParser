using System;
using System.Collections.Generic;
using System.IO;

namespace MonkeyIslandSpecialEditionSpriteEditor.PlayTest
{
	/// <summary>What a loose file in the game folder is, judged from its path.</summary>
	public enum OverrideKind
	{
		/// <summary>An edited room (art/rooms/*.room.xml override binary).</summary>
		Room,
		/// <summary>An edited costume (art/costumes/*.costume.xml override binary).</summary>
		Costume,
		/// <summary>A replaced or brand-new texture (.dxt / .dds).</summary>
		Texture,
		/// <summary>The hand-editable XML mirror the editor keeps under overrides/.</summary>
		OverrideXml,
		/// <summary>Loose classic SCUMM data (the classic/ folder: monkey1.000/001, walk box saves).</summary>
		Classic,
		/// <summary>Anything else that turned up in the scanned folders (backups, temp files).</summary>
		Other,
	}

	/// <summary>One loose file the game will load instead of (or in addition to) the pak.</summary>
	public sealed class LooseOverride
	{
		public LooseOverride( string relativePath, string fullPath, OverrideKind kind, bool? inPak, long length, DateTime lastWrite )
		{
			this.RelativePath = relativePath;
			this.FullPath = fullPath;
			this.Kind = kind;
			this.InPak = inPak;
			this.Length = length;
			this.LastWrite = lastWrite;
		}

		/// <summary>Path relative to the game folder, forward slashes (the pak's own naming).</summary>
		public string RelativePath { get; }
		public string FullPath { get; }
		public OverrideKind Kind { get; }
		/// <summary>True when the pak index has an entry of this name (a replacement), false when it is a
		/// brand-new asset the game loads only because the file exists, null when unknown.</summary>
		public bool? InPak { get; }
		public long Length { get; }
		public DateTime LastWrite { get; }
	}

	/// <summary>
	/// Finds the loose overrides in a game folder: everything under art/ (override binaries and
	/// textures - the game loads these instead of the pak entry, and loads new textures that are
	/// not in the pak at all), overrides/ (the editor's XML mirrors) and classic/ (loose SCUMM data).
	/// Lets the modder see, and revert, exactly what the real game is picking up.
	/// </summary>
	public static class OverrideScanner
	{
		/// <summary>The folders under the game directory that hold loose overrides.</summary>
		public static readonly string[] ScannedFolders = { "art", "overrides", "classic" };

		/// <summary>Classifies a game-folder-relative path (either slash style).</summary>
		public static OverrideKind Classify( string relativePath )
		{
			var path = Normalize( relativePath );
			var lower = path.ToLowerInvariant();

			if( lower.StartsWith( "overrides/" ) )
			{
				return OverrideKind.OverrideXml;
			}

			if( lower.StartsWith( "classic/" ) )
			{
				return OverrideKind.Classic;
			}

			if( lower.EndsWith( ".dxt" ) || lower.EndsWith( ".dds" ) )
			{
				return OverrideKind.Texture;
			}

			if( lower.EndsWith( ".room.xml" ) )
			{
				return OverrideKind.Room;
			}

			if( lower.EndsWith( ".costume.xml" ) )
			{
				return OverrideKind.Costume;
			}

			return OverrideKind.Other;
		}

		/// <summary>
		/// Scans the game folder. <paramref name="isInPak"/> answers whether a forward-slash relative
		/// path is a pak entry (null when no pak is loaded, leaving InPak unknown).
		/// </summary>
		public static List<LooseOverride> Scan( string? pakDirectory, Func<string, bool>? isInPak )
		{
			var result = new List<LooseOverride>();
			if( string.IsNullOrWhiteSpace( pakDirectory ) || !Directory.Exists( pakDirectory ) )
			{
				return result;
			}

			var root = Path.GetFullPath( pakDirectory! ).TrimEnd( Path.DirectorySeparatorChar ) + Path.DirectorySeparatorChar;
			foreach( var folder in ScannedFolders )
			{
				var folderPath = Path.Combine( root, folder );
				if( !Directory.Exists( folderPath ) )
				{
					continue;
				}

				foreach( var file in Directory.EnumerateFiles( folderPath, "*", SearchOption.AllDirectories ) )
				{
					var relative = Normalize( file.Substring( root.Length ) );
					var kind = Classify( relative );
					bool? inPak = null;
					if( isInPak != null && ( kind == OverrideKind.Room || kind == OverrideKind.Costume || kind == OverrideKind.Texture ) )
					{
						inPak = isInPak( relative );
					}

					var info = new FileInfo( file );
					result.Add( new LooseOverride( relative, file, kind, inPak, info.Length, info.LastWriteTime ) );
				}
			}

			result.Sort( ( a, b ) => string.CompareOrdinal( a.RelativePath, b.RelativePath ) );
			return result;
		}

		/// <summary>Reverts an override by deleting the loose file; the game then falls back to the pak.</summary>
		public static void Revert( LooseOverride entry )
		{
			if( File.Exists( entry.FullPath ) )
			{
				File.Delete( entry.FullPath );
			}
		}

		private static string Normalize( string path )
		{
			return path.Replace( '\\', '/' ).TrimStart( '/' );
		}
	}
}
