/*
 * mise-cprobe.dll — READ-ONLY runtime probe for the costume + room.xml in-place reload design.
 *
 * The RE workflow pinned the SAFE costume-reload sequence but gated it on runtime facts static RE can't
 * settle (it has produced wrong offsets before). This probe reads — never writes — those facts so the
 * seamless reload can be coded correctly:
 *   P1  the per-actor costume cache at [actor+0x950] (shape + how to clear it) — the load-bearing step.
 *   P2  the async selector *(0x5ba5a0): if nonzero, a forced costume load goes async (0xc0000000) and
 *       will NOT restore [handle+0x4] the same frame — we'd need a synchronous force.
 *   P3  the global costume descriptor gate [CH+0x4] (via the costume container *(0x5b9c44)).
 *   P7  the Room name container *(0x5b9c9c) entry (evicting it forces .room.xml re-parse).
 *
 * Actors are enumerable straight from HDobj: actor[i] = *(HDobj + 0x9b0 + i*4), i < *(HDobj+0x9f0)
 * (stride 4). We also hook resource_get 0x48c760 (as hdreload does) purely to snapshot the costume FILE
 * handles' loaded-flags [handle+0x4]. Everything else is a raw hex+ascii dump we interpret from the log.
 *
 * Trigger: F12, or the event Local\MISE_CProbe. Guarded by image base + two code signatures. No writes
 * to game memory (the resource_get hook only reads); safe to inject and dump repeatedly.
 */

#include <windows.h>
#include <string.h>

#define IMG_BASE      0x00400000u
#define RESGET_SITE   0x0048c760u
#define RESGET_RESUME 0x0048c769u
#define RESGET_STOLEN 9
#define LOOP_SITE     0x0048ff60u
#define LOOP_RESUME   0x0048ff67u
#define LOOP_STOLEN   7

#define HDOBJ_PTR     0x005b988cu
#define ACTOR_ARR_OFF 0x9b0u        /* HDobj+0x9b0 = actor pointer array (stride 4) */
#define ACTOR_CNT_OFF 0x9f0u        /* HDobj+0x9f0 = actor count */
#define ASYNC_SEL     0x005ba5a0u   /* per-thread async selector; nonzero => costume loads go async */
#define COSTUME_CONT  0x005b9c44u   /* costume name container/atom */
#define ROOM_CONT     0x005b9c9cu   /* room name container/atom */
#define MANAGER_PTR   0x005b98e4u   /* resource manager */

#define H_FLAG_OFF    0x04u
#define H_STRPTR_OFF  0x18u

#define EVENT_NAME    "Local\\MISE_CProbe"
#define HOTKEY        VK_F12

static const unsigned char SIG_LOOP[]   = { 0x66,0x8b,0x15,0x24,0x46,0x5c,0x00 };
static const unsigned char SIG_RESGET[] = { 0x55,0x8b,0xec,0x81,0xec,0x70,0x01,0x00,0x00 };

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

/* raw hex+ascii dump of nbytes at addr (read-only) */
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

/* resource_get hook: snapshot each handle's (name, handle, loaded-flag). Read-only. */
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

static int strnicmp_local( const char *a, const char *b, int n )
{
	for( int i = 0; i < n; ++i )
	{
		char ca = a[i], cb = b[i];
		if( ca >= 'A' && ca <= 'Z' ) ca += 32;
		if( cb >= 'A' && cb <= 'Z' ) cb += 32;
		if( ca != cb ) return ( (unsigned char)ca - (unsigned char)cb );
	}
	return 0;
}

static int contains_ci( const char *hay, const char *needle )
{
	int lh = lstrlenA( hay ), ln = lstrlenA( needle );
	for( int i = 0; i + ln <= lh; ++i )
	{
		if( strnicmp_local( hay + i, needle, ln ) == 0 )
		{
			return 1;
		}
	}
	return 0;
}

static void do_probe( void )
{
	int seq = (int)InterlockedIncrement( &g_seq );
	unsigned hd = *(unsigned *)HDOBJ_PTR;
	unsigned async_sel = readable( (void *)ASYNC_SEL, 4 ) ? *(unsigned *)ASYNC_SEL : 0xDEADBEEF;

	logline( "\r\n================= CPROBE #%d  (tid=%lu) =================\r\n", seq, GetCurrentThreadId() );
	logline( "HDobj=%08x   [P2] async selector *(0x5ba5a0)=%08x  (nonzero => costume loads go ASYNC)\r\n",
	         hd, async_sel );
	if( hd == 0 )
	{
		logline( "HDobj null — get into the HD renderer first.\r\n" );
		return;
	}

	/* [P1] live actors + their per-actor costume cache at [actor+0x950] */
	unsigned cnt = readable( (void *)( hd + ACTOR_CNT_OFF ), 4 ) ? *(unsigned *)( hd + ACTOR_CNT_OFF ) : 0;
	logline( "[P1] actor count [HDobj+0x9f0]=%u\r\n", cnt );
	for( unsigned i = 0; i < cnt && i < 8; ++i )
	{
		unsigned actor = *(unsigned *)( hd + ACTOR_ARR_OFF + i * 4 );
		logline( "  -- actor[%u] = %08x --\r\n", i, actor );
		if( !readable( (void *)actor, 0x40 ) )
		{
			logline( "     (unreadable)\r\n" );
			continue;
		}
		dumpmem( "actor+0x948", actor + 0x948, 0x40 );   /* +0x950 cache slot, +0x980/+0x984 gate area */
		dumpmem( "actor+0x9f0", actor + 0x9f0, 0x20 );
		dumpmem( "actor+0xad0", actor + 0xad0, 0x20 );   /* +0xad8 walk speed */
		unsigned mapRoot = readable( (void *)( actor + 0x950 ), 4 ) ? *(unsigned *)( actor + 0x950 ) : 0;
		logline( "     [actor+0x950] (cache root) = %08x\r\n", mapRoot );
		if( mapRoot )
		{
			dumpmem( "cacheRoot", mapRoot, 0x40 );
			/* the intrusive container reads a node via [root + (actor+0x950) + 8]; dump that too */
			unsigned node = readable( (void *)( mapRoot + ( actor + 0x950 ) + 8 ), 4 )
			                ? *(unsigned *)( mapRoot + ( actor + 0x950 ) + 8 ) : 0;
			logline( "     first node *(root+mapaddr+8) = %08x\r\n", node );
			if( node && node != mapRoot )
			{
				dumpmem( "cacheNode0", node, 0x40 );
			}
		}
	}

	/* [P3] costume container + [P7] room container + manager */
	logline( "[P3] costume container:\r\n" );
	dumpmem( "region@0x5b9c44", COSTUME_CONT, 0x10 );
	unsigned cc = *(unsigned *)COSTUME_CONT;
	logline( "     *(0x5b9c44)=%08x\r\n", cc );
	if( cc )
	{
		dumpmem( "costumeCont*", cc, 0x80 );
	}
	logline( "[P7] room container:\r\n" );
	dumpmem( "region@0x5b9c9c", ROOM_CONT, 0x10 );
	unsigned rc = *(unsigned *)ROOM_CONT;
	logline( "     *(0x5b9c9c)=%08x\r\n", rc );
	if( rc )
	{
		dumpmem( "roomCont*", rc, 0x80 );
	}
	unsigned mgr = readable( (void *)MANAGER_PTR, 4 ) ? *(unsigned *)MANAGER_PTR : 0;
	logline( "manager *(0x5b98e4)=%08x\r\n", mgr );
	if( mgr )
	{
		dumpmem( "mgr", mgr, 0x80 );
	}

	/* costume/room FILE handles we captured via resource_get, with their loaded-flags [+4] */
	logline( "[handles] costume/room resource handles (name -> handle -> [+4] loaded-flag):\r\n" );
	EnterCriticalSection( &g_tablock );
	int n = (int)g_ntab, shown = 0;
	for( int i = 0; i < n && shown < 60; ++i )
	{
		if( contains_ci( g_tab[i].name, "costume" ) || contains_ci( g_tab[i].name, "guybrush" ) ||
		    contains_ci( g_tab[i].name, ".room.xml" ) )
		{
			unsigned f = readable( g_tab[i].h, 8 ) ? *(unsigned *)( (char *)g_tab[i].h + H_FLAG_OFF ) : 0xDEADBEEF;
			logline( "    %08x  [+4]=%08x  %s\r\n", (unsigned)g_tab[i].h, f, g_tab[i].name );
			++shown;
		}
	}
	LeaveCriticalSection( &g_tablock );
	logline( "================= end cprobe #%d =================\r\n", seq );
}

/* --- triggers (interpreter thread) --- */
static int probe_requested( void )
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
	if( probe_requested() )
	{
		do_probe();
	}
}

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
		logline( "ABORT: scummLoop signature mismatch\r\n" );
		return 0;
	}
	if( memcmp( (void *)RESGET_SITE, SIG_RESGET, sizeof( SIG_RESGET ) ) != 0 )
	{
		logline( "ABORT: resource_get signature mismatch\r\n" );
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

	{
		int i = 0;
		rg_stub[i++] = 0x60;
		rg_stub[i++] = 0x9C;
		rg_stub[i++] = 0xFF; rg_stub[i++] = 0x74; rg_stub[i++] = 0x24; rg_stub[i++] = 0x2C; /* push [esp+0x2C] */
		rg_stub[i++] = 0xE8; *(DWORD *)( rg_stub + i ) = (DWORD)&record_handle - (DWORD)( rg_stub + i + 4 ); i += 4;
		rg_stub[i++] = 0x83; rg_stub[i++] = 0xC4; rg_stub[i++] = 0x04;
		rg_stub[i++] = 0x9D;
		rg_stub[i++] = 0x61;
		rg_stub[i++] = 0xE9; *(DWORD *)( rg_stub + i ) = (DWORD)rg_tramp - (DWORD)( rg_stub + i + 4 ); i += 4;

		memcpy( rg_tramp, (void *)RESGET_SITE, RESGET_STOLEN );
		rg_tramp[RESGET_STOLEN] = 0xE9;
		*(DWORD *)( rg_tramp + RESGET_STOLEN + 1 ) = RESGET_RESUME - ( (DWORD)( rg_tramp + RESGET_STOLEN ) + 5 );
	}
	{
		int i = 0;
		lp_stub[i++] = 0x60;
		lp_stub[i++] = 0x9C;
		lp_stub[i++] = 0xE8; *(DWORD *)( lp_stub + i ) = (DWORD)&handle_frame - (DWORD)( lp_stub + i + 4 ); i += 4;
		lp_stub[i++] = 0x9D;
		lp_stub[i++] = 0x61;
		lp_stub[i++] = 0xE9; *(DWORD *)( lp_stub + i ) = (DWORD)lp_tramp - (DWORD)( lp_stub + i + 4 ); i += 4;

		memcpy( lp_tramp, (void *)LOOP_SITE, LOOP_STOLEN );
		lp_tramp[LOOP_STOLEN] = 0xE9;
		*(DWORD *)( lp_tramp + LOOP_STOLEN + 1 ) = LOOP_RESUME - ( (DWORD)( lp_tramp + LOOP_STOLEN ) + 5 );
	}
	FlushInstructionCache( GetCurrentProcess(), mem, 256 );

	place_jmp( RESGET_SITE, rg_stub, RESGET_STOLEN );
	place_jmp( LOOP_SITE, lp_stub, LOOP_STOLEN );

	logline( "hooks installed (read-only): resource_get 0x%x, scummLoop 0x%x.\r\n", RESGET_SITE, LOOP_SITE );
	logline( "ready. Enter the room to load costumes, then press F12 (or SetEvent %s) to dump.\r\n", EVENT_NAME );
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
	lstrcpynA( path + nn, "mise-cprobe.log", (int)( MAX_PATH - nn ) );
	g_log = CreateFileA( path, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL );
	SYSTEMTIME st;
	GetLocalTime( &st );
	logline( "# mise-cprobe (read-only costume/room.xml probe) — %04d-%02d-%02d %02d:%02d:%02d\r\n",
	         st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond );
	g_event = CreateEventA( NULL, FALSE, FALSE, EVENT_NAME );
	logline( "probe event '%s': %s\r\n", EVENT_NAME, g_event ? "ready" : "FAILED" );
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
