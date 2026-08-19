/*
 * mise-mreload.dll — in-place METADATA hot-reload (.room.xml + .costume.xml) for MI1 Special Edition.
 *
 * RE (5-agent workflow, verified against re/gog.asm) established: the scene builder 0x4484e0 NEVER
 * parses — it rebuilds draw items from an ALREADY-parsed node-list. The parsed .room.xml/.costume.xml
 * lives at [handle+0x4] on the per-name resource handle held by the resource manager *(0x5b98e4); the
 * loader factory is [handle+0xc]. resource_get 0x48c760 gates on [handle+0x4] (0x48c7bf: nonzero =>
 * cached) and, with flag == -1, takes the SYNC branch that re-reads the file and stores the fresh parse
 * at [handle+0x4] (0x48c907). The game's own refresh paths pass 0xc0000000 (ASYNC, 0x44c895) which does
 * NOT restore [handle+0x4] — that async-on-zeroed-flag is exactly what orphaned the costume and crashed.
 *
 * So metadata reload = per handle, in ONE tick on the render thread, never leaving [+4]==0 across a
 * frame:  save old=[H+4]; [H+4]=0; call 0x48c760(resmgr, H, -1, 0, 0) SYNC (arg3/arg4=0 are ignored on
 * a re-load, 0x48c7ca); verify [H+4]!=0 (else restore old).  Then:
 *   ROOM    — nudge a rebuild: empty the node-view slot [HDobj+0x984]=0 so the orchestrator 0x44c7be
 *             miss path re-resolves H by name and rebuilds via 0x4484e0 (H already re-parsed).
 *   COSTUME — no nudge: the per-frame scene builder 0x453590->0x460be0 re-resolves it and rebuilds the
 *             actor's cels from the fresh [Hc+4] next frame. Emptying the slot would SKIP that builder.
 *
 * We run on the render thread by hooking the orchestrator 0x44c7be itself (arg's ebx=HDobj; steal the
 * 7-byte prologue cmp). We capture the metadata handles by name (and the real 0x48c760 call ABI) via a
 * resource_get 0x48c760 hook. F12 = read-only DRY-RUN dump (handles, [+4]/[+0xc], captured ABI, thread
 * ids) to confirm before acting. F11 = the actual reload, with per-handle failsafe (restore on failure).
 * Guarded by base + code sigs. Trigger also via events Local\MISE_MReload (F11) / Local\MISE_MDump (F12).
 */

#include <windows.h>
#include <string.h>

#define IMG_BASE      0x00400000u
#define RESGET_SITE   0x0048c760u
#define RESGET_RESUME 0x0048c769u
#define RESGET_STOLEN 9
#define ORCH_SITE     0x0044c7beu   /* HD orchestrator (render thread); ebx=HDobj */
#define ORCH_RESUME   0x0044c7c5u
#define ORCH_STOLEN   7

#define HDOBJ_PTR     0x005b988cu
#define RESMGR_PTR    0x005b98e4u
#define NODEVIEW_OFF  0x984u
#define ROOM_SEL      0x005b9944u
#define ROOM_OBJ_A    0x005b9948u
#define ROOM_OBJ_B    0x005b994cu
#define ROOM_NUM_OFF  0x1cu

#define H_FLAG_OFF    0x04u         /* parsed-data pointer / loaded gate */
#define H_LOADER_OFF  0x0cu         /* loader factory (vtable) */
#define H_STRPTR_OFF  0x18u         /* char* of the name string at handle+0x10 */

#define EVENT_RELOAD  "Local\\MISE_MReload"
#define EVENT_DUMP    "Local\\MISE_MDump"
#define KEY_RELOAD    VK_F11
#define KEY_DUMP      VK_F12

static const unsigned char SIG_RESGET[] = { 0x55,0x8b,0xec,0x81,0xec,0x70,0x01,0x00,0x00 };
static const unsigned char SIG_ORCH[]   = { 0x80,0xbb,0x46,0x09,0x00,0x00,0x00 };

/* the game's own resource_get, reached through our own hook (stub->trampoline->real) */
typedef int ( __stdcall *resget_t )( void *thisp, void *handle, unsigned flag, unsigned a3, unsigned a4 );
#define resource_get ( (resget_t)RESGET_SITE )

static HINSTANCE g_self;
static HANDLE    g_evReload = NULL, g_evDump = NULL;
static HANDLE    g_log = INVALID_HANDLE_VALUE;
static CRITICAL_SECTION g_loglock, g_tablock;
static volatile LONG g_seq = 0;
static volatile LONG g_resgetTid = 0;

typedef struct
{
	void     *h;
	unsigned  last4;
	unsigned  cthis, cflag, ca3, ca4;   /* last real 0x48c760 call ABI for this handle */
	char      name[192];
} HEntry;
#define TAB_MAX 2048
static HEntry g_tab[TAB_MAX];
static volatile LONG g_ntab = 0;

static void logline( const char *fmt, ... )
{
	if( g_log == INVALID_HANDLE_VALUE ) return;
	char buf[700];
	va_list ap; va_start( ap, fmt );
	int n = wvsprintfA( buf, fmt, ap );
	va_end( ap );
	DWORD w;
	EnterCriticalSection( &g_loglock );
	WriteFile( g_log, buf, (DWORD)n, &w, NULL );
	LeaveCriticalSection( &g_loglock );
}

static int readable( const void *p, unsigned len ) { return p != NULL && !IsBadReadPtr( p, len ); }

static void dumpmem( const char *label, unsigned addr, int nbytes )
{
	if( !readable( (void *)addr, (unsigned)nbytes ) ) { logline( "    %s @ %08x: (unreadable)\r\n", label, addr ); return; }
	logline( "    %s @ %08x:\r\n", label, addr );
	for( int off = 0; off < nbytes; off += 16 )
	{
		unsigned *wd = (unsigned *)( addr + off );
		unsigned char *bp = (unsigned char *)( addr + off );
		char asc[17];
		for( int b = 0; b < 16; ++b ) { unsigned char c = bp[b]; asc[b] = ( c >= 32 && c < 127 ) ? (char)c : '.'; }
		asc[16] = 0;
		logline( "      +%03x  %08x %08x %08x %08x  %s\r\n", off, wd[0], wd[1], wd[2], wd[3], asc );
	}
}

static int ends_ci( const char *s, const char *suf )
{
	int ls = lstrlenA( s ), lf = lstrlenA( suf );
	return ls >= lf && lstrcmpiA( s + ls - lf, suf ) == 0;
}
static int starts_with( const char *s, const char *pre ) { while( *pre ) { if( *s++ != *pre++ ) return 0; } return 1; }

/* --- resource_get hook: record (name, handle, [+4]) and the real call ABI (this/flag/a3/a4) --- */
static void __cdecl record_get( unsigned *args )   /* args -> [0]=this [1]=handle [2]=flag [3]=a3 [4]=a4 */
{
	if( g_resgetTid == 0 ) g_resgetTid = (LONG)GetCurrentThreadId();
	void *handle = (void *)args[1];
	if( handle == NULL ) return;
	EnterCriticalSection( &g_tablock );
	int n = (int)g_ntab, i;
	for( i = 0; i < n; ++i )
	{
		if( g_tab[i].h == handle )
		{
			g_tab[i].last4 = *(unsigned *)( (char *)handle + H_FLAG_OFF );
			g_tab[i].cthis = args[0]; g_tab[i].cflag = args[2]; g_tab[i].ca3 = args[3]; g_tab[i].ca4 = args[4];
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
			g_tab[n].cthis = args[0]; g_tab[n].cflag = args[2]; g_tab[n].ca3 = args[3]; g_tab[n].ca4 = args[4];
			lstrcpynA( g_tab[n].name, name, sizeof( g_tab[n].name ) );
			g_ntab = n + 1;
		}
	}
	LeaveCriticalSection( &g_tablock );
}

static int cur_room( void )
{
	unsigned roomObj = ( *(unsigned *)ROOM_SEL != 0 ) ? *(unsigned *)ROOM_OBJ_A : *(unsigned *)ROOM_OBJ_B;
	return ( roomObj != 0 && readable( (void *)( roomObj + ROOM_NUM_OFF ), 1 ) ) ? *(unsigned char *)( roomObj + ROOM_NUM_OFF ) : -1;
}

/* snapshot the metadata handles of interest out of the table (so we don't hold the lock across calls) */
typedef struct { void *h; char name[192]; } Snap;
static int collect( Snap *out, int max, int want_room, int room, const char *needle )
{
	int cnt = 0;
	EnterCriticalSection( &g_tablock );
	int n = (int)g_ntab;
	for( int i = 0; i < n && cnt < max; ++i )
	{
		const char *nm = g_tab[i].name;
		int match;
		if( want_room )
			match = ends_ci( nm, ".room.xml" ) && starts_with( nm, "art/rooms/" ) && ( room < 0 || strstr( nm, needle ) != NULL );
		else
			match = ends_ci( nm, ".costume.xml" );
		if( match ) { out[cnt].h = g_tab[i].h; lstrcpynA( out[cnt].name, nm, sizeof( out[cnt].name ) ); ++cnt; }
	}
	LeaveCriticalSection( &g_tablock );
	return cnt;
}

/* re-parse one metadata handle synchronously; returns 1 on success ([+4] repopulated) */
static int reparse_one( unsigned resmgr, void *h, const char *name )
{
	if( !readable( h, 0x10 ) ) { logline( "  skip %s: handle %p unreadable\r\n", name, h ); return 0; }
	unsigned oldP = *(unsigned *)( (char *)h + H_FLAG_OFF );
	if( oldP == 0 ) { logline( "  skip %s: [+4] already 0 (not loaded)\r\n", name ); return 0; }
	*(unsigned *)( (char *)h + H_FLAG_OFF ) = 0;                 /* evict: clear the cache gate */
	int hr = resource_get( (void *)resmgr, h, 0xffffffffu, 0, 0 );  /* SYNC re-parse (flag = -1) */
	unsigned newP = *(unsigned *)( (char *)h + H_FLAG_OFF );
	if( newP == 0 )
	{
		*(unsigned *)( (char *)h + H_FLAG_OFF ) = oldP;         /* FAILSAFE: never cross a frame with [+4]==0 */
		logline( "  FAIL %s: sync reload left [+4]=0 (hr=%d) — restored old %08x\r\n", name, hr, oldP );
		return 0;
	}
	logline( "  ok   %s: [+4] %08x -> %08x (hr=%d)\r\n", name, oldP, newP, hr );
	return 1;
}

static void do_reload( void )
{
	int seq = (int)InterlockedIncrement( &g_seq );
	unsigned hd = *(unsigned *)HDOBJ_PTR;
	unsigned resmgr = *(unsigned *)RESMGR_PTR;
	int room = cur_room();
	logline( "\r\n=== MRELOAD #%d  room=%d  HDobj=%08x resmgr=%08x tid=%lu(resget=%ld) ===\r\n",
	         seq, room, hd, resmgr, GetCurrentThreadId(), g_resgetTid );
	if( hd == 0 || resmgr == 0 ) { logline( "  abort: HDobj/resmgr null\r\n" ); return; }

	char needle[24]; wsprintfA( needle, "/%d_", room );
	Snap snap[32];

	/* ROOM: re-parse then nudge a rebuild by emptying the node view */
	int nr = collect( snap, 32, 1, room, needle );
	logline( "  [room] %d .room.xml handle(s)\r\n", nr );
	int anyRoom = 0;
	for( int i = 0; i < nr; ++i ) anyRoom |= reparse_one( resmgr, snap[i].h, snap[i].name );
	if( anyRoom )
	{
		unsigned nv = *(unsigned *)( hd + NODEVIEW_OFF );
		*(unsigned *)( hd + NODEVIEW_OFF ) = 0;                 /* nudge: orchestrator re-resolves + rebuilds */
		logline( "  [room] node view emptied (HDobj+0x984 %08x -> 0) to force rebuild\r\n", nv );
	}

	/* COSTUME: re-parse only; the per-frame scene builder re-resolves + rebuilds. NO node-view nudge. */
	int nc = collect( snap, 32, 0, room, needle );
	logline( "  [costume] %d .costume.xml handle(s)\r\n", nc );
	for( int i = 0; i < nc; ++i ) reparse_one( resmgr, snap[i].h, snap[i].name );

	logline( "=== end mreload #%d ===\r\n", seq );
}

static void dump_handle( const char *tag, void *h, unsigned cthis, unsigned cflag, unsigned ca3, unsigned ca4, const char *name )
{
	logline( "  %s %08x  %s\r\n", tag, (unsigned)h, name );
	if( readable( h, 0x40 ) )
	{
		logline( "     [+4]=%08x  [+0xc]=%08x  captured call: this=%08x flag=%08x a3=%08x a4=%08x\r\n",
		         *(unsigned *)( (char *)h + 4 ), *(unsigned *)( (char *)h + 0xc ), cthis, cflag, ca3, ca4 );
		dumpmem( "handle", (unsigned)h, 0x40 );
		unsigned loader = *(unsigned *)( (char *)h + 0xc );
		if( loader ) dumpmem( "loader[+0xc]", loader, 0x20 );
	}
}

static void do_dump( void )
{
	int seq = (int)InterlockedIncrement( &g_seq );
	unsigned hd = *(unsigned *)HDOBJ_PTR, resmgr = *(unsigned *)RESMGR_PTR;
	int room = cur_room();
	logline( "\r\n================= MDUMP #%d (read-only) =================\r\n", seq );
	logline( "HDobj=%08x resmgr *(0x5b98e4)=%08x room=%d  orch tid=%lu  resget tid=%ld\r\n",
	         hd, resmgr, room, GetCurrentThreadId(), g_resgetTid );
	logline( "thread check: %s\r\n",
	         ( g_resgetTid != 0 && (LONG)GetCurrentThreadId() == g_resgetTid )
	         ? "orchestrator hook is on the SAME thread as resource_get — sync call is safe"
	         : "DIFFERENT threads (or resget not seen yet) — verify before F11" );

	EnterCriticalSection( &g_tablock );
	int n = (int)g_ntab, shown = 0;
	for( int i = 0; i < n && shown < 16; ++i )
	{
		if( ends_ci( g_tab[i].name, ".room.xml" ) || ends_ci( g_tab[i].name, ".costume.xml" ) )
		{
			dump_handle( "META", g_tab[i].h, g_tab[i].cthis, g_tab[i].cflag, g_tab[i].ca3, g_tab[i].ca4, g_tab[i].name );
			++shown;
		}
	}
	LeaveCriticalSection( &g_tablock );
	logline( "================= end mdump #%d =================\r\n", seq );
}

/* --- orchestrator hook body (render thread, once per frame) --- */
static void __cdecl handle_orch( void )
{
	if( ( g_evDump && WaitForSingleObject( g_evDump, 0 ) == WAIT_OBJECT_0 ) || ( GetAsyncKeyState( KEY_DUMP ) & 1 ) )
		do_dump();
	if( ( g_evReload && WaitForSingleObject( g_evReload, 0 ) == WAIT_OBJECT_0 ) || ( GetAsyncKeyState( KEY_RELOAD ) & 1 ) )
		do_reload();
}

static void place_jmp( unsigned site, void *dest, int stolen )
{
	DWORD old;
	VirtualProtect( (void *)site, stolen, PAGE_EXECUTE_READWRITE, &old );
	unsigned char *p = (unsigned char *)site;
	p[0] = 0xE9; *(DWORD *)( p + 1 ) = (DWORD)dest - ( site + 5 );
	for( int i = 5; i < stolen; ++i ) p[i] = 0x90;
	VirtualProtect( (void *)site, stolen, old, &old );
	FlushInstructionCache( GetCurrentProcess(), p, stolen );
}

static int install_hooks( void )
{
	if( (unsigned)GetModuleHandleA( NULL ) != IMG_BASE ) { logline( "ABORT: host base %p != 0x%x\r\n", GetModuleHandleA( NULL ), IMG_BASE ); return 0; }
	if( memcmp( (void *)RESGET_SITE, SIG_RESGET, sizeof( SIG_RESGET ) ) != 0 ||
	    memcmp( (void *)ORCH_SITE, SIG_ORCH, sizeof( SIG_ORCH ) ) != 0 )
	{ logline( "ABORT: code signature mismatch — wrong build\r\n" ); return 0; }

	unsigned char *mem = (unsigned char *)VirtualAlloc( NULL, 256, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE );
	if( !mem ) { logline( "ABORT: VirtualAlloc failed (%lu)\r\n", GetLastError() ); return 0; }
	unsigned char *rg_stub = mem + 0, *rg_tramp = mem + 64;
	unsigned char *or_stub = mem + 112, *or_tramp = mem + 176;

	/* resource_get stub: pass &arg0 (= esp+0x28 after pushad+pushfd) to record_get */
	{
		int i = 0;
		rg_stub[i++] = 0x60; rg_stub[i++] = 0x9C;                                   /* pushad; pushfd */
		rg_stub[i++] = 0x8D; rg_stub[i++] = 0x44; rg_stub[i++] = 0x24; rg_stub[i++] = 0x28; /* lea eax,[esp+0x28] */
		rg_stub[i++] = 0x50;                                                        /* push eax */
		rg_stub[i++] = 0xE8; *(DWORD *)( rg_stub + i ) = (DWORD)&record_get - (DWORD)( rg_stub + i + 4 ); i += 4;
		rg_stub[i++] = 0x83; rg_stub[i++] = 0xC4; rg_stub[i++] = 0x04;              /* add esp,4 */
		rg_stub[i++] = 0x9D; rg_stub[i++] = 0x61;                                   /* popfd; popad */
		rg_stub[i++] = 0xE9; *(DWORD *)( rg_stub + i ) = (DWORD)rg_tramp - (DWORD)( rg_stub + i + 4 ); i += 4;
		memcpy( rg_tramp, (void *)RESGET_SITE, RESGET_STOLEN );
		rg_tramp[RESGET_STOLEN] = 0xE9; *(DWORD *)( rg_tramp + RESGET_STOLEN + 1 ) = RESGET_RESUME - ( (DWORD)( rg_tramp + RESGET_STOLEN ) + 5 );
	}
	/* orchestrator stub: run handle_orch each frame (no args) */
	{
		int i = 0;
		or_stub[i++] = 0x60; or_stub[i++] = 0x9C;
		or_stub[i++] = 0xE8; *(DWORD *)( or_stub + i ) = (DWORD)&handle_orch - (DWORD)( or_stub + i + 4 ); i += 4;
		or_stub[i++] = 0x9D; or_stub[i++] = 0x61;
		or_stub[i++] = 0xE9; *(DWORD *)( or_stub + i ) = (DWORD)or_tramp - (DWORD)( or_stub + i + 4 ); i += 4;
		memcpy( or_tramp, (void *)ORCH_SITE, ORCH_STOLEN );
		or_tramp[ORCH_STOLEN] = 0xE9; *(DWORD *)( or_tramp + ORCH_STOLEN + 1 ) = ORCH_RESUME - ( (DWORD)( or_tramp + ORCH_STOLEN ) + 5 );
	}
	FlushInstructionCache( GetCurrentProcess(), mem, 256 );

	place_jmp( RESGET_SITE, rg_stub, RESGET_STOLEN );
	place_jmp( ORCH_SITE, or_stub, ORCH_STOLEN );

	logline( "hooks installed: resource_get 0x%x (capture), orchestrator 0x%x (render-thread action).\r\n", RESGET_SITE, ORCH_SITE );
	logline( "ready. Enter the room to capture metadata handles, then F12=dry-run dump, F11=reload.\r\n" );
	return 1;
}

static DWORD WINAPI worker( LPVOID unused )
{
	(void)unused;
	InitializeCriticalSection( &g_loglock );
	InitializeCriticalSection( &g_tablock );
	char path[MAX_PATH];
	DWORD nn = GetModuleFileNameA( g_self, path, MAX_PATH );
	while( nn > 0 && path[nn - 1] != '\\' && path[nn - 1] != '/' ) --nn;
	lstrcpynA( path + nn, "mise-mreload.log", (int)( MAX_PATH - nn ) );
	g_log = CreateFileA( path, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL );
	SYSTEMTIME st; GetLocalTime( &st );
	logline( "# mise-mreload (in-place metadata reload) — %04d-%02d-%02d %02d:%02d:%02d\r\n",
	         st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond );
	g_evReload = CreateEventA( NULL, FALSE, FALSE, EVENT_RELOAD );
	g_evDump   = CreateEventA( NULL, FALSE, FALSE, EVENT_DUMP );
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
	return TRUE;
}
