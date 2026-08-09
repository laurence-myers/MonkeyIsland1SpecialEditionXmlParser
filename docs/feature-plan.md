# Feature plan: costume placement, walk box editing, and undo

*August 2026. Drafted from a modder's report (re-drawing all backgrounds and characters) and
verified against the code and against retail game data. Every design below was independently
reviewed against the sources cited; review amendments are folded into the text.*

The three requests:

1. Position costume sprites (the bar's pirate leaders) against the room background,
   pixel-accurately.
2. Edit walking areas with draggable points.
3. Undo in the editor.

---

## 1. Background: the pirate-leaders report

> "I know the pirate leaders are in costume textures but I cant move them like the other
> pirates in the main scene. [...] I also moved them up in costumes but there is no different
> when i reopen the scene."

### 1.1 Root cause — not a save/load bug

The modder edited a field the game ignores.

- The Screen X/Y spinners write `Sprite.ScreenX/ScreenY`
  (`UI/CostumeSpriteSheetEditorForm.cs:785,789`).
- The engine follows the classic renderer: each SE sprite is anchored **bottom-center to the
  classic costume cel it replaces**, so `ScreenX/ScreenY` only matter for sprites with no
  matching classic cel. This is already implemented and documented in
  `Formats/Costumes/Renderer.cs:200-223` (`GetAnchoredScreenRect`; evidence in commit
  `2e88934`: several retail costumes carry ScreenX/Y up to ~30 classic px away from their
  cels, yet render classic-aligned in-game).
- The room preview renders actors through the anchored path
  (`RenderStandingActor`, `Renderer.cs:254-255`), so it correctly showed no movement.
- The costume editor's own preview draws the **raw** `ScreenRect`
  (`UI/CostumePreviewControl.cs:250`), so the same edit visibly moved the sprite there —
  that inconsistency is the trap.
- Save and reload are fine: `Packer` round-trips the fields
  (`Formats/Costumes/Packer.cs:147-148`), and every load path prefers the loose override
  (`Helper.cs:541-566` → `Formats/LPAK/Parser.GetOverrideFilePath`).

### 1.2 Secondary findings (real bugs, fixed by Feature 1 Stage 0)

- `SpriteSheetEditorForm.costumeCache` (`UI/SpriteSheetEditorForm.cs:36`) is never cleared
  for the life of a room window, so an open room view misses costume saves even for fields
  that *do* matter (texture rects). There is no invalidation hook between the costume
  editor's save/import paths and open room forms.
- The only lever that moves a cel-matched sprite in-game is its **texture rect**: the anchor
  pins the rect bottom-center to the classic cel, so growing `TextureHeight` over transparent
  atlas rows below the art shifts the art up; width changes shift it horizontally around the
  center.

---

## 2. Walk boxes vs "walk paths": what the data actually contains

Verified empirically against retail data (all 83 classic rooms and every SE room file in
`Monkey1.pak`, plus the `Tests/Fixtures/028 - bar.dat` fixture, parsed byte-by-byte and
independently re-derived during review).

### 2.1 Classic side: boxes + a routing matrix; paths are computed, never stored

- **BOXD** (parsed today, `Formats/Scumm/Parser.cs:583-616`): `uint16 count`, then fixed
  **20-byte records** — 4 corners (int16 x,y each: UL, UR, LR, LL), mask byte, flags byte,
  scale uint16. Record *i* sits at `payload + 2 + i*20`; a single corner coordinate is one
  int16 at a computable offset. Room 28 "bar" has 12 boxes including the degenerate
  placeholder box 0 at (-32000,-32000).
- **BOXM** (not parsed today): the precomputed box-to-box routing table — "standing in box F,
  heading for box T, the next box to enter is N". Per source box, a run of 3-byte records
  `(destRangeStart, destRangeEnd, nextBox)` terminated by `0xFF`; runs concatenated in box
  order (room 28: 154 bytes, with a trailing `0x00` after the last run — any future
  regenerator must preserve exact chunk size). It stores routing hops, never coordinates.
- **SCAL** (not parsed today): 4 slots × 8 bytes `(scale1, y1, scale2, y2)`; actor size
  interpolation only, zero effect on routing. Box scale values with bit `0x8000` select a
  slot.
- **The actual walk path is derived at runtime**: the click point is snapped into the nearest
  walkable box, BOXM supplies the box sequence, and the engine computes the crossing point on
  each shared edge ("gate") as it walks. **There are no authored polylines or path points
  anywhere in SCUMM v5.**
- **Scripts can mutate box state**: opcode `0x30` matrixOps (decoded by the scanner,
  `Formats/Scumm/ScriptScanner.cs:941-943,1222-1238`) sub-ops set box flags (1), scale (2/3)
  and rebuild the box matrix in memory (4). Games routinely lock boxes during cutscenes.
  **No opcode moves box corners** — geometry edits in BOXD are authoritative; flag/scale
  edits can be overridden at runtime by scripts.

### 2.2 SE side: no walk data exists — nothing "remastered" to override

The previously-unidentified SE room structures were decoded far enough to rule them out:

- **Unknown6** = typed per-room **visual-FX records** (lights/glows). Header bytes 1-4 are a
  type hash, the int32 is a per-type sequence index; bodies decode as a second hash, IEEE
  floats (positions/extents in HD pixels) and **RGBA color bytes** (e.g. room 28's two
  moonlit-window blues). Corpus-wide, each type hash has a fixed record size, and counts
  track visible light/FX density (street lamps, torches, flames), not box counts. The
  room-28 coincidence (11 entries vs 12 classic boxes) dies on the full corpus — e.g.
  room 3 has 8 entries vs 38 boxes, room 58 has 0 vs 31, room 33 has 23 vs 13.
- **Unknown5** = small int32 id/command lists present in only **9** SE rooms, mixing small
  recurring tokens with classic object numbers. Purpose unresolved; confidently not walk
  data (absent from nearly all walkable rooms, far too small).
- The docx's trailing "Unknown4_1" record is just a `RoomObjectSprite` (already parsed,
  `Formats/Rooms/Parser.cs:253-263`) — its first int32 is a back-pointer into the
  texture-name pool. (A few header fields, e.g. `unknown9`, remain undecoded but are
  scalars, not tables.)
- The SE room format therefore carries **no walk boxes, no walk paths, no path points**.
  Walk geometry lives solely in classic `monkey1.001`.

### 2.3 The costume "PathPointList" is not about walking

`Formats/Costumes/Entities/PathPoint.cs` — 12-byte records (type 0-4, flag, padding, float
X/Y relative to the actor origin), referenced by `Sprite.PathPointIndex`: **prop attachment
anchors** (e.g. sword-fight costumes). They already round-trip through the existing SE
costume override pipeline (`Costumes/Parser.cs:134-161`, `Packer.cs:92-107`), so they are
fully editable today if ever needed. They have no relation to rooms or walking.

### 2.4 Conclusions

1. There is **no separate "walk paths" structure** to edit — walking = BOXD boxes + BOXM
   routing, and the polyline is computed at runtime.
2. "Path points" **can** be overridden individually on the classic side: one box corner is a
   single int16 pair inside a fixed-size record — a size-neutral in-place patch. Only
   adding/removing boxes changes sizes (and invalidates BOXM).
3. No SE-side data structure can define walk geometry, so the editor **must** patch classic
   data — which, given `.000`/`.001` cannot be redistributed, requires the end-user patch
   flow in §4.2.

---

## 3. Feature 1 — room-anchored costume editing

**Goals**: make the costume editor show sprites where the game shows them; composite the
costume over the SE room at its classic actor placement; keep open room windows in sync
after costume saves; give an honest lever for moving anchored sprites.

**Rejected alternative**: dragging actors in the *room* preview. Actors are a display-only
overlay (`UI/RoomPreviewControlActor.cs:8-9`), not SE room data — a drag has no field to
write that the engine honors; writing `ScreenX/Y` would recreate the original trap in
reverse.

### Stage 0 — fix the trap and the stale cache (shippable alone)

1. **Anchored drawing in `CostumePreviewControl`**: add `bool AnchorToClassic`; a single
   `GetDrawRect(sprite)` helper returns `Renderer.GetAnchoredScreenRect(...)` when on, raw
   `ScreenRect` when off. Use it in `OnPaint` (:250), the selection outline (:278),
   `HitTest` (:169) and `RefreshContent` bounds (:142).
2. **Form wiring**: new "Game placement" checkbox, default checked when a classic costume
   matched — set the default only on first load, not on Revert (Revert re-runs
   `LoadCostume`). When on and the selected sprite has a classic cel: disable the Screen X/Y
   spinners and explain via a label ("Anchored to classic cel — Screen X/Y is ignored by the
   game"); arrow-key nudges show the same message instead of silently editing the field.
   `UpdateAnimationBounds` (:374-397) unions anchored rects when the toggle is on.
3. **Cross-form cache invalidation**:
   - Clear `costumeCache` **and** `textureCache` at the top of `LoadRoom()` (today it clears
     neither, so Revert keeps stale costume art).
   - Add `SpriteSheetEditorForm.InvalidateCostume(int costumeId)`: evict the costume from
     `costumeCache`, evict its texture paths from `textureCache` (or clear it), snapshot
     each actor overlay's `Visible` flag, `BuildActorOverlays()`, re-apply visibility,
     repaint. (Without the visibility snapshot, every save resets the user's actor
     show/hide choices; without the texture eviction, re-drawn art stays stale.)
   - Call it from the costume editor after `TrySaveOverride` **and after texture imports**
     (`ImportTexturePng` / `ImportAllTexturesPng` bypass `TrySaveOverride` entirely — and
     texture import is the modder's primary workflow). Both forms already keep static
     instance lists; no new event infrastructure.

### Stage 1 — room backdrop in the costume editor

1. **New `Formats/Rooms/BackdropRenderer.cs`** (static, UI-free):
   `Render(Room, ClassicRoom, ClassicActorPlacement, textureLoader)` returns
   `{ Bitmap Below, Bitmap? Above, PointF ActorOriginHd }` using the existing
   `Formats/Rooms/Renderer` statics (transform :154, background :194, foreground :210,
   object sprites :270). Foreground handling must match the room preview: placement on a
   mask-0 box (the leaders) → foreground is appended to `Below` (drawn, under the actor);
   masked box (the storekeeper) → foreground becomes `Above`, drawn over the sprites.
   `ActorOriginHd` uses the same math as `UpdateActorPositions`
   (`UI/SpriteSheetEditorForm.cs:652-654`), including elevation.
2. **`CostumePreviewControl`**: `BackdropBelow`/`BackdropAbove`/`BackdropOffset` properties;
   draw below-image before the sprite loop, above-image after it; union content bounds with
   the backdrop rect (existing pan/zoom already copes with room-sized canvases).
3. **Form wiring**: a "Room" combo listing every classic placement of the open costume
   ("28 - bar: actor 20 @ (204,105) entry script"), populated from
   `classicData.RoomList[*].ActorPlacementList`. Resolve the SE room by matching pak entries
   ending in `.room.xml` whose basename starts with the room number (don't hard-anchor the
   directory — localized `xx.`-prefixed entries exist). Load via the override-preferring
   `LPAKFile.LoadRoom`. Render the backdrop through a **local, disposed-after-render texture
   loader** (room chunk textures are single-use here; don't leak ~12 large bitmaps into the
   costume form's cache). Selecting a room forces "Game placement" on and auto-selects the
   standing animation for the placement's facing; "(none)" clears and disposes.
   Note in the label that the backdrop reflects the *last saved* room.

Scale notes: for 144-line rooms the room transform (1037/144 = 7.201389; ÷1.2 = 6.001157) is
numerically identical to the costume `DefaultHdScale`, so backdrop and sprites compose 1:1.
Fullscreen 200-line rooms render actors proportionally larger than in-game — the same
approximation the room editor's actor overlay already makes.

### Stage 2 — entry point from the room editor

Context-menu item on the actors checklist: "Open costume editor here" → opens the costume
editor with that room + placement pre-selected as backdrop. (Double-click alone fights the
list's `CheckOnClick = true`; if kept as a bonus gesture, re-assert the pre-click check
state.) Disabled when the placement has no resolved costume.

### Stage 3 (optional; gate on the in-game test in §6) — "shift art within anchor" helper

A "Shift art up within anchor…" button that grows `TextureHeight` by N after scanning that
the atlas rows below the rect are fully transparent **and** that the grown rect stays inside
the texture bitmap (GDI+ samples out of range before `SanityChecker` would object). Document
the texture-rect trick in the disabled-spinner explanation regardless.

**Manual tests**: costume 24 over room 28 at several zooms; leaders' sprites jump to the
classic-anchored spot when the toggle turns on; costume save refreshes an open room window
including re-imported textures; storekeeper placement draws the counter over the sprite;
classic data absent → combo disabled with warning, no crashes.

---

## 4. Feature 2 — walk box editing

### 4.1 Editor feature

**Ground truth**: BOXD facts in §2.1. Corner/mask/flags/scale edits are size-neutral;
patch-in-place is the entire write strategy. Add/remove boxes is **deferred** (changes chunk
sizes → LECF/LFLF/ROOM size fixups, LOFF and `.000` directory rewrites, BOXM regeneration).

**Stage 1 — visualization (shippable alone).** "Walk boxes" checkbox in the room editor;
overlay drawn from a per-form working copy (deep-copy `classicRoom.BoxList` in `LoadRoom` —
clone the `Point[]`; the shared `ClassicData` cache must never be mutated by an open form).
Walkable boxes translucent green, non-walkable red/dashed, degenerate line/point boxes as
thick line/cross, index+mask label at the centroid. Classic→client mapping via the existing
`HdScale`/`HdOrigin` (same math as the calibration overlay, `UI/RoomPreviewControl.cs:561-565`),
so scale-field edits reposition boxes live. New `RoomPreviewControlWalkBox` view-model
mirroring the actor/room-object pattern.

**Stage 2 — selection, dragging, attributes, save.**

- **Hit-testing**: initial selection via edge/handle proximity only (a few px); interior
  clicks fall through to sprites unless the box is already selected (then interior drag
  moves the whole box). Boxes tile the entire floor — interior-first hits would make
  sprites unselectable while the overlay is on, which is exactly the modder's working mode.
- **Corner drags**: coincident corners of other boxes move together by default (boxes
  tessellate; keeps shared edges sealed — and keeps BOXM adjacency valid). **Ctrl** detaches
  a single corner (not Alt — releasing Alt activates the WinForms menu bar). Arrow keys
  nudge the selected box (branch must be inserted *before* the no-selection early-return in
  `HandlePreviewKeyDown`). Coordinates round to int classic px.
- **Attribute editors** (mask, walkable, scale — raw uint16, `0x8000` slot bit preserved) in
  a `GroupBox` appended below the existing controls (`panelProperties` is `AutoScroll`;
  don't re-flow its ~30 absolutely-positioned controls). Editing a mask/walkable flag
  recomputes each actor overlay's `DrawAboveForeground` live — instant feedback.
- **Separate dirty state**: "(walk boxes modified)" in the title, its own "Save walk boxes"
  button, and its own prompt wording on close/revert ("walk boxes save to the classic data
  file"). Clear the box selection when the overlay is unchecked (a stale selection would
  keep eating arrow keys).
- **Save = patch-in-place** (`Formats/Scumm/Packer.cs`, new): take the source bytes (loose
  file or pak entry), XOR-decode a **private copy** exactly once, locate the room's BOXD via
  a room iterator **shared with the reader** (`Parser.ReadBlocks`/`ReadRoomOffsets` promoted
  to internal — the reader has a three-way LOFF fallback that a simpler patcher would get
  wrong and silently patch another room), **byte-compare the BOXD records against a snapshot
  of what was originally loaded and refuse on mismatch**, splice the 20-byte records,
  re-encode, write via tmp + delete + move (pattern of `SaveRoomOverrideCommand.cs:56-64`).
  On success, copy the working boxes into the shared `ClassicRoom.BoxList` in place.
- **Write target**: never the pak. Loose-file origin → that file, with a one-time `.bak`.
  Pak-embedded origin → loose pair at `<pakDir>/<entry path>` (the override convention),
  **including the `.000` sibling extracted from the pak** — a loose `.001` without its
  `.000` loses room names and classic costume matching on the next launch.
- **Locator fix (hard prerequisite)**: `ClassicDataLocator.Load` tries pak-embedded first
  (`ClassicDataLocator.cs:102-123`), so the editor would not see its own saved loose file.
  Add a step 0 that prefers a loose file at the pak entry path. Add
  `ClassicDataLocator.Invalidate(pakPath)` for post-save cache eviction.
- **BOXM staleness warning**: on save, list box pairs that shared an edge in the original
  data but no longer touch (routing goes through shared edges; separating them leaves BOXM
  pointing across a gap).

**Stage 3 — deferred**: add/remove boxes + BOXM parse/regeneration (§2.1 gives the layout;
preserve exact chunk size including room 28's trailing `0x00`).

**Manual tests**: 12 boxes over the bar floor; shared-corner drag; Ctrl-drag detaches;
walkable toggle flips the storekeeper in front of/behind the counter; saved file byte-length
unchanged with only BOXD payload bytes differing (`fc /b`); close/reopen shows the moved
corner (proves the locator fix); in-game walk test in both SE and classic modes; `.bak`
restore returns original behavior.

### 4.2 Distribution: the classic-data patch (no `.000`/`.001` redistribution)

Modders cannot ship modified classic files. They ship a small **patch file** instead; end
users apply it with the editor against their own game data.

**Patch file: XML** (`<modname>.mi1classicpatch.xml`) — net481 has no built-in JSON, the app
ships with a single NuGet dependency, and the entire override ecosystem is already XML
(`Helper.cs` XmlSerializer helpers are the house style).

```xml
<ClassicPatch formatVersion="1" toolVersion="...">
  <Info name="..." author="..." notes="..."/>
  <BaseFingerprints>
    <!-- one per pristine variant the modder verified; hash of the encoded on-disk .001
         (XOR is symmetric, so hashing encoded bytes is fine) -->
    <Fingerprint label="Steam EN" dataFileLength="4779713" dataFileSha256="...">
      <Room number="28" boxdSha256="..." boxCount="12"/>  <!-- decoded BOXD payload hash -->
    </Fingerprint>
  </BaseFingerprints>
  <RoomEdits>
    <Room number="28" postBoxdSha256="...">               <!-- idempotency anchor -->
      <Box index="3" x1=".." y1=".." ... mask=".." flags=".." scale=".."/>
    </Room>
  </RoomEdits>
</ClassicPatch>
```

**Copyright stance**: the patch carries one-way hashes plus full 20-byte replacement records
for **edited boxes only** (modder-authored geometry; the few possibly-unchanged bytes inside
an edited record are unavoidable and de minimis). The exporter diffs against pristine and
omits byte-identical records. Never included: unedited records, raw chunk dumps, the files
themselves, `.bak`s.

**Export** (modder side, room editor File menu): pristine source = the `.bak` (or the pak
entry when no loose file predates the edits); diff, emit changed records, compute pristine
and post-apply hashes. The Steam EN fingerprint can be generated from the retail install.
An "Add fingerprint from folder…" sub-flow lets the modder register other verified variants
(GOG/localized); variants whose BOXD payloads differ are warned about and skipped in v1.

**Apply** (end-user side, LPAK form File menu — the patch is pak-level, not node-level):

1. Locate the user's classic data via the existing locator; read the raw encoded bytes;
   remember the origin.
2. Decode a **memory copy**; find each edited room's BOXD range via the shared room iterator.
3. Classify each room's payload by hash: `pristine` / `applied` (matches `postBoxdSha256`) /
   `foreign`. All-applied → "Patch already applied" no-op. All-pristine → proceed. Anything
   else → **refuse, nothing written**, naming the rooms and the supported fingerprint labels
   ("your classic data doesn't match 'Steam EN' — different game version/language, or
   another mod already changed room 28's walkboxes").
4. Splice with full bounds checks: `index < count` **and**
   `payload + 2 + (index+1)*20 <= blockEnd` (a malformed/truncated BOXD must not let a
   high-index splice damage bytes outside the block — the post-hash only covers the block).
   Verify `postBoxdSha256`; any failure aborts with nothing written.
5. Re-encode and commit exactly like the save path: loose target, `.000` sibling extraction
   for pak origin, tmp + delete + move (not atomic — a crash in the gap leaves no file, and
   the pak fallback covers it). **Always write the pristine pair as `.bak` first** when one
   doesn't exist (for pak origin, the `.bak` is the extraction itself — this makes Remove
   unambiguous).
6. Invalidate the locator cache; report the written path with the standing caveat that
   engine pickup of loose classic files needs one in-game verification (§6).

**Remove**: restore `.bak` via tmp+move (or delete the loose pair when the `.bak` hash shows
it equals the pak extraction), warning that this also reverts the user's own local walkbox
edits. Invalidate cache.

**Composability**: per-room pristine hashes make disjoint-room patches from different mods
compose naturally; same-room collisions surface as `foreign` with a clear message. No
ledger/receipt file — hashes are the truth.

**CLI**: ship UI-only in v1 (`Program.cs` ignores args; `BaseCommand` is coupled to
`MainForm`). Requirement: `PatchFile` + `ClassicPatcher` have zero UI references so a future
`--apply-classic-patch <pak> <patch>` verb is a ~20-line `Main` branch.

**Contingency** if the in-game test shows the engine does *not* load loose classic files:
the `classic/en/monkey1.001` entry sits **uncompressed at a fixed offset inside the user's
pak**, and the patch is size-neutral — so the applier can splice the pak entry in place
instead, backing up only that entry's byte range (4.78 MB), never the 1.24 GB pak. Same
patch file, same verification; only the commit step changes.

**New files**: `Formats/Scumm/PatchFile.cs` (XmlSerializer DTOs + Validate),
`Formats/Scumm/ClassicPatcher.cs` (pure byte-level: find ranges, hash, classify, apply),
`Formats/Scumm/Packer.cs` (shared with 4.1), `Commands/ExportClassicPatchCommand.cs`,
`Commands/ApplyClassicPatchCommand.cs`, `Commands/RemoveClassicPatchCommand.cs`. NUnit tests
on synthetic XOR-encoded LECF fixtures (`Tests/ScummParserTests.cs` already builds these —
no copyrighted bytes needed).

**Edge cases**: patch names a room absent from the user's data / BOXD missing / index out of
range → refuse pre-write; loose file `foreign` but `.bak` pristine → offer restore-then-apply;
compressed pak entry → refuse with message; read-only game dir → surface IO error, tmp
cleaned up; re-apply after the user's own later edits → `foreign`, message distinguishes
"already applied" from "locally modified".

---

## 5. Feature 3 — undo/redo

**Recommendation: closure-based command edits, not snapshots.** Snapshots are cheap here
(rooms ~5 KB, costumes ≤ ~23 KB, and the packers round-trip byte-identically) but restoring
one re-parses into *new* objects, and both editor forms key everything on object identity —
tree node tags, hidden-sprite sets, selection, atlas lists. Closure pairs write old/new
values back into the *same* objects, so all UI state survives and the existing refresh
helpers just work.

**Mutation inventory is closed**: every model mutation already calls `MarkDirty()`, and the
16 non-definition call sites (7 costume, 9 room) plus `AtlasViewControl.MoveSelectedRect`
are the complete surface (verified by independent grep). Convention change: `MarkDirty`
becomes private plumbing; all sites call `RecordEdit(edit)` instead, so stragglers are
grep-visible. Note: the paste handlers share refresh helpers that contain the `MarkDirty` —
the record must move into each handler (where old values are readable before the write).

**Infrastructure** (`UI/UndoableEdit.cs` + `UI/UndoStack.cs`, ~180 lines + tests):

- `UndoableEdit { Owner, Key, Description, Undo, Redo, Select, ExtraRefresh, Timestamp }`.
- Coalescing: merge when same `Owner`+`Key` within 750 ms — keep the older edit's `Undo`,
  adopt the newer `Redo`. Extend the merge window while an atlas drag is in progress (a
  >750 ms pause mid-drag must not split the gesture). Break merging after any Undo/Redo and
  across `MarkSaved()`.
- `MarkSaved()` / `IsAtSavedPosition` drive the dirty flag, so undoing back to the save
  point clears "(modified)" truthfully. Cap 200 edits; `Clear()` on load/revert.
- Menus: MDI-merged "Edit" menu with Undo (Ctrl+Z) / Redo (Ctrl+Y), same merge pattern as
  the existing File menus; no `KeyPreview` needed.

**Wiring amendments (from review, all load-bearing)**:

1. The post-undo/redo refresh must call `UpdateNumericEditors()` **unconditionally** — the
   `Select` closure early-returns when the sprite is already selected, and the spinner
   handlers' own refresh lists deliberately omit it; without this, undo reverts the entity
   while the spinner shows the stale value, and the next click silently re-applies the
   undone edit. (It is `suppressUiEvents`-guarded — no re-entrancy.)
2. Texture-retarget edits (`ApplyTextureToTarget`) must capture the affected `TreeNode` at
   record time and refresh *that* node — the current helper writes whatever node is
   selected, and `StaticSprite`/`RoomObjectImageChunk` targets have no node-lookup helper.
   The `StaticSprite` case's `ExtraRefresh` also rebuilds the baked background/foreground
   bitmaps.
3. Change-texture undo (the one structural edit: `GetOrAddTextureIndex` may append to
   `TextureFileNameList`): remove the appended entry only when the record captured that the
   append happened; `ExtraRefresh` = repopulate the combo, clamp its selected index, clear
   `textureCache`, `UpdateAtlas()`.
4. `AtlasViewControl.SpriteRectChanged` grows event args carrying old/new locations
   (currently raised empty).

**Stages**: (1) infrastructure + `UndoStackTests` (in the existing test project);
(2) costume form spinners/nudges/drags; (3) room form same; (4) remaining sites (pastes,
align-to-classic, texture assign/clear/retarget); (5) polish (menu texts from
`Description`, docs note that saves and texture PNG imports are not undoable).

**Out of scope**: undoing disk writes (override saves, texture imports), view-only state
(HD scale fields, visibility, zoom/pan, selection), cross-form undo.

---

## 6. Suggested build order and open verifications

1. **Feature 1 Stage 0** — small, kills the trap, immediately explains the modder's report.
2. **Feature 1 Stages 1–2** — unblocks character placement work.
3. **Undo Stages 1–3** — infrastructure plus the highest-value sites.
4. **Feature 2 (4.1 then 4.2)** — after undo lands, wire box edits through `RecordEdit` from
   day one instead of retrofitting.
5. Optional: Feature 1 Stage 3, Feature 2 Stage 3.

**In-game verifications worth doing early** (each is one session with a hex-edited or
editor-saved file):

- Does the engine load a **loose `classic/en/monkey1.001`** beside the pak? Decides whether
  the patch applier's primary (loose) or contingency (in-pak splice) commit path ships.
- Does a **BOXD corner move** change actor movement in both SE and classic display modes?
  (Expected yes; confirms the whole Feature 2 premise. SE sprite placement is screen-position
  based and should be unaffected.)
- Does the engine honor **ScreenX/Y on a modified override** for cel-matched sprites?
  (Expected no, per commit `2e88934` evidence; decides Feature 1 Stage 3's design.)
- Standing item from earlier work: does the game load loose `.dxt` textures that are not in
  the LPAK index?
