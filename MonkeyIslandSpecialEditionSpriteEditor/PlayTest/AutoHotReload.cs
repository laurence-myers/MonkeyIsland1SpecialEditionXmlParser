using MonkeyIslandSpecialEditionSpriteEditor.UI;

namespace MonkeyIslandSpecialEditionSpriteEditor.PlayTest
{
	/// <summary>
	/// Completes the hands-off play-test loop: when "Auto-write overrides on edit" is on, every
	/// override that lands on disk automatically also fires the in-game hot reload, so the modder
	/// needs neither F5 in the editor nor F11 in the game. A graceful no-op when the option is off
	/// or the hot-reload DLL is not injected (the game is not running or not yet armed).
	/// </summary>
	public static class AutoHotReload
	{
		/// <summary>
		/// Signals the running game to reload if auto-write is on. Returns true when the reload was
		/// actually signalled, so callers can say so in the status bar.
		/// </summary>
		public static bool SignalIfEnabled()
		{
			return UserSettings.Instance.AutoWriteOverrides && HotReloadClient.TrySignal();
		}
	}
}
