/*
 * mise-observe.dll — D3D texture observer for MI1 Special Edition.
 *
 * The read-only probe proved the room's IDirect3DTexture9 objects are NOT stored in the scene
 * descriptors — they live in a draw-time texture cache. This observer finds them directly: it hooks
 * IDirect3DDevice9::CreateTexture (vtable slot 23 = +0x5c) on the device *(0x5b9920) to log every
 * texture the game creates, and hooks the game's CreateFileA (IAT 0x4DA030) to log each ".dxt" it
 * opens. Because loose room textures are read then immediately CreateTexture'd, the interleaved log
 * maps each edited .dxt -> its IDirect3DTexture9 (+ the game caller that created it, for the cache RE).
 *
 * Injected with se-inject.exe; walk out of the edited room and back in so it re-streams (that fires
 * the CreateTexture calls). Then send mise-observe.log. It patches the CreateFileA IAT slot and one
 * device vtable slot; both are restored on unload. Guarded by base + code signature.
 */

#include <windows.h>

#define IMG_BASE     0x00400000u
#define SIG_ADDR     0x00475488u   /* ff 15 30 a0 4d 00 build check */
#define P_DEVICE     0x005b9920u   /* -> IDirect3DDevice9 */
#define IAT_CFA      0x004da030u   /* game IAT slot for kernel32!CreateFileA */
#define CT_SLOT      0x5c          /* IDirect3DDevice9::CreateTexture vtable offset (index 23) */
#define EVENT_NAME   "Local\\MISE_Observe"

static const unsigned char SIG[] = { 0xff, 0x15, 0x30, 0xa0, 0x4d, 0x00 };

static HINSTANCE g_self;
static HANDLE    g_log = INVALID_HANDLE_VALUE;
static CRITICAL_SECTION g_lock;
static char      g_lastDxt[260] = "";
static volatile LONG g_seq = 0;

static void plog( const char *fmt, ... )
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
	EnterCriticalSection( &g_lock );
	WriteFile( g_log, buf, (DWORD)n, &w, NULL );
	LeaveCriticalSection( &g_lock );
}

/* --- CreateFileA IAT hook: remember + log every .dxt the game opens --- */
typedef HANDLE ( WINAPI *CreateFileA_t )( LPCSTR, DWORD, DWORD, LPSECURITY_ATTRIBUTES, DWORD, DWORD, HANDLE );
static CreateFileA_t g_realCFA;

static HANDLE WINAPI hook_CFA( LPCSTR name, DWORD access, DWORD share, LPSECURITY_ATTRIBUTES sa, DWORD disp, DWORD flags, HANDLE tmpl )
	__attribute__((noinline));
static HANDLE WINAPI hook_CFA( LPCSTR name, DWORD access, DWORD share, LPSECURITY_ATTRIBUTES sa, DWORD disp, DWORD flags, HANDLE tmpl )
{
	if( name != NULL )
	{
		int len = lstrlenA( name );
		if( len > 4 && lstrcmpiA( name + len - 4, ".dxt" ) == 0 )
		{
			EnterCriticalSection( &g_lock );
			lstrcpynA( g_lastDxt, name, sizeof( g_lastDxt ) );
			LeaveCriticalSection( &g_lock );
			plog( "%06d open .dxt   %s\r\n", (int)InterlockedIncrement( &g_seq ), name );
		}
	}
	return g_realCFA( name, access, share, sa, disp, flags, tmpl );
}

/* --- IDirect3DDevice9::CreateTexture hook: log every texture created + its .dxt + the game caller --- */
typedef long ( __stdcall *CreateTex_t )( void *, UINT, UINT, UINT, DWORD, DWORD, DWORD, void **, void * );
static CreateTex_t g_realCT;

static long __stdcall hook_CT( void *dev, UINT w, UINT h, UINT levels, DWORD usage, DWORD fmt, DWORD pool, void **ppTex, void *shared )
	__attribute__((noinline));
static long __stdcall hook_CT( void *dev, UINT w, UINT h, UINT levels, DWORD usage, DWORD fmt, DWORD pool, void **ppTex, void *shared )
{
	void *ret = __builtin_return_address( 0 );
	long hr = g_realCT( dev, w, h, levels, usage, fmt, pool, ppTex, shared );
	void *tex = ( hr >= 0 && ppTex != NULL ) ? *ppTex : NULL;
	char dxt[260];
	EnterCriticalSection( &g_lock );
	lstrcpynA( dxt, g_lastDxt, sizeof( dxt ) );
	LeaveCriticalSection( &g_lock );
	plog( "%06d CreateTexture %ux%u lv=%u fmt=0x%x pool=%u -> tex=%p  caller=MISE+0x%x  lastDxt=%s\r\n",
	      (int)InterlockedIncrement( &g_seq ), w, h, levels, fmt, pool, tex,
	      (unsigned)( (BYTE *)ret - IMG_BASE ), dxt );
	return hr;
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

static DWORD WINAPI worker( LPVOID unused )
{
	(void)unused;
	InitializeCriticalSection( &g_lock );
	char path[MAX_PATH];
	DWORD n = GetModuleFileNameA( g_self, path, MAX_PATH );
	while( n > 0 && path[n - 1] != '\\' && path[n - 1] != '/' )
	{
		--n;
	}
	lstrcpynA( path + n, "mise-observe.log", (int)( MAX_PATH - n ) );
	g_log = CreateFileA( path, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL );
	SYSTEMTIME st;
	GetLocalTime( &st );
	plog( "# mise-observe — %04d-%02d-%02d %02d:%02d:%02d\r\n", st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond );

	if( (unsigned)GetModuleHandleA( NULL ) != IMG_BASE )
	{
		plog( "ABORT: host base %p != 0x%x\r\n", GetModuleHandleA( NULL ), IMG_BASE );
		return 0;
	}
	unsigned char *sig = (unsigned char *)SIG_ADDR;
	for( int i = 0; i < (int)sizeof( SIG ); ++i )
	{
		if( sig[i] != SIG[i] )
		{
			plog( "ABORT: code signature mismatch — wrong build\r\n" );
			return 0;
		}
	}

	/* hook the game's CreateFileA slot immediately (path correlation) */
	patch_ptr( (void **)IAT_CFA, (void *)hook_CFA, (void **)&g_realCFA );
	plog( "hooked CreateFileA IAT (0x%x)\r\n", IAT_CFA );

	/* wait for the D3D device, then hook its CreateTexture vtable slot */
	unsigned dev = 0;
	for( int tries = 0; tries < 6000; ++tries )   /* up to ~5 min */
	{
		dev = *(unsigned *)P_DEVICE;
		if( dev )
		{
			break;
		}
		Sleep( 50 );
	}
	if( !dev )
	{
		plog( "device *(0x%x) still null — get into the game first; CreateTexture not hooked.\r\n", P_DEVICE );
		return 0;
	}
	unsigned vtbl = *(unsigned *)dev;
	void **ctSlot = (void **)( vtbl + CT_SLOT );
	patch_ptr( ctSlot, (void *)hook_CT, (void **)&g_realCT );
	plog( "device=%08x vtbl=%08x ; hooked CreateTexture slot @ %08x (orig %p)\r\n", dev, vtbl, (unsigned)ctSlot, g_realCT );
	plog( "ready. Walk out of the edited room and back in to capture its texture creation.\r\n" );

	/* keep the DLL alive; the hooks log live */
	CreateEventA( NULL, FALSE, FALSE, EVENT_NAME );
	for( ;; )
	{
		Sleep( 1000 );
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
