# Monkey Island SE Sprite Editor — developer notes

A Windows editor for the art data of *The Secret of Monkey Island: Special Edition* (`Monkey1.pak`):
room and costume sprite sheets, textures, walk boxes, loose overrides, and a play-test loop that
hot-reloads edits into the running game. End-user documentation ships with the app:
[`Readme.txt`](MonkeyIslandSpecialEditionSpriteEditor/Readme.txt) and
[`Changes.txt`](MonkeyIslandSpecialEditionSpriteEditor/Changes.txt).

**Important:** you need a legally bought copy of Monkey Island Special Edition (Steam or GOG). Never
commit original, unaltered files from the Monkey Island games or any other copyrighted material.

## Repository layout

| Path | What |
|---|---|
| `MonkeyIslandSpecialEditionSpriteEditor/` | the WinForms editor (`MonkeyIslandSpecialEditionSpriteEditor.sln`) |
| `MonkeyIslandSpecialEditionSpriteEditor/PatchCli/` | `mi1se-patch.exe`, a standalone CLI that applies walk-box patches |
| `MonkeyIslandSpecialEditionSpriteEditor/Tests/` | NUnit tests (`dotnet test`) |
| `se-file-hook/` | `mise-mreload.dll` + `se-inject.exe`: the in-game hot-reload hook and its injector (32-bit C) |
| `build.ps1` | release build: version stamp, hook, solution, tests, `MISESE_<date>.zip` |
| `docs/` | design notes: [hot-reload mechanism / MISE.exe analysis](docs/se-hot-reload-hooks.md), [feature plan](docs/feature-plan.md) |
| `Documents/` | notes on the SE room / costume XML file types |
| `.github/workflows/build.yml` | CI: builds, tests and uploads the release zip on every push |

## Required toolset

Everything is Windows-only (WinForms editor on .NET Framework 4.8.1; the game and its hook are 32-bit
Windows binaries).

| Need | For |
|---|---|
| **Visual Studio 2026 (v18) or later** — any edition, with the **.NET desktop development** workload | the editor, CLI and tests: SDK-style projects targeting `net481`, **C# 14** (`LangVersion` 14 in `Directory.Build.props`), so the VS 2026 / **.NET 10 SDK** compiler is required. The workload brings the .NET 10 SDK and the .NET Framework 4.8.1 targeting pack. |
| **Desktop development with C++** workload (same VS) | `se-file-hook`: `se-file-hook\build.ps1` finds the MSVC **x86** toolset with `vswhere` and builds the two tools with `cl`. Only needed to build the hot-reload tools; the editor builds without it. |
| .NET 10 SDK on its own (no VS) | also enough for the managed parts: `dotnet build` / `dotnet test` work from the command line. |
| A copy of the game (Steam or GOG) | running the editor against real data and the play-test loop; not needed to build or run the tests. |

Optional — only for maintaining the hook's addresses, not for building or running anything: the
reverse-engineering substrate under `se-file-hook/re/` (git-ignored) is the DRM-free GOG `MISE.exe`
disassembled with **GNU binutils `objdump`** plus the `re/nav.sh` grep/xref helpers (any bash, e.g.
Git Bash). Regenerate it with `objdump -d -M intel MISE.exe > se-file-hook/re/gog.asm`. The Intel-syntax
flags and the helpers' patterns assume GNU `objdump` output; the easiest way to get it on Windows is an
MSYS2 shell with `pacman -S binutils` (or the `mingw-w64-x86_64-binutils` package). LLVM's
`llvm-objdump` formats its listing differently, so the helpers would need adjusting for it.

## Building

```bash
dotnet build MonkeyIslandSpecialEditionSpriteEditor/MonkeyIslandSpecialEditionSpriteEditor.sln -c Release
```

```bash
dotnet test MonkeyIslandSpecialEditionSpriteEditor/MonkeyIslandSpecialEditionSpriteEditor.sln -c Release
```

Or open the solution in Visual Studio and build/run as usual. Output goes to
`MonkeyIslandSpecialEditionSpriteEditor\bin\<Configuration>\net481\`; the editor keeps its
`UserSettings.xml` next to the exe, so Debug and Release builds have separate settings.

The hot-reload tools (needed for "Test in game" to reload the running game in place):

```bash
powershell -ExecutionPolicy Bypass -File se-file-hook/build.ps1
```

When running from the IDE the editor looks for `se-inject.exe` + `mise-mreload.dll` next to its own
exe, then in a `se-file-hook` folder up the directory tree (i.e. this repo's), then in
`%MISE_HOTRELOAD_DIR%` — so building them once in place is enough during development. Details, usage
and the mechanism: [`se-file-hook/README.md`](se-file-hook/README.md) and
[`docs/se-hot-reload-hooks.md`](docs/se-hot-reload-hooks.md).

## Release build and versioning

```bash
powershell -ExecutionPolicy Bypass -File build.ps1
```

`build.ps1` (Windows PowerShell 5.1 or pwsh) builds the hook, builds and tests the solution, stages
the editor output + `mi1se-patch.exe` + the hot-reload tools + `HotReload-Readme.md` into
`artifacts\MISESE_<yyyy-MM-dd>\`, and zips that folder to `artifacts\MISESE_<yyyy-MM-dd>.zip`.
Switches: `-SkipHook` (package the prebuilt tools already in `se-file-hook\`), `-SkipTests`,
`-Version 2026.08.19.1`, `-Configuration`, `-OutDir`.

**The application version is the build date**, `yyyy.MM.dd.0`, set for every project in
[`Directory.Build.props`](Directory.Build.props) (`AssemblyVersion` / `FileVersion` derive from it; the
About box shows it). There is nothing to bump by hand; the zip's date is the version's date. Pass
`-p:Version=...` (or `build.ps1 -Version`) to pin a different one, e.g. a second release on one day.

CI ([`build.yml`](.github/workflows/build.yml)) runs `build.ps1` on a Windows runner with VS 2026
(the runner's VS supplies the C++ toolset) and uploads `MISESE_<date>.zip` as a build artifact.

## Conventions

- Source files are tab-indented, CRLF, UTF-8 with BOM; match the surrounding style
  (`if( x )`, `this.` qualification).
- Every model edit in the editor must be undoable (recorded on the undo stack), not just marked dirty.
- Do **not** commit game data, `UserSettings.xml`, build outputs (`bin/`, `obj/`, `artifacts/`,
  `se-file-hook/obj/`, the built `.dll`/`.exe`) or the RE dumps under `se-file-hook/re/`; all are
  git-ignored.
