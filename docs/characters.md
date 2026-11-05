# Characters for the iOS release

The iOS game is built around two real characters, the owner's partner and their dog, and released publicly on the App Store.

- **The human:** blonde hair, blue eyes.
- **The dog:** Bodhi, a female goldendoodle. In the game she grows from an eight-week-old puppy to one year old (`dog_growth` in `prototype/web/js/levels.js`).

**How Bodhi is drawn now** (from goldendoodle breed descriptions; no reference photo was available in the cloud session): a shaggy, wavy-to-curly golden-apricot coat with no shine, a rounded "teddy bear" head with a top knot, a beard and moustache, a fringe over the eyes, long floppy ears in a darker shade, fluffy legs and a plumed tail. The puppy is a deeper apricot, because goldendoodles usually lighten as they grow. If the real Bodhi is cream, red or another shade, the colours are in `art/characters/bodhi.svg` (and the puppy's in `puppy_coat_colors` in `art/tools/draw_frames.js`).

## Decision: fixed main characters, no customization in the iOS release

- Constraint: the game is made for these two specifically, and the first release has to stay small enough to finish.
- Strongest rejected alternative: the design document's guided customization (dog body types, coats, human presets). It's the wider commercial hook.
- What breaks otherwise: every supported dog body needs its own skeleton, animations, passage sizes and reunion scene. That multiplies the art work before the chapter has proven anyone cares.
- Consequence: levels, camera positions and the reunion are designed for one goldendoodle's size. Customization becomes a later feature, most likely for the PC game.

## Recognizability comes from details and behavior, not a photoreal face

Players recognize a real dog by coat, silhouette and habits. People who know her will recognize her by hair, clothes, posture and how she talks to the dog.

On iPhone, Epic's own limits for MetaHuman characters are hair drawn as flat cards (no individual strands), a reduced level of detail (level 3 of their scale) and textures no larger than 2048 pixels. A photoreal face at that level risks looking uncanny. Slightly stylized realism is the safer target.

**Details to collect** (fill these in):

- Dog:
  - name: **Bodhi** (female)
  - coat color (cream / apricot / red / chocolate / other); the game uses golden apricot until you say otherwise
  - curl type (wavy / curly / fleece)
  - approximate weight and height
  - ear shape and tail
  - collar
  - favorite toy
  - three signature habits (for example: greeting, head tilt, leaning in when scared)
- Her:
  - in-game name
  - hair length and style
  - everyday clothes
  - how she calls the dog: a word, a phrase or a whistle. This should become the game's call mechanic.

## Technical risks to test before making final art (milestone M5)

1. **Curly fur on a phone.** A goldendoodle coat is fluffy and curly. On iPhone, fur is built from layers of semi-transparent cards. Every layer redraws the same screen pixels ("overdraw"), which is one of the most expensive things a phone GPU does.
   - Plan: sculpt the curl clumps into the body model itself, and use card layers only at the outline where the fluff shows against the background.
   - Measure the frame time in the 15-minute heat test.
2. **Her face.** Test one stylized and one realistic version on the phone and show both to her. Her reaction decides.

## Consent

A public release puts her likeness in front of strangers and in marketing.
- **Written agreement:** the owner confirmed on 5 November 2026 that they have it. Keep it somewhere safe, outside this repository.
- Keep giving her a say in how she's depicted, especially in marketing.

## Reference photos stay out of git

Photos of her and the dog never go in this repository. Cloud sessions clone it, and it was briefly public. Keep them in a private folder or a cloud drive and share them with an artist directly. The `references/` folder is listed in `.gitignore` as a guard.

**Dog photos for an artist or a 3D model:**
- daylight, no flash
- side, front, back and top views
- close-ups of the face, paws and coat texture
- a few short videos of walking, running, greeting and lying down, for animation reference
