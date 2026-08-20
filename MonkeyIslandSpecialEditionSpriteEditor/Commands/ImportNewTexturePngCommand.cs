
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;

namespace MonkeyIslandSpecialEditionSpriteEditor.Commands
{
	/// <summary>
	/// Encodes a PNG as a brand-new game texture at a resource path that does not exist in the
	/// LPAK, writing it as a loose .dxt next to the pak. Unlike <see cref="ImportTexturePngCommand"/>
	/// there is no original entry to copy the format and size from, so the caller chooses the DXT
	/// format (or lets it auto-detect) and the dimensions come from the PNG.
	/// <para>
	/// NB: the editor resolves the new texture through the override-first lookup, but whether the
	/// GAME loads a texture whose path is absent from the LPAK index is unverified - test in game
	/// before relying on it.
	/// </para>
	/// </summary>
	public class ImportNewTexturePngCommand( LPAKFile? lpakFile, string? resourcePath, string? pngFileName, string? fourCC, bool overwriteExisting ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			var result = Import( lpakFile, resourcePath, pngFileName, fourCC, overwriteExisting );

			// with auto-write on, the running game picks the texture up without F5/F11
			return result.IsSuccess && PlayTest.AutoHotReload.SignalIfEnabled()
				? CommandResult.Success( result.Value + " Hot-reloaded the running game." )
				: result;
		}

		/// <summary>
		/// The reusable core of the command. Encodes <paramref name="pngFileName"/> to the game's
		/// .dxt format and writes it to <paramref name="resourcePath"/> as a loose override.
		/// </summary>
		/// <param name="fourCC">"DXT1", "DXT5", or null/"Auto" to pick by alpha.</param>
		public static CommandResult Import( LPAKFile? lpakFile, string? resourcePath, string? pngFileName, string? fourCC, bool overwriteExisting )
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

			var validation = ValidateResourcePath( lpakFile, resourcePath!, overwriteExisting );
			if( !validation.IsSuccess )
			{
				return validation;
			}

			byte[] dxtBytes;
			using( var image = Image.FromFile( pngFileName! ) )
			using( var bitmap = new Bitmap( image ) )
			{
				// DXT compresses 4x4 blocks; the game's textures are all block-aligned and the
				// game's tolerance for partial-block textures is unknown, so require multiples of 4
				if( bitmap.Width % 4 != 0 || bitmap.Height % 4 != 0 )
				{
					return CommandResult.Fail( string.Concat(
						"PNG size ", bitmap.Width, "x", bitmap.Height,
						" must be a multiple of 4 in both dimensions. Pad the image and try again."
					) );
				}

				var targetFourCC = string.IsNullOrWhiteSpace( fourCC ) || fourCC == "Auto"
					? Helper.DetectDxtFourCC( bitmap )
					: fourCC!;

				try
				{
					dxtBytes = Helper.DxtBytesFromImage( bitmap, targetFourCC );
				}
				catch( NotSupportedException exception )
				{
					return CommandResult.Fail( exception.Message );
				}
			}

			return ImportTexturePngCommand.WriteTextureOverride( lpakFile, resourcePath!, dxtBytes );
		}

		/// <summary>
		/// Validates a would-be new texture path: it must be a relative, ASCII, forward-slash
		/// .dxt path that does not collide with a packed entry (case-insensitively, because the
		/// filesystem is case-insensitive while the pak lookup is not) and, unless
		/// <paramref name="overwriteExisting"/> is set, does not already exist on disk.
		/// </summary>
		public static CommandResult ValidateResourcePath( LPAKFile lpakFile, string resourcePath, bool overwriteExisting )
		{
			if( string.IsNullOrWhiteSpace( resourcePath ) )
			{
				return CommandResult.Fail( "Invalid resource path" );
			}
			if( resourcePath.Contains( '\\' ) )
			{
				return CommandResult.Fail( "Use forward slashes in the resource path (e.g. art/custom/my-texture.dxt)." );
			}
			if( !resourcePath.EndsWith( ".dxt", StringComparison.OrdinalIgnoreCase ) )
			{
				return CommandResult.Fail( "The resource path must end in .dxt" );
			}
			if( resourcePath.Any( c => c > 127 ) )
			{
				return CommandResult.Fail( "The resource path must use ASCII characters only." );
			}
			if( Path.IsPathRooted( resourcePath ) )
			{
				return CommandResult.Fail( "The resource path must be relative to the game folder, not an absolute path." );
			}

			var invalidChars = Path.GetInvalidFileNameChars();
			foreach( var segment in resourcePath.Split( '/' ) )
			{
				if( segment.Length == 0 )
				{
					return CommandResult.Fail( "The resource path has an empty folder or file name." );
				}
				if( segment == "." || segment == ".." )
				{
					return CommandResult.Fail( "The resource path must not contain '.' or '..' segments." );
				}
				if( segment.IndexOfAny( invalidChars ) >= 0 )
				{
					return CommandResult.Fail( "The resource path contains characters that are not valid in a file name." );
				}
			}

			var exactIndex = lpakFile.FindEntryIndex( name => name == resourcePath );
			if( exactIndex >= 0 )
			{
				return CommandResult.Fail( "A texture already exists at this path in the LPAK. Use \"Import texture PNG\" to replace it instead." );
			}
			var caseIndex = lpakFile.FindEntryIndex( name => string.Equals( name, resourcePath, StringComparison.OrdinalIgnoreCase ) );
			if( caseIndex >= 0 )
			{
				return CommandResult.Fail( string.Concat(
					"A texture already exists at this path with different capitalisation (",
					lpakFile.PakFileNames[caseIndex].FileName,
					"). Choose a different name so it will not shadow the packed texture."
				) );
			}

			var existingOverride = Formats.LPAK.Parser.GetOverrideFilePath( lpakFile.FileNameOnDisk, resourcePath );
			if( existingOverride != null && !overwriteExisting )
			{
				return CommandResult.Fail( "A loose texture file already exists at this path. Confirm the overwrite and try again." );
			}

			return CommandResult.Success( "" );
		}
	}
}
