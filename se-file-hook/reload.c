/*
 * mise-hotreload.dll — hot-reload of edited room art in the running MI1 Special Edition.
 *
 * v3 — startScene bounce. (v1 forced the HD scene rebuild -> showed cached art. v2 flipped the
 * published room -> desynced the HD from the classic engine, drove the renderer to an unloaded room,
 * and crashed. The live logs proved the only path that actually re-reads the .dxt from disk is a
 * REAL room change: leave the room and come back. So we reproduce exactly that, programmatically.)
 *
 * Mechanism (reverse-engineered from the DRM-free GOG build, byte-identical to Steam; base
 * 0x400000, no ASLR): the HD "producer" (Async Resources Fiber Manager) re-reads a room's textures
 * from disk only when that room leaves and re-enters its working set, which is driven by the classic
 * room changing. The classic room change is startScene(room,actor,obj) @0x4951b0. We run a real
 * bounce on the SCUMM interpreter's own thread (so nothing races): startScene(scratch) to leave (this
 * fully loads the scratch room, so no desync/crash and it evicts the edited room), then
 * startScene(home) to return (re-streams the edited room -> re-reads the loose .dxt from disk).
 *
 * To run on the SCUMM thread we hook the scummLoop back-edge at 0x48ff60 (reached every interpreter
 * frame) with a register-preserving stub, and drive a small state machine there. Only startScene is
 * called; the scratch room is a real, loadable neighbour (33/dock for the SCUMM Bar), so the engine
 * stays consistent. Cost: a brief flash of the scratch room (like walking out and back in, but
 * automatic and instant). Trigger: a named auto-reset event the editor sets, or F11 for testing.
 * Guarded by base + two code signatures; it refuses to patch anything but this exact build.
 */

#include <windows.h>
#include <string.h>

#define IMG_BASE      0x00400000u
#define HOOK_SITE     0x0048ff60u   /* scummLoop back-edge (SCUMM thread, per frame) */
#define HOOK_RESUME   0x0048ff67u   /* first byte after the 7 stolen bytes */
#define STARTSCENE    0x004951b0u   /* void __cdecl startScene(int room, int actor, int obj) */
#define CURROOM       0x005c21e0u   /* _currentRoom (byte) */
#define HDOBJ_PTR     0x005b988cu   /* -> HD engine object; [HDobj+0x980] = HD's current room byte */
#define HD_LASTROOM   0x980u
#define EVENT_NAME    "Local\\MISE_HotReload"
#define HOTKEY        VK_F11
#define STOLEN        7
#define SCRATCH_ROOM  33            /* dock — the SCUMM Bar's (room 28) verified neighbour */
#define ALT_SCRATCH   28
#define HD_SETTLE_MS  180           /* after the HD reaches the scratch room, let the fiber stream */
#define MAX_WAIT_MS   1500

static const unsigned char SIG_LOOP[]  = { 0x66,0x8b,0x15,0x24,0x46,0x5c,0x00 };
static const unsigned char SIG_SCENE[] = { 0x55,0x8b,0xec,0x83,0xe4,0xf8 };

typedef void ( __cdecl *startScene_t )( int, int, int );
#define startScene ( (startScene_t)STARTSCENE )

static HINSTANCE g_self;
static HANDLE    g_event = NULL;
static HANDLE    g_log   = INVALID_HANDLE_VALUE;
static CRITICAL_SECTION g_loglock;
static volatile LONG g_seq = 0;

/* bounce state machine (touched only from the SCUMM thread via handle_frame) */
static int   g_state  = 0;     /* 0 = idle, 1 = left, waiting to return */
static int   g_home   = 0;
static int   g_scratch = 0;
static DWORD g_leave_tick = 0;

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

static int reload_requested( void )
{
	if( g_event != NULL && WaitForSingleObject( g_event, 0 ) == WAIT_OBJECT_0 )
	{
		return 1;
	}
	if( GetAsyncKeyState( HOTKEY ) & 1 )   /* low bit = pressed since the previous poll */
	{
		return 1;
	}
	return 0;
}

/* Runs on the SCUMM thread once per interpreter frame (from the hook stub). Drives the bounce so
   every startScene call happens on the engine's own thread, at a frame boundary. */
static void __cdecl handle_frame( void )
{
	if( g_state == 0 )
	{
		if( reload_requested() )
		{
			g_home    = *(unsigned char *)CURROOM;
			g_scratch = ( g_home != SCRATCH_ROOM ) ? SCRATCH_ROOM : ALT_SCRATCH;
			startScene( g_scratch, 0, 0 );      /* leave: loads scratch, evicts `home` from the HD set */
			g_leave_tick = GetTickCount();
			g_state = 1;
			logline( "bounce #%d: room %d -> %d (leaving to force evict)\r\n",
			         (int)InterlockedIncrement( &g_seq ), g_home, g_scratch );
		}
	}
	else
	{
		unsigned hd = *(unsigned *)HDOBJ_PTR;
		int hd_on_scratch = ( hd != 0 ) && ( *(unsigned char *)( hd + HD_LASTROOM ) == (unsigned char)g_scratch );
		DWORD elapsed = GetTickCount() - g_leave_tick;
		if( ( hd_on_scratch && elapsed >= HD_SETTLE_MS ) || elapsed >= MAX_WAIT_MS )
		{
			startScene( g_home, 0, 0 );         /* return: re-streams `home` -> re-reads the edited .dxt */
			g_state = 0;
			logline( "bounce: returned to %d (%ums, hd_reached_scratch=%d)\r\n", g_home, (unsigned)elapsed, hd_on_scratch );
		}
	}
}

static int install_hook( void )
{
	if( (unsigned)GetModuleHandleA( NULL ) != IMG_BASE )
	{
		logline( "ABORT: host base is %p, not 0x%x — not the expected MISE.exe\r\n", GetModuleHandleA( NULL ), IMG_BASE );
		return 0;
	}
	if( memcmp( (void *)HOOK_SITE, SIG_LOOP, sizeof( SIG_LOOP ) ) != 0 )
	{
		logline( "ABORT: scummLoop signature mismatch at 0x%x — wrong build; not patching\r\n", HOOK_SITE );
		return 0;
	}
	if( memcmp( (void *)STARTSCENE, SIG_SCENE, sizeof( SIG_SCENE ) ) != 0 )
	{
		logline( "ABORT: startScene signature mismatch at 0x%x — wrong build; not patching\r\n", STARTSCENE );
		return 0;
	}

	/* one exec page holds the stub (preserves all regs around handle_frame) and the trampoline
	   (the stolen loop instruction + a jump back into the loop). */
	unsigned char *mem = (unsigned char *)VirtualAlloc( NULL, 64, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE );
	if( mem == NULL )
	{
		logline( "ABORT: VirtualAlloc failed (%lu)\r\n", GetLastError() );
		return 0;
	}
	unsigned char *stub  = mem;
	unsigned char *tramp = mem + 32;

	int i = 0;
	stub[i++] = 0x60;                                                             /* pushad */
	stub[i++] = 0x9C;                                                             /* pushfd */
	stub[i++] = 0xE8; *(DWORD *)( stub + i ) = (DWORD)&handle_frame - (DWORD)( stub + i + 4 ); i += 4;  /* call handle_frame */
	stub[i++] = 0x9D;                                                             /* popfd */
	stub[i++] = 0x61;                                                             /* popad */
	stub[i++] = 0xE9; *(DWORD *)( stub + i ) = (DWORD)tramp - (DWORD)( stub + i + 4 ); i += 4;          /* jmp tramp */

	memcpy( tramp, (void *)HOOK_SITE, STOLEN );                                   /* stolen loop instruction */
	tramp[STOLEN] = 0xE9; *(DWORD *)( tramp + STOLEN + 1 ) = HOOK_RESUME - ( (DWORD)( tramp + STOLEN ) + 5 );  /* jmp back */
	FlushInstructionCache( GetCurrentProcess(), mem, 64 );

	DWORD old;
	VirtualProtect( (void *)HOOK_SITE, STOLEN, PAGE_EXECUTE_READWRITE, &old );
	unsigned char *p = (unsigned char *)HOOK_SITE;
	p[0] = 0xE9; *(DWORD *)( p + 1 ) = (DWORD)stub - ( HOOK_SITE + 5 );           /* jmp stub */
	p[5] = 0x90; p[6] = 0x90;                                                     /* pad the 7 stolen bytes */
	VirtualProtect( (void *)HOOK_SITE, STOLEN, old, &old );
	FlushInstructionCache( GetCurrentProcess(), p, STOLEN );

	logline( "hook installed at scummLoop 0x%x (stub %p, tramp %p). Trigger: SetEvent(%s) or F11.\r\n",
	         HOOK_SITE, stub, tramp, EVENT_NAME );
	return 1;
}

static DWORD WINAPI worker( LPVOID unused )
{
	(void)unused;
	InitializeCriticalSection( &g_loglock );
	open_log();
	SYSTEMTIME st;
	GetLocalTime( &st );
	logline( "# mise-hotreload v3 (startScene bounce) — %04d-%02d-%02d %02d:%02d:%02d\r\n",
	         st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond );
	g_event = CreateEventA( NULL, FALSE /*auto-reset*/, FALSE, EVENT_NAME );
	logline( "reload event '%s': %s\r\n", EVENT_NAME, g_event ? "ready" : "FAILED" );
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
	/* No unhook on detach: we only unload at process exit; leaving the patch avoids racing the
	   SCUMM thread mid-call. */
	return TRUE;
}
