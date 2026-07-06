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
HOW DO I FIX MISALIGNED ROOM SPRITES?
================================

- Right-click a room in the explorer and choose "Open in Spritesheet Editor"
  (or use File > Open in Spritesheet Editor from the room viewer)
- The preview shows the room as the game composites it: each sprite is placed
  at its classic SCUMM object position scaled to HD (x6.0 / x7.2) plus the
  sprite's offset. The classic data is read from the pak itself
  (classic/en/monkey1.000/001) or a loose "classic" folder
- Click a sprite in the atlas view or the preview to select it
  - Arrow keys in the atlas move the sprite's texture rectangle
  - Arrow keys in the preview nudge the sprite's offset
  - Hold Shift for steps of 10; the numeric fields allow exact values
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
