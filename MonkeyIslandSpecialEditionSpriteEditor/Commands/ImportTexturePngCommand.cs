using System;
using System.Drawing;
using System.IO;
using System.Text;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;

namespace MonkeyIslandSpecialEditionSpriteEditor.Commands
{
	/// <summary>
	/// Encodes a repainted PNG back into the game's .dxt wrapper format (matching the
	/// original texture's DXT format and dimensions) and writes it as a loose override
	/// file next to the LPAK, where the game loads it instead of the packed texture.
	/// </summary>
	public class ImportTexturePngCommand( LPAKFile? lpakFile, string? resourcePath, string? pngFileName ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			var result = ImportTexturePngCommand.Import( lpakFile, resourcePath, pngFileName );

			// with auto-write on, the running game picks the texture up without F5/F11
			return result.IsSuccess && PlayTest.AutoHotReload.SignalIfEnabled()
				? CommandResult.Success( result.Value + " Hot-reloaded the running game." )
				: result;
		}

		/// <summary>
		/// The reusable core of the command, so the batch import can run it per file
		/// without spamming the status bar through Execute().
		/// </summary>
		internal static CommandResult Import( LPAKFile? lpakFile, string? resourcePath, string? pngFileName )
		{
			if( lpakFile == null || string.IsNullOrWhiteSpace( lpakFile.FileNameOnDisk ) )
			{
				return CommandResult.Fail( "Invalid LPAK file" );
			}
			if( string.IsNullOrWhiteSpace( resourcePath ) )
			{
				return CommandResult.Fail( "Invalid resource path" );
			}
			if( string.IsNullOrWhiteSpace( pngFileName ) || !File.Exists( pngFileName ) )
			{
				return CommandResult.Fail( "PNG file does not exist" );
			}

			// the original texture dictates the DXT format and the expected dimensions
			var originalIndex = lpakFile.FindEntryIndex( name => name == resourcePath );
			if( originalIndex < 0 )
			{
				return CommandResult.Fail( "Texture not found in LPAK: " + resourcePath );
			}
			var originalBytes = lpakFile.ReadEntryBytes( originalIndex );
			if( originalBytes.Length < 12 )
			{
				return CommandResult.Fail( "Original texture has no valid .dxt header" );
			}
			var fourCC = Encoding.ASCII.GetString( originalBytes, 0, 4 );
			var originalWidth = BitConverter.ToInt32( originalBytes, 4 );
			var originalHeight = BitConverter.ToInt32( originalBytes, 8 );

			byte[] dxtBytes;
			using( var image = Image.FromFile( pngFileName! ) )
			using( var bitmap = new Bitmap( image ) )
			{
				if( bitmap.Width != originalWidth || bitmap.Height != originalHeight )
				{
					return CommandResult.Fail( string.Concat(
						"PNG size ", bitmap.Width, "x", bitmap.Height,
						" does not match the original texture size ", originalWidth, "x", originalHeight
					) );
				}

				try
				{
					dxtBytes = Helper.DxtBytesFromImage( bitmap, fourCC );
				}
				catch( NotSupportedException exception )
				{
					return CommandResult.Fail( exception.Message );
				}
			}

			return WriteTextureOverride( lpakFile, resourcePath!, dxtBytes );
		}

		/// <summary>
		/// Writes the encoded .dxt bytes as a loose override next to the LPAK, the same location
		/// the game checks before the packed archive. Shared with the new-texture import.
		/// </summary>
		internal static CommandResult WriteTextureOverride( LPAKFile lpakFile, string resourcePath, byte[] dxtBytes )
		{
			var lpakDirectory = Path.GetDirectoryName( lpakFile.FileNameOnDisk );
			if( lpakDirectory == null )
			{
				return CommandResult.Fail( "Invalid LPAK directory" );
			}
			var overridePath = Path.Combine( lpakDirectory, resourcePath );
			var overrideDirectory = Path.GetDirectoryName( overridePath );
			if( !string.IsNullOrWhiteSpace( overrideDirectory ) && !Directory.Exists( overrideDirectory ) )
			{
				Directory.CreateDirectory( overrideDirectory );
			}

			// write via a temp file so a failed write can't leave a truncated
			// texture behind that the game would then load
			var tempPath = overridePath + ".tmp";
			try
			{
				File.WriteAllBytes( tempPath, dxtBytes );
				if( File.Exists( overridePath ) )
				{
					File.Delete( overridePath );
				}
				File.Move( tempPath, overridePath );
			}
			catch( Exception exception )
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
					// the temp file is inert; leaving it behind is better than masking the original error
				}
				return CommandResult.Fail( "Failed to write texture override: " + exception.Message );
			}

			return CommandResult.Success( $"Texture override written to {overridePath}" );
		}
	}
}
