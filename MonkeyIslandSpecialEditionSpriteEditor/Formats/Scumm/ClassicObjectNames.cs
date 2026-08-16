using System.Collections.Generic;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm
{
	/// <summary>
	/// Shared formatting for classic object names and number lists, so the room-entity grouper, the
	/// room-state evaluator and the editor label the same objects the same way (one OBNA-padding rule,
	/// one object-range format).
	/// </summary>
	public static class ClassicObjectNames
	{
		/// <summary>Strips the OBNA padding (trailing '@', spaces and nulls) from an object name.</summary>
		public static string CleanName( string? name )
		{
			if( string.IsNullOrEmpty( name ) )
			{
				return "";
			}
			return name!.TrimEnd( '@', ' ', '\0' ).Trim();
		}

		/// <summary>Upper-cases the first character of a word.</summary>
		public static string Capitalize( string word )
		{
			return word.Length == 0 ? word : char.ToUpperInvariant( word[0] ) + word.Substring( 1 );
		}

		/// <summary>
		/// Formats object numbers as a compact range ("317-319") when they run consecutively, else a
		/// comma-separated list. An empty list gives "".
		/// </summary>
		public static string FormatRange( IReadOnlyList<int> ids )
		{
			if( ids.Count == 0 )
			{
				return "";
			}
			var consecutive = true;
			for( var i = 1; i < ids.Count; i++ )
			{
				if( ids[i] != ids[i - 1] + 1 )
				{
					consecutive = false;
					break;
				}
			}
			return consecutive && ids.Count > 1
				? ids[0] + "-" + ids[ids.Count - 1]
				: string.Join( ", ", ids );
		}
	}
}
