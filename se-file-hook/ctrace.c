/*
 * mise-ctrace.dll — READ-ONLY runtime tracer for the seamless costume reload design.
 *
 * The first probe disproved the static costume model: 0x5b9c44/0x5b9c9c are node-type-name TABLES (not
 * caches), and HDobj+0x9b0[i] are NOT the costume-bearing actors (their +0x950 was empty). So this
 * tracer captures the REAL linkage live, by hooking the two functions that actually touch it:
 *   - actor build 0x453590 (arg0 @ [esp+4] = the real actor object) -> authoritative actor list.
 *   - costume part resolve 0x460be0 (arg0 @ [esp+4] = the cache container base, arg2 @ [esp+0xc] = the
 *     part key). Per RE, 0x453590 calls it with base = actor+0x950; it has other callers too, so we
 *     validate base-0x950 against the captured actors. Inside it does eax=[base]; node=*(eax+base+8) —
 *     the cached part that references the parsed costume descriptor CH -> we dump that node to find CH.
 * We also hook resource_get 0x48c760 to snapshot the costume FILE handles ([+4] = loaded-data ptr).
 * Everything is a raw dump we interpret from the log; NO writes to game memory.
 *
 * Goal: pin (a) the real actor object + its costume cache container (shape + how to clear it), (b) the
 * descriptor CH reachable from the cache node, so the seamless reload can clear the cache + evict CH +
 * evict/sync-reload the files in one tick without dangling.
 *
 * Trigger: F12 or event Local\MISE_CTrace. Guarded by base + code sigs. Read-only.
 */

#include <windows.h>
#include <string.h>

#define IMG_BASE      0x00400000u
#define RESGET_SITE   0x0048c760u
#define RESGET_RESUME 0x0048c769u
#define RESGET_STOLEN 9
#define ACTOR_SITE    0x00453590u   /* actor build; arg0 = actor */
#define ACTOR_RESUME  0x00453596u
#define ACTOR_STOLEN  6
#define RESOLVE_SITE  0x00460be0u   /* costume part resolve; arg0 = cache base, arg2 = key */
#define RESOLVE_RESUME 0x00460be8u
#define RESOLVE_STOLEN 8
#define LOOP_SITE     0x0048ff60u
#define LOOP_RESUME   0x0048ff67u
#define LOOP_STOLEN   7

#define HDOBJ_PTR     0x005b988cu
#define MANAGER_PTR   0x005b98e4u
#define CACHE_OFF     0x950u        /* RE: cache container base = actor + 0x950 */

#define H_FLAG_OFF    0x04u
#define H_STRPTR_OFF  0x18u

#define EVENT_NAME    "Local\\MISE_CTrace"
#define HOTKEY        VK_F12

static const unsigned char SIG_LOOP[]    = { 0x66,0x8b,0x15,0x24,0x46,0x5c,0x00 };
static const unsigned char SIG_RESGET[]  = { 0x55,0x8b,0xec,0x81,0xec,0x70,0x01,0x00,0x00 };
static const unsigned char SIG_ACTOR[]   = { 0x55,0x8b,0xec,0x83,0xe4,0xf8,0x81,0xec };
static const unsigned char SIG_RESOLVE[] = { 0x83,0xec,0x60,0x53,0x8b,0x5c,0x24,0x68 };

static HINSTANCE g_self;
static HANDLE    g_event = NULL;
static HANDLE    g_log   = INVALID_HANDLE_VALUE;
static CRITICAL_SECTION g_loglock;
static CRITICAL_SECTION g_tablock;
static volatile LONG g_seq = 0;

typedef struct { void *h; unsigned last4; char name[192]; } HEntry;
#define TAB_MAX 2048
static HEntry g_tab[TAB_MAX];
static volatile LONG g_ntab = 0;

#define ACT_MAX 64
static void *g_actors[ACT_MAX];
static volatile LONG g_nact = 0;

typedef struct { void *base; char key[96]; } REntry;
#define RES_MAX 128
static REntry g_res[RES_MAX];
static volatile LONG g_nres = 0;

static void logline( const char *fmt, ... )
{
	if( g_log == INVALID_HANDLE_VALUE )
	{
		return;
	}
	char buf[700];
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

static void dumpmem( const char *label, unsigned addr, int nbytes )
{
	if( !readable( (void *)addr, (unsigned)nbytes ) )
	{
		logline( "    %s @ %08x: (unreadable)\r\n", label, addr );
		return;
	}
	logline( "    %s @ %08x:\r\n", label, addr );
	for( int off = 0; off < nbytes; off += 16 )
	{
		unsigned *wd = (unsigned *)( addr + off );
		unsigned char *bp = (unsigned char *)( addr + off );
		char asc[17];
		for( int b = 0; b < 16; ++b )
		{
			unsigned char c = bp[b];
			asc[b] = ( c >= 32 && c < 127 ) ? (char)c : '.';
		}
		asc[16] = 0;
		logline( "      +%03x  %08x %08x %08x %08x  %s\r\n", off, wd[0], wd[1], wd[2], wd[3], asc );
	}
}

/* --- captures (all read-only) --- */
static void __cdecl record_actor( void *a )
{
	if( a == NULL || !readable( a, 4 ) )
	{
		return;
	}
	EnterCriticalSection( &g_tablock );
	int n = (int)g_nact;
	for( int i = 0; i < n; ++i )
	{
		if( g_actors[i] == a ) { LeaveCriticalSection( &g_tablock ); return; }
	}
	if( n < ACT_MAX ) { g_actors[n] = a; g_nact = n + 1; }
	LeaveCriticalSection( &g_tablock );
}

static void __cdecl record_resolve( void *base, void *key )
{
	if( base == NULL )
	{
		return;
	}
	EnterCriticalSection( &g_tablock );
	int n = (int)g_nres;
	for( int i = 0; i < n; ++i )
	{
		if( g_res[i].base == base ) { LeaveCriticalSection( &g_tablock ); return; }
	}
	if( n < RES_MAX )
	{
		g_res[n].base = base;
		g_res[n].key[0] = 0;
		if( readable( key, 1 ) )
		{
			lstrcpynA( g_res[n].key, (const char *)key, sizeof( g_res[n].key ) );
		}
		g_nres = n + 1;
	}
	LeaveCriticalSection( &g_tablock );
}

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

static int is_captured_actor( void *a )
{
	for( int i = 0; i < (int)g_nact; ++i )
	{
		if( g_actors[i] == a ) { return 1; }
	}
	return 0;
}

/* tiny case-insensitive substring test */
static int ct_substr_ci( const char *hay, const char *needle )
{
	int lh = lstrlenA( hay ), ln = lstrlenA( needle );
	for( int i = 0; i + ln <= lh; ++i )
	{
		int k = 0;
		for( ; k < ln; ++k )
		{
			char a = hay[i + k], b = needle[k];
			if( a >= 'A' && a <= 'Z' ) a += 32;
			if( b >= 'A' && b <= 'Z' ) b += 32;
			if( a != b ) break;
		}
		if( k == ln ) { return 1; }
	}
	return 0;
}

static void do_trace( void )
{
	int seq = (int)InterlockedIncrement( &g_seq );
	unsigned hd = *(unsigned *)HDOBJ_PTR;
	logline( "\r\n================= CTRACE #%d  (tid=%lu) =================\r\n", seq, GetCurrentThreadId() );
	logline( "HDobj=%08x  manager *(0x5b98e4)=%08x\r\n", hd,
	         readable( (void *)MANAGER_PTR, 4 ) ? *(unsigned *)MANAGER_PTR : 0 );

	EnterCriticalSection( &g_tablock );
	int nact = (int)g_nact, nres = (int)g_nres, ntab = (int)g_ntab;

	/* real actors captured from 0x453590 */
	logline( "[actors] %d captured from actor-build 0x453590:\r\n", nact );
	for( int i = 0; i < nact && i < 6; ++i )
	{
		unsigned a = (unsigned)g_actors[i];
		logline( "  -- actor[%d] = %08x --\r\n", i, a );
		dumpmem( "actor+0x900", a + 0x900, 0x1c0 );      /* covers +0x950 cache, +0x980/+0x984 gate, +0x9a0 */
		unsigned cache = readable( (void *)( a + CACHE_OFF ), 4 ) ? *(unsigned *)( a + CACHE_OFF ) : 0;
		logline( "     [actor+0x950]=%08x  [actor+0x984]=%08x\r\n",
		         cache, readable( (void *)( a + 0x984 ), 4 ) ? *(unsigned *)( a + 0x984 ) : 0 );
	}

	/* resolve sites from 0x460be0: base, key, and whether base-0x950 is a real actor */
	logline( "[resolves] %d captured from part-resolve 0x460be0:\r\n", nres );
	for( int i = 0; i < nres && i < 24; ++i )
	{
		unsigned base = (unsigned)g_res[i].base;
		unsigned cand = base - CACHE_OFF;
		int match = is_captured_actor( (void *)cand );
		logline( "  -- resolve[%d] base=%08x  base-0x950=%08x %s  key='%s'\r\n",
		         i, base, cand, match ? "(== a captured ACTOR)" : "", g_res[i].key );
		dumpmem( "base", base, 0x30 );
		/* the cached node: eax=[base]; node=*(eax+base+8) */
		unsigned eax = readable( (void *)base, 4 ) ? *(unsigned *)base : 0;
		if( eax )
		{
			unsigned nodeAddr = eax + base + 8;
			unsigned node = readable( (void *)nodeAddr, 4 ) ? *(unsigned *)nodeAddr : 0;
			logline( "     [base]=%08x  node=*(%08x)=%08x\r\n", eax, nodeAddr, node );
			if( node && node != base )
			{
				dumpmem( "cacheNode", node, 0x50 );   /* the cached part -> should reference CH */
			}
		}
	}

	/* costume/room FILE handles + loaded-flags */
	logline( "[handles] costume/room resource handles ([+4]=loaded-data ptr):\r\n" );
	int shown = 0;
	for( int i = 0; i < ntab && shown < 40; ++i )
	{
		const char *nm = g_tab[i].name;
		int ln = lstrlenA( nm );
		int isc = ( ln > 8 && ( lstrcmpiA( nm + ln - 12, "costume.xml" ) == 0 || lstrcmpiA( nm + ln - 4, ".dxt" ) == 0 ) );
		/* keep it focused on costumes + room.xml */
		if( ( lstrlenA( nm ) >= 12 && ( ct_substr_ci( nm, "costume" ) || ct_substr_ci( nm, ".room.xml" ) ) ) || isc )
		{
			unsigned f = readable( g_tab[i].h, 8 ) ? *(unsigned *)( (char *)g_tab[i].h + H_FLAG_OFF ) : 0xDEADBEEF;
			logline( "    %08x  [+4]=%08x  %s\r\n", (unsigned)g_tab[i].h, f, nm );
			++shown;
		}
	}
	LeaveCriticalSection( &g_tablock );
	logline( "================= end ctrace #%d =================\r\n", seq );
}

/* --- triggers --- */
static int trace_requested( void )
{
	if( g_event != NULL && WaitForSingleObject( g_event, 0 ) == WAIT_OBJECT_0 ) { return 1; }
	if( GetAsyncKeyState( HOTKEY ) & 1 ) { return 1; }
	return 0;
}

static void __cdecl handle_frame( void )
{
	if( trace_requested() )
	{
		do_trace();
	}
}

static void place_jmp( unsigned site, void *dest, int stolen )
{
	DWORD old;
	VirtualProtect( (void *)site, stolen, PAGE_EXECUTE_READWRITE, &old );
	unsigned char *p = (unsigned char *)site;
	p[0] = 0xE9;
	*(DWORD *)( p + 1 ) = (DWORD)dest - ( site + 5 );
	for( int i = 5; i < stolen; ++i ) { p[i] = 0x90; }
	VirtualProtect( (void *)site, stolen, old, &old );
	FlushInstructionCache( GetCurrentProcess(), p, stolen );
}

/* build a register-preserving stub that pushes `nargs` dwords (from entry-relative esp offsets) and
   calls fn, then jmps to tramp. argOff[] are offsets from esp AFTER pushad+pushfd (0x24=retaddr). */
static unsigned char *emit_capture_stub( unsigned char *p, void *fn, unsigned char *tramp,
                                         const int *argOff, int nargs )
{
	int i = 0;
	p[i++] = 0x60;                 /* pushad */
	p[i++] = 0x9C;                 /* pushfd */
	/* push args right-to-left so C sees them left-to-right; account for each push shifting esp by 4 */
	for( int k = nargs - 1; k >= 0; --k )
	{
		int off = argOff[k] + ( nargs - 1 - k ) * 4;   /* earlier pushes moved esp down */
		p[i++] = 0xFF; p[i++] = 0x74; p[i++] = 0x24; p[i++] = (unsigned char)off;  /* push [esp+off] */
	}
	p[i++] = 0xE8; *(DWORD *)( p + i ) = (DWORD)fn - (DWORD)( p + i + 4 ); i += 4;  /* call fn */
	p[i++] = 0x83; p[i++] = 0xC4; p[i++] = (unsigned char)( nargs * 4 );            /* add esp, n*4 */
	p[i++] = 0x9D;                 /* popfd */
	p[i++] = 0x61;                 /* popad */
	p[i++] = 0xE9; *(DWORD *)( p + i ) = (DWORD)tramp - (DWORD)( p + i + 4 ); i += 4; /* jmp tramp */
	return p + i;
}

static void emit_tramp( unsigned char *tramp, unsigned site, int stolen, unsigned resume )
{
	memcpy( tramp, (void *)site, stolen );
	tramp[stolen] = 0xE9;
	*(DWORD *)( tramp + stolen + 1 ) = resume - ( (DWORD)( tramp + stolen ) + 5 );
}

static int install_hooks( void )
{
	if( (unsigned)GetModuleHandleA( NULL ) != IMG_BASE )
	{
		logline( "ABORT: host base %p != 0x%x\r\n", GetModuleHandleA( NULL ), IMG_BASE );
		return 0;
	}
	if( memcmp( (void *)LOOP_SITE, SIG_LOOP, sizeof( SIG_LOOP ) ) != 0 ||
	    memcmp( (void *)RESGET_SITE, SIG_RESGET, sizeof( SIG_RESGET ) ) != 0 ||
	    memcmp( (void *)ACTOR_SITE, SIG_ACTOR, sizeof( SIG_ACTOR ) ) != 0 ||
	    memcmp( (void *)RESOLVE_SITE, SIG_RESOLVE, sizeof( SIG_RESOLVE ) ) != 0 )
	{
		logline( "ABORT: a code signature mismatched — wrong build; not patching\r\n" );
		return 0;
	}

	unsigned char *mem = (unsigned char *)VirtualAlloc( NULL, 640, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE );
	if( mem == NULL )
	{
		logline( "ABORT: VirtualAlloc failed (%lu)\r\n", GetLastError() );
		return 0;
	}
	/* layout: 4 (stub,tramp) pairs, 80 bytes apart */
	unsigned char *rg_stub = mem + 0,   *rg_tramp = mem + 48;
	unsigned char *ac_stub = mem + 80,  *ac_tramp = mem + 128;
	unsigned char *rs_stub = mem + 160, *rs_tramp = mem + 224;
	unsigned char *lp_stub = mem + 272, *lp_tramp = mem + 336;

	/* entry-relative offsets (after pushad+pushfd, retaddr at 0x24): argN at 0x28 + N*4 */
	int rgOff[1] = { 0x28 };            /* handle = arg0 */
	int acOff[1] = { 0x28 };            /* actor = arg0 */
	int rsOff[2] = { 0x28, 0x30 };      /* base = arg0, key = arg2 */

	emit_capture_stub( rg_stub, (void *)&record_handle,  rg_tramp, rgOff, 1 );
	emit_capture_stub( ac_stub, (void *)&record_actor,   ac_tramp, acOff, 1 );
	emit_capture_stub( rs_stub, (void *)&record_resolve, rs_tramp, rsOff, 2 );
	/* scummLoop stub: no args */
	{
		int i = 0;
		lp_stub[i++] = 0x60; lp_stub[i++] = 0x9C;
		lp_stub[i++] = 0xE8; *(DWORD *)( lp_stub + i ) = (DWORD)&handle_frame - (DWORD)( lp_stub + i + 4 ); i += 4;
		lp_stub[i++] = 0x9D; lp_stub[i++] = 0x61;
		lp_stub[i++] = 0xE9; *(DWORD *)( lp_stub + i ) = (DWORD)lp_tramp - (DWORD)( lp_stub + i + 4 ); i += 4;
	}

	emit_tramp( rg_tramp, RESGET_SITE,  RESGET_STOLEN,  RESGET_RESUME );
	emit_tramp( ac_tramp, ACTOR_SITE,   ACTOR_STOLEN,   ACTOR_RESUME );
	emit_tramp( rs_tramp, RESOLVE_SITE, RESOLVE_STOLEN, RESOLVE_RESUME );
	emit_tramp( lp_tramp, LOOP_SITE,    LOOP_STOLEN,    LOOP_RESUME );

	FlushInstructionCache( GetCurrentProcess(), mem, 640 );

	place_jmp( RESGET_SITE,  rg_stub, RESGET_STOLEN );
	place_jmp( ACTOR_SITE,   ac_stub, ACTOR_STOLEN );
	place_jmp( RESOLVE_SITE, rs_stub, RESOLVE_STOLEN );
	place_jmp( LOOP_SITE,    lp_stub, LOOP_STOLEN );

	logline( "hooks installed (read-only): resource_get, actor-build 0x453590, part-resolve 0x460be0, scummLoop.\r\n" );
	logline( "ready. Enter the room + move Guybrush a little, then press F12 (or SetEvent %s).\r\n", EVENT_NAME );
	return 1;
}

static DWORD WINAPI worker( LPVOID unused )
{
	(void)unused;
	InitializeCriticalSection( &g_loglock );
	InitializeCriticalSection( &g_tablock );
	char path[MAX_PATH];
	DWORD nn = GetModuleFileNameA( g_self, path, MAX_PATH );
	while( nn > 0 && path[nn - 1] != '\\' && path[nn - 1] != '/' ) { --nn; }
	lstrcpynA( path + nn, "mise-ctrace.log", (int)( MAX_PATH - nn ) );
	g_log = CreateFileA( path, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL );
	SYSTEMTIME st;
	GetLocalTime( &st );
	logline( "# mise-ctrace (read-only actor/costume linkage tracer) — %04d-%02d-%02d %02d:%02d:%02d\r\n",
	         st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond );
	g_event = CreateEventA( NULL, FALSE, FALSE, EVENT_NAME );
	logline( "trace event '%s': %s\r\n", EVENT_NAME, g_event ? "ready" : "FAILED" );
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
