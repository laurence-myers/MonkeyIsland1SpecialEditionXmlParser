using System;
using System.IO;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm;

namespace MonkeyIslandSpecialEditionSpriteEditor.Commands
{
	/// <summary>
	/// Writes a redistributable walkbox patch capturing the difference between the pak's pristine
	/// classic data and the loose override the modder has been editing. The patch holds only the
	/// changed box records and one-way fingerprints, never the original game bytes.
	/// </summary>
	public class ExportClassicPatchCommand( LPAKFile? lpakFile, string? outputPath, ClassicPatchInfo info, string fingerprintLabel, string toolVersion ) : BaseCommand
	{
		protected override CommandResult InnerExecute()
		{
			if( lpakFile == null || string.IsNullOrWhiteSpace( lpakFile.FileNameOnDisk ) )
			{
				return CommandResult.Fail( "Invalid LPAK file" );
			}
			if( string.IsNullOrWhiteSpace( outputPath ) )
			{
				return CommandResult.Fail( "Invalid output path" );
			}

			var dataIndex = ClassicOverride.ResourceEntryIndex( lpakFile );
			if( dataIndex < 0 )
			{
				return CommandResult.Fail( "This pak has no classic data (.001) to build a patch from." );
			}
			if( ClassicOverride.ResourceEntryIsCompressed( lpakFile, dataIndex ) )
			{
				return CommandResult.Fail( "The classic data is compressed in this pak, which the walk box patch tools do not support." );
			}

			byte[] pristine;
			byte[] edited;
			try
			{
				pristine = lpakFile.ReadEntryBytes( dataIndex );
				var dataEntryName = lpakFile.PakFileNames[dataIndex].FileName;
				edited = lpakFile.ReadResourceBytes( dataEntryName ) ?? pristine;
			}
			catch( Exception exception )
			{
				return CommandResult.Fail( "Could not read the classic data: " + exception.Message );
			}

			ClassicPatch? patch;
			try
			{
				patch = ClassicPatcher.BuildPatch( pristine, edited, info, fingerprintLabel, toolVersion );
			}
			catch( Exception exception )
			{
				return CommandResult.Fail( "Could not build the patch: " + exception.Message );
			}

			if( patch == null )
			{
				return CommandResult.Fail( "There are no saved walkbox changes to export. Edit and save walk boxes first." );
			}

			try
			{
				Helper.WriteObjectToFile( outputPath!, patch );
			}
			catch( Exception exception )
			{
				return CommandResult.Fail( "Could not write the patch file: " + exception.Message );
			}

			return CommandResult.Success( string.Concat( "Walkbox patch written to ", outputPath, " (", patch.RoomEdits.Count, " room(s))." ) );
		}
	}
}
