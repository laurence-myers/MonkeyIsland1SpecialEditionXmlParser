/*
 * se-inject.exe — load se-file-hook.dll into a running process (default: MISE.exe).
 *
 * Standard CreateRemoteThread(LoadLibraryA) injection. Both this injector and the target are 32-bit,
 * so kernel32 sits at the same base in both and LoadLibraryA's address here is valid there. Run it
 * AFTER the game is up (at the menu), so the Steam DRM stub has decrypted .text and the game is
 * actually issuing file calls to observe.
 *
 *   se-inject.exe [dll] [process-name-or-pid]
 *   se-inject.exe                       -> injects .\se-file-hook.dll into MISE.exe
 *   se-inject.exe se-file-hook.dll 1234 -> injects into pid 1234
 *
 * No admin needed if the game runs at the same integrity as this injector (the normal case).
 */

#include <windows.h>
#include <tlhelp32.h>
#include <stdio.h>
#include <stdlib.h>

static DWORD find_pid( const char *name )
{
	HANDLE snap = CreateToolhelp32Snapshot( TH32CS_SNAPPROCESS, 0 );
	if( snap == INVALID_HANDLE_VALUE )
	{
		return 0;
	}

	PROCESSENTRY32 pe;
	pe.dwSize = sizeof( pe );
	DWORD pid = 0;
	if( Process32First( snap, &pe ) )
	{
		do
		{
			if( lstrcmpiA( pe.szExeFile, name ) == 0 )
			{
				pid = pe.th32ProcessID;
				break;
			}
		} while( Process32Next( snap, &pe ) );
	}
	CloseHandle( snap );
	return pid;
}

int main( int argc, char **argv )
{
	const char *dll  = ( argc > 1 ) ? argv[1] : "se-file-hook.dll";
	const char *proc = ( argc > 2 ) ? argv[2] : "MISE.exe";

	char full[MAX_PATH];
	if( GetFullPathNameA( dll, MAX_PATH, full, NULL ) == 0 ||
	    GetFileAttributesA( full ) == INVALID_FILE_ATTRIBUTES )
	{
		printf( "error: DLL not found: %s\n", full );
		return 1;
	}

	DWORD pid = 0;
	if( argc > 2 )
	{
		/* allow a numeric pid as the 2nd arg */
		char *end = NULL;
		unsigned long asnum = strtoul( proc, &end, 10 );
		if( end && *end == '\0' && asnum != 0 )
		{
			pid = (DWORD)asnum;
		}
	}
	if( pid == 0 )
	{
		pid = find_pid( proc );
	}
	if( pid == 0 )
	{
		printf( "error: process not running: %s (launch the game first)\n", proc );
		return 2;
	}

	HANDLE h = OpenProcess( PROCESS_CREATE_THREAD | PROCESS_VM_OPERATION | PROCESS_VM_WRITE |
	                        PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, FALSE, pid );
	if( h == NULL )
	{
		printf( "error: OpenProcess(pid %lu) failed (%lu) — if the game is elevated, run this elevated too\n",
		        pid, GetLastError() );
		return 3;
	}

	SIZE_T len = lstrlenA( full ) + 1;
	void *remote = VirtualAllocEx( h, NULL, len, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE );
	if( remote == NULL || !WriteProcessMemory( h, remote, full, len, NULL ) )
	{
		printf( "error: could not write DLL path into the target (%lu)\n", GetLastError() );
		CloseHandle( h );
		return 4;
	}

	FARPROC load = GetProcAddress( GetModuleHandleA( "kernel32.dll" ), "LoadLibraryA" );
	HANDLE thread = CreateRemoteThread( h, NULL, 0, (LPTHREAD_START_ROUTINE)load, remote, 0, NULL );
	if( thread == NULL )
	{
		printf( "error: CreateRemoteThread failed (%lu)\n", GetLastError() );
		VirtualFreeEx( h, remote, 0, MEM_RELEASE );
		CloseHandle( h );
		return 5;
	}

	WaitForSingleObject( thread, INFINITE );
	DWORD module = 0;
	GetExitCodeThread( thread, &module );  /* == LoadLibraryA's HMODULE (32-bit) */
	VirtualFreeEx( h, remote, 0, MEM_RELEASE );
	CloseHandle( thread );
	CloseHandle( h );

	if( module == 0 )
	{
		printf( "error: injected into pid %lu but LoadLibraryA returned 0 (the DLL failed to load)\n", pid );
		return 6;
	}

	printf( "ok: injected %s into %s (pid %lu), module=0x%08lx\n", full, proc, pid, module );
	printf( "    log is se-file-hook.log next to the DLL; re-enter a room in the game to capture its loads\n" );
	return 0;
}
