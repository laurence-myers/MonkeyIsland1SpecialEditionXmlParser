using System;
using System.Collections.Generic;
using System.IO;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm.Entities;

namespace MonkeyIslandSpecialEditionSpriteEditor.Commands
{
	/// <summary>
	/// Writes edited walkboxes back to the classic SCUMM resource file (monkey1.001). The edit is
	/// size-neutral, so the BOXD records are patched in place and every other byte is preserved.
	/// The original file is never destroyed: loose classic data is backed up to a .bak once before
	/// the first write, and pak-embedded data is written to a loose override beside the pak (the
	/// pak stays the pristine copy), with the .000 index sibling extracted so room names and
	/// costumes still load.
	/// </summary>
	public class SaveWalkBoxesCommand( LPAKFile? lpakFile, ClassicData? classicData, int roomNumber, IReadOnlyList<ClassicBox> editedBoxes, IReadOnlyList<ClassicBox> originalBoxes ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			if( lpakFile == null || string.IsNullOrWhiteSpace( lpakFile.FileNameOnDisk ) )
			{
				return CommandResult.Fail( "Invalid LPAK file" );
			}
			if( classicData == null )
			{
				return CommandResult.Fail( "No classic data is loaded, so walkboxes cannot be saved." );
			}
			if( editedBoxes.Count == 0 )
			{
				return CommandResult.Fail( "There are no walkboxes to save." );
			}

			string targetPath;
			byte[] sourceBytes;
			string? backupPath = null;

			if( !string.IsNullOrWhiteSpace( classicData.LooseDataFilePath ) )
			{
				// loose origin: patch the loose file in place, backing it up once
				targetPath = classicData.LooseDataFilePath!;
				if( !File.Exists( targetPath ) )
				{
					return CommandResult.Fail( string.Concat( "Classic data file not found: ", targetPath ) );
				}
				try
				{
					sourceBytes = File.ReadAllBytes( targetPath );
				}
				catch( Exception exception )
				{
					return CommandResult.Fail( "Could not read the classic data file: " + exception.Message );
				}
				backupPath = targetPath + ".bak";
			}
			else if( !string.IsNullOrWhiteSpace( classicData.PakDataEntryName ) )
			{
				// pak origin: write a loose override beside the pak; the pak itself is the backup
				var pakDirectory = Path.GetDirectoryName( lpakFile.FileNameOnDisk );
				if( pakDirectory == null )
				{
					return CommandResult.Fail( "Invalid LPAK directory" );
				}
				targetPath = Path.Combine( pakDirectory, classicData.PakDataEntryName! );

				// source from an existing loose override when present, else the pristine pak entry
				var read = lpakFile.ReadResourceBytes( classicData.PakDataEntryName );
				if( read == null )
				{
					return CommandResult.Fail( "Could not read the classic data from the pak." );
				}
				sourceBytes = read;

				var extractError = ClassicOverride.ExtractIndexSibling( lpakFile, pakDirectory );
				if( extractError != null )
				{
					return CommandResult.Fail( extractError );
				}
			}
			else
			{
				return CommandResult.Fail( "The classic data's origin is unknown, so walkboxes cannot be saved." );
			}

			// patch into a private buffer (verifies the file still holds the loaded boxes)
			byte[] patched;
			try
			{
				patched = Packer.PatchRoomBoxes( sourceBytes, roomNumber, editedBoxes, originalBoxes );
			}
			catch( Exception exception )
			{
				return CommandResult.Fail( "Could not patch the walkboxes: " + exception.Message );
			}

			// back up the loose original once, before it is overwritten
			if( backupPath != null && !File.Exists( backupPath ) )
			{
				try
				{
					File.Copy( targetPath, backupPath );
				}
				catch( Exception exception )
				{
					return CommandResult.Fail( "Could not create a backup, so nothing was written: " + exception.Message );
				}
			}

			// atomic swap: File.Replace never leaves the target missing on a failed write
			try
			{
				ClassicOverride.AtomicWrite( targetPath, patched );
			}
			catch( Exception exception )
			{
				return CommandResult.Fail( "Failed to write the walkboxes: " + exception.Message );
			}

			return CommandResult.Success( string.Concat( "Walkboxes saved to ", targetPath, " - verify in game." ) );
		}
	}
}
