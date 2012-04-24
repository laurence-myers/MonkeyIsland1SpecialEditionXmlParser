using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using System;
using System.Collections.Generic;

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
			while( ( @byte = reader.BaseStream.ReadByte() ) != 0 ) ;
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

		public static void WriteObjectToFile( string fileName, object value )
		{
			Stream stream = null;
			XmlSerializer serializer = null;

			try
			{
				stream = File.Create( fileName );
				serializer = new XmlSerializer( value.GetType() );
				serializer.Serialize( stream, value );
			}
			finally
			{
				if( stream != null )
				{
					stream.Close();
					stream.Dispose();
					stream = null;
				}
			}
		}

		public static T ReadObjectFromFile<T>( string fileName )
		{
			Stream stream = null;
			XmlSerializer serializer = null;

			try
			{
				stream = File.OpenRead( fileName );
				serializer = new XmlSerializer( typeof( T ) );
				var value = serializer.Deserialize( stream );
				return (T)value;
			}
			finally
			{
				if( stream != null )
				{
					stream.Close();
					stream.Dispose();
					stream = null;
				}
			}
		}

		public static string[] UpdateRecentList( this string[] array, string entry, int maxEntries )
		{
			if( array == null )
			{
				array = new[] { entry };
				return array;
			}

			array = new[] { entry }.Union( array.Where( e => e != entry ) ).Take( maxEntries ).ToArray();
			return array;
		}

		/// <summary>
		/// Gets the full path to the assembly that is currently executing,
		/// which would always be the one holding this class.
		/// </summary>
		/// <returns>
		/// Full path to the executing assembly.
		/// </returns>
		public static string GetExecutingAssemblyDirectory()
		{
			var assembly = Assembly.GetExecutingAssembly();
			var location = assembly.Location;
			var directory = Path.GetDirectoryName( location );
			return directory;
		}

		/// <summary>
		/// Reads a Int32 from the current stream and adds the byte position to the Int32 if predicate is meet.
		/// </summary>
		/// <param name="reader">
		/// The reader to read from.
		/// </param>
		/// <returns>
		/// The Int32 being read.
		/// </returns>
		public static int ReadInt32PlusBytePosition( this BinaryReader reader, Func<int, bool> predicate )
		{
			var position = (int)reader.BaseStream.Position;
			var value = reader.ReadInt32();
			if( predicate( value ) )
			{
				value += position;
			}
			return value;
		}

		public static int[] FindOffsets( this BinaryReader reader, int targetAddress )
		{
			var originalPosition = reader.BaseStream.Position;
			reader.BaseStream.Position = 0;

			var offsets = new List<int>();

			while( reader.BaseStream.Position < targetAddress )
			{
				var currentPosition = (int)reader.BaseStream.Position;
				var currentOffset = reader.ReadInt32();
				if( currentOffset + currentPosition == targetAddress )
				{
					offsets.Add( currentPosition );
				}
			}

			reader.BaseStream.Position = originalPosition;
			return offsets.ToArray();
		}

		public static int[] ReadInt32s( this BinaryReader reader, int count )
		{
			var integers = new int[count];
			for( var index = 0; index < count; index++ )
			{
				integers[index] = reader.ReadInt32();
			}
			return integers;
		}

		public static void ClearWithTransparancyGrid( this Graphics graphics )
		{
			var gray = new SolidBrush( Color.FromArgb( 255, 191, 191, 191 ) );
			for( var y = graphics.VisibleClipBounds.Y; y < graphics.VisibleClipBounds.Height; y += 10 )
			{
				for( var x = graphics.VisibleClipBounds.X; x < graphics.VisibleClipBounds.Width; x += 10 )
				{
					var brushIndex = ( x / 10 + y / 10 ) % 2;
					var brush = brushIndex == 0 ? Brushes.White : gray;
					graphics.FillRectangle( brush, x, y, 10, 10 );
				}
			}
		}
	}
}
