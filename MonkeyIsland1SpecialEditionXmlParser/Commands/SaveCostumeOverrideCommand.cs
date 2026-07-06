using System;
using System.IO;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes;
using MonkeyIsland1SpecialEditionXmlParser.Formats.Costumes.Entities;
using MonkeyIsland1SpecialEditionXmlParser.Formats.LPAK;

namespace MonkeyIsland1SpecialEditionXmlParser.Commands
{
	/// <summary>
	/// Writes an edited costume as a loose override binary (the file the game loads instead
	/// of the LPAK entry) and refreshes the overrides XML export so hand-editing stays
	/// possible.
	/// </summary>
	public class SaveCostumeOverrideCommand( LPAKFile? lpakFile, string? resourcePath, Costume? costume ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			if( lpakFile == null || string.IsNullOrWhiteSpace( lpakFile.FileNameOnDisk ) )
			{
				return CommandResult.Fail( "Invalid LPAK file" );
			}
			if( string.IsNullOrWhiteSpace( resourcePath ) )
			{
				return CommandResult.Fail( "Invalid resource path" );
			}
			if( costume == null )
			{
				return CommandResult.Fail( "No costume to save" );
			}

			// never write a costume the parser would reject
			try
			{
				SanityChecker.Check( costume );
			}
			catch( Exception exception )
			{
				return CommandResult.Fail( "Sanity check failed: " + exception.Message );
			}

			var lpakDirectory = Path.GetDirectoryName( lpakFile.FileNameOnDisk );
			if( lpakDirectory == null )
			{
				return CommandResult.Fail( "Invalid LPAK directory" );
			}

			// the loose override path is where Formats.LPAK.Parser.GetOverrideFilePath looks
			var binaryPath = Path.Combine( lpakDirectory, resourcePath );
			var binaryDirectory = Path.GetDirectoryName( binaryPath );
			if( !string.IsNullOrWhiteSpace( binaryDirectory ) && !Directory.Exists( binaryDirectory ) )
			{
				Directory.CreateDirectory( binaryDirectory );
			}

			// write via a temp file so a failed write can't leave a truncated
			// override behind that the game (and this tool) would then load
			var tempPath = binaryPath + ".tmp";
			try
			{
				Packer.WriteCostumeToBinaryFile( tempPath, costume );
				if( File.Exists( binaryPath ) )
				{
					File.Delete( binaryPath );
				}
				File.Move( tempPath, binaryPath );
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
				return CommandResult.Fail( "Failed to write override: " + exception.Message );
			}

			// keep the overrides XML in sync for hand-editing
			var xmlResult = new ExportToOverrideXmlCommand( lpakFile.FileNameOnDisk, resourcePath, costume ).Execute();
			if( !xmlResult.IsSuccess )
			{
				return CommandResult.Fail( "Override binary written, but XML export failed: " + xmlResult.Error );
			}

			return CommandResult.Success( $"Override saved to {binaryPath}" );
		}
	}
}
