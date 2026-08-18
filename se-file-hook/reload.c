/*
 * mise-hotreload.dll — hot-reload of edited room art in the running MI1 Special Edition.
 *
 * When the modder edits a room's loose overrides (art\rooms\*.room.xml, *.dxt, art\costumes\*) the
 * running game only re-reads them on room entry. This DLL makes an edit apply IMMEDIATELY without
 * walking the character out and back in.
 *
 * How (reverse-engineered from the DRM-free GOG build, which is byte-identical to the Steam build;
 * image base 0x400000, no ASLR, so these addresses are valid in the running Steam game too):
 *   - The HD renderer streams a room's art only when the room NUMBER changes. Each frame, the render
 *     thread calls the "HD room-change detector" at 0x44e5c0(HDobj): it compares the current room to
 *     HDobj's stored last-room byte at [HDobj+0x980]; if they differ it re-streams the room's art
 *     (room.xml + DXT atlases) from disk — re-reading the loose overrides.
 *   - So to force a reload of the CURRENT room we make that byte differ: write 0xFF into
 *     [HDobj+0x980]. On the detector's next tick (its own thread) it sees a delta and re-streams the
 *     current room. No actor reset, no dummy-room bounce; it runs on the render thread's own path.
 *
 * We inline-hook 0x44e5c0 (HDobj arrives as its single stdcall arg) and, when a reload is requested,
 * poke that byte before calling the original. Requests come from a named auto-reset event the editor
 * sets, or from the F11 hotkey for manual testing.
 *
 * This is a modding aid for the user's own game: it reads/writes only the running game's own memory
 * and forces its own asset-reload code path. Guarded by a 13-byte build signature so it refuses to
 * patch anything but this exact build.
 */

#include <windows.h>

/* --- build-specific addresses (GOG == Steam build; base 0x400000, no ASLR) --- */
#define IMG_BASE       0x00400000u
#define HD_DETECT      0x0044e5c0u   /* HD room-change detector: int __stdcall f(void* HDobj) */
#define OFF_LASTROOM   0x980u        /* [HDobj+0x980] = last streamed room number (byte) */
#define ADDR_CURROOM   0x005c21e0u   /* _currentRoom (byte) — for logging only */
#define EVENT_NAME     "Local\\MISE_HotReload"
#define HOTKEY         VK_F11

/* first 13 bytes of 0x44e5c0: push ebp; mov ebp,esp; and esp,-8; sub esp,0xc; push ebx; mov ebx,[ebp+8] */
static const unsigned char SIG[] = { 0x55,0x8b,0xec,0x83,0xe4,0xf8,0x83,0xec,0x0c,0x53,0x8b,0x5d,0x08 };
#define STOLEN 6   /* bytes overwritten by the jmp patch (whole instructions: 55 / 8bec / 83e4f8) */

typedef int (__stdcall *hd_fn)( void * );

static HINSTANCE g_self;
static HANDLE    g_event = NULL;
static HANDLE    g_log   = INVALID_HANDLE_VALUE;
static hd_fn     g_real  = NULL;       /* trampoline -> original detector */
static volatile LONG g_reloads = 0;
static CRITICAL_SECTION g_loglock;

/* --- logging (via our own imports; independent of the game) --- */
static void logline( const char *fmt, ... )
{
	if( g_log == INVALID_HANDLE_VALUE )
	{
		return;
	}
	char buf[512];
	va_list ap;
	va_start( ap, fmt );
	int n = wvsprintfA( buf, fmt, ap );
	va_end( ap );
	DWORD w;
	EnterCriticalSection( &g_loglock );
	WriteFile( g_log, buf, (DWORD)n, &w, NULL );
	LeaveCriticalSection( &g_loglock );
}

static void open_log( void )
{
	char path[MAX_PATH];
	DWORD n = GetModuleFileNameA( g_self, path, MAX_PATH );
	while( n > 0 && path[n - 1] != '\\' && path[n - 1] != '/' )
	{
		--n;
	}
	lstrcpynA( path + n, "mise-hotreload.log", (int)( MAX_PATH - n ) );
	g_log = CreateFileA( path, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL );
}

/* true when a reload was asked for this frame: editor SetEvent, or the F11 hotkey edge */
static int reload_requested( void )
{
	int t = 0;
	if( g_event != NULL && WaitForSingleObject( g_event, 0 ) == WAIT_OBJECT_0 )
	{
		t = 1;
	}
	if( GetAsyncKeyState( HOTKEY ) & 1 )   /* low bit = pressed since the previous call */
	{
		t = 1;
	}
	return t;
}

/* the hook: runs on the render thread each frame in place of 0x44e5c0 */
static int __stdcall hook_detect( void *hdobj ) __attribute__((noinline));
static int __stdcall hook_detect( void *hdobj )
{
	if( hdobj != NULL && reload_requested() )
	{
		unsigned char room = *(unsigned char *)ADDR_CURROOM;
		*( (unsigned char *)hdobj + OFF_LASTROOM ) = 0xFF;   /* force a room-number delta -> re-stream */
		LONG n = InterlockedIncrement( &g_reloads );
		logline( "reload #%d: forced HD re-stream of room %u (HDobj=%p)\r\n", (int)n, room, hdobj );
	}
	return g_real( hdobj );
}

static int install_hook( void )
{
	unsigned char *t = (unsigned char *)HD_DETECT;

	if( (unsigned)GetModuleHandleA( NULL ) != IMG_BASE )
	{
		logline( "ABORT: host base is %p, not 0x%x — not the expected MISE.exe\r\n",
		         GetModuleHandleA( NULL ), IMG_BASE );
		return 0;
	}
	for( int i = 0; i < (int)sizeof( SIG ); ++i )
	{
		if( t[i] != SIG[i] )
		{
			logline( "ABORT: signature mismatch at 0x%x+%d (got %02x, want %02x) — wrong game build; not patching\r\n",
			         HD_DETECT, i, t[i], SIG[i] );
			return 0;
		}
	}

	/* trampoline = the STOLEN original bytes + an absolute-ish jmp back to HD_DETECT+STOLEN */
	unsigned char *tr = (unsigned char *)VirtualAlloc( NULL, 32, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE );
	if( tr == NULL )
	{
		logline( "ABORT: trampoline VirtualAlloc failed (%lu)\r\n", GetLastError() );
		return 0;
	}
	CopyMemory( tr, t, STOLEN );
	tr[STOLEN] = 0xE9;   /* jmp rel32 -> t + STOLEN */
	*(DWORD *)( tr + STOLEN + 1 ) = (DWORD)( t + STOLEN ) - (DWORD)( tr + STOLEN + 5 );
	FlushInstructionCache( GetCurrentProcess(), tr, 32 );
	g_real = (hd_fn)tr;

	/* patch the target: E9 rel32 -> hook_detect, pad the 6th byte with nop */
	DWORD old;
	if( !VirtualProtect( t, STOLEN, PAGE_EXECUTE_READWRITE, &old ) )
	{
		logline( "ABORT: VirtualProtect failed (%lu)\r\n", GetLastError() );
		return 0;
	}
	t[0] = 0xE9;
	*(DWORD *)( t + 1 ) = (DWORD)&hook_detect - (DWORD)( t + 5 );
	t[5] = 0x90;
	VirtualProtect( t, STOLEN, old, &old );
	FlushInstructionCache( GetCurrentProcess(), t, STOLEN );

	logline( "hook installed at 0x%x (trampoline %p). Trigger: SetEvent(%s) or press F11 in game.\r\n",
	         HD_DETECT, tr, EVENT_NAME );
	return 1;
}

static DWORD WINAPI worker( LPVOID unused )
{
	(void)unused;
	InitializeCriticalSection( &g_loglock );
	open_log();
	SYSTEMTIME st;
	GetLocalTime( &st );
	logline( "# mise-hotreload — %04d-%02d-%02d %02d:%02d:%02d\r\n",
	         st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond );

	g_event = CreateEventA( NULL, FALSE /*auto-reset*/, FALSE, EVENT_NAME );
	logline( "reload event '%s': %s\r\n", EVENT_NAME, g_event ? "ready" : "FAILED to create" );

	install_hook();
	return 0;
}

BOOL WINAPI DllMain( HINSTANCE inst, DWORD reason, LPVOID reserved )
{
	(void)reserved;
	if( reason == DLL_PROCESS_ATTACH )
	{
		g_self = inst;
		DisableThreadLibraryCalls( inst );
		CreateThread( NULL, 0, worker, NULL, 0, NULL );
	}
	/* Deliberately no unhook on detach: we only unload at process exit, and the trampoline/patch are
	   harmless then. Leaving them avoids a race with the render thread mid-call. */
	return TRUE;
}
