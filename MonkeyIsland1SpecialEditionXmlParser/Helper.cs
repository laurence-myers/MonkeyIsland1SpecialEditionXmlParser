using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;

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
			reader.PadTheMonkey( position );

			return text;
		}

		public static string ReadStringMonkeyNoPadding( this BinaryReader reader )
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

			// move cursor beyond zero termination
			reader.BaseStream.Position++;

			return text;
		}

		public static string ReadStringMonkeyNoPadding( this BinaryReader reader, int length )
		{
			var bytes = reader.ReadBytes( length );
			var text = Encoding.ASCII.GetString( bytes );

			return text;
		}

		/// <summary>
		/// Offsets the position of the reader's base stream to the next 16 bytes.
		/// </summary>
		/// <param Name="reader">
		/// The reader who's position to offset.
		/// </param>
		public static void PadTheMonkey( this BinaryReader reader, long startPosition )
		{
			var mod = ( reader.BaseStream.Position - startPosition  ) % 16;
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

		public static Int16[] ReadInt16s( this byte[] bytes )
		{
			var integers = new Int16[bytes.Length / 2];
			for( var index = 0; index < bytes.Length; index++ )
			{
				integers[index / 2] = BitConverter.ToInt16( bytes, index );
			}
			return integers;
		}

		public static int[] ReadInt32s( this byte[] bytes )
		{
			var integers = new int[bytes.Length / 4];
			for( var index = 0; index < bytes.Length; index++ )
			{
				integers[index/ 4] = BitConverter.ToInt32( bytes, index );
			}
			return integers;
		}


		public static float[] ReadFloats( this byte[] bytes )
		{
			var floats = new float[bytes.Length / 4];
			for( var index = 0; index < bytes.Length; index++ )
			{
				floats[index / 4] = BitConverter.ToSingle( bytes, index );
			}
			return floats;
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

		public static List<Int16> ToInt16List( this List<byte> byteList )
		{
			var bytes = byteList.ToArray();
			var list = new List<Int16>();
			for( var index = 0; index < bytes.Length; index += 2 )
			{
				var value = BitConverter.ToInt16( bytes, index );
				list.Add( value );
			}
			return list;
		}

		public static List<Int32> ToInt32List( this List<byte> byteList )
		{
			var bytes = byteList.ToArray();
			var list = new List<Int32>();
			for( var index = 0; index < bytes.Length; index += 4 )
			{
				var value = BitConverter.ToInt32( bytes, index );
				list.Add( value );
			}
			return list;
		}

		public static List<float> ToFloatList( this List<byte> byteList )
		{
			var bytes = byteList.ToArray();
			var list = new List<float>();
			for( var index = 0; index < bytes.Length; index += 4 )
			{
				var value = BitConverter.ToSingle( bytes, index );
				list.Add( value );
			}
			return list;
		}

		public static string Reverse( this string text )
		{
			if( text == null )
			{
				return null;
			}
			var array = text.ToCharArray();
			Array.Reverse( array );
			return new string( array );
		}

		public static TAttribute[] GetCustomAttributes<TAttribute>( this MemberInfo member, bool inherit )
		{
			var attributes = member.GetCustomAttributes( typeof( TAttribute ), inherit );
			if( attributes == null || attributes.Length == 0 )
			{
				return new TAttribute[0];
			}

			var typedAttributes = attributes.OfType<TAttribute>().Where( a => a != null ).ToArray();
			return typedAttributes;
		}

		public static int IndexOfPredicate<T>( this T[] array, Predicate<T> predicate )
		{
			for( var index = 0; index < array.Length; index++ )
			{
				var element = array[index];
				if( predicate( element ) )
				{
					return index;
				}
			}

			return -1;
		}

		public static int IndexOfPredicate( this TreeNodeCollection nodes, Predicate<TreeNode> predicate )
		{
			for( var index = 0; index < nodes.Count; index++ )
			{
				var node = nodes[index];
				if( predicate( node ) )
				{
					return index;
				}
			}

			return -1;
		}

		public static int IndexOfPredicate( this TreeNodeCollection nodes, Predicate<TreeNode> predicate, int notFoundValue )
		{
			var index = nodes.IndexOfPredicate( predicate );
			if( index == -1 )
			{
				return notFoundValue;
			}
			return index;
		}

		public static string[] Split( this string text, StringSplitOptions options, params char[] separators )
		{
			if( text == null )
			{
				return new string[0];
			}
			if( text == string.Empty )
			{
				return new string[] { string.Empty };
			}
			var result = text.Split( separators, options );
			return result;
		}

		public static StringBuilder Append( this StringBuilder builder, params object[] args )
		{
			builder.Append( string.Concat( args ) );
			return builder;
		}

		public static void ReadBinaryFile( string fileName, Action<BinaryReader> action )
		{
			Stream stream = null;
			BinaryReader reader = null;

			try
			{
				stream = System.IO.File.OpenRead( fileName );
				reader = new BinaryReader( stream );
				action( reader );
			}
			finally
			{
				if( reader != null )
				{
					reader.Dispose();
					reader = null;
				}
				if( stream != null )
				{
					stream.Close();
					stream.Dispose();
					stream = null;
				}
			}
		}

		public static void WriteBinaryFile( string fileName, Action<BinaryWriter> action )
		{
			Stream stream = null;
			BinaryWriter writer = null;

			try
			{
				stream = System.IO.File.OpenWrite( fileName );
				writer = new BinaryWriter( stream );
				action( writer );
			}
			finally
			{
				if( writer != null )
				{
					writer.Dispose();
					writer = null;
				}
				if( stream != null )
				{
					stream.Close();
					stream.Dispose();
					stream = null;
				}
			}
		}

		public static string ReadBytesAsHexEditor( this BinaryReader reader, int length )
		{
			var bytes = reader.ReadBytes( length );
			var builder = new StringBuilder();

			for( var y = 0; y < length / 16; y++ )
			{
				for( var x = 0; x < 16 && ( y * 16 ) + x < length; x++ )
				{
					var @byte = bytes[( y * 16 ) + x];
					var @char = Encoding.ASCII.GetString( new[] { @byte } )[0];
					builder
						.Append( @byte.ToString( "x2" ) )
						.Append( " " )
						;
				}
				for( var x = 0; x < 16 && ( y * 16 ) + x < length; x++ )
				{
					var @byte = bytes[( y * 16 ) + x];
					var @char = Encoding.ASCII.GetString( new[] { @byte } )[0];
					builder.Append( char.IsLetterOrDigit( @char ) ? @char : '.' );
				}
				builder.AppendLine();
			}

			return builder.ToString();
		}

		public static string WriteTempFile( byte[] content )
		{
			var fileName = Path.GetTempFileName();
			File.WriteAllBytes( fileName, content );
			return fileName;
		}

		public static Image ImageFromDxtBytes( byte[] bytes )
		{
			Image image = null;
			var tempFileName = Path.GetTempFileName();
			Helper.WriteBinaryFile( tempFileName, writer =>
			{
				writer.Write( Encoding.ASCII.GetBytes( "DDS " ) );
				writer.Write( 124 ); // size (of what???)
				writer.Write( 528391 ); // some flags: DDSD_CAPS, DDSD_HEIGHT, DDSD_WIDTH, DDSD_PIXELFORMAT and DDSD_LINEARSIZE
				writer.Write( BitConverter.ToInt32( bytes, 8 ) ); // height
				writer.Write( BitConverter.ToInt32( bytes, 4 ) ); // width
				writer.Write( bytes.Length - 12 ); // dwPitchOrLinearSize
				writer.Write( 0 ); // depth
				writer.Write( 0 ); // mipmap count
				for( var reserved = 0; reserved < 11; reserved++ ) writer.Write( 0 );
				writer.Write( 32 ); // size (of what???)
				writer.Write( 0x00000004 ); // more flags: DDPF_FOURCC
				writer.Write( BitConverter.ToInt32( bytes, 0 ) ); // DXTn
				writer.Write( 0 ); // dwRGBBitCount
				writer.Write( 0 ); // dwRBitMask
				writer.Write( 0 ); // dwGBitMask
				writer.Write( 0 ); // dwBBitMask
				writer.Write( 0 ); // dwAlphaBitMask
				writer.Write( 0x00001000 ); // dwCaps1 : DDSCAPS_TEXTURE
				writer.Write( 0 ); // dwCaps2
				writer.Write( 0 ); // dwDDSX
				writer.Write( 0 ); // dwReserved
				writer.Write( 0 ); // dwReserved2
				writer.Write( bytes, 12, bytes.Length - 12 );
			} );
			image = DevIL.DevIL.LoadBitmap( tempFileName );
			File.Delete( tempFileName );
			return image;
		}

		public static Image LoadImage( this LPAKFile file, string fileName )
		{
			var index = file.PakFileNames.IndexOfPredicate( e => e.FileName == fileName );
			var entry = file.PakFileEntries[index];
			Image image = null;
			Helper.ReadBinaryFile( file.FileNameOnDisk, reader =>
			{
				reader.BaseStream.Position = entry.OffsetToStartOfData + file.PakHeader.StartOfData;
				var bytes = reader.ReadBytes( entry.SizeOfData1 );
				image = Helper.ImageFromDxtBytes( bytes );
			} );
			return image;
		}

		public static Type GetFieldOrPropertyType( this MemberInfo member )
		{
			var field = member as FieldInfo;
			if( field != null )
			{
				return field.FieldType;
			}

			var property = member as PropertyInfo;
			if( property != null )
			{
				return property.PropertyType;
			}

			return null;
		}

		public static void SetMemberValue( this MemberInfo member, object instance, object value )
		{
			var field = member as FieldInfo;
			if( field != null )
			{
				field.SetValue( instance, value );
				return;
			}

			var property = member as PropertyInfo;
			if( property != null )
			{
				property.SetValue( instance, value, null );
				return;
			}
		}

		public static object GetMemberValue( this MemberInfo member, object instance )
		{
			var field = member as FieldInfo;
			if( field != null )
			{
				return field.GetValue( instance );
			}

			var property = member as PropertyInfo;
			if( property != null )
			{
				return property.GetValue( instance, null );
			}

			return null;
		}
	}
}
