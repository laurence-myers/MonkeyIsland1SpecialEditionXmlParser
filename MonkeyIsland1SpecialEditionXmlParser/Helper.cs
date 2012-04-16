using System.IO;
using System.Text;
using System.Drawing;

namespace MonkeyIsland1SpecialEditionXmlParser
{
	/// <summary>
	/// Provides a set of common helper methods.
	/// </summary>
	public static class Helper
	{
		/// <summary>
		/// Reads a string from the current stream and pads to the next 16 bytes.
		/// </summary>
		/// <param Name="reader">
		/// The reader to read from.
		/// </param>
		/// <returns>
		/// The string being read.
		/// </returns>
		public static string ReadStringMonkey( this BinaryReader reader )
		{
			// store the original position
			var position = reader.BaseStream.Position;

			// figure out how long the string is
			int @byte;
			while( ( @byte = reader.BaseStream.ReadByte() ) != 0 );
			var endPosition = reader.BaseStream.Position - 1;
			var length = (int)( endPosition - position );

			// restore the original position
			reader.BaseStream.Position = position;

			var bytes = reader.ReadBytes( length );
			var text = Encoding.ASCII.GetString( bytes );

			// skip the padding
			reader.PadTheMonkey();

			return text;
		}

		/// <summary>
		/// Offsets the position of the reader's base stream to the next 16 bytes.
		/// </summary>
		/// <param Name="reader">
		/// The reader who's position to offset.
		/// </param>
		public static void PadTheMonkey( this BinaryReader reader )
		{
			var mod = reader.BaseStream.Position % 16;
			reader.BaseStream.Position += 16 - mod;
		}

		/// <summary>
		/// Reads a color from the current stream.
		/// </summary>
		/// <param name="reader">
		/// The reader to read from.
		/// </param>
		/// <returns>
		/// The color being read.
		/// </returns>
		public static Color ReadColor( this BinaryReader reader )
		{
			var argb = reader.ReadInt32();
			var color = Color.FromArgb( argb );
			return color;
		}

		/// <summary>
		/// Returns the next available 32-bit integer and does not advance the byte position.
		/// </summary>
		/// <param name="reader">
		/// The reader to peek at.
		/// </param>
		/// <returns>
		/// The 32-bit integer being peeked at.
		/// </returns>
		public static int PeekInt32( this BinaryReader reader )
		{
			var value = reader.ReadInt32();
			reader.BaseStream.Position -= 4;
			return value;
		}

		/// <summary>
		/// Returns a value indicating whether or not the specified value is between min (inclusive) and max (exclusive).
		/// </summary>
		/// <param name="value">
		/// The value to check.
		/// </param>
		/// <param name="min">
		/// The inclusive minimum value to check against.
		/// </param>
		/// <param name="max">
		/// The exclusive maximum value to check against.
		/// </param>
		/// <returns>
		/// true if the value is between min and max; otherwise false.
		/// </returns>
		public static bool IsInRange( this int value, int min, int max )
		{
			return value >= min && value < max;
		}
	}
}
