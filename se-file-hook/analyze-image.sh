#!/usr/bin/env bash
# Phase 2 first-pass RE of the decrypted image dump (MISE.image.bin, produced by injecting
# se-file-hook.dll — the on-disk .text is Steam-encrypted, this dump is the plaintext).
#
# It surfaces the room-load call graph we need to drive an on-demand reload:
#   - the two room loaders  (classic path builder via "classic/%s/%s"; HD via "LoadRoom"/"ScummResources")
#   - the _access / CreateFileA VFS wrappers (the Phase-1 leaves) and their loose-override callers
#   - candidates for the room-entry driver (the common caller of both loaders) and the _currentRoom global
#
# Everything is anchored on absolute VAs, which appear verbatim as immediates in the code (no ASLR,
# image based at 0x400000). Read the report, then use `callers`/`refs` (exported below) to climb.
#
#   ./analyze-image.sh [MISE.image.bin]
#   source ./analyze-image.sh   # to get the entryof/callers/refs helpers in your shell
set -uo pipefail

IMG="${1:-MISE.image.bin}"
OBJDUMP="${OBJDUMP:-E:/Apps/Dev/msys64/mingw64/bin/objdump.exe}"
BASE=0x400000
ASM="${IMG%.*}.asm"

if [ ! -f "$IMG" ]; then
	echo "usage: $0 MISE.image.bin   (inject se-file-hook.dll into MISE.exe to produce it)" >&2
	return 1 2>/dev/null || exit 1
fi

if [ ! -f "$ASM" ] || [ "$IMG" -nt "$ASM" ]; then
	echo ">> disassembling $IMG -> $ASM (once, ~a few seconds) ..." >&2
	"$OBJDUMP" -D -b binary -mi386 -M intel --adjust-vma=$BASE "$IMG" > "$ASM"
fi

# entryof <hexVA> : VA of the function enclosing <hexVA> — scan up to the first instruction that
# follows a terminator (ret/int3/leave padding). Frame pointers are omitted in the release build, so
# this heuristic (not an EBP walk) is how we find function starts.
entryof() {
	awk -v target="$1" '
		BEGIN { FS="\t"; entry=0; want=1 }
		/^[ ]*[0-9a-f]+:/ {
			a=$1; gsub(/[ :]/,"",a); addr=strtonum("0x" a)
			insn=$3
			is_pad  = (insn ~ /^(int3|nop)/)
			is_term = (insn ~ /^ret/)
			if (want && !is_pad && !is_term && insn !~ /^\.byte/) { entry=addr; want=0 }
			if (addr >= strtonum(target)) { printf "0x%x\n", entry; found=1; exit }
			if (is_term) want=1
		}
		END { if (!found) print "0x0" }' "$ASM"
}

# callers <hexVA> : call sites that target <hexVA> (i.e. who calls this function)
callers() { grep -nE "call +(0x0*)${1#0x}\b" "$ASM"; }

# refs <hexVA> : code addresses that reference <hexVA> as an immediate (string / global xref)
refs() {
	grep -E "0x0*${1#0x}\b" "$ASM" | awk '
		BEGIN { FS="\t" }
		{ a=$1; gsub(/[ :]/,"",a); if (a ~ /^[0-9a-f]+$/) print "0x" a }'
}

report_string() {
	local name="$1" va="$2"
	echo
	echo "## '$name' @ $va"
	local n=0
	for site in $(refs "$va"); do
		echo "   referenced at $site   (enclosing func: $(entryof "$site"))"
		n=$((n+1)); [ "$n" -ge 6 ] && break
	done
	[ "$n" -eq 0 ] && echo "   (no immediate references found — check the VA / that the dump covers .rdata)"
}

echo "=================================================================="
echo " se-file-hook — Phase 2 room-load call graph from $IMG"
echo " (disassembly cached in $ASM; helpers: entryof/callers/refs)"
echo "=================================================================="

# Anchor string VAs (VA = 0x400000 + the RVAs recorded in docs/se-hot-reload-hooks.md).
report_string "classic/%s/%s (classic loader)" 0x4ebfe8
report_string "XXX.lfl (classic room file)"    0x4ec020
report_string "Resource Manager"               0x4daf50
report_string "LoadRoom (HD room load)"        0x4ea7dc
report_string "ScummResources (HD loader)"     0x4eb050
report_string "Async Resources Fiber Manager"  0x4ebfb8

echo
echo "## file wrappers (Phase-1 leaves)"
echo "   _access wrapper     ~0x470fca  -> enclosing func $(entryof 0x470fca)"
echo "   CreateFileA wrapper ~0x475488  -> enclosing func $(entryof 0x475488)"

cat <<'NEXT'

>> Climb the graph:
   1. For the classic loader and the HD LoadRoom funcs above, run:  callers <funcVA>
   2. The function that calls BOTH is the room-entry driver (startScene-equivalent).
      Cross-check: its VA should recur in the Phase-1 'stk:' chains on every file open of one entry.
   3. At the driver's top, the room-number argument is stored to a global: that is _currentRoom.
      Find the interpreter main loop via the LockScummMutex string xref; a cmp/jne on _currentRoom
      right before the call to the driver is the deferred 'requested room' lever (the poke target).
NEXT
