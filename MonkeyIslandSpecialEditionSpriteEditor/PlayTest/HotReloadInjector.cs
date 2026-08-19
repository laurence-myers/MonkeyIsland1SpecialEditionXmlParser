using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MonkeyIslandSpecialEditionSpriteEditor.PlayTest
{
	/// <summary>
	/// Injects <c>mise-mreload.dll</c> (the in-place hot-reload hook) into the running Special Edition so
	/// "Test in game" reloads edits without the modder running <c>se-inject.exe</c> by hand. The injector
	/// and the game are both 32-bit, so we shell out to the 32-bit <c>se-inject.exe</c> (which works from an
	/// editor of any architecture) rather than injecting from managed code. It is a graceful no-op — a
	/// false return with a reason — when the game is not reachable or the tools are not built. Once
	/// injected, the DLL stays loaded for the game's lifetime, so this runs at most once per game session.
	/// </summary>
	public static class HotReloadInjector
	{
		/// <summary>The 32-bit injector built by <c>se-file-hook/build.ps1</c>.</summary>
		public const string InjectorExe = "se-inject.exe";

		/// <summary>The in-place hot-reload hook DLL built by <c>se-file-hook/build.ps1</c>.</summary>
		public const string ReloadDll = "mise-mreload.dll";

		/// <summary>Optional override for the folder holding se-inject.exe + mise-mreload.dll.</summary>
		public const string DirEnvVar = "MISE_HOTRELOAD_DIR";

		/// <summary>
		/// Ensures the hot-reload DLL is injected into the running game. Returns true (a no-op) when it is
		/// already injected; otherwise runs the injector and waits for the DLL to come up. Returns false
		/// with a human-readable reason when the tools are missing or the injection did not take.
		/// </summary>
		public static bool TryEnsureInjected( out string message )
		{
			if( HotReloadClient.IsAvailable() )
			{
				message = "hot-reload already active";
				return true;
			}

			var dir = LocateToolsDir();
			if( dir == null )
			{
				message = "hot-reload tools not found — build se-file-hook (or set " + DirEnvVar + ")";
				return false;
			}

			var injector = Path.Combine( dir, InjectorExe );
			var dll = Path.Combine( dir, ReloadDll );
			try
			{
				var start = new ProcessStartInfo( injector, "\"" + dll + "\" " + GameLauncher.ExeName )
				{
					WorkingDirectory = dir,
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
				};

				string output;
				using( var process = Process.Start( start ) )
				{
					if( process == null )
					{
						message = "could not start " + InjectorExe;
						return false;
					}

					output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
					process.WaitForExit( 8000 );
				}

				// the DLL creates its event on a worker thread — give it a moment before we conclude
				for( int i = 0; i < 40 && !HotReloadClient.IsAvailable(); ++i )
				{
					Thread.Sleep( 50 );
				}

				if( HotReloadClient.IsAvailable() )
				{
					message = "injected " + ReloadDll;
					return true;
				}

				message = "ran " + InjectorExe + " but the hot-reload DLL did not load: " + FirstLine( output );
				return false;
			}
			catch( Exception exception )
			{
				message = "hot-reload inject failed: " + exception.Message;
				return false;
			}
		}

		/// <summary>
		/// Injects the hot-reload DLL as soon as a just-launched game has a window — i.e. at the menu,
		/// before any room loads. This is what makes costumes (and textures) reload: the resource_get
		/// hook must be active before the room streams its resources in, or their handles are never
		/// captured (unlike .room.xml, which the DLL reads from a fixed address and so reloads either
		/// way). Runs on a background thread so the editor UI does not block waiting for the game window.
		/// </summary>
		public static void InjectWhenReady( int timeoutMs = 30000 )
		{
			Task.Run( () =>
			{
				var deadline = Environment.TickCount + timeoutMs;
				while( Environment.TickCount - deadline < 0 )
				{
					if( HotReloadClient.IsAvailable() )
					{
						return;   // already injected (this call or an earlier one)
					}

					using( var running = GameLauncher.FindRunning() )
					{
						if( running != null )
						{
							TryEnsureInjected( out _ );
							return;
						}
					}

					Thread.Sleep( 500 );
				}
			} );
		}

		private static string FirstLine( string text )
		{
			var trimmed = ( text ?? string.Empty ).Trim();
			var newline = trimmed.IndexOfAny( new[] { '\r', '\n' } );
			return newline < 0 ? trimmed : trimmed.Substring( 0, newline );
		}

		private static string? LocateToolsDir()
		{
			var overrideDir = Environment.GetEnvironmentVariable( DirEnvVar );
			if( !string.IsNullOrWhiteSpace( overrideDir ) && HasTools( overrideDir ) )
			{
				return overrideDir;
			}

			foreach( var candidate in CandidateDirs() )
			{
				if( HasTools( candidate ) )
				{
					return candidate;
				}
			}
			return null;
		}

		/// <summary>Where the tools might live: next to the editor exe, or in a se-file-hook folder up the tree.</summary>
		private static IEnumerable<string> CandidateDirs()
		{
			var baseDir = AppContext.BaseDirectory;
			yield return baseDir;
			yield return Path.Combine( baseDir, "se-file-hook" );

			var dir = new DirectoryInfo( baseDir );
			for( int i = 0; i < 8 && dir != null; ++i, dir = dir.Parent )
			{
				yield return Path.Combine( dir.FullName, "se-file-hook" );
			}
		}

		private static bool HasTools( string dir )
		{
			return File.Exists( Path.Combine( dir, InjectorExe ) ) && File.Exists( Path.Combine( dir, ReloadDll ) );
		}
	}
}
