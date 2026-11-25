# Art bible for the Unity version

The look of *Until We Find Home* in Unity. Every colour, light, effect and character rig follows this page. The game's existing direction, "warm lamplight in cold, wet dark" (`docs/art_and_production.md`), is the owner's choice, and this page builds on it rather than replacing it.

**The bar:** the smoothness and depth of the best 2D and side-on games. Silksong and Stray are **quality references only**. We study why they read well and copy nothing: no characters, shapes, palettes, compositions or motifs. Section 9 says honestly which parts code can reach and which need a human artist.

**Where it came from:** this page applies Unity's `craft-game-floors` skill (its art direction, camera, VFX, character motion, game feel, placeholder and scorecard references) and its `urp-postprocessing` skill. It also draws on Unity's manual pages on 2D lights and 2D Animation (sources at the end). The craft skill says plainly that it doesn't yet cover 2D sprite art direction, 2D lights or sprite placeholders. Those sections here come from Unity's manual and the browser game's proven choices, not from the skill.

## 1. The three lines

1. **Style:** painterly side-on 2D. Soft painted shapes, strong silhouettes, skeletal rigs with springy secondary motion, and real 2D light falling on painted surfaces.
2. **Palette:** the twelve colours in section 2, each with one job. Every colour in the game reads from one file, `look/Runtime/Palette.cs`.
3. **Mood:** every scene is cold blue dark with **one warm light**: a lamp, a fire, a window, the sun coming up. Warmth is where the two of them are safe, and the amber accent is kept for warmth and for reunions.

## 2. Palette

| Name | Hex | Its job |
|---|---|---|
| `night` | `#1B2433` | The darkest plane directly behind the characters |
| `rain` | `#34465C` | Middle distance: walls, wet buildings, far trees |
| `far_mist` | `#5C7186` | The farthest layer, which fades toward the sky |
| `wet_ground` | `#2A2F36` | The ground they stand on |
| `shadow` | `#121722` | Shadows. Blue, never pure black |
| `her_coat` | `#2C6563` | Her teal raincoat |
| `her_scarf` | `#B9532A` | Her rust scarf |
| `her_hair` | `#DCB866` | Her blonde hair, the lightest part of her silhouette |
| `bodhi_coat` | `#E2B47B` | Bodhi's apricot curls |
| `bodhi_shade` | `#B8875A` | The shaded side of his curls |
| `moon_rim` | `#9FC3D9` | The cold rim light along both characters' backs |
| `warm_amber` | `#F0B65E` | The accent: lamplight, firelight, reunions. Nothing else |

All the character colours come from the browser game (`prototype/web/js/art.js`).

**A finding the numbers forced.** The usual measure of how well two colours stand apart is the contrast ratio (WCAG). On it, her teal coat stands only 1.3 to 1.5 to 1 against the middle and far planes. At phone size it would melt into them. So the palette has three rules, and the tests in `look/` check all three:
- The plane directly behind the characters is always `night`, never `rain` or `far_mist`.
- Both characters carry a `moon_rim` rim light. It stands 8.4 to 1 against `night`, and that rim is what makes her silhouette read.
- Bodhi's coat and her hair stand at least 4.5 to 1 against `night`, so the two of them read at a glance, even small.

## 3. The frame: depth in four planes

| Plane | What's there | Moves with the camera | Value and colour |
|---|---|---|---|
| Far | Sky, skyline, hills | 0.15× | Lightest and greyest, fading into the sky (`far_mist`) |
| Back | Walls, buildings, trees | 0.5× | Middle values (`rain`), some detail |
| Play | Both characters, everything they touch | 1× | Full colour; the darkest backdrop (`night`), so they read |
| Front | Dark shapes passing close to the lens | 1.3× | Near black (`shadow`), blurred. Fades out if it would cover a character |

This is the depth trick of film and of the best 2D games: values get lighter and greyer with distance, and things move slower with distance. Unity does this with ordinary sprite layers on a **Sorting Layer** each, and a small script that moves each layer by its factor. Fog is a gradient sprite per plane, not a post effect, so it costs almost nothing on a phone.

## 3½. The 3D scenes (2.5D)

In November 2026 the owner chose 2.5D: 3D characters, scenery and lighting, with movement still along a side-on path. The title screen is the first 3D scene. It keeps this page's palette and mood (cold blue night, one warm light: Em's lantern, with the cabin's window far off), lit by URP's 3D renderer instead of 2D lights:

- **Materials:** one shader, `Bodhi/Lit`, takes its colours from the meshes, wraps light softly round the dark side, and adds the `moon_rim` edge light on both characters (strength 0.55 to 0.65), as section 2 requires.
- **Light:** moonlight (cool, soft shadows, 30 m of shadow distance); a faint green fill from the aurora; the warm lantern; light from the sky as three colours (overhead, horizon, ground); and fog thickening with distance, so far mountains fade.
- **Post-processing:** section 5's values, made in code by `WorldLook.make_post_processing`.
- **Code:** everything is in `game/Runtime/World/` and `game/Resources/Shaders/`; see `game/README.md`.

## 4. Light (URP's 2D renderer)

Unity's 2D renderer lights flat paintings as if they had shape. Per scene:

| Light | Light 2D type | Settings to start from |
|---|---|---|
| The cold base light | **Global**, one per sorting layer | `rain` colour, intensity 0.35. The world is dim but never black |
| The one warm light | **Spot** (inner and outer radius, falloff) | `warm_amber`, intensity 1.2 to 1.6, shadows on at strength 0.6. It goes above the bloom threshold on purpose, so it glows |
| Windows, doorways | **Freeform** (a hand-drawn shape) | Cold or warm by the scene's story, falloff 0.3 |
| Halos, lamp glow, firefly scent motes | **Sprite** (a soft texture) | Additive blend style |
| The rim along their backs | **Spot**, parented to each character, behind them | `moon_rim`, intensity 0.8, small radius, Additive |

- **Normal maps** (a hidden second painting saying which way each part of the surface faces) go on both characters and on props they touch, added as the sprite's secondary texture. Set Normal Map Quality to **Fast** on phones; Unity notes it is cheaper but can show artefacts, so check it on the iPhone.
- **Shadows** come from a **Shadow Caster 2D** on the characters and on large props near the warm light only. Shadows cost the most, so one warm light casts them and the others don't.
- **Light Batching Debugger** (Unity's tool for seeing how lights are grouped) gets checked once per chapter, so light count doesn't creep up.

### Each chapter's light

| Chapter | Cold base | The warm light | Weather |
|---|---|---|---|
| Coming Home | Soft afternoon, then moonlight | Kitchen lamp; bedside lamp at night | None |
| The Storm | Storm dark, lightning flashes | Her torch | Rain, rising water |
| The Last Morning | Grey dawn | A café window | Drizzle |
| The Other Side | Flooded station at night | One working platform lamp | Rain, drips |
| The Motorway | Overcast dusk | A breakdown van's hazard lights | Wind, spray |
| Visiting Hours | Hospital night corridor | A nurses' station desk lamp | None |
| The Orchard | Late afternoon, long shadows | Low sun through the trees | Pollen, midges |
| Lessons | Blue school morning | A classroom's lit window | Light rain |
| First Snow | White-blue moor | The shepherd's hut stove | Snow, breath fog |
| The Crossing | Flood at dusk | Northgate's lights across the water | Rain, river spray |
| High Farm | Night on the hill | The fire | Embers, smoke |
| Home | Evening, then night | The bedside lamp, as on the first night | None |

## 5. Post-processing (one global Volume, and a few local ones)

Values for a phone, from the `urp-postprocessing` skill's clean mobile recipe, warmed for this game's mood:

| Effect | Setting |
|---|---|
| Tonemapping | Neutral (keeps painted colours true; ACES pushes them orange) |
| Bloom | threshold 0.9, intensity 0.6, scatter 0.65. Only the warm light and the rim cross the threshold |
| Color Adjustments | contrast +10, saturation −8 |
| Split Toning | shadows toward `night` blue, highlights toward `warm_amber` |
| Vignette | intensity 0.22, smoothness 0.45 |
| Left off on phones | Film Grain, Depth of Field, Motion Blur: costly, and they blur the motion we've worked to make sharp |

**Reunions and care moments** get a local Volume that blends in over 0.7 s: saturation +10, vignette 0.35, warmer highlights. The light warms when they're together, as the existing direction asks.

Checks from the skill: the camera's `renderPostProcessing` is on, the URP asset has HDR on, and the Volume's layer is in the camera's Volume Layer Mask.

## 6. Characters

### How they're built

Both characters are painted as separate layers in one `.psb` file each (Photoshop's large format, which Unity's PSD Importer reads), in a neutral standing pose. Unity's **2D Animation** package puts bones inside them, bends the painting around the bones, and blends poses smoothly at any frame rate. This is what replaces the frame-by-frame drawings that made the browser game stutter (finding F7 in `graphics_audit.md`).

| Character | Layers (near and far sides painted separately) | Bones | Inverse kinematics |
|---|---|---|---|
| Her | Head, face (swappable), hair, ponytail ×2, neck, torso (coat), coat hem, scarf, scarf tail ×2, satchel, upper arm, forearm, hand, thigh, shin, boot: 22 layers | About 24 | **Limb** solver on each arm and leg (two-bone) |
| Bodhi | Head, face (swappable), ear ×2, muzzle, neck ruff, chest, body, hips, tail ×4, upper leg, lower leg, paw ×4: 24 layers, with a curl-edge overlay on the outline | About 28 | **Limb** on each leg; **Chain (CCD)** on the tail |

- **Faces swap**, they don't stretch. With **Sprite Swap**, eyes open, half and closed, and the mouth and ear shapes, are separate small paintings.
- **Turning rotates through.** A side-on character flipped in one frame looks like paper. Each character gets one three-quarter turn drawing per direction, swapped in for about 0.12 to 0.15 s, the times the browser game proved.
- **Silhouette test:** filled solid black at phone size, she reads by the ponytail, scarf tail and satchel, and he reads by his ears, curls and tail. If it doesn't read, exaggerate those parts about 30%, the craft skill's rule.

### How they move

Animation clips hold the key poses (walk, jog, trot, gallop, jump, land, kneel, shake off, sniff, sit, head tilt). Code adds everything that makes them feel alive, using the numbers in `docs/art_and_production.md`:
- **Springs** on ears, tail, ponytail, scarf, coat hem and satchel: each lags its parent and settles at its own rate.
- **Lean** into speed and braking, up to 7°.
- **Squash on landing** (height 0.85 for 0.1 s), and **breathing** when idle, never frozen.
- **Feet stick to the ground**: inverse kinematics on slopes and stairs.

The spring and follow maths lives in `look/Runtime/` as plain C#, tested to give the same motion at 30, 60 and 120 frames a second. That frame-rate independence is a large part of why top games feel smooth on every screen.

## 7. Camera

Cinemachine 3 (Unity's camera system), never a hand-written camera:
- **Position Composer** following the player's character: dead zone about 0.1 × 0.1 of the screen, look-ahead 0.5 s, damping loose sideways (0.6) and tight up and down (0.25), so jumps don't seasick the frame.
- **Framing:** the ground they stand on sits about two thirds of the way down the screen.
- **Target Group** of both characters when they're apart but near, at barriers, and coming into a reunion, gliding over 0.7 s.
- **Impulse** (Cinemachine's camera shake) only for the big moments: the walkway collapse, the bridge deck dropping. It never moves the camera directly.

## 8. Effects

Built-in particles, coloured from the palette, pooled (made once and reused, never created per event). Each fades and shrinks to nothing, never pops off.

| Effect | Look | Count alive |
|---|---|---|
| Rain | Thin streaks in `moon_rim` at 30% opacity, slanted by the wind; small splashes on the ground plane | 150 to 250 |
| Scent trail | Small warm motes drifting toward where the scent leads, brightest at the source | 10 to 20 |
| Shake off | A spray of drops both ways when Bodhi climbs out of water | 20, for 0.5 s |
| Footfall puffs | Mud or snow puffs on landing, in the ground's colour | 3 to 4, for 0.3 s |
| Dust in lamplight | Slow motes inside the warm light's cone only | 10 to 20 |
| Breath | A small fog puff from both of them in the snow | 2 to 3 |
| Embers | Rising sparks from the fire at High Farm, above the bloom threshold | 15 to 30 |
| Reunion | A soft warm bloom swell and the light warming. No confetti: this game is quiet | None |

## 9. Honestly: what code can reach, and what needs an artist

| Part | Reaches the bar from code? | Why |
|---|---|---|
| Smooth motion: rigs, springs, blends, 120 Hz | **Yes** | It's maths and tuning. The browser game's numbers carry over |
| Light, shadow, rim, bloom, colour grading | **Yes** | Unity's 2D renderer does the heavy work; this page sets the values |
| Camera, depth planes, parallax, fog | **Yes** | Code and values |
| Particles: rain, scent, embers, snow | **Yes** | Code and values |
| Grey-box levels in palette values, to playtest puzzles | **Yes** | Code-drawn shapes |
| The painted characters as layered `.psb` files | **No**, a 2D illustrator | The paintings themselves set the quality ceiling. The browser game's part sheets (`art/characters/her.svg`, `bodhi.svg`) can be exported as stand-in layers to build and test the rigs first |
| Normal maps for the characters | **Partly** | Tools can generate them from the paintings; an artist touches them up |
| Painted backgrounds, four planes per chapter | **No**, a background painter | This is most of what makes the best 2D games look rich. 11 of 12 chapters have none yet (finding F6) |
| Key poses for the emotional moments (the reunion, the bath, the hug) | **No**, an animator | Code blends between poses. The poses themselves are acting |

So code alone gets the game to **feeling** like a top game: smooth, lit and alive. It **looks** like one only once a 2D illustrator paints the two characters and a background painter paints the chapters. The cheapest order: rig the stand-in parts and prove the motion first, then commission one character and one chapter's four planes, and judge them on the iPhone before commissioning the rest.

## 10. How we check the look

1. **Machine checks in `look/`** (`dotnet test`): every palette colour is valid, shadows are blue and never black, the contrast rules in section 2 hold, and the springs give the same motion at 30, 60 and 120 frames a second.
2. **In Unity, once the project exists:** no sprite left with the default white material, no stock font, and every particle colour traced to the palette.
3. **The scorecard** from the craft skill's 2D set, each scored 1 to 5, with 3 as the floor: palette discipline, style uniformity, silhouette at gameplay zoom, outline and rim discipline, UI intentionality, and feedback visibility. It's scored from three captures on the iPhone: gameplay, a character close-up, and a care moment.

## Sources

- Light 2D component reference, Unity 6.0: https://docs.unity.com/en-us/engine/6000.0/manual/unity2d/2d-urp/2d-index/2dlight-properties
- 2D lighting in URP, Unity 6.3: https://docs.unity3d.com/6000.3/Documentation/Manual/urp/2d-index.html
- 2D Inverse Kinematics, 2D Animation 10.2: https://docs.unity3d.com/Packages/com.unity.2d.animation@10.2/manual/2DIK.html
- Preparing character artwork, 2D Animation 6.0: https://docs.unity3d.com/Packages/com.unity.2d.animation@6.0/manual/PreparingArtwork.html
- Sprite Swap, 2D Animation 6.0: https://docs.unity3d.com/Packages/com.unity.2d.animation@6.0/manual/SpriteSwapIntro.html
