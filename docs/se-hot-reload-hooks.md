# In-place hot-reload of edited assets in the running Special Edition

*August 2026. Goal: make an edit in the editor show up in the **running** Special Edition immediately,
instead of the modder walking the character out of the room and back in. **This is shipped** —
`se-file-hook/mise-mreload.dll`, auto-injected by the editor's "Test in game" button. This note keeps the
static analysis of `MISE.exe` that got us there (§1–3) and documents the mechanism that was actually
built (§4), which is quite different from — and better than — the original plan.*

## 0. Bottom line (what shipped)

- **Fully in-place, no room change, no interpreter disruption.** The editor writes loose overrides,
  injects `mise-mreload.dll`, and signals a reload. On the game's own render thread the DLL re-parses the
  edited **`.room.xml` / `.costume.xml`** metadata and **LockRect-swaps** the edited **`.dxt`** textures
  in place. Actor state, script state, scroll position, and the camera are untouched. Verified in-game
  for room layout, costume placement, and room/costume textures (on-screen and off-screen chunks).
- **The reload is NOT a room re-entry.** The original plan (below, §4 history) was to drive the engine's
  own room-entry — an interpreter poke or a save/reload. That turned out to be unnecessary: the parsed
  resource can be evicted and re-parsed directly, and textures updated in their existing GPU objects, so
  nothing about the scene/actors is rebuilt from scratch. This avoids the flicker and the crash surface
  of forcing a scene change.
- **Static RE became possible via the GOG build.** The Steam exe's `.text` is DRM-encrypted (nothing to
  disassemble on disk), but the **DRM-free GOG `MISE.exe` is byte-identical** — same build, plaintext
  `.text`, same addresses (no ASLR, base `0x400000`). So the whole thing was reverse-engineered
  statically from the GOG disassembly (`se-file-hook/re/gog.asm`), with read-only runtime probes to
  confirm struct offsets. Injection still targets whichever copy the user runs (Steam or GOG).

## 1. What the binary is (facts from `MISE.exe`)

| Property | Value | Consequence |
|---|---|---|
| Image base | `0x00400000` | Fixed. |
| `DllCharacteristics` | `0x0000` (no ASLR/DEP) | Addresses are identical every run — no slide to compute. |
| Relocations | **stripped** | Loads only at `0x400000`. |
| Entry point | `0x005CC2ED` — inside **`.bind`** | Steam builds start in the SteamStub DRM wrapper, which decrypts `.text` and jumps to the real OEP. |
| `.text` | `0x401000`, 888 KB | **Encrypted on disk in the Steam build** (garbage to a disassembler). **Plaintext in the GOG build** — same bytes once decrypted. |
| `.rdata` / `.data` / `.rsrc` | plaintext | Strings, imports, format strings, RTTI names — the anchors for RE. |

**The GOG unblock.** The user owns the GOG copy (`MISE.exe`, no `.bind` section, plaintext `.text`),
verified byte-identical to Steam: identical IAT slots and identical anchor-string VAs. So static RE is
done on the GOG exe (`objdump -d -M intel` → `se-file-hook/re/gog.asm`, git-ignored; helpers in
`re/nav.sh`), and every address is valid in the running Steam game too. The code is heavy C++ with vtable
/ function-pointer indirection (namespace `lec`); call-graph climbing anchors on data (globals, strings)
and the SCUMM v5 structure. Runtime struct offsets were confirmed with read-only probes (static RE was
repeatedly wrong on layout), never trusted blind.

## 2. The file surface — one choke point, intact imports

SteamStub left the imports readable and unbound, so the IAT holds real kernel32 addresses at runtime. The
game opens **every** file through `kernel32!CreateFileA` (game IAT slot `0x4DA030`) and probes for loose
overrides with `msvcr80!_access` (`0x4DA178`) — verified: no `fopen`/`_open`/`CreateFileW`, so those two
slots are the *complete* file surface. Injection uses a plain `CreateRemoteThread(LoadLibraryA)`
injector (`se-inject.exe`); `IsDebuggerPresent` (`0x4DA07C`) is a retail anti-debug check that does not
block DLL injection.

## 3. The resource architecture (two loaders, both room-keyed)

- **Classic SCUMM side:** the original interpreter's `Resource Manager` (`classic/%s/%s`, `XXX.lfl`)
  reading `classic/<lang>/monkey1.000/001`.
- **HD side:** an `Async Resources Fiber Manager` streaming HD assets — `LoadRoom`, `ScummResources`,
  `Bundle`, scene-node types (`GlobalsNode`, `ObjectNode`, `AnimationNode`, …) via `lec::FileStream` /
  `lec::CompressedStream`, from `Monkey1.pak` or the loose `art/...` tree.

A room change fans out into both a classic room-block load and an HD `LoadRoom`; the loose-override
reload the play-test loop relies on is these paths re-probing on each entry. **All the editor's edits are
HD-side** (`art/rooms/*.room.xml`, `*.dxt`, `art/costumes/*`), so hot-reload is entirely about the HD
resource system — which is what §4 drives directly, without a room change.

## 4. Implementation (shipped) — `mise-mreload.dll`

Reverse-engineered from `re/gog.asm`; all offsets below are for this exact build. Everything runs **on
the render thread** by inline-hooking the HD orchestrator `0x44c7be` (which owns the resource system), so
the reload never races the engine. A read-only diagnostic dump is on **F12**; the reload is on **F11** or
the editor's event.

### 4.1 Metadata (`.room.xml`, `.costume.xml`) — evict + synchronous re-parse

The scene builder `0x4484e0` never parses — it rebuilds draw items from an **already-parsed node-list**.
That parsed node-list is the resource's loaded data, cached at **`[handle+0x4]`** on the per-name
resource handle held by the resource manager `*(0x5b98e4)`; `[handle+0xc]` is the loader factory. The
resource fetch `resource_get 0x48c760` gates on `[handle+0x4]` (nonzero → return cached).

So a re-parse is: zero `[handle+0x4]`, then **drive the fetch synchronously** —
`resource_get(0x48c760)(resmgr, handle, 0xffffffff /*=-1*/, 0, 0)`. Flag `-1` takes the synchronous
branch that re-reads the edited file and stores the fresh parse back at `[handle+0x4]` (`0x48c907`); any
other flag takes the **async** branch that enqueues and does *not* restore `[handle+0x4]`. The game's own
refresh paths pass `0xc0000000` (async), which is exactly why an earlier "evict and let the game
reload it" attempt left the resource unloaded and crashed — we must drive the sync path ourselves, all in
one tick so no frame ever observes `[handle+0x4] == 0`.

Then rebuild:
- **Room:** empty the node-view slot `[HDobj+0x984]` so the orchestrator re-resolves the room by name and
  rebuilds via `0x4484e0` from the fresh parse. The `.room.xml` handle is reachable robustly at
  `*(HDobj+0x984)` (so this works even if injected mid-room).
- **Costume:** no nudge — the per-frame scene builder `0x453590 → 0x460be0` re-resolves each actor's
  costume every frame and rebuilds its cels from the fresh `[handle+0x4]`.

Handles are captured by hooking `resource_get 0x48c760` (the name is a `char*` at `handle+0x18`).

### 4.2 Textures (`.dxt`) — in-place LockRect

Each room/costume chunk is exactly **one shared `D3DPOOL_MANAGED` `IDirect3DTexture9`**, so overwriting
its DXT blocks with `LockRect`/memcpy/`UnlockRect` (Flags = 0, on the render thread between frames)
updates **every** binding of that texture — on-screen *and* off-screen scrolling-room chunks — with no
rebuild. This mirrors what the game's own loader does to populate the texture, so it is maximally
compatible.

The texture is **not** reachable from the `.dxt` handle by any fixed offset (it lives in a device-side,
name-keyed draw cache). The reliable `.dxt → texture` map is behavioural: hook
`IDirect3DDevice9::CreateTexture` (device `*(0x5b9920)`, vtable `+0x5c`; only `pool == 1` DXT5/DXT1), and
tag each created texture with the name of the `resource_get` handle whose load is in progress — the
texture is created *inside* that call, so the correlation is exact (this replaced a fragile
"last CreateFileA" guess that produced an empty map). The swap validates fourCC + width + height and
honours the locked `Pitch` (chunks are power-of-two padded).

### 4.3 Trigger + workflow

The reload fires on the named auto-reset event `Local\MISE_HotReload` (the editor's `HotReloadClient`
sets it) or **F11**; **F12** dumps a read-only diagnostic (handle state, thread check, texture-map
counts). The editor's **Test in game** (`TestInGameCommand`) writes the overrides, auto-injects the DLL
(`HotReloadInjector` → `se-inject.exe`, once per session), and signals — so it is one button.

Because handles + the texture map fill only as resources stream in, the one-time setup is: **inject at
the menu, enter the room, and scroll it once.** After that, edit → reload, repeatedly, with no re-entry.

### 4.4 What was rejected (history)

The original plan here was to re-run the engine's room entry — an **interpreter poke** (set the pending
room + transition flag under `LockScummMutex`) or a **save/reload** — because an out-of-band scene change
had once crashed the engine. A working **`startScene` bounce** (leave to a scratch room and back on the
SCUMM thread) was even shipped as an interim (`reload.c`, now removed). All of these are room re-entries:
they reload everything but re-run entry scripts and flash. The in-place re-parse + LockRect approach in
§4.1–4.2 supersedes them — it refreshes exactly the edited resources with no scene rebuild, so it needs
neither a room change nor the interpreter. Also removed: the Phase-1 recon hook and the diagnostic probes
that mapped all of the above; their findings are baked into `mise-mreload.dll` and the memory notes.

## 5. Status & fit

Shipped and verified in-game (2026-08-19): `.room.xml`, `.costume.xml`, room `.dxt`, and costume `.dxt`
all hot-reload in place from the editor's Test-in-game button. This is the real-SE counterpart to the
in-process ScummVM "Show this room" jump (`live-preview-status-2026-08`) — same goal (see the edit live in
the real renderer) but without a scene change: we refresh the SE's own HD resources on its own render
thread rather than forcing a re-entry through a second engine.

**Caveats.** Windows-only, 32-bit; the addresses are for this exact 2009 build (Steam/GOG), and the DLL's
signature guard aborts cleanly on any other build. A native crash in the injected hook takes the game
down (not the editor — separate process). Purely a modding aid for the user's own installed game.
Build + usage: `se-file-hook/README.md`. Full RE trail: the `se-file-hook-analysis` memory note.
