/*
 * mise-hotreload.dll — hot-reload of edited room art in the running MI1 Special Edition.
 *
 * v2 — published-room flip. (v1 forced the HD scene-rebuild by poking [HDobj+0x980]; that only
 * re-ran the cache-gated scene *builder* and re-showed the cached texture — it never re-read the
 * .dxt from disk.)
 *
 * What a real room change actually does (reverse-engineered from the DRM-free GOG build, byte-
 * identical to Steam; base 0x400000, no ASLR, so these addresses are valid in the running Steam
 * game too): the HD disk-streaming "producer" (the Async Resources Fiber Manager) manages a working
 * set of loaded resources keyed off the PUBLISHED room. The classic engine publishes _roomResource
 * (byte at 0x5c21e1) into the render snapshot every frame (via 0x4898f0). When the published room
 * changes, the producer evicts the leaving room's resources and streams the entering room's — and
 * the loose-override loaders (0x46bd70 → _access 0x470f90 → CreateFileA 0x4753d0) re-read the .dxt
 * from disk with no residency check. The HD side knows the room ONLY through this published value.
 *
 * So to reload the CURRENT room's edited art we make it briefly leave and re-enter the working set:
 * write a different room into 0x5c21e1, wait a few frames for the producer to evict it and stream
 * the scratch room, then restore the real room so the producer re-streams it — re-reading the edited
 * .dxt. This does NOT touch the classic engine's real room state (_currentRoom 0x5c21e0), so actors
 * and scripts are undisturbed; the only visible cost is a brief flash of the scratch room's art.
 *
 * Trigger: a named auto-reset event the editor sets, or the F11 hotkey for manual testing. Pair with
 * se-file-hook.dll (also injected) to log whether the .dxt actually re-read. Guarded by base + a code
 * signature so it refuses to touch anything but this exact build.
 */

#include <windows.h>

#define IMG_BASE       0x00400000u
#define ADDR_ROOMRES   0x005c21e1u   /* _roomResource (byte) — the PUBLISHED room source */
#define ADDR_CURROOM   0x005c21e0u   /* _currentRoom (byte) — classic engine's real room (untouched) */
#define SIG_ADDR       0x00475488u   /* CreateFileA call site: ff 15 30 a0 4d 00 (build check) */
#define EVENT_NAME     "Local\\MISE_HotReload"
#define HOTKEY         VK_F11
#define LEAVE_MS       260           /* time to let the producer evict + stream the scratch room */
#define SETTLE_MS      60            /* time after restoring before we call it done */

static const unsigned char SIG[] = { 0xff, 0x15, 0x30, 0xa0, 0x4d, 0x00 };

static HINSTANCE g_self;
static HANDLE    g_event = NULL;
static HANDLE    g_log   = INVALID_HANDLE_VALUE;
static volatile LONG g_seq = 0;
static CRITICAL_SECTION g_loglock;

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

/* The reload: bounce the published room so the streaming producer evicts + re-streams the current
   room, re-reading its edited .dxt. Runs on our worker thread; only touches the HD-published byte. */
static void do_reload( void )
{
	unsigned char *roomres = (unsigned char *)ADDR_ROOMRES;
	unsigned char home = *roomres;
	unsigned char cur  = *(unsigned char *)ADDR_CURROOM;
	/* a different, plausibly-valid room to force the working set to change (avoid 0 = VOID, which
	   loads nothing and may not evict); flip to an adjacent number */
	unsigned char scratch = ( home >= 2 ) ? (unsigned char)( home - 1 ) : (unsigned char)( home + 1 );

	LONG s = InterlockedIncrement( &g_seq );
	logline( "reload #%d: publish %u -> %u -> %u (classic room %u untouched)\r\n", (int)s, home, scratch, home, cur );

	*roomres = scratch;   /* room `home` leaves the working set -> producer evicts it */
	Sleep( LEAVE_MS );    /* let several frames publish + the fiber evict/stream */
	*roomres = home;      /* room `home` re-enters -> producer re-streams it -> re-reads the .dxt */
	Sleep( SETTLE_MS );
	logline( "reload #%d: done (published room restored to %u)\r\n", (int)s, home );
}

static int triggered( void )
{
	int t = 0;
	if( g_event != NULL && WaitForSingleObject( g_event, 30 ) == WAIT_OBJECT_0 )
	{
		t = 1;
	}
	else if( GetAsyncKeyState( HOTKEY ) & 1 )   /* low bit = pressed since the previous poll */
	{
		t = 1;
	}
	return t;
}

static DWORD WINAPI worker( LPVOID unused )
{
	(void)unused;
	InitializeCriticalSection( &g_loglock );
	open_log();
	SYSTEMTIME st;
	GetLocalTime( &st );
	logline( "# mise-hotreload v2 (published-room flip) — %04d-%02d-%02d %02d:%02d:%02d\r\n",
	         st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond );

	if( (unsigned)GetModuleHandleA( NULL ) != IMG_BASE )
	{
		logline( "ABORT: host base is %p, not 0x%x — not the expected MISE.exe\r\n", GetModuleHandleA( NULL ), IMG_BASE );
		return 0;
	}
	unsigned char *sig = (unsigned char *)SIG_ADDR;
	for( int i = 0; i < (int)sizeof( SIG ); ++i )
	{
		if( sig[i] != SIG[i] )
		{
			logline( "ABORT: code signature mismatch at 0x%x+%d (got %02x, want %02x) — wrong build\r\n", SIG_ADDR, i, sig[i], SIG[i] );
			return 0;
		}
	}

	g_event = CreateEventA( NULL, FALSE /*auto-reset*/, FALSE, EVENT_NAME );
	logline( "ready. Trigger: SetEvent(%s) or press F11 in game. (Also inject se-file-hook.dll to log .dxt reads.)\r\n", EVENT_NAME );

	for( ;; )
	{
		if( triggered() )
		{
			do_reload();
		}
	}
	/* not reached */
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
	return TRUE;
}
