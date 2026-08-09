using System;
using System.IO;
using MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm;
using LpakParser = MonkeyIslandSpecialEditionSpriteEditor.Formats.LPAK.Parser;

namespace MonkeyIslandSpecialEditionSpriteEditor.PatchCli
{
	/// <summary>
	/// The console tool's logic, kept separate from <see cref="Program"/> so it can be tested.
	/// It applies (or removes) a redistributable walkbox patch against a pak, reusing the same
	/// <see cref="ClassicPatchService"/> the editor uses - no game data is ever destroyed (the pak
	/// is untouched; edits go to a loose override).
	/// </summary>
	public static class CliRunner
	{
		private const int ExitOk = 0;
		private const int ExitError = 1;
		private const int ExitUsage = 2;

		/// <summary>
		/// Runs the tool. Returns a process exit code: 0 success, 1 error, 2 bad usage.
		/// </summary>
		public static int Run( string[] args, TextWriter output, TextWriter error )
		{
			if( args.Length == 0 )
			{
				PrintUsage( output );
				return ExitUsage;
			}

			var verb = args[0].ToLowerInvariant();
			if( verb == "-h" || verb == "--help" || verb == "help" )
			{
				PrintUsage( output );
				return ExitOk;
			}

			if( ( verb != "apply" && verb != "remove" ) || args.Length != 3 )
			{
				error.WriteLine( "Usage: mi1se-walkbox-patch <apply|remove> <Monkey1.pak> <patch.mi1classicpatch.xml>" );
				return ExitUsage;
			}

			var pakPath = args[1];
			var patchPath = args[2];
			if( !File.Exists( pakPath ) )
			{
				error.WriteLine( string.Concat( "Pak file not found: ", pakPath ) );
				return ExitError;
			}
			if( !File.Exists( patchPath ) )
			{
				error.WriteLine( string.Concat( "Patch file not found: ", patchPath ) );
				return ExitError;
			}

			Formats.LPAK.LPAKFile? lpakFile;
			try
			{
				lpakFile = LpakParser.Parse( pakPath );
			}
			catch( Exception exception )
			{
				error.WriteLine( "Could not open the pak: " + exception.Message );
				return ExitError;
			}
			if( lpakFile == null )
			{
				error.WriteLine( string.Concat( "Could not open the pak: ", pakPath ) );
				return ExitError;
			}

			var result = verb == "apply"
				? ClassicPatchService.Apply( lpakFile, patchPath )
				: ClassicPatchService.Remove( lpakFile, patchPath );

			if( result.IsSuccess )
			{
				output.WriteLine( result.Value );
				return ExitOk;
			}

			error.WriteLine( result.Error );
			return ExitError;
		}

		private static void PrintUsage( TextWriter writer )
		{
			writer.WriteLine( "mi1se-walkbox-patch - apply a Monkey Island SE walk box patch to your own game data." );
			writer.WriteLine();
			writer.WriteLine( "Usage:" );
			writer.WriteLine( "  mi1se-walkbox-patch apply  <Monkey1.pak> <patch.mi1classicpatch.xml>" );
			writer.WriteLine( "  mi1se-walkbox-patch remove <Monkey1.pak> <patch.mi1classicpatch.xml>" );
			writer.WriteLine();
			writer.WriteLine( "The pak is never modified: the patch is written as a loose classic-data override" );
			writer.WriteLine( "beside it, which the game loads instead. 'remove' restores the pristine data." );
			writer.WriteLine( "A patch only applies to the game version it was built for, and refuses to touch a" );
			writer.WriteLine( "room another mod has already changed." );
		}
	}
}
