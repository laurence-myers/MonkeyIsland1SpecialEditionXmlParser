using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace MonkeyIslandSpecialEditionSpriteEditor.PlayTest
{
	/// <summary>
	/// Starts or brings forward the real Special Edition game for the play-test loop. The game
	/// re-reads a room's loose overrides every time the room is entered, so "test in game" is:
	/// write the overrides, then hand the modder the running game to walk out of the room and back
	/// in. Nothing here touches the game process beyond launching it and focusing its window.
	/// </summary>
	public static class GameLauncher
	{
		/// <summary>The SE executable, next to Monkey1.pak in every known distribution (Steam, GOG).</summary>
		public const string ExeName = "MISE.exe";

		/// <summary>The process name Windows reports for the running game (the exe without extension).</summary>
		public const string ProcessName = "MISE";

		/// <summary>Steam app id of The Secret of Monkey Island: Special Edition, for the URL fallback.</summary>
		public const string SteamAppId = "32360";

		/// <summary>The full path of the game exe next to the pak, or null when it is not there.</summary>
		public static string? ResolveExePath( string? pakDirectory )
		{
			if( string.IsNullOrWhiteSpace( pakDirectory ) )
			{
				return null;
			}

			var exe = Path.Combine( pakDirectory, ExeName );
			return File.Exists( exe ) ? exe : null;
		}

		/// <summary>The steam:// URL that launches the game through Steam (used when the exe is missing).</summary>
		public static string SteamRunUrl => "steam://rungameid/" + SteamAppId;

		/// <summary>The running game process with a main window, or null when the game is not running.</summary>
		public static Process? FindRunning()
		{
			foreach( var process in Process.GetProcessesByName( ProcessName ) )
			{
				try
				{
					if( process.MainWindowHandle != IntPtr.Zero )
					{
						return process;
					}
				}
				catch( Exception )
				{
					// a process that exited between enumeration and inspection; skip it
				}

				process.Dispose();
			}

			return null;
		}

		/// <summary>
		/// Brings the running game to the front if it is running, otherwise launches it (the exe next
		/// to the pak, falling back to Steam). Returns a short human-readable outcome.
		/// </summary>
		public static string LaunchOrFocus( string? pakDirectory )
		{
			using( var running = FindRunning() )
			{
				if( running != null )
				{
					Focus( running.MainWindowHandle );
					return "game is running - switch to it and re-enter the room";
				}
			}

			var exe = ResolveExePath( pakDirectory );
			if( exe != null )
			{
				Process.Start( new ProcessStartInfo( exe ) { WorkingDirectory = pakDirectory!, UseShellExecute = true } )?.Dispose();
				return "launched " + ExeName + " - load a save in the room to see the overrides";
			}

			Process.Start( new ProcessStartInfo( SteamRunUrl ) { UseShellExecute = true } )?.Dispose();
			return "launched via Steam - load a save in the room to see the overrides";
		}

		private static void Focus( IntPtr window )
		{
			// a minimised game window has to be restored before it can take the foreground
			if( IsIconic( window ) )
			{
				ShowWindow( window, SwRestore );
			}

			SetForegroundWindow( window );
		}

		private const int SwRestore = 9;

		[DllImport( "user32.dll" )]
		[return: MarshalAs( UnmanagedType.Bool )]
		private static extern bool SetForegroundWindow( IntPtr hWnd );

		[DllImport( "user32.dll" )]
		[return: MarshalAs( UnmanagedType.Bool )]
		private static extern bool ShowWindow( IntPtr hWnd, int nCmdShow );

		[DllImport( "user32.dll" )]
		[return: MarshalAs( UnmanagedType.Bool )]
		private static extern bool IsIconic( IntPtr hWnd );
	}
}
