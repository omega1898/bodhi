# The browser game: Until We Find Home

The whole story, playable now, in any modern browser, including Safari on an iPhone. This version tests story, feel, light and pacing before the Unreal version exists. It's the blueprint for that version, not a replacement.

## Play it

Open `index.html` in Chrome or Safari: double-click it on the Mac, or drag it into a browser window.

**Controls:**

| Action | Keyboard | Phone |
|---|---|---|
| Walk | ← → or A D | ◀ ▶ |
| Jump | Space / W / ↑ | Jump |
| Use | E | Use |
| Call (her whistle, the dog's bark) | Q | Whistle / Bark |
| Kneel, or sniff as the dog | S | Kneel / Sniff |
| Switch character (only at the final obstacle) | Tab | Switch |
| Pause | Esc / P | II |

## Files

| File | What it holds |
|---|---|
| `index.html` | The page: drawing surface, menus, story text, phone buttons, styles |
| `js/levels.js` | **The story as data:** every chapter's floors, gates, items, areas and story rules. Start reading here. |
| `js/game.js` | The engine: movement, the companion who follows you, calls, the reunion, camera, screens |
| `js/art.js` | Every picture, drawn in code: characters, places, light, rain, the quiet scenes |
| `js/audio.js` | Every sound, made in code: rain, whistle, bark, music |
| `js/motion.js` | How bodies respond to movement: springs, turning, leaning, bending joints, blending poses |
| `js/render_cache.js` | Speed: soft glows, light holes, mist and blurred shapes painted once and reused; kept gradients |
| `tests/playtest.js` | A robot that plays every chapter to the end and measures frame time |
| `tests/filmstrip.js` | Films walking, turning, jumping and the dog running into contact sheets; fails if motion goes stiff |
| `tests/frame_rate.js` | Measures frames per second at iPhone size; fails below 50 |

## How a chapter works

Something happens (she walks into an area, pulls a lever, the dog barks), and the engine records an **event**. Each chapter has **rules**: "when this event happens, open this gate, say these lines, give control to the dog". A designer can change the story by editing `levels.js` without touching the engine.

This mirrors `core/` (the C++ rules for Unreal): events by name in, story decisions out.

## Test it

From the repository root, in a cloud session (Playwright and Chromium are installed there):
```
node prototype/web/tests/playtest.js
```
Expected: `PLAYTEST PASSED: every chapter finished`, plus the frame time for each chapter.

To check smoothness at iPhone size (expected: `FRAME RATE CHECK PASSED`):
```
node prototype/web/tests/frame_rate.js
```

To check how the motion looks:
```
node prototype/web/tests/filmstrip.js
```
Expected: `MOTION CHECK PASSED`, plus two contact sheets (images of every frame) to look at. Screenshots go to `prototype/web/tests/screenshots/`. Git ignores that folder, because the screenshots are regenerated on every run.

## Security notes

- **Saved progress** (unlocked chapters, found keepsakes) is in the browser's storage. On loading, it's treated as untrusted: anything other than a short list of short names is ignored.
- **The page loads no outside code.** Only fonts come from Google Fonts.
- **Story text reaches the page as plain text** (`textContent`), never as HTML, so a line of dialogue can't inject markup.
