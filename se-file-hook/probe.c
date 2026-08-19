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
#define C_ROOM      0x005b9c9cu   /* -> "Room" category name atom (NOT a container, per dump #1) */
#define C_COSTUME   0x005b9c44u   /* -> "Costume" category name atom */
#define P_MGR       0x005b98e4u   /* -> the real HD resource manager */
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

/* Scan [base, base+n) for the GPU texture: a dword that IS a D3D COM object (vtable in d3d9), or
   POINTS TO one, or points to an array whose [0] is one. Logs every hit + its offset/stride clue. */
static void scan_d3d( const char *label, unsigned base, unsigned n )
{
	int found = 0;
	for( unsigned o = 0; o < n; o += 4 )
	{
		unsigned p = rd32( base + o );
		if( is_d3d_obj( p ) )
		{
			plog( "    %s+%03x = %08x  IS a D3D texture (d3d9 vtable %08x)\r\n", label, o, p, rd32( p ) );
			found = 1;
		}
		else if( is_readable( p ) )
		{
			unsigned pp = rd32( p );
			if( is_d3d_obj( pp ) )
			{
				plog( "    %s+%03x -> %08x -> D3D texture %08x  (next@+0x2c=%08x +0x4=%08x)\r\n",
				      label, o, p, pp, rd32( p + 0x2c ), rd32( p + 4 ) );
				found = 1;
			}
		}
	}
	if( !found )
	{
		plog( "    (%s: no D3D texture in +0..%x)\r\n", label, n );
	}
}

/* Follow a resource object: dump it, scan it, then follow its +0x00 pointer (where the dump showed the
   texture likely lives) one/two levels, scanning each for the actual IDirect3DTexture9. */
static void probe_resource( unsigned res )
{
	if( !is_readable( res ) )
	{
		return;
	}
	hexdump( "resObj +0x00..0x100", res, 0x100 );
	scan_d3d( "resObj", res, 0x100 );
	unsigned r0 = rd32( res + 0x00 );
	if( is_readable( r0 ) && r0 != res )
	{
		plog( "  resObj[+0x00] = %08x — following:\r\n", r0 );
		hexdump( "resObj[+0] +0x00..0x100", r0, 0x100 );
		scan_d3d( "resObj[+0]", r0, 0x100 );
		unsigned r00 = rd32( r0 + 0x00 );
		if( is_readable( r00 ) && r00 != r0 && r00 != res )
		{
			hexdump( "resObj[+0][+0] +0x00..0x80", r00, 0x80 );
			scan_d3d( "resObj[+0][+0]", r00, 0x80 );
		}
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

	/* walk EACH node -> first draw item, and hunt the GPU texture: draw item +0x4 (material/page) and
	   +0x8 (resObj) and +0x18 (subrect), following pointers and scanning for a d3d9 vtable object. */
	for( unsigned k = 0; k < 16 && k < nodeCount; ++k )
	{
		unsigned node = rd32( sceneObj + 0x10 + k * 4 );
		unsigned vt = rd32( node );
		plog( "\r\n  node[%d]=%08x vtbl=%08x\r\n", k, node, vt );
		if( vt != 0x004ea5b8 )      /* only this vtable is a draw-item-bearing node */
		{
			plog( "    (node type %08x is not a draw-item node — skipping)\r\n", vt );
			continue;
		}
		unsigned items = rd32( node + 0x8 );
		unsigned icount = rd32( node + 0x18 );
		plog( "    items[+8]=%08x count[+18]=%d\r\n", items, icount );
		if( !is_readable( items ) || icount == 0 )
		{
			continue;
		}
		hexdump( "drawItem[0] (0x1c)", items, 0x1c );
		scan_d3d( "drawItem", items, 0x1c );
		unsigned mat = rd32( items + 0x4 );        /* draw item +0x4 = material/page (texture likely here) */
		if( is_readable( mat ) )
		{
			plog( "  drawItem+0x04 (material/page) = %08x:\r\n", mat );
			hexdump( "material +0x00..0x100", mat, 0x100 );
			scan_d3d( "material", mat, 0x100 );
			unsigned m0 = rd32( mat );
			if( is_readable( m0 ) && m0 != mat )
			{
				hexdump( "material[+0] +0x00..0x80", m0, 0x80 );
				scan_d3d( "material[+0]", m0, 0x80 );
			}
		}
		plog( "  drawItem+0x08 (resObj) = %08x:\r\n", rd32( items + 0x8 ) );
		probe_resource( rd32( items + 0x8 ) );
	}

	/* the vertex/quad buffer may hold the bound page texture */
	unsigned vbuf = rd32( sceneObj + 0x84 );
	if( is_readable( vbuf ) )
	{
		plog( "\r\nvbuf (sceneObj+0x84) = %08x:\r\n", vbuf );
		hexdump( "vbuf +0x00..0x80", vbuf, 0x80 );
		scan_d3d( "vbuf", vbuf, 0x80 );
	}

	/* the REAL resource manager (0x5b9c9c/0x5b9c44 turned out to be the "Room"/"Costume" name atoms) */
	unsigned mgr = rd32( P_MGR );
	plog( "\r\nresource manager *(%08x) = %08x ; nameAtoms: Room=%08x Costume=%08x\r\n",
	      P_MGR, mgr, rd32( C_ROOM ), rd32( C_COSTUME ) );
	if( is_readable( mgr ) )
	{
		hexdump( "mgr +0x00..0x140", mgr, 0x140 );
	}
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
