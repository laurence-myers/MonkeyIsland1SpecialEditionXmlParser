using System.IO;
using MonkeyIslandSpecialEditionSpriteEditor.PatchCli;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class CliRunnerTests
	{
		private static int Run( out string output, out string errorText, params string[] args )
		{
			using( var outputWriter = new StringWriter() )
			using( var errorWriter = new StringWriter() )
			{
				var exitCode = CliRunner.Run( args, outputWriter, errorWriter );
				output = outputWriter.ToString();
				errorText = errorWriter.ToString();
				return exitCode;
			}
		}

		[Test]
		public void NoArguments_PrintsUsageAndReturnsUsageCode()
		{
			var exit = Run( out var output, out _, System.Array.Empty<string>() );
			Assert.That( exit, Is.EqualTo( 2 ) );
			Assert.That( output, Does.Contain( "Usage" ) );
		}

		[Test]
		public void Help_PrintsUsageAndSucceeds()
		{
			var exit = Run( out var output, out _, "--help" );
			Assert.That( exit, Is.EqualTo( 0 ) );
			Assert.That( output, Does.Contain( "apply" ) );
		}

		[Test]
		public void UnknownVerb_ReturnsUsageCode()
		{
			var exit = Run( out _, out var errorText, "frobnicate", "a", "b" );
			Assert.That( exit, Is.EqualTo( 2 ) );
			Assert.That( errorText, Does.Contain( "Usage" ) );
		}

		[Test]
		public void Apply_WrongArgumentCount_ReturnsUsageCode()
		{
			var exit = Run( out _, out _, "apply", "onlyonearg" );
			Assert.That( exit, Is.EqualTo( 2 ) );
		}

		[Test]
		public void Apply_MissingPak_ReturnsErrorCode()
		{
			var missingPak = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".pak" );
			var missingPatch = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".xml" );
			var exit = Run( out _, out var errorText, "apply", missingPak, missingPatch );
			Assert.That( exit, Is.EqualTo( 1 ) );
			Assert.That( errorText, Does.Contain( "Pak file not found" ) );
		}

		[Test]
		public void Apply_MissingPatch_ReturnsErrorCode()
		{
			// a real (empty) pak path that exists, but a missing patch
			var pakPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".pak" );
			File.WriteAllBytes( pakPath, new byte[] { 0 } );
			var missingPatch = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() + ".xml" );
			try
			{
				var exit = Run( out _, out var errorText, "apply", pakPath, missingPatch );
				Assert.That( exit, Is.EqualTo( 1 ) );
				Assert.That( errorText, Does.Contain( "Patch file not found" ) );
			}
			finally
			{
				File.Delete( pakPath );
			}
		}
	}
}
