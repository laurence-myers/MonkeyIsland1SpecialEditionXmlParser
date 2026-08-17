# Hooking the SE runtime for on-demand loose-file reload

*August 2026. Question: can we make an edit in the editor show up in the **running** Special
Edition immediately, instead of the modder walking out of the room and back in? The play-test
loop today writes loose overrides and relies on the verified fact that the SE re-reads them on
room re-entry (`se-loose-override-loading-verified`). This note is the static analysis of
`MISE.exe` that decides whether a DLL hook can (a) confirm **which** functions load those files
and **when**, and (b) **trigger** that reload on demand. All facts below come from reading the
on-disk `MISE.exe` with `objdump`/`strings` — no code was run or patched.*

Binary analysed: `F:\Games\Steam\steamapps\common\The Secret of Monkey Island Special Edition\MISE.exe`
(1,359,872 bytes, PE32 i386, linked 2009-07-08, MSVC 8.0 / 2005 runtime).

## 0. Bottom line

- **The hypothesis is right.** Room entry drives the loads. The SE carries a **classic SCUMM
  Resource Manager** (`classic/%s/%s`, `XXX.lfl`) *and* an **"Async Resources Fiber Manager"**
  that runs a `LoadRoom` pulling `ScummResources` / `Bundle` / HD textures. Both are keyed by
  room, and both re-probe loose files each entry — which is exactly the reload we already
  observed.
- **Reconnaissance is cheap and the user's instinct is correct** — a file hook is not just
  convenient, it is *required*, because the game's code section is encrypted on disk (Steam DRM)
  so there is nothing to disassemble statically. But the hook target is a gift: **no ASLR,
  relocations stripped, image pinned at `0x400000`, and a fully intact import table.** All file
  I/O goes through `kernel32!CreateFileA` (game IAT slot at the fixed VA **`0x4DA030`**), so one
  inline hook logs every path and, via caller return addresses, points straight at the room-load
  code inside the decrypted `.text`.
- **"Immediate" is the hard half.** Serving edited bytes through the hook only changes what the
  *next* load sees; the room's assets are already cached in memory, so a true in-place refresh
  means making the loader run again for the current room. That function must be located at
  runtime (RE on a memory dump — the recon hook localises it first). Lower-effort substitutes
  exist (drive a room re-enter, or a programmatic quick-load) if full RE is not worth it.

## 1. What the binary is (facts from `MISE.exe`)

| Property | Value | Consequence |
|---|---|---|
| Image base | `0x00400000` | Fixed. |
| `DllCharacteristics` | `0x0000` (no ASLR/DEP) | Addresses are identical every run — no slide to compute. |
| Relocations | **stripped** | Can only load at `0x400000`; reinforces the above. |
| Entry point | `0x005CC2ED` — inside **`.bind`** | Execution starts in the Steam DRM stub, which decrypts `.text` and jumps to the real OEP. |
| `.text` | `0x401000`, 888 KB | **Encrypted on disk** — disassembles to garbage (`js`/`outsb`/`repnz mov` with random immediates). No static code RE possible. |
| `.rdata` / `.data` / `.rsrc` | plaintext | Strings, imports, format strings, the few RTTI names are all readable (this is where every fact here comes from). |
| `.bind` | 344 KB, CODE+DATA | SteamStub wrapper. Anti-tamper protects the *file*; it does not stop DLL injection into the *running* process. |

Corollary: **the on-disk file cannot be usefully reverse-engineered.** Everything code-level has
to be done on a dump taken *after* the stub decrypts `.text` (i.e. once the game is at the menu),
or live via an injected hook. This is the concrete reason the "DLL intercepting hook" is the
right first tool.

## 2. The import table is intact and unbound — the file hook is trivial to target

SteamStub left the imports readable and unbound (`Bound-To` empty, Bound Import Directory = 0),
so the Windows loader resolves them normally at startup and the IAT holds real kernel32 addresses
at runtime. The file surface (RVA in the file; VA = base + RVA at the fixed `0x400000`):

| Function | Module | IAT VA | Role |
|---|---|---|---|
| `CreateFileA` | KERNEL32 | **`0x4DA030`** | **The choke point — every file open.** |
| `GetFileSize` | KERNEL32 | `0x4DA038` | pak-index / asset sizing |
| `ReadFile` | KERNEL32 | `0x4DA03C` | bulk reads |
| `SetFilePointer` | KERNEL32 | `0x4DA0E4` | pak seek-to-entry |
| `CloseHandle` | KERNEL32 | `0x4DA034` | |
| `_access` | MSVCR80 | `0x4DA178` | **loose-file existence probe** (the "is there an override?" test) |
| `fread` | MSVCR80 | `0x4DA238` | CRT read path |

Notes that shape the hook design:
- **No `LoadLibrary`/`GetProcAddress` in the game's imports** — the game never resolves APIs
  dynamically, so nothing escapes an import-level view. (`GetModuleHandleA` is present but only
  returns a base.)
- **The game has no `fopen`/`_open`/`CreateFileW` — it opens every file through `CreateFileA` and
  probes overrides with `_access`.** (`fread` is imported but there is no CRT `fopen` path.) So
  patching the game's own two IAT slots (`0x4DA030`, `0x4DA178`) is the *complete* view of its file
  access — there is no CRT-open route bypassing the IAT to worry about. This is what Phase 1
  implements (`se-file-hook/`): a name-based IAT walk that repoints those two slots, so every caller
  offset it logs lands in the game's own decrypted `.text` — exactly the Phase 2 seed we want.
- **IAT hooking's one caveat: caching.** An IAT patch only affects calls that *re-read* the slot
  after the patch. Code that hoisted the imported pointer into a register/global beforehand keeps
  calling the original. This is why we inject at the menu — the room-loader runs *later* and re-reads
  the patched slot when it runs. If some room-entry loads still turn up missing, the escape hatch is
  an **inline hook** on `kernel32!CreateFileA` (patch the function body, MinHook/Detours), which is
  caching-proof because it intercepts wherever execution lands, not which pointer you called through.
- `IsDebuggerPresent` is imported (`0x4DA07C`) — a retail anti-debug check. Injection is not
  debugging, so it does not block a `LoadLibrary` injector; only attaching a debugger would trip
  it. [twevs/MISEPatcher](https://github.com/twevs/MISEPatcher) already patches this exact game
  at runtime, proving injection works here.

## 3. The resource architecture (from `.rdata` strings) — two loaders, both room-keyed

Readable strings map the whole loading system:

- **Classic SCUMM side:** `Resource Manager` (`0xDAF50`), `classic/%s/%s` (`0xEBFE8`),
  `XXX.lfl` (`0xEC020`). This is the original interpreter's resource manager reading
  `classic/<lang>/monkey1.000/001`. Loose classic overrides live under `classic/`.
- **HD side:** **`Async Resources Fiber Manager`** (`0xEBFB8`) — HD assets stream in on a
  dedicated fiber (this is why the exe imports `ConvertThreadToFiber`/`CreateFiber`/
  `SwitchToFiber`). `LoadRoom` (`0xEA7DC`), `ScummResources` (`0xEB050`), `Bundle`/`bundle`,
  `monkey1_retail.scumm.xml`, and the scene-node types (`GlobalsNode`, `ObjectNode`,
  `PointLightNode`, `AnimationNode`, …). Reads go through `lec::FileStream` /
  `lec::CompressedStream` (the only RTTI descriptor in the file confirms the `lec` namespace and
  per-chunk compression), pulling from `Monkey1.pak` or the loose `art/...` tree.
- Both are driven by the room number, so a room change fans out into a classic room-block load
  **and** an HD `LoadRoom`. The loose-override reload we verified is these two paths re-running
  their `_access`/`CreateFileA` probes on each entry. No built-in file-watch or hot-reload string
  exists — nothing like `watch`/`reload`/`refresh` for assets — so the game will not reload on
  its own; we have to make a load happen.

## 4. Plan

### Phase 1 — Recon hook — **BUILT** (`se-file-hook/`)

A small injected DLL that IAT-hooks the game's `CreateFileA` + `_access` slots and logs, per call:
the path, the caller's return address (as `MISE.exe+0xNNNNN`), the thread id, and a timestamp.
Injected with a `CreateRemoteThread(LoadLibrary)` injector once the game is at the menu (so `.text`
is decrypted). Built 32-bit (MSYS2 i686); the DLL depends only on system DLLs. Validated headlessly
via a stand-in target (`se-selftest.exe`): the self-load path captures `CreateFileA`/`_access` with
caller offsets, and real cross-process injection captures a moving target's post-injection calls.
See `se-file-hook/README.md` to build and run it. Left as a hook and injector rather than an inline
hook because the game has no CRT-open path (so the two IAT slots are the complete file surface); the
inline hook is the documented escape hatch if caching hides any loads.

What it settles, in one afternoon:
1. **Proves the hypothesis** — walk out of a room and back in, watch the exact set of files the SE
   opens (pak seeks vs. loose `art/…` / `classic/…` reads) and confirm it happens on entry.
2. **Localises the loader** — the logged return addresses are addresses *inside the decrypted
   `.text`*. Grouped by room-entry, they are the call sites of the room-load / async-resource
   code — the seed for Phase 2 with zero blind searching.
3. **Independently upgrades the play-test loop** even if we stop here: the editor could watch the
   hook's log to *detect* when the SE has reloaded and report "change is now live" instead of
   telling the modder to guess.

This is also the natural home for a **file-redirect** later (serve the editor's unsaved bytes
straight from memory, so the modder need not even write loose files) — but note redirect alone
does **not** make edits immediate; it only changes what the *next* load reads.

### Phase 2 — Trigger the reload on demand

The room's assets are cached in memory after the first load, so "immediate" means re-running the
load for the *current* room. **What Phase 1 established in-game (room 28 log):** every open funnels
through one `_access` probe wrapper (`+0x70fca`) and one `CreateFileA` wrapper (`+0x75488`); the
resolution order is `art\rooms\<lang>.<name>.room.xml` → `art\rooms\<name>.room.xml` → `monkey1.pak`;
and **one room entry drives both loaders** (the classic `classic\en\monkey1.001`/pak block *and* the
HD DXT/costume stream) on a single thread. So the reload lever is the room-entry itself.

**Design (adversarially reviewed).** The one hard runtime lesson on record is that forcing a scene
change out-of-band crashed the engine once (ScummVM `error()`→`exit`). That dominates the choice: use
the engine's *own* in-band room-entry machinery, on the interpreter's *own* thread, at a safe point.

- **PRIMARY — interpreter-poke.** Set the classic interpreter's "requested/pending room" to the room
  it is already in and raise the transition flag, then let the interpreter run its own
  `startScene`-equivalent on its own thread. One re-entry fans out to *both* loaders exactly like a
  doorway does — the reload we verified — while keeping live actor/game state, no menu, no flicker.
  Crash-safety: do what the engine does — take `LockScummMutex`, write the state, let the main loop
  consume it at its top-of-frame boundary; never call into a loader mid-load. If the build has no
  deferred pending-room flag (room changes are opcode-only), escalate *within* this approach: call
  the `startScene`-equivalent directly but marshalled onto the interpreter thread under the mutex.
  (Calling the HD `LoadRoom` in isolation is the trap — it likely runs on the resources fiber and
  would refresh only the HD half, leaving the classic block stale/racing.)
- **FALLBACK — save-reload.** If the pending-room lever doesn't cleanly re-fan to the HD side, use the
  engine's most crash-safe first-class op: auto-save to an editor-owned slot, then load it (loading
  re-runs `startScene`). Works in every room, restores actor state, but costs a visible load
  transition and needs a save/load trigger (an RE'd callable entry, or synthesised menu input).
- **Not chosen:** standalone direct-call of a loader (most RE, partial-reload hazard); input
  synthesis of walk-out/walk-in (unreliable, room-geometry dependent, drags the actor off-spot) —
  manual last resort only.

**First-pass RE (automated by `se-file-hook/analyze-image.sh` over `MISE.image.bin`):** disassemble
the decrypted dump; xref the anchor strings to pin the two loaders (`classic/%s/%s` @ `0x4EBFE8` →
classic path builder; `LoadRoom` @ `0x4EA7DC` / `ScummResources` @ `0x4EB050` → HD loader); confirm
the `+0x70fca`/`+0x75488` wrappers and their loose-override callers; the function that calls **both**
loaders — and that recurs in the Phase-1 `stk:` chains for one entry — is the room-entry driver; the
room number it stores on entry is `_currentRoom`; a `cmp`/`jne` on `_currentRoom` at the top of the
interpreter loop (found via the `LockScummMutex` string xref) is the deferred poke target, if one
exists.

### Risks / caveats

- A native crash in an injected hook takes the game down (not the editor — separate process,
  which is *better* isolation than the in-process ScummVM route). Anti-tamper is on the file, not
  on in-memory injection, but keep the hook minimal and reversible.
- This is Windows-only and tied to this exact build; the fixed addresses (`0x4DA030` etc.) are a
  build-specific convenience and would need re-deriving if Steam reships MISE.exe. The kernel32
  export hook and the string anchors are build-independent.
- Purely a modding aid for the user's own installed game — read-then-serve of their own files; it
  ships nothing from the game.

## 5. How this fits the existing work

This is the real-SE counterpart to the demoted in-process ScummVM "Show this room" jump
(`live-preview-status-2026-08`): same idea (re-enter the current room to reload), but in the SE's
own HD renderer instead of ScummVM's classic framebuffer, and without the mid-cutscene
`startScene` crash that sank the ScummVM jump — because we would be poking the SE's *own*
interpreter with its own mutex, not forcing a scene change through a second engine. It upgrades
the play-test loop (`se-loose-override-loading-verified`) from "walk out and back in (~5 s)" to
"the editor triggers the reload," keeping the real renderer as ground truth.
