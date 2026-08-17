# se-file-hook — Phase 1 file reconnaissance for MI1 Special Edition

A tiny injected DLL that records **which files `MISE.exe` opens, when, and from where in its code**,
so we can confirm the room-entry reload mechanism and locate the room-load routine for Phase 2
(making an edit reload on demand). Background and the full plan: `docs/se-hot-reload-hooks.md`.

It is a read-only observer: it forwards every call unchanged and only logs it. It ships nothing from
the game and modifies nothing on disk.

## How it works

`MISE.exe` opens every file through `kernel32!CreateFileA` and probes for loose overrides with
`msvcr80!_access` — the whole file surface is just those two (verified: no `fopen`/`CreateFileW`).
Both are ordinary IAT imports (`CreateFileA` at `MISE.exe+0xDA030`, `_access` at `+0xDA178`; the
image is pinned at `0x400000`, no ASLR). The DLL walks the host module's import table, finds those
two thunks **by name**, and repoints the IAT slots at its own functions. Each call is logged with the
caller's return address as a module-relative offset (`MISE.exe+0xNNNNN`) — that offset lands inside
the runtime-decrypted `.text` and is the seed for the Phase 2 reverse-engineering.

The `_access` log is the prize: it shows every loose-override path the game *probes* on room entry —
including files that don't exist yet — i.e. the game telling you the exact filename that overrides
each asset.

## Files

| File | What |
|---|---|
| `hook.c` | the injected DLL (`se-file-hook.dll`) |
| `inject.c` | the injector (`se-inject.exe`) — `CreateRemoteThread(LoadLibraryA)` |
| `selftest.c` | `se-selftest.exe` — a headless stand-in for `MISE.exe` to validate the tool |
| `build.sh` | builds all three (32-bit) |

## Build

Needs the MSYS2 i686 (32-bit) toolchain — the game is 32-bit, so the DLL must be too:

```sh
pacman -S --needed mingw-w64-i686-gcc      # one-time, in MSYS2
bash build.sh                              # re-execs itself into the MSYS2 MINGW32 env
```

Outputs `se-file-hook.dll`, `se-inject.exe`, `se-selftest.exe`. The DLL depends only on system DLLs
(KERNEL32/USER32/msvcrt) — copy it and the injector wherever you like.

### Validate without the game

```sh
./se-selftest.exe            # loads the DLL into itself, makes a few file calls
cat se-file-hook.log         # should list the CreateFileA/_access calls with caller offsets
```

## Run against the real game

1. Launch the SE and get it to the **main menu** (so the Steam DRM stub has decrypted `.text` and the
   game is actually issuing file calls). Windowed helps: `%APPDATA%\LucasArts\The Secret of Monkey
   Island Special Edition\Settings.ini` → `[display] windowed=1`.
2. Inject (no admin needed if the game runs at your integrity level):
   ```sh
   ./se-inject.exe se-file-hook.dll MISE.exe
   ```
   It prints `ok: injected ... module=0x........`. The log is `se-file-hook.log` next to the DLL.
3. In the game, **walk out of a room and back in** (or load an in-room save). Then read the log.

If the game is elevated (launched by an elevated Steam), run the injector elevated too.

## Reading the log

```
000042 14:03:11.887 tid=1a2c MISE.exe+0x07be14 _access mode=0 "art/rooms/room28.room.xml"
000043 14:03:11.889 tid=1a2c MISE.exe+0x07be9a CreateFileA "F:\...\Monkey1.pak"
```

- `MISE.exe+0xNNNNN` is the **caller** — load `MISE.exe` at base `0x400000` in a disassembler on a
  full memory dump (the on-disk `.text` is encrypted; dump the running process) and go to that offset
  to find the room-load / resource code. Group the offsets that appear on a room re-entry: those are
  Phase 2's targets.
- `_access` lines are the loose-override probe paths (what the game looks for, existing or not).
- `CreateFileA` lines are the actual opens (loose hits + the pak).

## Limitations

- **IAT hooking only catches calls that read the IAT slot *after* injection.** If a piece of code
  cached the imported pointer in a register/global before we patched, those specific calls are
  missed. This is why we inject at the menu — the room-loader runs *later* and re-reads the patched
  slot. (The self-test is built `-O0` precisely because at `-O2` its startup hoists the pointers into
  registers and a later patch never sees the loop — the worst case; the real loader isn't shaped like
  that.) If in practice some room-entry loads are missing, the escape hatch is an **inline hook** on
  `kernel32!CreateFileA` (patch the function body via MinHook/Detours) which intercepts every caller
  regardless of caching — more machinery, but caching-proof.
- **Build-specific offsets.** The two IAT slots and every logged offset are for this exact `MISE.exe`
  (Steam, 2009-07-08, `0x400000`, no ASLR). If Steam reships the exe, the slots are found by name
  anyway, but the reported offsets must be re-mapped. To re-derive the slot RVAs:
  `objdump -p MISE.exe | grep -E "CreateFileA|_access"`.
- Windows-only; 32-bit throughout.

## Reverting

The DLL restores the original IAT pointers on unload and the game frees it at exit; nothing persists.
To stop observing, just close the game. Delete `se-file-hook.log` to clear captured data.
