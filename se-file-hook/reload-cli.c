/*
 * mise-reload.exe — signal the injected mise-hotreload.dll to reload the current room's art now.
 * (The editor will do this itself via the same named event; this CLI is for manual testing.)
 */
#include <windows.h>
#include <stdio.h>

int main( void )
{
	HANDLE e = OpenEventA( EVENT_MODIFY_STATE, FALSE, "Local\\MISE_HotReload" );
	if( e == NULL )
	{
		printf( "no hot-reload event — is mise-hotreload.dll injected into MISE.exe?\n" );
		return 1;
	}
	SetEvent( e );
	CloseHandle( e );
	printf( "reload signalled\n" );
	return 0;
}
