using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;

namespace MonkeyIslandSpecialEditionSpriteEditor
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

			// figure out how long the string is (-1 = end of stream, on malformed data)
			int @byte;
			while( ( @byte = reader.BaseStream.ReadByte() ) > 0 ) ;
			var endPosition = @byte == 0 ? reader.BaseStream.Position - 1 : reader.BaseStream.Position;
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

			// figure out how long the string is (-1 = end of stream, on malformed data)
			int @byte;
			while( ( @byte = reader.BaseStream.ReadByte() ) > 0 ) ;
			var endPosition = @byte == 0 ? reader.BaseStream.Position - 1 : reader.BaseStream.Position;
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
			Stream? stream = null;
			XmlSerializer? serializer = null;

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
			Stream? stream = null;
			XmlSerializer? serializer = null;

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

		public static string[] UpdateRecentList( this string[]? array, string entry, int maxEntries )
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
		public static string? GetExecutingAssemblyDirectory()
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

		public static int[] ReadInt32s( this BinaryReader reader, int count )
		{
			var integers = new int[count];
			for( var index = 0; index < count; index++ )
			{
				integers[index] = reader.ReadInt32();
			}
			return integers;
		}

		public static void ClearWithTransparencyGrid( this Graphics graphics )
		{
			// scrolling repaints only the newly exposed part, so the clip bounds can start
			// anywhere; snap to whole cells and key the parity on absolute cell indices to
			// keep the pattern anchored to the control rather than to the clip region
			var bounds = graphics.VisibleClipBounds;
			var firstCellX = (int)Math.Floor( bounds.X / 10.0f );
			var firstCellY = (int)Math.Floor( bounds.Y / 10.0f );
			using( var gray = new SolidBrush( Color.FromArgb( 255, 191, 191, 191 ) ) )
			{
				for( var cellY = firstCellY; cellY * 10 < bounds.Bottom; cellY++ )
				{
					for( var cellX = firstCellX; cellX * 10 < bounds.Right; cellX++ )
					{
						var brush = ( cellX + cellY ) % 2 == 0 ? Brushes.White : gray;
						graphics.FillRectangle( brush, cellX * 10, cellY * 10, 10, 10 );
					}
				}
			}
		}

		public static string? Reverse( this string? text )
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

		public static StringBuilder Append( this StringBuilder builder, params object[] args )
		{
			builder.Append( string.Concat( args ) );
			return builder;
		}

		public static void ReadBinaryFile( string fileName, Action<BinaryReader> action )
		{
			Stream? stream = null;
			BinaryReader? reader = null;

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
			Stream? stream = null;
			BinaryWriter? writer = null;

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

		public static Image ImageFromDxtBytes( byte[] bytes )
		{
			// Wrap the game's 12 byte .dxt blob (fourCC, width, height followed by raw DXT
			// blocks) in a standard 128 byte DDS header, then decode it with Pfim. This is
			// the inverse of DxtBytesFromImage.
			using( var stream = new MemoryStream() )
			{
				using( var writer = new BinaryWriter( stream, Encoding.ASCII, leaveOpen: true ) )
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
				}

				stream.Position = 0;
				using( var dds = Pfim.Pfimage.FromStream( stream ) )
				{
					return BitmapFromPfim( dds );
				}
			}
		}

		/// <summary>
		/// Copies a Pfim decoded image into a GDI+ owned <see cref="Bitmap"/>. The copy is
		/// required because the returned bitmap must stay valid after the Pfim image (and
		/// its backing byte array) is disposed.
		/// </summary>
		private static Bitmap BitmapFromPfim( Pfim.IImage image )
		{
			System.Drawing.Imaging.PixelFormat pixelFormat;
			if( image.Format == Pfim.ImageFormat.Rgba32 )
			{
				pixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
			}
			else if( image.Format == Pfim.ImageFormat.Rgb24 )
			{
				pixelFormat = System.Drawing.Imaging.PixelFormat.Format24bppRgb;
			}
			else
			{
				throw new NotSupportedException( string.Concat( "Pfim image format ", image.Format, " is not supported." ) );
			}

			var bitmap = new Bitmap( image.Width, image.Height, pixelFormat );
			var bitmapData = bitmap.LockBits(
				new Rectangle( 0, 0, image.Width, image.Height ),
				System.Drawing.Imaging.ImageLockMode.WriteOnly,
				pixelFormat
			);
			try
			{
				var rowBytes = Math.Min( image.Stride, bitmapData.Stride );
				for( var y = 0; y < image.Height; y++ )
				{
					System.Runtime.InteropServices.Marshal.Copy(
						image.Data, y * image.Stride,
						IntPtr.Add( bitmapData.Scan0, y * bitmapData.Stride ), rowBytes
					);
				}
			}
			finally
			{
				bitmap.UnlockBits( bitmapData );
			}
			return bitmap;
		}

		public static Image? LoadImage( this LPAKFile file, string? fileName )
		{
			var bytes = file.ReadResourceBytes( fileName );
			if( bytes == null )
			{
				return null;
			}
			var image = Helper.ImageFromDxtBytes( bytes );
			return image;
		}

		/// <summary>
		/// Reads the bytes of a resource, preferring a loose override file (the same mechanism
		/// the game uses) over the LPAK entry.
		/// </summary>
		public static byte[]? ReadResourceBytes( this LPAKFile file, string? fileName )
		{
			if( string.IsNullOrWhiteSpace( fileName ) )
			{
				return null;
			}

			var overrideFilePath = Formats.LPAK.Parser.GetOverrideFilePath( file.FileNameOnDisk, fileName! );
			if( overrideFilePath != null )
			{
				return File.ReadAllBytes( overrideFilePath );
			}

			var index = file.PakFileNames.IndexOfPredicate( e => e.FileName == fileName );
			if( index < 0 )
			{
				return null;
			}
			return file.ReadEntryBytes( index );
		}

		/// <summary>
		/// Reads the raw bytes of a file entry from the LPAK file on disk.
		/// </summary>
		public static byte[] ReadEntryBytes( this LPAKFile file, int index )
		{
			var entry = file.PakFileEntries[index];
			var bytes = new byte[0];
			Helper.ReadBinaryFile( file.FileNameOnDisk, reader =>
			{
				reader.BaseStream.Position = entry.OffsetToStartOfData + file.PakHeader.StartOfData;
				bytes = reader.ReadBytes( entry.SizeOfData1 );
			} );
			return bytes;
		}

		/// <summary>
		/// Finds the index of the first file entry whose name matches the predicate, or -1.
		/// </summary>
		public static int FindEntryIndex( this LPAKFile file, Predicate<string> predicate )
		{
			return file.PakFileNames.IndexOfPredicate( e => e.FileName != null && predicate( e.FileName ) );
		}

		/// <summary>
		/// Loads a room by file index, preferring a loose override file when one exists.
		/// </summary>
		public static Formats.Rooms.Entities.Room? LoadRoom( this LPAKFile file, int fileIndex )
		{
			var fileName = file.PakFileNames[fileIndex].FileName;
			if( string.IsNullOrWhiteSpace( fileName ) )
			{
				return null;
			}

			// Check if an override file exists
			var overrideFilePath = Formats.LPAK.Parser.GetOverrideFilePath( file.FileNameOnDisk, fileName! );
			if( overrideFilePath != null )
			{
				// Load from override file
				return Formats.Rooms.Parser.ReadRoomFromBinaryFile( overrideFilePath );
			}

			// Load from LPAK
			Formats.Rooms.Entities.Room? room = null;
			var fileEntry = file.PakFileEntries[fileIndex];
			Helper.ReadBinaryFile( file.FileNameOnDisk, reader =>
			{
				reader.BaseStream.Position = fileEntry.OffsetToStartOfData + file.PakHeader.StartOfData;
				room = Formats.Rooms.Parser.ReadRoom( reader );
			} );
			return room;
		}

		/// <summary>
		/// Loads a costume by file index, preferring a loose override file when one exists.
		/// </summary>
		public static Formats.Costumes.Entities.Costume? LoadCostume( this LPAKFile file, int fileIndex )
		{
			var fileName = file.PakFileNames[fileIndex].FileName;
			if( string.IsNullOrWhiteSpace( fileName ) )
			{
				return null;
			}

			// Check if an override file exists
			var overrideFilePath = Formats.LPAK.Parser.GetOverrideFilePath( file.FileNameOnDisk, fileName! );
			if( overrideFilePath != null )
			{
				// Load from override file
				return Formats.Costumes.Parser.ReadCostumeFromBinaryFile( overrideFilePath );
			}

			// Load from LPAK
			Formats.Costumes.Entities.Costume? costume = null;
			var fileEntry = file.PakFileEntries[fileIndex];
			Helper.ReadBinaryFile( file.FileNameOnDisk, reader =>
			{
				reader.BaseStream.Position = fileEntry.OffsetToStartOfData + file.PakHeader.StartOfData;
				costume = Formats.Costumes.Parser.ReadCostume( reader );
			} );
			return costume;
		}

		/// <summary>
		/// Picks the DXT format for a bitmap: "DXT5" when any pixel is not fully opaque (so its
		/// alpha survives), otherwise the smaller "DXT1". Matches how the retail textures are
		/// authored and gives the new-texture import a sensible default.
		/// </summary>
		/// <param name="bitmap">The image to inspect.</param>
		/// <returns>"DXT1" or "DXT5".</returns>
		public static string DetectDxtFourCC( Bitmap bitmap )
		{
			var width = bitmap.Width;
			var height = bitmap.Height;

			// read all pixels at once; GetPixel would be far too slow for 1024x1024 sheets
			var pixels = new int[width * height];
			var bitmapData = bitmap.LockBits(
				new Rectangle( 0, 0, width, height ),
				System.Drawing.Imaging.ImageLockMode.ReadOnly,
				System.Drawing.Imaging.PixelFormat.Format32bppArgb
			);
			System.Runtime.InteropServices.Marshal.Copy( bitmapData.Scan0, pixels, 0, pixels.Length );
			bitmap.UnlockBits( bitmapData );

			foreach( var pixel in pixels )
			{
				// Format32bppArgb packs alpha in the high byte
				if( ( ( pixel >> 24 ) & 0xFF ) != 0xFF )
				{
					return "DXT5";
				}
			}
			return "DXT1";
		}

		/// <summary>
		/// Encodes a bitmap into the game's .dxt wrapper format (12 byte header: fourCC,
		/// width, height; followed by raw DXT blocks). The inverse of ImageFromDxtBytes.
		/// DevIL.NET2 cannot control the DXT compression format on save, so the blocks are
		/// encoded with a minimal managed DXT1/DXT5 range-fit encoder.
		/// </summary>
		/// <param name="bitmap">The image to encode.</param>
		/// <param name="fourCC">The target format: "DXT1" or "DXT5".</param>
		/// <returns>The bytes in the game's .dxt wrapper format.</returns>
		public static byte[] DxtBytesFromImage( Bitmap bitmap, string fourCC )
		{
			if( fourCC != "DXT1" && fourCC != "DXT5" )
			{
				throw new NotSupportedException( string.Concat( "DXT format ", fourCC, " is not supported." ) );
			}
			var dxt5 = fourCC == "DXT5";

			var width = bitmap.Width;
			var height = bitmap.Height;

			// read all pixels at once; GetPixel would be far too slow for 1024x1024 sheets
			var pixels = new int[width * height];
			var bitmapData = bitmap.LockBits(
				new Rectangle( 0, 0, width, height ),
				System.Drawing.Imaging.ImageLockMode.ReadOnly,
				System.Drawing.Imaging.PixelFormat.Format32bppArgb
			);
			System.Runtime.InteropServices.Marshal.Copy( bitmapData.Scan0, pixels, 0, pixels.Length );
			bitmap.UnlockBits( bitmapData );

			var blocksX = ( width + 3 ) / 4;
			var blocksY = ( height + 3 ) / 4;
			var blockSize = dxt5 ? 16 : 8;
			var bytes = new byte[12 + blocksX * blocksY * blockSize];

			// the game's 12 byte wrapper header
			Encoding.ASCII.GetBytes( fourCC ).CopyTo( bytes, 0 );
			BitConverter.GetBytes( width ).CopyTo( bytes, 4 );
			BitConverter.GetBytes( height ).CopyTo( bytes, 8 );

			var offset = 12;
			var blockArgb = new int[16];
			for( var blockY = 0; blockY < blocksY; blockY++ )
			{
				for( var blockX = 0; blockX < blocksX; blockX++ )
				{
					// gather the 4x4 block, replicating edge pixels on partial blocks
					for( var y = 0; y < 4; y++ )
					{
						for( var x = 0; x < 4; x++ )
						{
							var pixelX = Math.Min( blockX * 4 + x, width - 1 );
							var pixelY = Math.Min( blockY * 4 + y, height - 1 );
							blockArgb[y * 4 + x] = pixels[pixelY * width + pixelX];
						}
					}

					if( dxt5 )
					{
						EncodeDxt5AlphaBlock( blockArgb, bytes, offset );
						offset += 8;
					}
					EncodeDxtColorBlock( blockArgb, dxt5, bytes, offset );
					offset += 8;
				}
			}

			return bytes;
		}

		private static void EncodeDxt5AlphaBlock( int[] blockArgb, byte[] output, int offset )
		{
			var minAlpha = 255;
			var maxAlpha = 0;
			foreach( var argb in blockArgb )
			{
				var alpha = ( argb >> 24 ) & 0xFF;
				minAlpha = Math.Min( minAlpha, alpha );
				maxAlpha = Math.Max( maxAlpha, alpha );
			}

			// alpha0 > alpha1 selects the 8 value palette
			var alpha0 = maxAlpha;
			var alpha1 = minAlpha;
			output[offset] = (byte)alpha0;
			output[offset + 1] = (byte)alpha1;

			var palette = new int[8];
			palette[0] = alpha0;
			palette[1] = alpha1;
			for( var index = 2; index < 8; index++ )
			{
				palette[index] = ( ( 8 - index ) * alpha0 + ( index - 1 ) * alpha1 ) / 7;
			}

			var indexBits = 0UL;
			for( var pixel = 0; pixel < 16; pixel++ )
			{
				var alpha = ( blockArgb[pixel] >> 24 ) & 0xFF;
				var bestIndex = 0;
				var bestDistance = int.MaxValue;
				for( var index = 0; index < 8; index++ )
				{
					var distance = Math.Abs( palette[index] - alpha );
					if( distance < bestDistance )
					{
						bestDistance = distance;
						bestIndex = index;
					}
				}
				indexBits |= (ulong)bestIndex << ( pixel * 3 );
			}

			for( var index = 0; index < 6; index++ )
			{
				output[offset + 2 + index] = (byte)( ( indexBits >> ( index * 8 ) ) & 0xFF );
			}
		}

		private static void EncodeDxtColorBlock( int[] blockArgb, bool alwaysFourColors, byte[] output, int offset )
		{
			// DXT1 encodes transparency via the 3 color + transparent mode; DXT3/5 color
			// blocks are always decoded in 4 color mode
			var hasTransparency = false;
			if( !alwaysFourColors )
			{
				foreach( var argb in blockArgb )
				{
					if( ( ( argb >> 24 ) & 0xFF ) < 128 )
					{
						hasTransparency = true;
						break;
					}
				}
			}

			// bounding box of the opaque pixels
			int minR = 255, minG = 255, minB = 255;
			int maxR = 0, maxG = 0, maxB = 0;
			var opaqueCount = 0;
			foreach( var argb in blockArgb )
			{
				if( hasTransparency && ( ( argb >> 24 ) & 0xFF ) < 128 )
				{
					continue;
				}
				opaqueCount++;
				var r = ( argb >> 16 ) & 0xFF;
				var g = ( argb >> 8 ) & 0xFF;
				var b = argb & 0xFF;
				minR = Math.Min( minR, r );
				minG = Math.Min( minG, g );
				minB = Math.Min( minB, b );
				maxR = Math.Max( maxR, r );
				maxG = Math.Max( maxG, g );
				maxB = Math.Max( maxB, b );
			}

			if( opaqueCount == 0 )
			{
				// fully transparent block: 3 color mode with every index transparent
				output[offset] = 0;
				output[offset + 1] = 0;
				output[offset + 2] = 0;
				output[offset + 3] = 0;
				output[offset + 4] = 0xFF;
				output[offset + 5] = 0xFF;
				output[offset + 6] = 0xFF;
				output[offset + 7] = 0xFF;
				return;
			}

			var color0 = To565( maxR, maxG, maxB );
			var color1 = To565( minR, minG, minB );

			if( hasTransparency )
			{
				// 3 color + transparent mode requires color0 <= color1
				if( color0 > color1 )
				{
					var swap = color0;
					color0 = color1;
					color1 = swap;
				}
			}
			else
			{
				// 4 color mode requires color0 > color1
				if( color0 < color1 )
				{
					var swap = color0;
					color0 = color1;
					color1 = swap;
				}
			}

			output[offset] = (byte)( color0 & 0xFF );
			output[offset + 1] = (byte)( color0 >> 8 );
			output[offset + 2] = (byte)( color1 & 0xFF );
			output[offset + 3] = (byte)( color1 >> 8 );

			// build the palette the way a decoder would
			var paletteCount = hasTransparency ? 3 : 4;
			var palette = new int[4][];
			palette[0] = From565( color0 );
			palette[1] = From565( color1 );
			if( hasTransparency )
			{
				palette[2] = new[]
				{
					( palette[0][0] + palette[1][0] ) / 2,
					( palette[0][1] + palette[1][1] ) / 2,
					( palette[0][2] + palette[1][2] ) / 2,
				};
				palette[3] = new[] { 0, 0, 0 };
			}
			else
			{
				palette[2] = new[]
				{
					( 2 * palette[0][0] + palette[1][0] ) / 3,
					( 2 * palette[0][1] + palette[1][1] ) / 3,
					( 2 * palette[0][2] + palette[1][2] ) / 3,
				};
				palette[3] = new[]
				{
					( palette[0][0] + 2 * palette[1][0] ) / 3,
					( palette[0][1] + 2 * palette[1][1] ) / 3,
					( palette[0][2] + 2 * palette[1][2] ) / 3,
				};
			}

			var indexBits = 0U;
			for( var pixel = 0; pixel < 16; pixel++ )
			{
				var argb = blockArgb[pixel];
				int bestIndex;
				if( hasTransparency && ( ( argb >> 24 ) & 0xFF ) < 128 )
				{
					bestIndex = 3;
				}
				else
				{
					var r = ( argb >> 16 ) & 0xFF;
					var g = ( argb >> 8 ) & 0xFF;
					var b = argb & 0xFF;
					bestIndex = 0;
					var bestDistance = int.MaxValue;
					for( var index = 0; index < paletteCount; index++ )
					{
						var deltaR = palette[index][0] - r;
						var deltaG = palette[index][1] - g;
						var deltaB = palette[index][2] - b;
						var distance = deltaR * deltaR + deltaG * deltaG + deltaB * deltaB;
						if( distance < bestDistance )
						{
							bestDistance = distance;
							bestIndex = index;
						}
					}
				}
				indexBits |= (uint)bestIndex << ( pixel * 2 );
			}

			output[offset + 4] = (byte)( indexBits & 0xFF );
			output[offset + 5] = (byte)( ( indexBits >> 8 ) & 0xFF );
			output[offset + 6] = (byte)( ( indexBits >> 16 ) & 0xFF );
			output[offset + 7] = (byte)( ( indexBits >> 24 ) & 0xFF );
		}

		private static int To565( int r, int g, int b )
		{
			return ( ( ( r * 31 + 127 ) / 255 ) << 11 )
				| ( ( ( g * 63 + 127 ) / 255 ) << 5 )
				| ( ( b * 31 + 127 ) / 255 );
		}

		private static int[] From565( int color )
		{
			var r5 = ( color >> 11 ) & 0x1F;
			var g6 = ( color >> 5 ) & 0x3F;
			var b5 = color & 0x1F;
			return new[]
			{
				( r5 << 3 ) | ( r5 >> 2 ),
				( g6 << 2 ) | ( g6 >> 4 ),
				( b5 << 3 ) | ( b5 >> 2 ),
			};
		}

		public static Type? GetFieldOrPropertyType( this MemberInfo member )
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

		public static void SetMemberValue( this MemberInfo member, object instance, object? value )
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

		public static object? GetMemberValue( this MemberInfo member, object instance )
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
