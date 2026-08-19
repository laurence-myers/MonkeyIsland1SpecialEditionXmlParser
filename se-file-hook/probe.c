/*
 * mise-probe.dll — READ-ONLY structure probe for MI1 Special Edition (MISE.exe).
 *
 * Confirms the in-memory struct offsets the seamless-reload design infers (see
 * docs/se-hot-reload-hooks.md): the HD scene object, the draw-item hierarchy, the shared texture
 * object T and its holder array (Design A's one unknown), and the name-keyed room/costume resource
 * containers (whose evict/refcount API is the room-metadata reload's blocking unknown).
 *
 * It only READS game memory (via ReadProcessMemory on self, so a bad pointer returns false instead
 * of faulting) and writes hex dumps to mise-probe.log. It patches nothing. Inject with se-inject.exe;
 * press F12 in the game (or SetEvent Local\MISE_Probe) to capture a dump. Take one dump in the edited
 * room, then (for the evict study) walk to another room and back, dumping in each, and send the log.
 */

#include <windows.h>

#define IMG_BASE    0x00400000u
#define SIG_ADDR    0x00475488u   /* CreateFileA call site: ff 15 30 a0 4d 00 (build check) */
#define P_HDOBJ     0x005b988cu   /* -> HD engine/app object */
#define P_DEVICE    0x005b9920u   /* -> IDirect3DDevice9 */
#define C_ROOM      0x005b9c9cu   /* name-keyed room resource container */
#define C_COSTUME   0x005b9c44u   /* name-keyed costume resource container */
#define EVENT_NAME  "Local\\MISE_Probe"
#define HOTKEY      VK_F12

static const unsigned char SIG[] = { 0xff, 0x15, 0x30, 0xa0, 0x4d, 0x00 };

static HINSTANCE g_self;
static HANDLE    g_event = NULL;
static HANDLE    g_log   = INVALID_HANDLE_VALUE;
static volatile LONG g_dumps = 0;
static unsigned  g_d3dLo = 0, g_d3dHi = 0;   /* d3d9.dll range, to recognise D3D COM vtables */

/* --- logging --- */
static void plog( const char *fmt, ... )
{
	if( g_log == INVALID_HANDLE_VALUE )
	{
		return;
	}
	char buf[1024];
	va_list ap;
	va_start( ap, fmt );
	int n = wvsprintfA( buf, fmt, ap );
	va_end( ap );
	DWORD w;
	WriteFile( g_log, buf, (DWORD)n, &w, NULL );
}

/* --- safe reads (ReadProcessMemory never faults on a bad pointer) --- */
static int rd( unsigned addr, void *buf, unsigned n )
{
	SIZE_T got = 0;
	return addr && ReadProcessMemory( GetCurrentProcess(), (void *)addr, buf, n, &got ) && got == n;
}

static unsigned rd32( unsigned addr )
{
	unsigned v = 0;
	return rd( addr, &v, 4 ) ? v : 0;
}

static int is_readable( unsigned addr )
{
	unsigned char b;
	SIZE_T got = 0;
	return addr && ReadProcessMemory( GetCurrentProcess(), (void *)addr, &b, 1, &got ) && got == 1;
}

/* is `p` a COM object whose vtable lies in d3d9.dll? (i.e. an IDirect3DTexture9 etc.) */
static int is_d3d_obj( unsigned p )
{
	unsigned vt = rd32( p );
	return vt >= g_d3dLo && vt < g_d3dHi;
}

static void hexdump( const char *label, unsigned addr, unsigned n )
{
	plog( "  %s @ %08x:\r\n", label, addr );
	unsigned char buf[16];
	for( unsigned off = 0; off < n; off += 16 )
	{
		if( !rd( addr + off, buf, 16 ) )
		{
			plog( "    +%03x  <unreadable>\r\n", off );
			break;
		}
		plog( "    +%03x  %02x%02x%02x%02x %02x%02x%02x%02x %02x%02x%02x%02x %02x%02x%02x%02x\r\n",
		      off, buf[0],buf[1],buf[2],buf[3], buf[4],buf[5],buf[6],buf[7],
		      buf[8],buf[9],buf[10],buf[11], buf[12],buf[13],buf[14],buf[15] );
	}
}

/* Scan T for a pointer to an array of 0x2c-stride records whose [+0] is a D3D texture -> the holder
   array. Logs every candidate offset so the exact holder base + stride is confirmed from real data. */
static void find_texture_holder( unsigned T )
{
	plog( "  holder search in T (looking for ptr -> [texture,...] stride 0x2c):\r\n" );
	int found = 0;
	for( unsigned o = 0; o <= 0xc0; o += 4 )
	{
		unsigned p = rd32( T + o );
		if( !is_readable( p ) )
		{
			continue;
		}
		/* case A: T+o IS the holder array base (entry[0][+0] is a texture) */
		if( is_d3d_obj( rd32( p ) ) )
		{
			plog( "    T+%03x -> %08x : entry[0].tex=%08x (d3d) entry[1].tex=%08x  <-- holder-array candidate (stride 0x2c)\r\n",
			      o, p, rd32( p ), rd32( p + 0x2c ) );
			found = 1;
		}
		/* case B: T+o points to a struct whose +0 is the holder base */
		unsigned pp = rd32( p );
		if( is_readable( pp ) && is_d3d_obj( rd32( pp ) ) )
		{
			plog( "    T+%03x -> %08x -> %08x : tex=%08x (d3d)  <-- indirect holder candidate\r\n", o, p, pp, rd32( pp ) );
			found = 1;
		}
	}
	if( !found )
	{
		plog( "    (no D3D-texture holder found within T+0..0xc0 — dump above for manual analysis)\r\n" );
	}
}

static void dump_container( const char *name, unsigned cptr )
{
	unsigned c = rd32( cptr );
	plog( "%s container *(%08x) = %08x\r\n", name, cptr, c );
	if( c )
	{
		hexdump( "container header", c, 0x60 );
	}
}

static void do_dump( void )
{
	LONG n = InterlockedIncrement( &g_dumps );
	SYSTEMTIME st;
	GetLocalTime( &st );
	plog( "\r\n================= DUMP #%d  %02d:%02d:%02d.%03d =================\r\n",
	      (int)n, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds );

	unsigned hdobj = rd32( P_HDOBJ );
	plog( "HDobj *(%08x) = %08x ; device *(%08x) = %08x ; d3d9=[%08x,%08x)\r\n",
	      P_HDOBJ, hdobj, P_DEVICE, rd32( P_DEVICE ), g_d3dLo, g_d3dHi );
	if( !hdobj )
	{
		plog( "  HDobj null — HD layer not active; open the game to a room and retry.\r\n" );
		return;
	}

	unsigned char roomByte = 0;
	rd( hdobj + 0x980, &roomByte, 1 );
	unsigned vb = rd32( hdobj + 0x984 ), ve = rd32( hdobj + 0x988 );
	plog( "  roomByte[+980]=%02x  nodeView[+984]=%08x..[+988]=%08x (nodes=%d)  actors[+9f0]=%d\r\n",
	      roomByte, vb, ve, ( vb && ve && ve > vb ) ? (int)( ( ve - vb ) / 4 ) : 0, rd32( hdobj + 0x9f0 ) );
	hexdump( "HDobj+0x940..0xa00", hdobj + 0x940, 0xc0 );

	/* sceneObj is embedded at HDobj+0x9a0; draw-item array = node ptrs at sceneObj+0x10 (cap 16) */
	unsigned sceneObj = hdobj + 0x9a0;
	unsigned nodeCount = rd32( sceneObj + 0x50 );
	plog( "  sceneObj=HDobj+0x9a0: nodeArray[+10] count[+50]=%d vbuf[+84]=%08x backptr[+60]=%08x\r\n",
	      nodeCount, rd32( sceneObj + 0x84 ), rd32( sceneObj + 0x60 ) );

	/* walk the first node -> its draw-item vector -> the first draw item -> T */
	unsigned T = 0;
	for( unsigned k = 0; k < 16 && k < nodeCount && T == 0; ++k )
	{
		unsigned node = rd32( sceneObj + 0x10 + k * 4 );
		if( !is_readable( node ) )
		{
			continue;
		}
		unsigned items = rd32( node + 0x8 );      /* draw-item vector data ptr */
		unsigned icount = rd32( node + 0x18 );
		plog( "  node[%d]=%08x vtbl=%08x items[+8]=%08x count[+18]=%d\r\n", k, node, rd32( node ), items, icount );
		if( is_readable( items ) && icount )
		{
			hexdump( "drawItem[0] (0x1c)", items, 0x1c );
			T = rd32( items + 0x8 );               /* draw item +0x8 = resolved texture object T */
		}
	}

	if( T )
	{
		plog( "  --> T (resource object) = %08x ; width[+10]=%d height[+14]=%d holderCount[+b0]=%d\r\n",
		      T, rd32( T + 0x10 ), rd32( T + 0x14 ), rd32( T + 0xb0 ) );
		hexdump( "T+0x00..0x100", T, 0x100 );
		find_texture_holder( T );
	}
	else
	{
		plog( "  (could not reach a T via the scene — is the room fully built? retry after the room settles)\r\n" );
	}

	dump_container( "ROOM", C_ROOM );
	dump_container( "COSTUME", C_COSTUME );
	plog( "================= end dump #%d =================\r\n", (int)n );
	FlushFileBuffers( g_log );
}

static void open_log( void )
{
	char path[MAX_PATH];
	DWORD n = GetModuleFileNameA( g_self, path, MAX_PATH );
	while( n > 0 && path[n - 1] != '\\' && path[n - 1] != '/' )
	{
		--n;
	}
	lstrcpynA( path + n, "mise-probe.log", (int)( MAX_PATH - n ) );
	g_log = CreateFileA( path, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL );
}

static void find_d3d9_range( void )
{
	HMODULE d3d = GetModuleHandleA( "d3d9.dll" );
	if( d3d == NULL )
	{
		return;
	}
	IMAGE_DOS_HEADER dos;
	IMAGE_NT_HEADERS nt;
	if( rd( (unsigned)d3d, &dos, sizeof( dos ) ) && rd( (unsigned)d3d + dos.e_lfanew, &nt, sizeof( nt ) ) )
	{
		g_d3dLo = (unsigned)d3d;
		g_d3dHi = (unsigned)d3d + nt.OptionalHeader.SizeOfImage;
	}
}

static DWORD WINAPI worker( LPVOID unused )
{
	(void)unused;
	open_log();
	SYSTEMTIME st;
	GetLocalTime( &st );
	plog( "# mise-probe (read-only) — %04d-%02d-%02d %02d:%02d:%02d\r\n",
	      st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond );

	if( (unsigned)GetModuleHandleA( NULL ) != IMG_BASE )
	{
		plog( "ABORT: host base %p != 0x%x — not MISE.exe\r\n", GetModuleHandleA( NULL ), IMG_BASE );
		return 0;
	}
	unsigned char *sig = (unsigned char *)SIG_ADDR;
	for( int i = 0; i < (int)sizeof( SIG ); ++i )
	{
		if( sig[i] != SIG[i] )
		{
			plog( "ABORT: code signature mismatch at 0x%x — wrong build\r\n", SIG_ADDR );
			return 0;
		}
	}
	find_d3d9_range();
	g_event = CreateEventA( NULL, FALSE, FALSE, EVENT_NAME );
	plog( "ready. Get into the edited room, then press F12 (or SetEvent %s) to capture a dump.\r\n", EVENT_NAME );

	for( ;; )
	{
		if( ( g_event && WaitForSingleObject( g_event, 40 ) == WAIT_OBJECT_0 ) || ( GetAsyncKeyState( HOTKEY ) & 1 ) )
		{
			do_dump();
		}
	}
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
