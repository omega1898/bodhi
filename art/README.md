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
| `characters/` | `her.svg` and `bodhi.svg`: each character painted as separate body parts (faces, coat, satchel, ears) |
| `animations/` | The animation frames the game draws: one file per animation, frames side by side. Generated, but each frame can be redrawn by hand |
| `scenery/` | Background paintings, one file per depth layer, in 1600 × 800 tiles that repeat sideways |
| `tools/` | `draw_frames.js` draws every animation frame; `build_atlas.js` maps the painted parts (used before the frames load) |

## How the animation frames work

The game animates frame by frame, the way hand-drawn 2D games do: every frame is a whole new drawing of the body, not parts turning on pins.

- **Where the poses come from:** `tools/draw_frames.js` keys the poses as numbers, like an animator's key poses. A walk has contact, down, passing and up. The dog has a real trot and a rotary gallop.
- **In-betweens:** like an animator, the tool keys a few poses and then makes the drawings in between. It follows smooth curves through the keys rather than straight lines, so movement flows through each key instead of turning sharply at it. A planted foot or paw moves in a straight line at an even speed, so it never slides or dips through the floor. Walks and runs have 16 drawings per cycle, the gallop 12, and idle breathing 12.
- **Moves that hand over:**
  - Landings, turns and stops all settle into the same home stance that idle breathes from, so one move flows into the next.
  - The jump is one arc of 9 drawings, and the game picks the drawing from how fast the character is rising or falling. Landings start from a touch-down drawing that matches the end of the jump.
  - Setting off, landing into a run and coming out of a skid start the stride at the drawing whose feet are closest to the feet just shown.
  - Getting up is the getting-down drawings (kneel, sit, the dog's rise into the hug) played backwards.
- **Skids:** reversing at speed shows a skid, drawn facing the way the character is still sliding, with dust from the feet.
- **The hand-over gate:** `draw_frames.js` measures how far the body moves between the last drawing of one move and the first of the next. If anything jumps more than 2.5 game units, it refuses to write the frames and names the hand-over to fix.
- **The dog's fur** is drawn in the body's own coordinates, so its curls and fluffy edge move with the body instead of swimming across it.
- **How a frame is drawn:** limbs are drawn as one bending shape. The dog's back, chest and legs merge into one furry silhouette. Faces, the coat and the satchel come from the painted sheets in `characters/`, so the style matches.
- **What it writes:** one SVG per animation into `animations/`, frames in rows of up to 8 (a very wide sheet is too wide for a phone to paint), and `animations/frames_atlas.js`. The atlas says where each frame is, how far each walking cycle travels, and where her hand and the dog's mouth and eye are in each frame.
- **No sliding feet:** walking and running frames advance with distance covered. The planted foot moves back by the same amount every frame, and the cycle length in the atlas is worked out from that.
- **Frames:** her frames are 64 × 106 game units with the feet at (30, 99). The dog's are 90 × 84 with the feet at (42, 76).
- **Two dog sets:** `bodhi_*` is the grown goldendoodle; `bodhi_puppy_*` is the same moves with a puppy's build (`puppy_build` in `tools/draw_frames.js`: a bigger head and paws, shorter legs, a short tail) in a deeper apricot. The game uses the puppy set until about six months and draws either set smaller while she grows. A third set, `biscuit_*`, is the old dog next door: the grown dog's moves in an older build, recoloured grey and white with a blue collar and a smoother coat (`biscuit_changes` in `tools/draw_frames.js`). Only the sets in use are painted, to save memory on a phone.

After changing a pose or a painted part:
```
node art/tools/draw_frames.js
node art/tools/draw_frames_test.js
```
Expected: a hand-over list with every line `ok`, then `drew 1078 frames...`, then the test's `ok` lines. Then make the Unity pictures (next section) and look at the result in Unity. (The browser game's frames check was removed with the browser game.)

## Pictures for the Unity iPhone app

A phone's graphics chip draws pictures, not SVG, so the frames are turned into PNG pictures once, at the size the iPhone shows them, by `tools/export_sprites.py`. It trims each drawing, fills the colour of its see-through rim from the fur beside it (so no dark fringe shows when the chip blends pixels), and packs each animation into one picture in `game/Resources/Frames/`. It also writes `game/Logic/FrameSheets.Data.cs`, which says where every drawing is in its picture and where its feet are. Needs Python 3 with Pillow and numpy, and ffmpeg.

After running `draw_frames.js`, also run:
```
python3 art/tools/export_sprites.py
python3 art/tools/export_sprites_test.py
```
Expected: one line per animation (`her_walk  32 drawings 2030x660` and so on), then `All sprite export tests pass.`

## How the character parts work (painted sheets)

- **Part names:** each part is a group named `part_something`, for example `part_thigh` or `part_ear`.
- **Joints:** the group's origin (where its `translate(x, y)` puts it) is the joint the part turns around. Limbs hang straight down from their joint.
- **Scale:** 10 drawing units = 1 game unit. Her thigh is 230 units long because her thigh in the game is 23.
- **The far side:** the game draws the far-side arm and leg from the same parts, darkened.
- **Spacing:** keep at least 60 units of empty space around every part. The tool refuses a sheet whose parts are too close, because each cut-out would carry a sliver of its neighbour.

## After editing a character

The painted sheets feed the frame drawings: run `node art/tools/draw_frames.js`, then the Unity picture export above. (The old `build_atlas.js` and pose sheet were for the browser game and were removed with it.)

## Adding a chapter's painted scenery

Paint `far`, `mid` and `near` layers into `scenery/`. Use 1600 × 800 tiles with transparent sky, and put the horizon at y = 500 on the far layer. Bringing them into a Unity chapter is still to be built (`docs/unity_art_bible.md`).

## Rules

- **No photos of the real people or the real dog** go in this repository (`docs/characters.md`).
- **Use drawings and descriptions only.**

## 3D models (Blender)

The 3D characters are built by scripts in `blender/`, run in Blender without opening its window. Each script builds a model from nothing (body grown round a skeleton, curls, colours, bones), saves pictures of it to check, and exports it into `game/Resources/Models/`, where the game puts it on the matching code-built figure (`game/Runtime/Characters/SkinnedModel.cs`):

```
blender -b --factory-startup -P art/blender/build_bodhi.py -- game/Resources/Models/bodhi.fbx previews/
```

Blender is free (`brew install --cask blender`). Change the joints, thicknesses or colours at the top of a script and run it again; the exported model replaces the old one. Never edit the `.fbx` by hand.
