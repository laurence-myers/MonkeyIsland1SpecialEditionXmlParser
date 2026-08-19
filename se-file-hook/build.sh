#!/usr/bin/env bash
# Build the MI1SE in-place hot-reload tool (32-bit, to match MISE.exe):
#   se-inject.exe    — CreateRemoteThread(LoadLibraryA) injector
#   mise-mreload.dll — the hot-reload hook (metadata re-parse + texture LockRect swap)
#
# Needs the MSYS2 i686 toolchain:  pacman -S --needed mingw-w64-i686-gcc
# Run from anywhere:               bash build.sh
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# cc1.exe (the real compiler) loads its sibling DLLs by PATH order; a plain Git-Bash PATH can shadow
# the mingw32 ones and make gcc fail with NO output. The MSYS2 MINGW32 environment resolves them
# cleanly, so re-exec there if we are not already in it.
if [ "${MSYSTEM:-}" != "MINGW32" ]; then
	for b in "${MI_MSYS2_BASH:-}" "E:/Apps/Dev/msys64/usr/bin/bash.exe" "/e/Apps/Dev/msys64/usr/bin/bash.exe"; do
		if [ -n "$b" ] && [ -x "$b" ]; then
			exec env MSYSTEM=MINGW32 "$b" -lc 'cd "$1" && exec bash build.sh' _ "$here"
		fi
	done
	echo "warning: MSYS2 MINGW32 bash not found; building in the current environment" >&2
fi

cd "$here"

GCC="${MI_I686_GCC:-gcc}"
mach="$("$GCC" -dumpmachine 2>/dev/null || true)"
case "$mach" in
	i686-*|i386-*) : ;;
	*) echo "error: '$GCC' targets '${mach:-unknown}', need a 32-bit i686 mingw gcc" >&2
	   echo "       install it with: pacman -S --needed mingw-w64-i686-gcc" >&2
	   exit 1 ;;
esac
echo "using $GCC ($mach)"

WARN="-Wall -Wextra -Wno-unused-parameter"

# The injector. -static keeps it self-contained (no libgcc/libwinpthread dependency).
"$GCC" $WARN -O2 -static -o se-inject.exe inject.c -lkernel32

# The hot-reload hook DLL. -static: self-contained; -fno-omit-frame-pointer: reliable stack frames.
"$GCC" $WARN -O1 -fno-omit-frame-pointer -static -shared -o mise-mreload.dll mreload.c -lkernel32 -luser32

echo "built:"
ls -la se-inject.exe mise-mreload.dll
