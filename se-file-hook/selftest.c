/*
 * se-selftest.exe — a headless stand-in for MISE.exe that validates the hook mechanism without
 * touching the real game. It imports CreateFileA (kernel32) and _access (msvcrt) through its own
 * IAT, exactly like the game does, so the same hook DLL that patches MISE.exe patches this too.
 *
 *   se-selftest.exe          -> loads se-file-hook.dll into itself, makes a few file calls,
 *                               then exits. The calls must appear in se-file-hook.log.
 *   se-selftest.exe wait     -> loops, calling _access on a heartbeat path every 400ms for ~30s,
 *                               so se-inject.exe can inject into it from another shell — this
 *                               exercises the real cross-process injection path end to end.
 */

#include <windows.h>
#include <io.h>      /* _access */
#include <stdio.h>
#include <string.h>

static void make_calls( void )
{
	_access( "selftest/probe-missing.room.xml", 0 );          /* a loose-override style probe (miss) */
	HANDLE f = CreateFileA( "se-selftest-created.tmp", GENERIC_WRITE, 0, NULL, CREATE_ALWAYS,
	                        FILE_ATTRIBUTE_NORMAL, NULL );
	if( f != INVALID_HANDLE_VALUE )
	{
		CloseHandle( f );
	}
	_access( "se-selftest-created.tmp", 0 );                   /* a probe that hits */
	CreateFileA( "selftest/definitely-missing.dxt", GENERIC_READ, FILE_SHARE_READ, NULL,
	             OPEN_EXISTING, 0, NULL );                      /* an open that fails — still logged */
}

int main( int argc, char **argv )
{
	int wait_mode = ( argc > 1 && lstrcmpiA( argv[1], "wait" ) == 0 );

	if( wait_mode )
	{
		printf( "selftest: pid %lu waiting for injection (~12s); run se-inject.exe se-file-hook.dll %lu\n",
		        GetCurrentProcessId(), GetCurrentProcessId() );
		fflush( stdout );
		volatile int sink = 0;
		for( int i = 0; i < 40; ++i )
		{
			char path[64];
			wsprintfA( path, "selftest/heartbeat-%03d.room.xml", i );
			sink += _access( path, 0 );  /* use the result so the call can't be optimised away */
			Sleep( 300 );
		}
		printf( "selftest: done waiting (sink=%d)\n", sink );
		return 0;
	}

	HMODULE hk = LoadLibraryA( "se-file-hook.dll" );   /* simulate the injector */
	if( hk == NULL )
	{
		printf( "selftest: FAILED to load se-file-hook.dll (%lu)\n", GetLastError() );
		return 1;
	}
	Sleep( 300 );   /* let the DLL's worker thread install the hooks */
	make_calls();
	Sleep( 100 );
	printf( "selftest: done — inspect se-file-hook.log for the CreateFileA/_access lines above\n" );
	return 0;
}
