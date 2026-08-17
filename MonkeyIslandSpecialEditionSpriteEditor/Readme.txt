================================
WHERE DO I FIND THE MONKEY ISLAND DATA FILE?
================================

1. Money Island 1 Special Edition via Steam

	You will find the file inside your Steam installation:
	-->	Steam\steamapps\common\the secret of monkey island special edition

2. Other

	Sorry I don't know :(
	
================================
HOW DO I OVERRIDE ASSETS?
================================

- Use the right-click context menu to "Export override XML"
  - The exported file will be saved in the game directory, within `overrides/`
- Open the exported XML file in a text editor
- Modify the XML file as you wish and save it
- Use the right-click context menu to "Apply override"

================================
HOW DO I TEST MY CHANGES IN THE GAME? (play-test loop)
================================

The real Special Edition loads loose override files from its game folder,
and re-reads a room's overrides every time you enter the room. So the loop
is: edit, write the overrides, then in game walk out of the room and back
in - the change appears in the game's own renderer. New textures that are
not in the pak at all are loaded too.

- Game > "Test in game" (F5): writes every unsaved room and costume edit in
  the open editors as overrides (texture imports are written the moment you
  import), then starts the game or switches to it if it is already running.
  In game, leave the room and come back to see the change.
- Game > "Auto-write overrides on edit": every edit is written shortly after
  you make it - nudge a sprite, alt-tab, re-enter the room. No saving needed.
- Game > "Loose overrides...": lists every override file the game is loading
  (edited rooms/costumes, replaced and NEW textures, the XML mirrors), and
  lets you revert any of them - deleting the file restores the pak's version.
  These files change the installed game persistently, so this is where to
  see and undo all of them.

Tip: keep an in-game save inside the room you are working on, so a freshly
launched game is two clicks from the room.

================================
HOW DO I FIX MISALIGNED ROOM SPRITES?
================================

- Right-click a room in the explorer and choose "Open in Spritesheet Editor"
  (or just double-click the room)
- The preview shows the room as the game composites it: each sprite is placed
  at its classic SCUMM object position scaled to HD (x6.0 / x7.2) plus the
  sprite's offset. The classic data is read from the pak itself
  (classic/en/monkey1.000/001) or a loose "classic" folder
- Actors the classic scripts place in the room are drawn over it in their
  standing pose; tick or untick them in the "Actors" list. Each actor's first
  placement (the room's own setup) starts visible, later ones (mostly cutscene
  positions) start hidden
- Click a sprite in the atlas view or the preview to select it
  - Arrow keys in the atlas move the sprite's texture rectangle
  - Arrow keys in the preview nudge the sprite's offset
  - Hold Shift for steps of 10; the numeric fields allow exact values
- Use File > "Save override" to write the loose override binary the game
  loads, plus the overrides XML for hand-editing

================================
HOW DO I FIX MISALIGNED COSTUME SPRITES?
================================

- Right-click a costume in the explorer and choose "Open in Spritesheet
  Editor" (or just double-click the costume)
- Pick an animation; the preview composites its sprites relative to the actor
  origin (the crosshair), exactly as positioned by each sprite's Screen X/Y.
  Use the frame stepper or "Play" to watch the animation
- Enable "Classic overlay" to draw each sprite's classic SCUMM cel rectangle
  (dashed cyan) scaled to HD (x6.0012 / x7.2014); the "Classic:" readout
  shows the delta between the sprite and its classic position
- Click a sprite in the atlas view, the preview or the tree to select it
  - Arrow keys in the atlas move the sprite's texture rectangle
  - Arrow keys in the preview nudge the sprite's screen position
  - Hold Shift for steps of 10; the numeric fields allow exact values
  - "Align to classic position" snaps the sprite to its classic cel position
- Use File > "Save override" to write the loose override binary the game
  loads, plus the overrides XML for hand-editing

================================
HOW DO I REPLACE A SPRITESHEET IMAGE?
================================

- In the Spritesheet Editor choose the texture, then File > "Export texture
  as PNG...", repaint the PNG (keep the same size!), and import it back via
  File > "Import texture PNG..."
- Textures can also be replaced from the explorer: right-click a texture
  under "Textures" and choose "Import texture PNG..."
- The replacement is written as a loose override .dxt file next to the pak;
  delete the file to restore the original
