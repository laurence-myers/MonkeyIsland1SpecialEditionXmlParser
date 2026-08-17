/*
 * se-file-hook.dll — Phase 1 reconnaissance hook for MI1 Special Edition (MISE.exe).
 *
 * Goal (see docs/se-hot-reload-hooks.md): find out WHICH files the SE opens and WHEN, and from
 * WHERE in its (runtime-decrypted) code, so we can later trigger a room reload on demand.
 *
 * How: MISE.exe opens every file through kernel32!CreateFileA and probes for loose overrides with
 * msvcr80!_access. Both are ordinary IAT imports (verified: CreateFileA at MISE+0xDA030, _access at
 * MISE+0xDA178; the image is pinned at 0x400000, no ASLR). We walk the host module's import table,
 * find those two thunks BY NAME (so the same binary also works against the bundled self-test, which
 * imports _access from msvcrt), and overwrite the IAT slots with our own functions. Each call is
 * logged with the caller's return address as a module-relative offset (MISE+0xNNNNN) — that offset
 * lands inside the decrypted .text and is the seed for the Phase 2 reverse-engineering.
 *
 * This is a read-then-forward observer: it never changes what the game reads, only records it. It
 * has no dependency beyond system DLLs (kernel32/user32/msvcrt). Injected via se-inject.exe once the
 * game is at the menu (so .text is decrypted); the hooks sit dormant until the game calls them.
 */

#include <windows.h>

/* --- original (real) targets, filled in when we patch their IAT slots --- */
typedef HANDLE (WINAPI *CreateFileA_t)( LPCSTR, DWORD, DWORD, LPSECURITY_ATTRIBUTES, DWORD, DWORD, HANDLE );
typedef int    (__cdecl *access_t)( const char *, int );

static CreateFileA_t g_realCreateFileA = NULL;
static access_t      g_realAccess      = NULL;

/* --- module + logging state --- */
static HINSTANCE       g_self = NULL;    /* this DLL, for locating the log next to it */
static BYTE           *g_base = NULL;    /* host module (the game exe) base */
static char            g_host[64] = "MISE.exe";  /* host module basename, used in caller offsets */
static HANDLE          g_log  = INVALID_HANDLE_VALUE;
static CRITICAL_SECTION g_lock;
static volatile LONG   g_seq  = 0;

/* remember what we patched so we can put it back on unload */
static void          **g_slots[8];
static void           *g_origs[8];
static int             g_nslots = 0;

/* --- logging (guarded; the DLL's own file calls go through its own unpatched IAT, so no recursion) --- */

static void log_raw( const char *buf, int len )
{
	if( g_log == INVALID_HANDLE_VALUE )
	{
		return;
	}

	DWORD written;
	EnterCriticalSection( &g_lock );
	WriteFile( g_log, buf, (DWORD)len, &written, NULL );
	LeaveCriticalSection( &g_lock );
}

static void hlog( const char *fmt, ... )
{
	char buf[1400];
	va_list ap;
	va_start( ap, fmt );
	int len = wvsprintfA( buf, fmt, ap );  /* Win32 formatter — no CRT locale, handles %s/%x/%d/%p */
	va_end( ap );
	log_raw( buf, len );
}

/* one line per intercepted call; mode < 0 means "no mode field" (i.e. a CreateFileA, not an _access) */
static void log_call( const char *fn, void *ret, const char *path, int mode )
{
	SYSTEMTIME st;
	GetLocalTime( &st );
	LONG seq = InterlockedIncrement( &g_seq );
	DWORD rva = (DWORD)( (BYTE *)ret - g_base );
	DWORD tid = GetCurrentThreadId();

	if( mode >= 0 )
	{
		hlog( "%06d %02d:%02d:%02d.%03d tid=%04x %s+0x%06x %s mode=%d \"%s\"\r\n",
		      (int)seq, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds, (int)tid, g_host, rva, fn, mode,
		      path ? path : "(null)" );
	}
	else
	{
		hlog( "%06d %02d:%02d:%02d.%03d tid=%04x %s+0x%06x %s \"%s\"\r\n",
		      (int)seq, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds, (int)tid, g_host, rva, fn,
		      path ? path : "(null)" );
	}
}

/* --- the hooks. Calling conventions MUST match the originals or the stack corrupts. --- */

static HANDLE WINAPI hook_CreateFileA( LPCSTR name, DWORD access, DWORD share, LPSECURITY_ATTRIBUTES sa,
                                       DWORD disp, DWORD flags, HANDLE tmpl )
	__attribute__((noinline));
static HANDLE WINAPI hook_CreateFileA( LPCSTR name, DWORD access, DWORD share, LPSECURITY_ATTRIBUTES sa,
                                       DWORD disp, DWORD flags, HANDLE tmpl )
{
	void *ret = __builtin_return_address( 0 );
	log_call( "CreateFileA", ret, name, -1 );
	return g_realCreateFileA( name, access, share, sa, disp, flags, tmpl );
}

static int __cdecl hook_access( const char *path, int mode ) __attribute__((noinline));
static int __cdecl hook_access( const char *path, int mode )
{
	void *ret = __builtin_return_address( 0 );
	log_call( "_access", ret, path, mode );
	return g_realAccess( path, mode );
}

/* --- IAT walk: for every imported symbol, hand the caller its (dll, name, &slot) --- */

typedef void (*import_cb)( const char *dll, const char *name, void **slot );

static void for_each_import( import_cb cb )
{
	IMAGE_DOS_HEADER *dos = (IMAGE_DOS_HEADER *)g_base;
	if( dos->e_magic != IMAGE_DOS_SIGNATURE )
	{
		return;
	}

	IMAGE_NT_HEADERS *nt = (IMAGE_NT_HEADERS *)( g_base + dos->e_lfanew );
	if( nt->Signature != IMAGE_NT_SIGNATURE )
	{
		return;
	}

	DWORD imp = nt->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT].VirtualAddress;
	if( imp == 0 )
	{
		return;
	}

	for( IMAGE_IMPORT_DESCRIPTOR *desc = (IMAGE_IMPORT_DESCRIPTOR *)( g_base + imp ); desc->Name; ++desc )
	{
		const char *dll = (const char *)( g_base + desc->Name );

		/* OriginalFirstThunk (the INT) keeps the names even after the loader overwrites the IAT */
		DWORD names_rva = desc->OriginalFirstThunk ? desc->OriginalFirstThunk : desc->FirstThunk;
		IMAGE_THUNK_DATA *names = (IMAGE_THUNK_DATA *)( g_base + names_rva );
		IMAGE_THUNK_DATA *iat   = (IMAGE_THUNK_DATA *)( g_base + desc->FirstThunk );

		for( ; names->u1.AddressOfData; ++names, ++iat )
		{
			if( names->u1.Ordinal & IMAGE_ORDINAL_FLAG )
			{
				continue;  /* imported by ordinal, no name to match */
			}

			IMAGE_IMPORT_BY_NAME *nm = (IMAGE_IMPORT_BY_NAME *)( g_base + names->u1.AddressOfData );
			cb( dll, (const char *)nm->Name, (void **)&iat->u1.Function );
		}
	}
}

static void patch_slot( void **slot, void *hook, void **save_orig )
{
	if( *slot == hook )
	{
		return;  /* already ours (double install) */
	}

	DWORD old;
	VirtualProtect( slot, sizeof( void * ), PAGE_READWRITE, &old );
	void *orig = *slot;
	*slot = hook;
	VirtualProtect( slot, sizeof( void * ), old, &old );

	if( *save_orig == NULL )
	{
		*save_orig = orig;
	}
	if( g_nslots < 8 )
	{
		g_slots[g_nslots] = slot;
		g_origs[g_nslots] = orig;
		++g_nslots;
	}
}

static void install_cb( const char *dll, const char *name, void **slot )
{
	if( lstrcmpA( name, "CreateFileA" ) == 0 )
	{
		patch_slot( slot, (void *)hook_CreateFileA, (void **)&g_realCreateFileA );
		hlog( "  hooked CreateFileA (from %s) at %s+0x%06x -> orig %p\r\n",
		      dll, g_host, (DWORD)( (BYTE *)slot - g_base ), *(void **)&g_realCreateFileA );
	}
	else if( lstrcmpA( name, "_access" ) == 0 )
	{
		patch_slot( slot, (void *)hook_access, (void **)&g_realAccess );
		hlog( "  hooked _access (from %s) at %s+0x%06x -> orig %p\r\n",
		      dll, g_host, (DWORD)( (BYTE *)slot - g_base ), *(void **)&g_realAccess );
	}
}

static void unhook_all( void )
{
	for( int i = 0; i < g_nslots; ++i )
	{
		DWORD old;
		VirtualProtect( g_slots[i], sizeof( void * ), PAGE_READWRITE, &old );
		*g_slots[i] = g_origs[i];
		VirtualProtect( g_slots[i], sizeof( void * ), old, &old );
	}
	g_nslots = 0;
}

/* --- setup on a worker thread (DllMain must not do heavy work under the loader lock) --- */

static void open_log( void )
{
	char path[MAX_PATH];
	DWORD n = GetModuleFileNameA( g_self, path, MAX_PATH );  /* the DLL's own path */
	while( n > 0 && path[n - 1] != '\\' && path[n - 1] != '/' )
	{
		--n;
	}
	lstrcpynA( path + n, "se-file-hook.log", (int)( MAX_PATH - n ) );

	g_log = CreateFileA( path, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS,
	                     FILE_ATTRIBUTE_NORMAL, NULL );
	OutputDebugStringA( "[se-file-hook] log: " );
	OutputDebugStringA( path );
}

static DWORD WINAPI worker( LPVOID unused )
{
	(void)unused;
	g_base = (BYTE *)GetModuleHandleA( NULL );  /* the host exe (MISE.exe, or the self-test) */
	InitializeCriticalSection( &g_lock );
	open_log();

	char host[MAX_PATH];
	DWORD hn = GetModuleFileNameA( NULL, host, MAX_PATH );
	DWORD hb = hn;                                    /* strip to the basename for the offset prefix */
	while( hb > 0 && host[hb - 1] != '\\' && host[hb - 1] != '/' )
	{
		--hb;
	}
	lstrcpynA( g_host, host + hb, (int)sizeof( g_host ) );

	SYSTEMTIME st;
	GetLocalTime( &st );
	hlog( "# se-file-hook — host %s @ base %p — %04d-%02d-%02d %02d:%02d:%02d\r\n",
	      host, g_base, st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond );
	hlog( "# columns: seq  time  tid  caller  function  [mode]  path\r\n" );

	for_each_import( install_cb );

	if( g_nslots == 0 )
	{
		hlog( "# WARNING: no CreateFileA/_access import slots found in the host — nothing hooked\r\n" );
	}
	else
	{
		hlog( "# %d slot(s) hooked; recording. Re-enter a room to capture its loads.\r\n", g_nslots );
	}
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
	else if( reason == DLL_PROCESS_DETACH )
	{
		unhook_all();
		if( g_log != INVALID_HANDLE_VALUE )
		{
			CloseHandle( g_log );
			g_log = INVALID_HANDLE_VALUE;
		}
	}
	return TRUE;
}
