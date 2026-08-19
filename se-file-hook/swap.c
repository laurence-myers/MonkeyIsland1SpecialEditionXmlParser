/*
 * mise-hotswap.dll — TRULY seamless HD texture hot-reload for MI1 Special Edition.
 *
 * The D3D observer proved each edited room chunk (art\rooms\images\<room>\*.dxt) becomes exactly one
 * D3DPOOL_MANAGED IDirect3DTexture9, created by the game's loader at MISE+0x82da9. So a reload needs
 * neither a room change, a scene rebuild, nor cache surgery: just LockRect the EXISTING texture and
 * memcpy the edited .dxt's DXT blocks into it. The texture pointer never changes, so every draw
 * binding stays valid and the new pixels appear next frame.
 *
 * How it works:
 *  - Hook the game's CreateFileA (IAT 0x4DA030) to remember each ".dxt" it opens.
 *  - Hook IDirect3DDevice9::CreateTexture (device *(0x5b9920) vtable +0x5c) to record, per loose .dxt,
 *    its live { texture, width, height, fourCC } in a map (re-keyed by path across reloads).
 *  - Hook the scummLoop back-edge 0x48ff60 (main thread): each frame, clear the map on a room change,
 *    and on F11 / SetEvent(Local\MISE_HotSwap) LockRect+memcpy each mapped texture from its edited .dxt.
 *
 * .dxt layout (from the editor's own writer): +0 fourCC "DXT5"/"DXT1", +4 int32 W, +8 int32 H,
 *  +12 raw S3TC blocks (DXT1=8B/block, DXT5=16B/block), tightly packed row-major.
 *
 * Only edits that keep the same dimensions+format are applied in place (the normal case); others are
 * skipped with a log line. Everything runs on the game's single main thread; hooks are restored on
 * unload. Guarded by base + code signature.
 */

#include <windows.h>
#include <string.h>

#define IMG_BASE      0x00400000u
#define SIG_ADDR      0x00475488u
#define P_DEVICE      0x005b9920u
#define IAT_CFA       0x004da030u
#define CT_SLOT       0x5c          /* IDirect3DDevice9::CreateTexture */
#define LOCKRECT      0x4c          /* IDirect3DTexture9::LockRect */
#define UNLOCKRECT    0x50          /* IDirect3DTexture9::UnlockRect */
#define HOOK_SITE     0x0048ff60u   /* scummLoop back-edge (main thread) */
#define HOOK_RESUME   0x0048ff67u
#define CURROOM       0x005c21e0u
#define EVENT_NAME    "Local\\MISE_HotSwap"
#define HOTKEY        VK_F11
#define STOLEN        7
#define FOURCC_DXT5   0x35545844u
#define FOURCC_DXT1   0x31545844u
#define MAP_MAX       128

static const unsigned char SIG_LOOP[]  = { 0x66,0x8b,0x15,0x24,0x46,0x5c,0x00 };
static const unsigned char SIG_CFA[]   = { 0xff,0x15,0x30,0xa0,0x4d,0x00 };

typedef struct { char path[260]; void *tex; unsigned w, h, fourCC; } MapEntry;

static HINSTANCE g_self;
static HANDLE    g_event = NULL, g_log = INVALID_HANDLE_VALUE;
static CRITICAL_SECTION g_lock;
static char      g_lastDxt[260] = "";
static MapEntry  g_map[MAP_MAX];
static int       g_mapCount = 0;
static unsigned  g_lastRoom = 0xffffffffu;
static unsigned  g_d3dLo = 0, g_d3dHi = 0;
static int       g_ctLogged = 0;    /* bound CreateTexture logging */

/* --- logging --- */
static void plog( const char *fmt, ... )
{
	if( g_log == INVALID_HANDLE_VALUE ) return;
	char buf[512];
	va_list ap; va_start( ap, fmt );
	int n = wvsprintfA( buf, fmt, ap );
	va_end( ap );
	DWORD w; WriteFile( g_log, buf, (DWORD)n, &w, NULL );
}

static int is_d3d_obj( unsigned p )
{
	unsigned vt = 0;
	SIZE_T got = 0;
	if( !p || !ReadProcessMemory( GetCurrentProcess(), (void *)p, &vt, 4, &got ) || got != 4 ) return 0;
	return vt >= g_d3dLo && vt < g_d3dHi;
}

/* --- the .dxt -> texture map (main thread only; the CreateTexture hook + swaps share this thread) --- */
static void map_put( const char *path, void *tex, unsigned w, unsigned h, unsigned fourCC )
{
	for( int i = 0; i < g_mapCount; ++i )
	{
		if( lstrcmpiA( g_map[i].path, path ) == 0 )
		{
			g_map[i].tex = tex; g_map[i].w = w; g_map[i].h = h; g_map[i].fourCC = fourCC;
			return;
		}
	}
	if( g_mapCount < MAP_MAX )
	{
		lstrcpynA( g_map[g_mapCount].path, path, sizeof( g_map[0].path ) );
		g_map[g_mapCount].tex = tex; g_map[g_mapCount].w = w; g_map[g_mapCount].h = h; g_map[g_mapCount].fourCC = fourCC;
		++g_mapCount;
		plog( "  mapped[%d] %ux%u fcc=%08x tex=%p  %s\r\n", g_mapCount, w, h, fourCC, tex, path );
	}
}

/* --- CreateFileA IAT hook: remember the last .dxt the game opened --- */
typedef HANDLE ( WINAPI *CreateFileA_t )( LPCSTR, DWORD, DWORD, LPSECURITY_ATTRIBUTES, DWORD, DWORD, HANDLE );
static CreateFileA_t g_realCFA;
static HANDLE WINAPI hook_CFA( LPCSTR name, DWORD a, DWORD s, LPSECURITY_ATTRIBUTES sa, DWORD d, DWORD f, HANDLE t ) __attribute__((noinline));
static HANDLE WINAPI hook_CFA( LPCSTR name, DWORD a, DWORD s, LPSECURITY_ATTRIBUTES sa, DWORD d, DWORD f, HANDLE t )
{
	if( name )
	{
		int len = lstrlenA( name );
		if( len > 4 && lstrcmpiA( name + len - 4, ".dxt" ) == 0 )
		{
			EnterCriticalSection( &g_lock );
			lstrcpynA( g_lastDxt, name, sizeof( g_lastDxt ) );
			LeaveCriticalSection( &g_lock );
		}
	}
	return g_realCFA( name, a, s, sa, d, f, t );
}

/* --- CreateTexture hook: a texture created right after a .dxt open belongs to that .dxt --- */
typedef long ( __stdcall *CreateTex_t )( void *, UINT, UINT, UINT, DWORD, DWORD, DWORD, void **, void * );
static CreateTex_t g_realCT;
static long __stdcall hook_CT( void *dev, UINT w, UINT h, UINT lv, DWORD usage, DWORD fmt, DWORD pool, void **ppTex, void *sh ) __attribute__((noinline));
static long __stdcall hook_CT( void *dev, UINT w, UINT h, UINT lv, DWORD usage, DWORD fmt, DWORD pool, void **ppTex, void *sh )
{
	long hr = g_realCT( dev, w, h, lv, usage, fmt, pool, ppTex, sh );
	if( g_ctLogged < 120 )
	{
		plog( "  CreateTexture %ux%u lv=%u fmt=%08x pool=%u -> %p  lastDxt=%s\r\n",
		      w, h, lv, fmt, pool, ( ppTex ? *ppTex : NULL ), g_lastDxt[0] ? g_lastDxt : "(none)" );
		++g_ctLogged;
	}
	if( hr >= 0 && ppTex && *ppTex && g_lastDxt[0] )
	{
		map_put( g_lastDxt, *ppTex, w, h, fmt );
		g_lastDxt[0] = 0;   /* consume — the next texture is a different resource */
	}
	return hr;
}

/* --- the swap: LockRect the live texture and memcpy the edited .dxt's blocks in place --- */
typedef long ( __stdcall *LockRect_t )( void *, UINT, void *, const void *, DWORD );
typedef long ( __stdcall *UnlockRect_t )( void *, UINT );

static void swap_one( const MapEntry *e )
{
	if( !is_d3d_obj( (unsigned)e->tex ) ) return;   /* texture released since mapping — skip */

	/* build the loose-override full path (next to the game exe) and read it */
	char full[MAX_PATH];
	DWORD n = GetModuleFileNameA( NULL, full, MAX_PATH );
	while( n > 0 && full[n - 1] != '\\' && full[n - 1] != '/' ) --n;
	lstrcpynA( full + n, e->path, (int)( MAX_PATH - n ) );

	HANDLE fh = g_realCFA ? g_realCFA( full, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL )
	                      : CreateFileA( full, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL );
	if( fh == INVALID_HANDLE_VALUE ) return;         /* not a loose override — nothing to reload */
	DWORD size = GetFileSize( fh, NULL );
	if( size < 12 || size > 0x2000000 ) { CloseHandle( fh ); return; }
	BYTE *buf = (BYTE *)HeapAlloc( GetProcessHeap(), 0, size );
	DWORD got = 0;
	if( !buf || !ReadFile( fh, buf, size, &got, NULL ) || got != size ) { CloseHandle( fh ); if( buf ) HeapFree( GetProcessHeap(), 0, buf ); return; }
	CloseHandle( fh );

	unsigned fcc = *(unsigned *)buf, fw = *(unsigned *)( buf + 4 ), fh2 = *(unsigned *)( buf + 8 );
	if( fcc != e->fourCC || fw != e->w || fh2 != e->h )
	{
		plog( "  skip %s: file %ux%u fcc=%08x != texture %ux%u %08x (dims/format changed)\r\n", e->path, fw, fh2, fcc, e->w, e->h, e->fourCC );
		HeapFree( GetProcessHeap(), 0, buf );
		return;
	}

	void *vtbl = *(void **)e->tex;
	LockRect_t lock = *(LockRect_t *)( (char *)vtbl + LOCKRECT );
	UnlockRect_t unlock = *(UnlockRect_t *)( (char *)vtbl + UNLOCKRECT );
	struct { int Pitch; void *pBits; } lr;
	if( lock( e->tex, 0, &lr, NULL, 0 ) >= 0 && lr.pBits )
	{
		int blockSize = ( e->fourCC == FOURCC_DXT5 ) ? 16 : 8;
		int rowBlocks = (int)( ( e->w + 3 ) / 4 );
		int blockRows = (int)( ( e->h + 3 ) / 4 );
		int srcRow = rowBlocks * blockSize;
		BYTE *src = buf + 12;
		for( int r = 0; r < blockRows; ++r )
		{
			memcpy( (BYTE *)lr.pBits + (SIZE_T)r * lr.Pitch, src + (SIZE_T)r * srcRow, srcRow );
		}
		unlock( e->tex, 0 );
		plog( "  swapped %s (%ux%u, %d block-rows, pitch=%d)\r\n", e->path, e->w, e->h, blockRows, lr.Pitch );
	}
	HeapFree( GetProcessHeap(), 0, buf );
}

static void do_swaps( void )
{
	plog( "reload: %d mapped textures\r\n", g_mapCount );
	if( g_mapCount == 0 )
	{
		plog( "  (no textures mapped — inject BEFORE entering the room, then walk in so each .dxt's\r\n"
		      "   CreateTexture is observed; the CreateTexture/mapped lines above show what was seen.)\r\n" );
	}
	for( int i = 0; i < g_mapCount; ++i )
	{
		swap_one( &g_map[i] );
	}
	plog( "reload done\r\n" );
}

/* --- main-thread per-frame driver (from the scummLoop stub) --- */
static void __cdecl handle_frame( void )
{
	unsigned room = *(unsigned char *)CURROOM;
	if( room != g_lastRoom )     /* just log the room change; do NOT wipe the map — map_put re-keys by
	                                path on re-entry, and swap_one skips any texture that was released
	                                (is_d3d_obj). Wiping here was zeroing the map before a reload. */
	{
		plog( "room %u -> %u (map has %d textures)\r\n", g_lastRoom, room, g_mapCount );
		g_lastRoom = room;
	}
	int trig = 0;
	if( g_event && WaitForSingleObject( g_event, 0 ) == WAIT_OBJECT_0 ) trig = 1;
	if( GetAsyncKeyState( HOTKEY ) & 1 ) trig = 1;
	if( trig ) do_swaps();
}

static void patch_ptr( void **slot, void *val, void **saveOrig )
{
	DWORD old;
	VirtualProtect( slot, sizeof( void * ), PAGE_READWRITE, &old );
	if( *saveOrig == NULL ) *saveOrig = *slot;
	*slot = val;
	VirtualProtect( slot, sizeof( void * ), old, &old );
}

static int install_scummloop_hook( void )
{
	unsigned char *t = (unsigned char *)HOOK_SITE;
	if( memcmp( t, SIG_LOOP, sizeof( SIG_LOOP ) ) != 0 ) { plog( "ABORT: scummLoop sig mismatch\r\n" ); return 0; }
	unsigned char *mem = (unsigned char *)VirtualAlloc( NULL, 64, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE );
	if( !mem ) return 0;
	unsigned char *stub = mem, *tr = mem + 32;
	int i = 0;
	stub[i++] = 0x60; stub[i++] = 0x9C;                                                    /* pushad; pushfd */
	stub[i++] = 0xE8; *(DWORD *)( stub + i ) = (DWORD)&handle_frame - (DWORD)( stub + i + 4 ); i += 4;  /* call */
	stub[i++] = 0x9D; stub[i++] = 0x61;                                                    /* popfd; popad */
	stub[i++] = 0xE9; *(DWORD *)( stub + i ) = (DWORD)tr - (DWORD)( stub + i + 4 ); i += 4;              /* jmp tr */
	memcpy( tr, t, STOLEN );
	tr[STOLEN] = 0xE9; *(DWORD *)( tr + STOLEN + 1 ) = HOOK_RESUME - ( (DWORD)( tr + STOLEN ) + 5 );
	FlushInstructionCache( GetCurrentProcess(), mem, 64 );
	DWORD old;
	VirtualProtect( t, STOLEN, PAGE_EXECUTE_READWRITE, &old );
	t[0] = 0xE9; *(DWORD *)( t + 1 ) = (DWORD)stub - ( HOOK_SITE + 5 ); t[5] = 0x90; t[6] = 0x90;
	VirtualProtect( t, STOLEN, old, &old );
	FlushInstructionCache( GetCurrentProcess(), t, STOLEN );
	return 1;
}

static DWORD WINAPI worker( LPVOID unused )
{
	(void)unused;
	InitializeCriticalSection( &g_lock );
	char path[MAX_PATH];
	DWORD n = GetModuleFileNameA( g_self, path, MAX_PATH );
	while( n > 0 && path[n - 1] != '\\' && path[n - 1] != '/' ) --n;
	lstrcpynA( path + n, "mise-hotswap.log", (int)( MAX_PATH - n ) );
	g_log = CreateFileA( path, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL );
	SYSTEMTIME st; GetLocalTime( &st );
	plog( "# mise-hotswap — %04d-%02d-%02d %02d:%02d:%02d\r\n", st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond );

	if( (unsigned)GetModuleHandleA( NULL ) != IMG_BASE ) { plog( "ABORT: host base %p != 0x%x\r\n", GetModuleHandleA( NULL ), IMG_BASE ); return 0; }
	unsigned char *sig = (unsigned char *)SIG_ADDR;
	for( int i = 0; i < (int)sizeof( SIG_CFA ); ++i ) if( sig[i] != SIG_CFA[i] ) { plog( "ABORT: code sig mismatch\r\n" ); return 0; }

	HMODULE d3d = GetModuleHandleA( "d3d9.dll" );
	if( d3d ) { IMAGE_DOS_HEADER *dos = (IMAGE_DOS_HEADER *)d3d; IMAGE_NT_HEADERS *nt = (IMAGE_NT_HEADERS *)( (char *)d3d + dos->e_lfanew ); g_d3dLo = (unsigned)d3d; g_d3dHi = (unsigned)d3d + nt->OptionalHeader.SizeOfImage; }

	patch_ptr( (void **)IAT_CFA, (void *)hook_CFA, (void **)&g_realCFA );
	plog( "hooked CreateFileA IAT\r\n" );

	unsigned dev = 0;
	for( int tries = 0; tries < 6000 && !dev; ++tries ) { dev = *(unsigned *)P_DEVICE; if( !dev ) Sleep( 50 ); }
	if( !dev ) { plog( "device null — enter the game first\r\n" ); return 0; }
	unsigned vtbl = *(unsigned *)dev;
	patch_ptr( (void **)( vtbl + CT_SLOT ), (void *)hook_CT, (void **)&g_realCT );
	plog( "hooked CreateTexture (device %08x vtbl %08x)\r\n", dev, vtbl );

	if( install_scummloop_hook() )
	{
		g_event = CreateEventA( NULL, FALSE, FALSE, EVENT_NAME );
		plog( "ready. Enter the edited room to build the texture map, then edit + press F11 (or SetEvent %s).\r\n", EVENT_NAME );
	}
	for( ;; ) Sleep( 1000 );
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
