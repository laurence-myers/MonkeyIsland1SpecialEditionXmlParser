using System;
using System.Threading;

namespace MonkeyIslandSpecialEditionSpriteEditor.PlayTest
{
	/// <summary>
	/// Signals the injected <c>mise-hotreload.dll</c> in the running Special Edition to reload the
	/// current room's edited art immediately (a real room bounce on the engine's own thread), via the
	/// named event the DLL waits on. It is a graceful no-op — <see cref="TrySignal"/> returns false —
	/// when the DLL is not injected, so callers fall back to the walk-out/in play-test loop.
	/// See <c>se-file-hook/reload.c</c> for the game-side hook.
	/// </summary>
	public static class HotReloadClient
	{
		/// <summary>The auto-reset event <c>mise-hotreload.dll</c> creates inside the running game.</summary>
		public const string EventName = "Local\\MISE_HotReload";

		/// <summary>
		/// Fires the in-game reload if the hot-reload DLL is injected. Returns true when the reload was
		/// signalled, false when the DLL is not present (or the event is not accessible).
		/// </summary>
		public static bool TrySignal()
		{
			try
			{
				using( var evt = EventWaitHandle.OpenExisting( EventName ) )
				{
					evt.Set();
					return true;
				}
			}
			catch( WaitHandleCannotBeOpenedException )
			{
				return false;   // the DLL is not injected (the event does not exist)
			}
			catch( UnauthorizedAccessException )
			{
				return false;   // event exists but is not accessible (e.g. integrity-level mismatch)
			}
		}

		/// <summary>True when the hot-reload DLL is currently injected (its event exists).</summary>
		public static bool IsAvailable()
		{
			try
			{
				using( EventWaitHandle.OpenExisting( EventName ) )
				{
					return true;
				}
			}
			catch( WaitHandleCannotBeOpenedException )
			{
				return false;
			}
			catch( UnauthorizedAccessException )
			{
				return false;
			}
		}
	}
}
