using System.Collections.Generic;

namespace MonkeyIslandSpecialEditionSpriteEditor.Formats.Scumm
{
	/// <summary>
	/// The variables the SCUMM version 5 engine keeps for itself, with their numbers and names.
	/// The list comes from ScummVM (ScummEngine::setupScummVars plus ScummEngine_v5, for a
	/// version 5 game).
	///
	/// The difference matters to an evaluator. The engine clears the whole variable table when a
	/// new game starts, so a plain game variable that no script has written still holds 0. But
	/// the engine writes the variables in this list itself - the timers, the mouse position, the
	/// room number, the machine speed - so their value cannot be assumed.
	/// </summary>
	internal static class ScummEngineVariables
	{
		private static readonly Dictionary<int, string> NamesByNumber = new Dictionary<int, string>
		{
			{ 0, "VAR_KEYPRESS" },
			{ 1, "VAR_EGO" },
			{ 2, "VAR_CAMERA_POS_X" },
			{ 3, "VAR_HAVE_MSG" },
			{ 4, "VAR_ROOM" },
			{ 5, "VAR_OVERRIDE" },
			{ 6, "VAR_MACHINE_SPEED" },
			{ 7, "VAR_ME" },
			{ 8, "VAR_NUM_ACTOR" },
			{ 9, "VAR_CURRENT_LIGHTS" },
			{ 10, "VAR_CURRENTDRIVE" },
			{ 11, "VAR_TMR_1" },
			{ 12, "VAR_TMR_2" },
			{ 13, "VAR_TMR_3" },
			{ 14, "VAR_MUSIC_TIMER" },
			{ 15, "VAR_ACTOR_RANGE_MIN" },
			{ 16, "VAR_ACTOR_RANGE_MAX" },
			{ 17, "VAR_CAMERA_MIN_X" },
			{ 18, "VAR_CAMERA_MAX_X" },
			{ 19, "VAR_TIMER_NEXT" },
			{ 20, "VAR_VIRT_MOUSE_X" },
			{ 21, "VAR_VIRT_MOUSE_Y" },
			{ 22, "VAR_ROOM_RESOURCE" },
			{ 23, "VAR_LAST_SOUND" },
			{ 24, "VAR_CUTSCENEEXIT_KEY" },
			{ 25, "VAR_TALK_ACTOR" },
			{ 26, "VAR_CAMERA_FAST_X" },
			{ 27, "VAR_SCROLL_SCRIPT" },
			{ 28, "VAR_ENTRY_SCRIPT" },
			{ 29, "VAR_ENTRY_SCRIPT2" },
			{ 30, "VAR_EXIT_SCRIPT" },
			{ 31, "VAR_EXIT_SCRIPT2" },
			{ 32, "VAR_VERB_SCRIPT" },
			{ 33, "VAR_SENTENCE_SCRIPT" },
			{ 34, "VAR_INVENTORY_SCRIPT" },
			{ 35, "VAR_CUTSCENE_START_SCRIPT" },
			{ 36, "VAR_CUTSCENE_END_SCRIPT" },
			{ 37, "VAR_CHARINC" },
			{ 38, "VAR_WALKTO_OBJ" },
			{ 39, "VAR_DEBUGMODE" },
			{ 40, "VAR_HEAPSPACE" },
			{ 42, "VAR_RESTART_KEY" },
			{ 43, "VAR_PAUSE_KEY" },
			{ 44, "VAR_MOUSE_X" },
			{ 45, "VAR_MOUSE_Y" },
			{ 46, "VAR_TIMER" },
			{ 47, "VAR_TIMER_TOTAL" },
			{ 48, "VAR_SOUNDCARD" },
			{ 49, "VAR_VIDEOMODE" },
			{ 50, "VAR_MAINMENU_KEY" },
			{ 51, "VAR_FIXEDDISK" },
			{ 52, "VAR_CURSORSTATE" },
			{ 53, "VAR_USERPUT" },
			{ 54, "VAR_V5_TALK_STRING_Y" },
			{ 56, "VAR_SOUNDRESULT" },
			{ 57, "VAR_TALKSTOP_KEY" },
			{ 59, "VAR_FADE_DELAY" },
			{ 60, "VAR_NOSUBTITLES" },
			{ 64, "VAR_SOUNDPARAM" },
			{ 65, "VAR_SOUNDPARAM2" },
			{ 66, "VAR_SOUNDPARAM3" },
			{ 67, "VAR_INPUTMODE" },
			{ 68, "VAR_MEMORY_PERFORMANCE" },
			{ 69, "VAR_VIDEO_PERFORMANCE" },
			{ 70, "VAR_ROOM_FLAG" },
			{ 71, "VAR_GAME_LOADED" },
			{ 72, "VAR_NEW_ROOM" },
		};

		/// <summary>
		/// Whether the engine writes this variable itself. Such a variable has no known value at
		/// the start of a game, even when no script has written it.
		/// </summary>
		public static bool IsEngineVariable( int variableNumber )
		{
			return NamesByNumber.ContainsKey( variableNumber );
		}

		/// <summary>
		/// The engine's name for a variable, or null when the game alone uses it.
		/// </summary>
		public static string? GetName( int variableNumber )
		{
			string name;
			return NamesByNumber.TryGetValue( variableNumber, out name ) ? name : null;
		}
	}
}
