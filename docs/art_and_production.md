# Art direction, performance, team, and replayability

## 1. Art direction

**One line:** warm lamplight in cold, wet dark. Soft shapes, strong silhouettes.

| Element | Rule | Why |
|---|---|---|
| Palette | Each chapter has cold ambient light plus one warm light source (lamps, fire, sunrise). The amber accent `#f0b65e` is reserved for warmth and for the reunion. | The eye goes to warmth, and warmth is where the two of them are safe. |
| The dog | Read the coat as soft scalloped curls, never as rings. Light from above with a rim of light along the back. Ears and tail carry emotion: the tail drops when afraid, the ear lifts at a call. Signature habit: a head tilt when standing still. | People recognise a real dog by its silhouette and habits (`docs/characters.md`). |
| Her | Teal raincoat, rust scarf, blonde hair and blue eyes (from `docs/characters.md`). The ponytail and scarf lag behind her movement. | Secondary motion makes a small figure feel alive. |
| Light | Lamps throw cones, a halo and a pool on the floor. On wet floors the light reflects as streaks. Darkness is a separate layer with soft holes cut where light falls. | Depth and mood at almost no cost. |
| Depth | Background layers move slower than the world and fade toward the sky colour. Dark foreground shapes move faster, and fade out whenever they would cover the player. | The three-layer depth of film. |
| Moments | Reunions push the camera in, warm the light and swell the music. The scenes between chapters are drawn large and still. | The player gets time to look at the characters properly. |

**The browser game (`prototype/web/`) is the art blueprint, not the final look.** It proves composition, colour, light and timing. The Unreal version keeps those decisions and adds real 3D, materials and fur.

## 2. Getting close to Unreal quality on an iPhone

**Facts:**
- An iPhone has about 16.7 ms per frame at 60 frames a second, or 33.3 ms at 30.
- Phones get hot and slow themselves down after several minutes. A short benchmark therefore proves little; `docs/characters.md` already plans a 15-minute heat test.

**Recalled, not verified:** Unreal's mobile renderer doesn't run Lumen (dynamic global illumination) or Nanite (very high-detail meshes) the way the desktop renderer does. Check Epic's current mobile rendering documentation for the exact engine version before relying on this.

**How small studios get a high-end look on phones** (standard practice; the numbers are estimates to test, not promises):

| Technique | What it buys | Cost on a phone |
|---|---|---|
| Baked lighting (lightmaps) plus 1 to 3 moving lights per scene | Soft, rich light without computing it live | Low. Lighting is computed once on the Mac. |
| Strong post-processing: colour grading, mild bloom, fog | Most of the "cinematic" feel | Low to medium |
| Small, enclosed scenes with fixed side-on cameras (as in this design) | The GPU only draws what the camera sees | Low. This design already suits phones. |
| Fur as a modelled curly shape plus a few card layers at the outline only (`docs/characters.md`) | A believable goldendoodle | Medium: the main risk, measured at milestone M5 |
| Particle effects (Niagara) with small counts: rain, scent, dust | Atmosphere | Low if kept to hundreds of particles, not thousands |
| Texture limit of 2048 pixels and levels of detail for characters | Memory and heat under control | Low |

**Performance rules for both versions:**
1. Fixed physics steps, with drawing blended between them. The browser game already does this, so 120 Hz iPhone screens don't judder.
2. Draw anything that never changes once (backdrops), then reuse it every frame.
3. Draw darkness and light at half resolution.
4. Measure on the oldest iPhone you'll support, not the newest.

**What the browser game measures today:** about 2 ms of work per frame in a desktop test browser, averaged over a full playthrough of every chapter (`prototype/web/tests/playtest.js`). That's a desktop number. A real iPhone will be slower, so test on the device.

## 3. The team, and how agents fit into it

You are the **director**: you own the story, the feel and every merge. Agents are the specialists. None of them merges anything.

| Role | Owns | Hands off to | Gate before handoff |
|---|---|---|---|
| Producer (one coordinating agent) | The milestone list in `docs/build_plan.md`, splitting work into small pull requests | Every specialist | Each task names files, a test and a "done" check |
| Narrative | `docs/story.md`, the `say` lines in `levels.js` | Level design | You read every line aloud |
| Level design | Floors, gates and zones in `levels.js` | Engineering, QA | The robot playtest finishes the chapter |
| Engineering | `core/` (C++ rules), `prototype/web/js/game.js`, later the Unreal adapter | QA | Builds pass, sanitizer passes, new break-it tests |
| Art and technical art | `art.js` now; Unreal materials and lighting later | QA | Screenshots reviewed; frame time measured |
| Audio | `audio.js` now; recorded sound later | QA | Played with headphones and on a phone speaker |
| QA and performance | `prototype/web/tests/playtest.js`, the M5 heat test | You | Every chapter finishes, no page errors, frame budget met |

**Escalation rule:** an agent stops and asks when a task needs the Mac, art, a real person's likeness, or a change to the story's promise.

**What you must understand yourself:** the event and rule system (`fire_event`, `apply_effects`), why gates are data rather than code, and how the playtest proves a chapter can be finished.

## 4. What keeps people playing

This is a short narrative game. People come back to it for **attachment and discovery**, not for scores. Repetition loops would cheapen the reunions.

**Built now:**
- **Keepsakes:** three per chapter, each a small story about someone who cared for someone. They're saved between visits.
- **Finds only the dog can make:** hidden keepsakes appear only when the dog sniffs nearby. This rewards playing as the dog, the design's most distinctive mechanic.
- **The memory box:** a collection on the title screen. Missing entries show hints, which gives a reason to replay a chapter.
- **Chapter select:** every finished chapter can be replayed.

**Next, in order of value:**
1. **Replies that change on a replay.** She says something different when you return to a place, and the dog behaves differently once it trusts you more.
2. **Photo mode** in the quiet scenes. Players share pictures of the two of them; this is free word of mouth.
3. **A daily walk:** a two-minute calm scene (fetch, sniffing, an occasional keepsake) that changes with the day. It's the gentlest daily hook possible, and it fits the theme.
4. **Later, for PC:** customisation (dog body types, coats). `docs/characters.md` explains why it's left out of the iOS release.

**How to know it works (milestone M4 playtest):** count how many testers open the memory box unprompted, and how many replay a chapter to find a missing keepsake.
