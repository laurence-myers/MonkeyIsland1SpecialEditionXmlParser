/*
 * mise-hdreload.dll — HD-only hot-reload for MI1 Special Edition (no interpreter disruption).
 *
 * The startScene bounce (mise-hotreload v3) reloads edited art but re-runs the room's entry scripts
 * (a real room change). This tool does what the user asked for instead: reload ONLY the HD assets
 * (.room.xml + .dxt textures) in place, leaving the SCUMM script interpreter untouched.
 *
 * Mechanism (reverse-engineered from the DRM-free GOG build, byte-identical to Steam; base 0x400000,
 * no ASLR). The HD renderer rebuilds each frame in orchestrator 0x44c7be:
 *   - It rebuilds the scene from a cached "node view" at HDobj+0x984 and only RE-STREAMS the room's
 *     resources from disk (0x43f320) when that node view is empty or stamped for a different room
 *     (cache check at 0x44c846..0x44c858). Otherwise it rebuilds geometry from the CACHED resources
 *     (which is why forcing just the rebuild — v1 — showed stale textures).
 *   - Each resource read goes through resource_get 0x48c760, which returns the CACHED copy unless the
 *     handle's loaded-flag [handle+0x4] is zero; zero forces a fresh read from disk (CreateFileA).
 *
 * So an in-place HD reload is two pokes, done on the interpreter thread at a frame boundary:
 *   (1) EVICT: for the current room's resource handles, set [handle+0x4] = 0 so resource_get re-reads
 *       the edited files from disk.
 *   (2) FORCE RE-STREAM: set the node-view slot HDobj+0x984 = 0 so the orchestrator takes its miss
 *       path next frame — it re-streams the resources for the SAME (already-published, valid) room
 *       (no room change, so no v2-style desync/crash) and rebuilds the scene from the fresh data.
 * The script interpreter is never called; startScene is never called.
 *
 * We capture the resource handles live by hooking resource_get 0x48c760 (handle = arg1 = [esp+8] at
 * entry; its name is an engine string at handle+0x10 whose char* is at +0x8, i.e. name =
 * *(char**)(handle+0x18); loaded-flag at handle+0x4). We act on the interpreter thread by hooking the
 * scummLoop back-edge 0x48ff60 (as v3 does) and, when triggered, run the reload there. The CreateFileA
 * IAT (0x4da030) is hooked only to LOG the resulting disk re-reads, so we can confirm the reload.
 *
 * Trigger: the editor's existing event Local\MISE_HotReload (the "Test in game" button), or F11.
 * Guarded by image base + two code signatures; it refuses to patch any other build. This first cut
 * also DUMPS the node-view layout on each reload so we can verify the runtime structure and, if the
 * empty-force needs refining (e.g. stamp the front node instead of nulling the slot), do it precisely.
 */

#include <windows.h>
#include <string.h>

#define IMG_BASE      0x00400000u

/* code we hook */
#define RESGET_SITE   0x0048c760u   /* resource_get(?, handle, ...); handle = arg1 */
#define RESGET_RESUME 0x0048c769u   /* after the 9 stolen bytes */
#define RESGET_STOLEN 9
#define LOOP_SITE     0x0048ff60u   /* scummLoop back-edge (interpreter thread, per frame) */
#define LOOP_RESUME   0x0048ff67u
#define LOOP_STOLEN   7
#define IAT_CFA       0x004da030u   /* game IAT slot for kernel32!CreateFileA */

/* data we read/poke */
#define HDOBJ_PTR     0x005b988cu   /* -> HD engine object */
#define NODEVIEW_OFF  0x984u        /* HDobj+0x984 = node-view container slot (source for the rebuild) */
#define ROOM_SEL      0x005b9944u   /* if [0x5b9944]!=0 use [0x5b9948] else [0x5b994c] as the room object */
#define ROOM_OBJ_A    0x005b9948u
#define ROOM_OBJ_B    0x005b994cu
#define ROOM_NUM_OFF  0x1cu         /* roomObj+0x1c = published HD room byte */

/* handle layout (from resource_get / 0x46e500) */
#define H_FLAG_OFF    0x04u         /* loaded-flag; 0 => resource_get re-reads from disk */
#define H_STR_OFF     0x10u         /* engine string object */
#define H_STRPTR_OFF  0x18u         /* char* is at string+0x8 => handle+0x18 */

#define EVENT_NAME    "Local\\MISE_HotReload"
#define HOTKEY        VK_F11

static const unsigned char SIG_LOOP[]   = { 0x66,0x8b,0x15,0x24,0x46,0x5c,0x00 };
static const unsigned char SIG_RESGET[] = { 0x55,0x8b,0xec,0x81,0xec,0x70,0x01,0x00,0x00 };

static HINSTANCE g_self;
static HANDLE    g_event = NULL;
static HANDLE    g_log   = INVALID_HANDLE_VALUE;
static CRITICAL_SECTION g_loglock;
static CRITICAL_SECTION g_tablock;
static volatile LONG g_seq = 0;
static volatile LONG g_reads_since_reload = -1;   /* >=0 while we are counting post-reload re-reads */

/* --- captured resource handles (name -> handle), filled by the resource_get hook --- */
typedef struct
{
	void        *h;
	unsigned     last4;
	char         name[192];
} HEntry;

#define TAB_MAX 2048
static HEntry g_tab[TAB_MAX];
static volatile LONG g_ntab = 0;

static void logline( const char *fmt, ... )
{
	if( g_log == INVALID_HANDLE_VALUE )
	{
		return;
	}
	char buf[600];
	va_list ap;
	va_start( ap, fmt );
	int n = wvsprintfA( buf, fmt, ap );
	va_end( ap );
	DWORD w;
	EnterCriticalSection( &g_loglock );
	WriteFile( g_log, buf, (DWORD)n, &w, NULL );
	LeaveCriticalSection( &g_loglock );
}

static int readable( const void *p, unsigned len )
{
	return p != NULL && !IsBadReadPtr( p, len );
}

static int starts_with( const char *s, const char *pre )
{
	while( *pre )
	{
		if( *s++ != *pre++ )
		{
			return 0;
		}
	}
	return 1;
}

/* An asset the ROOM re-stream (emptying the node view) will actually reload. Restricted to the
   current room's own art/rooms/ resources: evicting anything else (costumes, UI) orphans live
   consumers — e.g. a live actor's costume — because nothing re-streams them, so the flag stays 0 and
   the costume reads as "unloaded" (actor vanishes, then a crash on use). `needle` = "/<room>_" scopes
   to the current room's folder so other rooms' cached art is left alone. Costume/room-metadata reload
   are handled by their own paths, not this raw handle evict. */
static int is_current_room_asset( const char *name, int room, const char *needle )
{
	if( !starts_with( name, "art/rooms/" ) )
	{
		return 0;
	}
	if( room >= 0 && strstr( name, needle ) == NULL )
	{
		return 0;
	}
	return 1;
}

/* --- resource_get hook: record each handle's (name, handle, loaded-flag). Called a lot, so the hot
   path is just a dedup scan + one read; the pointer-validation + name copy runs only the FIRST time we
   see a handle (handle is resource_get's arg1, so it and [handle+0x4] are safe to read). */
static void __cdecl record_handle( void *handle )
{
	if( handle == NULL )
	{
		return;
	}
	EnterCriticalSection( &g_tablock );
	int n = (int)g_ntab;
	for( int i = 0; i < n; ++i )
	{
		if( g_tab[i].h == handle )
		{
			g_tab[i].last4 = *(unsigned *)( (char *)handle + H_FLAG_OFF );
			LeaveCriticalSection( &g_tablock );
			return;
		}
	}
	if( n < TAB_MAX && readable( handle, 0x20 ) )
	{
		char *name = *(char **)( (char *)handle + H_STRPTR_OFF );
		if( readable( name, 1 ) )
		{
			g_tab[n].h = handle;
			g_tab[n].last4 = *(unsigned *)( (char *)handle + H_FLAG_OFF );
			lstrcpynA( g_tab[n].name, name, sizeof( g_tab[n].name ) );
			g_ntab = n + 1;
		}
	}
	LeaveCriticalSection( &g_tablock );
}

/* --- the actual HD-only reload, run on the interpreter thread from the scummLoop hook --- */
static void do_reload( void )
{
	unsigned hd = *(unsigned *)HDOBJ_PTR;
	if( hd == 0 )
	{
		logline( "reload skipped: HD object null (not in the HD renderer)\r\n" );
		return;
	}

	/* published HD room (for logging + a sanity gate) */
	unsigned roomObj = ( *(unsigned *)ROOM_SEL != 0 ) ? *(unsigned *)ROOM_OBJ_A : *(unsigned *)ROOM_OBJ_B;
	int room = ( roomObj != 0 && readable( (void *)( roomObj + ROOM_NUM_OFF ), 1 ) )
	           ? *(unsigned char *)( roomObj + ROOM_NUM_OFF ) : -1;

	unsigned nv_slot = hd + NODEVIEW_OFF;
	unsigned nv = *(unsigned *)nv_slot;

	int seq = (int)InterlockedIncrement( &g_seq );
	logline( "\r\n=== HD reload #%d  (room=%d) ===\r\n", seq, room );
	logline( "HDobj=%08x  nodeView[+984]=%08x  roomObj=%08x\r\n", hd, nv, roomObj );

	/* diagnostic: dump the node-view container head (to verify layout / refine the force if needed) */
	if( readable( (void *)nv, 0x24 ) )
	{
		unsigned *c = (unsigned *)nv;
		logline( "  nodeView[0..0x20]: %08x %08x %08x %08x %08x %08x %08x %08x\r\n",
		         c[0], c[1], c[2], c[3], c[4], c[5], c[6], c[7] );
	}

	/* (1) EVICT the current room's own art/rooms/ assets: zero the loaded-flag so resource_get
	   re-reads from disk. Scoped to the current room so we never orphan a live actor's costume. */
	char needle[24];
	wsprintfA( needle, "/%d_", room );
	int evicted = 0, listed = 0;
	EnterCriticalSection( &g_tablock );
	int n = (int)g_ntab;
	for( int i = 0; i < n; ++i )
	{
		if( !is_current_room_asset( g_tab[i].name, room, needle ) )
		{
			continue;
		}
		if( readable( g_tab[i].h, 0x08 ) )
		{
			*(unsigned *)( (char *)g_tab[i].h + H_FLAG_OFF ) = 0;   /* evict */
			++evicted;
		}
		if( listed < 40 )
		{
			logline( "  evict %08x  flag->0  %s\r\n", (unsigned)g_tab[i].h, g_tab[i].name );
			++listed;
		}
	}
	LeaveCriticalSection( &g_tablock );
	logline( "  evicted %d of %d captured handles (room-scoped '%s'; costumes/UI left intact)\r\n",
	         evicted, n, needle );

	/* (2) FORCE RE-STREAM: empty the node view so the orchestrator takes its miss path (0x44c85e),
	   re-streaming the SAME published room's resources from disk and rebuilding the scene. No room
	   change, so no interpreter/desync. The orchestrator (0x43f1c0) rebuilds this slot itself. */
	if( nv != 0 )
	{
		*(unsigned *)nv_slot = 0;
		logline( "  node view emptied (HDobj+0x984: %08x -> 0) — orchestrator will re-stream + rebuild\r\n", nv );
	}
	else
	{
		logline( "  node view already empty; rebuild will re-stream\r\n" );
	}

	g_reads_since_reload = 0;   /* start counting disk re-reads the CreateFileA hook sees */
	logline( "  reload issued; watch for .dxt/.xml re-reads below.\r\n" );
}

/* --- scummLoop hook body: poll the trigger on the interpreter thread and reload there --- */
static int reload_requested( void )
{
	if( g_event != NULL && WaitForSingleObject( g_event, 0 ) == WAIT_OBJECT_0 )
	{
		return 1;
	}
	if( GetAsyncKeyState( HOTKEY ) & 1 )
	{
		return 1;
	}
	return 0;
}

static void __cdecl handle_frame( void )
{
	if( reload_requested() )
	{
		do_reload();
	}
}

/* --- CreateFileA IAT hook: log the disk re-reads triggered by the reload --- */
typedef HANDLE ( WINAPI *CreateFileA_t )( LPCSTR, DWORD, DWORD, LPSECURITY_ATTRIBUTES, DWORD, DWORD, HANDLE );
static CreateFileA_t g_realCFA;

static HANDLE WINAPI hook_CFA( LPCSTR name, DWORD access, DWORD share, LPSECURITY_ATTRIBUTES sa, DWORD disp, DWORD flags, HANDLE tmpl )
	__attribute__((noinline));
static HANDLE WINAPI hook_CFA( LPCSTR name, DWORD access, DWORD share, LPSECURITY_ATTRIBUTES sa, DWORD disp, DWORD flags, HANDLE tmpl )
{
	if( name != NULL && g_reads_since_reload >= 0 )
	{
		int len = lstrlenA( name );
		if( ( len > 4 && lstrcmpiA( name + len - 4, ".dxt" ) == 0 ) ||
		    ( len > 4 && lstrcmpiA( name + len - 4, ".xml" ) == 0 ) )
		{
			logline( "  re-read[%d] %s\r\n", (int)InterlockedIncrement( &g_reads_since_reload ), name );
		}
	}
	return g_realCFA( name, access, share, sa, disp, flags, tmpl );
}

static void patch_ptr( void **slot, void *val, void **saveOrig )
{
	DWORD old;
	VirtualProtect( slot, sizeof( void * ), PAGE_READWRITE, &old );
	if( *saveOrig == NULL )
	{
		*saveOrig = *slot;
	}
	*slot = val;
	VirtualProtect( slot, sizeof( void * ), old, &old );
}

/* place a 5-byte jmp at `site` to `dest`, filling the remaining stolen bytes with nops */
static void place_jmp( unsigned site, void *dest, int stolen )
{
	DWORD old;
	VirtualProtect( (void *)site, stolen, PAGE_EXECUTE_READWRITE, &old );
	unsigned char *p = (unsigned char *)site;
	p[0] = 0xE9;
	*(DWORD *)( p + 1 ) = (DWORD)dest - ( site + 5 );
	for( int i = 5; i < stolen; ++i )
	{
		p[i] = 0x90;
	}
	VirtualProtect( (void *)site, stolen, old, &old );
	FlushInstructionCache( GetCurrentProcess(), p, stolen );
}

static int install_hooks( void )
{
	if( (unsigned)GetModuleHandleA( NULL ) != IMG_BASE )
	{
		logline( "ABORT: host base %p != 0x%x\r\n", GetModuleHandleA( NULL ), IMG_BASE );
		return 0;
	}
	if( memcmp( (void *)LOOP_SITE, SIG_LOOP, sizeof( SIG_LOOP ) ) != 0 )
	{
		logline( "ABORT: scummLoop signature mismatch at 0x%x — wrong build\r\n", LOOP_SITE );
		return 0;
	}
	if( memcmp( (void *)RESGET_SITE, SIG_RESGET, sizeof( SIG_RESGET ) ) != 0 )
	{
		logline( "ABORT: resource_get signature mismatch at 0x%x — wrong build\r\n", RESGET_SITE );
		return 0;
	}

	unsigned char *mem = (unsigned char *)VirtualAlloc( NULL, 256, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE );
	if( mem == NULL )
	{
		logline( "ABORT: VirtualAlloc failed (%lu)\r\n", GetLastError() );
		return 0;
	}
	unsigned char *rg_stub  = mem + 0;
	unsigned char *rg_tramp = mem + 64;
	unsigned char *lp_stub  = mem + 112;
	unsigned char *lp_tramp = mem + 176;

	/* resource_get stub: capture handle (= arg1 = [esp+8] at entry) then run the stolen prologue. */
	{
		int i = 0;
		rg_stub[i++] = 0x60;                                                            /* pushad */
		rg_stub[i++] = 0x9C;                                                            /* pushfd */
		rg_stub[i++] = 0xFF; rg_stub[i++] = 0x74; rg_stub[i++] = 0x24; rg_stub[i++] = 0x2C; /* push [esp+0x2C] (handle) */
		rg_stub[i++] = 0xE8; *(DWORD *)( rg_stub + i ) = (DWORD)&record_handle - (DWORD)( rg_stub + i + 4 ); i += 4; /* call */
		rg_stub[i++] = 0x83; rg_stub[i++] = 0xC4; rg_stub[i++] = 0x04;                  /* add esp,4 */
		rg_stub[i++] = 0x9D;                                                            /* popfd */
		rg_stub[i++] = 0x61;                                                            /* popad */
		rg_stub[i++] = 0xE9; *(DWORD *)( rg_stub + i ) = (DWORD)rg_tramp - (DWORD)( rg_stub + i + 4 ); i += 4; /* jmp tramp */

		memcpy( rg_tramp, (void *)RESGET_SITE, RESGET_STOLEN );
		rg_tramp[RESGET_STOLEN] = 0xE9;
		*(DWORD *)( rg_tramp + RESGET_STOLEN + 1 ) = RESGET_RESUME - ( (DWORD)( rg_tramp + RESGET_STOLEN ) + 5 );
	}

	/* scummLoop stub: run handle_frame every interpreter frame, preserving all regs/flags. */
	{
		int i = 0;
		lp_stub[i++] = 0x60;                                                            /* pushad */
		lp_stub[i++] = 0x9C;                                                            /* pushfd */
		lp_stub[i++] = 0xE8; *(DWORD *)( lp_stub + i ) = (DWORD)&handle_frame - (DWORD)( lp_stub + i + 4 ); i += 4; /* call */
		lp_stub[i++] = 0x9D;                                                            /* popfd */
		lp_stub[i++] = 0x61;                                                            /* popad */
		lp_stub[i++] = 0xE9; *(DWORD *)( lp_stub + i ) = (DWORD)lp_tramp - (DWORD)( lp_stub + i + 4 ); i += 4; /* jmp tramp */

		memcpy( lp_tramp, (void *)LOOP_SITE, LOOP_STOLEN );
		lp_tramp[LOOP_STOLEN] = 0xE9;
		*(DWORD *)( lp_tramp + LOOP_STOLEN + 1 ) = LOOP_RESUME - ( (DWORD)( lp_tramp + LOOP_STOLEN ) + 5 );
	}

	FlushInstructionCache( GetCurrentProcess(), mem, 256 );

	/* hook CreateFileA first (log re-reads), then the two code sites */
	patch_ptr( (void **)IAT_CFA, (void *)hook_CFA, (void **)&g_realCFA );
	place_jmp( RESGET_SITE, rg_stub, RESGET_STOLEN );
	place_jmp( LOOP_SITE, lp_stub, LOOP_STOLEN );

	logline( "hooks installed: resource_get 0x%x (capture), scummLoop 0x%x (reload), CreateFileA IAT.\r\n",
	         RESGET_SITE, LOOP_SITE );
	logline( "ready. Enter the room to capture its handles, then edit + press F11 (or SetEvent %s).\r\n", EVENT_NAME );
	return 1;
}

static DWORD WINAPI worker( LPVOID unused )
{
	(void)unused;
	InitializeCriticalSection( &g_loglock );
	InitializeCriticalSection( &g_tablock );
	char path[MAX_PATH];
	DWORD nn = GetModuleFileNameA( g_self, path, MAX_PATH );
	while( nn > 0 && path[nn - 1] != '\\' && path[nn - 1] != '/' )
	{
		--nn;
	}
	lstrcpynA( path + nn, "mise-hdreload.log", (int)( MAX_PATH - nn ) );
	g_log = CreateFileA( path, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL );
	SYSTEMTIME st;
	GetLocalTime( &st );
	logline( "# mise-hdreload (HD-only, no interpreter) — %04d-%02d-%02d %02d:%02d:%02d\r\n",
	         st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond );
	g_event = CreateEventA( NULL, FALSE /*auto-reset*/, FALSE, EVENT_NAME );
	logline( "reload event '%s': %s\r\n", EVENT_NAME, g_event ? "ready" : "FAILED" );
	install_hooks();
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
	/* No unhook: we only unload at process exit; leaving the patches avoids racing the game threads. */
	return TRUE;
}
