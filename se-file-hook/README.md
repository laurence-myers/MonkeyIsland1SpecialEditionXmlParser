# MI1 Special Edition — in-place hot-reload

Makes edited assets appear **immediately** in the running Monkey Island 1: Special Edition, with no room
change and no interruption to the game script — instead of the modder walking the character out of the
room and back in. Reloads all three edit classes in place:

- **`.room.xml`** — room layout / object placement
- **`.costume.xml`** — costume cel placement
- **`.dxt`** — room-background and costume textures (on-screen *and* off-screen scrolling-room chunks)

Two files, both 32-bit to match the game:

| File | What |
|---|---|
| `mise-mreload.dll` | the hot-reload hook (injected into the game) |
| `se-inject.exe` | `CreateRemoteThread(LoadLibraryA)` injector |

Build: `.\se-file-hook\build.ps1` (PowerShell). It needs Visual Studio with the "Desktop development with C++"
workload - the same VS that builds the editor; the toolset is located with vswhere, nothing else to install.
The repo-root `build.ps1` runs it as part of a release build and packages both files next to the editor.

## Use it from the editor (automatic)

The editor's **Test in game** button (`TestInGameCommand`) writes the loose overrides, auto-injects
`mise-mreload.dll` into the running game (via `HotReloadInjector`, which shells out to `se-inject.exe`),
and signals the reload — no manual injection. The editor finds the two binaries next to itself, in a
`se-file-hook` folder up the tree, or wherever `MISE_HOTRELOAD_DIR` points.

Workflow: launch the game, get to the **menu**, click **Test in game** once (injects the DLL), then enter
the room and **scroll it once** (this is what streams every chunk's texture so it can be mapped). From
then on, edit → **Test in game** → the change appears instantly, repeatedly.

## Use it by hand

```sh
# 1. launch the game, at the MENU inject the hook:
./se-inject.exe mise-mreload.dll MISE.exe
# 2. enter the room and scroll it fully (captures the resource handles + texture map)
# 3. edit assets + save the loose overrides, then reload:
#    press F11 in the game   (F12 = read-only diagnostic dump)
```

The editor and F11 use the same named auto-reset event `Local\MISE_HotReload`. Diagnostics are written
to `mise-mreload.log` next to the DLL. It refuses to patch anything but this exact build (image base +
two code signatures).

## How it works

Reverse-engineered from the DRM-free GOG build, which is byte-identical to Steam (same addresses, no
ASLR, base `0x400000`). Everything runs on the render thread by hooking the HD orchestrator `0x44c7be`,
so the reload never races the engine:

- **Metadata (`.xml`).** The parsed node-list is cached at `[handle+0x4]` on the per-name resource handle
  in the resource manager `*(0x5b98e4)`. The game's own refresh is asynchronous and never restores that
  pointer (which orphaned earlier attempts), so we drive a **synchronous** re-parse ourselves:
  `resource_get(0x48c760)(resmgr, handle, -1, 0, 0)` re-reads the edited file and stores the fresh parse
  at `[handle+0x4]` — all in one tick so no frame ever sees it half-loaded. The room then nudges a
  rebuild by emptying the node-view slot `[HDobj+0x984]`; costumes rebuild via the per-frame scene builder.
- **Textures (`.dxt`).** Each chunk is one shared `D3DPOOL_MANAGED` `IDirect3DTexture9`, so we **LockRect
  its blocks in place** (Flags=0, between frames) — every binding, visible or off-screen, updates with no
  rebuild. The `.dxt → texture` map is built by hooking `CreateTexture` and tagging each texture with the
  name of the `resource_get` handle whose load is in progress (the texture is created inside that call).

Handles + the texture map fill only as resources load, so inject at the menu and scroll the room once.

## Notes / limitations

- **Windows-only, 32-bit throughout.** Build-specific: the addresses are for this exact `MISE.exe`
  (Steam/GOG 2009 build, base `0x400000`, no ASLR); the signature guard aborts cleanly on any other build.
- If the game runs elevated, run the injector (or the editor) elevated too, or `OpenProcess` fails.
- `re/` holds the reverse-engineering substrate used to derive all of the above: `gog.asm` (the GOG exe
  disassembled with `objdump -d -M intel`, git-ignored — regenerate from the GOG `MISE.exe`) and
  `nav.sh` (grep/xref helpers). Not needed to build or run; kept for maintenance.
