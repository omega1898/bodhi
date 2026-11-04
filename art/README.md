# Art

All the game's art, as editable vector files (SVG). Open any file in Figma, Adobe Illustrator or Inkscape, which is free.

The game reads these files directly. For an engine that needs plain picture files (PNG), they can be exported from these at any size, sharp on every screen.

## Style

- **Painted vector illustration:** soft gradients, a fine paper grain, warm key light from the front, cool rim light from behind.
- **Silhouettes first:** both characters must read at a glance, even small and in the dark.
- **The dog's coat:** made by two filters.
  - "fluffy" pushes every edge in and out, so outlines read as curly fur.
  - "curls" lays a soft curl texture over the colour.

## Folders

| Folder | What's in it |
|---|---|
| `characters/` | `her.svg` and `bodhi.svg`: each character painted as separate body parts for cut-out animation |
| `scenery/` | Background paintings, one file per depth layer, in 1600 × 800 tiles that repeat sideways |
| `tools/` | `build_atlas.js`: measures the character parts and writes the map the game uses |

## How the character parts work

- **Part names:** each part is a group named `part_something`, for example `part_thigh` or `part_ear`.
- **Joints:** the group's origin (where its `translate(x, y)` puts it) is the joint the part turns around. Limbs hang straight down from their joint.
- **Scale:** 10 drawing units = 1 game unit. Her thigh is 230 units long because her thigh in the game is 23.
- **The far side:** the game draws the far-side arm and leg from the same parts, darkened.
- **Spacing:** keep at least 60 units of empty space around every part. The tool refuses a sheet whose parts are too close, because each cut-out would carry a sliver of its neighbour.

## After editing a character

From the repository root:
```
node art/tools/build_atlas.js
```
Expected: one line per character listing its parts, then `wrote prototype/web/js/art_atlas.js`.

Then look at every pose at once:
```
node prototype/web/tests/pose_sheet.js
```
This writes `pose_sheet.png` into `prototype/web/tests/screenshots/`.

## Adding a chapter's painted scenery

1. Paint `far`, `mid` and `near` layers into `scenery/`. Use 1600 × 800 tiles with transparent sky, and put the horizon at y = 500 on the far layer.
2. In `prototype/web/js/levels.js`, add them to the chapter's `scenery`:
```
art: { far: 'art/scenery/name_far.svg', mid: 'art/scenery/name_mid.svg', near: 'art/scenery/name_near.svg' },
```

## Rules

- **No photos of the real people or the real dog** go in this repository (`docs/characters.md`).
- **Use drawings and descriptions only.**
