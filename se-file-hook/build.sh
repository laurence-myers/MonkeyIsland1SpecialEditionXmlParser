#!/usr/bin/env bash
# Build the Phase-1 file-recon tool (32-bit, to match MISE.exe).
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

# The injected DLL. -static keeps it self-contained (no libgcc/libwinpthread dependency);
# -fno-omit-frame-pointer keeps __builtin_return_address(0) reliable for the caller offsets.
"$GCC" $WARN -O1 -fno-omit-frame-pointer -static -shared \
	-o se-file-hook.dll hook.c -lkernel32 -luser32

# The injector.
"$GCC" $WARN -O2 -static -o se-inject.exe inject.c -lkernel32

# The self-test stand-in. Built at -O0 on purpose: at -O2 gcc hoists the imported CreateFileA/_access
# pointers into registers at startup and the wait-loop reuses them, so an IAT patch applied later
# never sees those calls. The real game's room-loader instead runs (and re-reads the IAT) only after
# we inject at the menu, so -O0 here matches the real capture scenario. See README "Limitations".
"$GCC" $WARN -O0 -static -o se-selftest.exe selftest.c -lkernel32 -luser32

"$GCC" $WARN -O1 -fno-omit-frame-pointer -static -shared -o mise-hotreload.dll reload.c -lkernel32 -luser32
"$GCC" $WARN -O2 -static -o mise-reload.exe reload-cli.c -lkernel32

"$GCC" $WARN -O1 -static -shared -o mise-probe.dll probe.c -lkernel32 -luser32

"$GCC" $WARN -O1 -fno-omit-frame-pointer -static -shared -o mise-observe.dll observe.c -lkernel32 -luser32

"$GCC" $WARN -O1 -fno-omit-frame-pointer -static -shared -o mise-hotswap.dll swap.c -lkernel32 -luser32

"$GCC" $WARN -O1 -fno-omit-frame-pointer -static -shared -o mise-hdreload.dll hdreload.c -lkernel32 -luser32

"$GCC" $WARN -O1 -fno-omit-frame-pointer -static -shared -o mise-cprobe.dll cprobe.c -lkernel32 -luser32

"$GCC" $WARN -O1 -fno-omit-frame-pointer -static -shared -o mise-ctrace.dll ctrace.c -lkernel32 -luser32

"$GCC" $WARN -O1 -fno-omit-frame-pointer -static -shared -o mise-mreload.dll mreload.c -lkernel32 -luser32

echo "built:"
ls -la se-file-hook.dll se-inject.exe se-selftest.exe mise-hotreload.dll mise-reload.exe mise-probe.dll mise-observe.dll mise-hotswap.dll mise-hdreload.dll
