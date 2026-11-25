# Characters for the iOS release

The iOS game is built around two real characters, the owner's partner and their dog, and released publicly on the App Store.

- **The human:** Em (`StoryText.her_name` in `rules/Runtime/ChapterScript.cs`, `her_name` in `prototype/web/js/levels.js`): honey-blonde hair, parted in the middle and worn down straight past her shoulders, blue-grey eyes, fair skin with rosy cheeks.
- **The dog:** Bodhi, a male goldendoodle (he/him). In the game he grows from an eight-week-old puppy to one year old (`dog_growth` in `prototype/web/js/levels.js`).

**Invented supporting characters** (not based on real people): Mina Osei (10) and her mother Ama, the neighbours two doors down, and their old dog Biscuit (13, grey). See "The thread" in `docs/story.md`.

**How they are drawn now** (redrawn in November 2026 to look like the real pair):
- **Bodhi:** a red-apricot curly coat, darker ginger on the head and ears, a cream-white muzzle, beard and moustache, a black nose, dark eyes, a cream-white bib down his chest and paler paws. The puppy is a deeper red, because he is lightening as he grows. Colours: `art/characters/bodhi.svg`; the puppy's are `puppy_coat_colors` and the bib and paws are `blaze_color` and `paw_color`, all in `art/tools/draw_frames.js`.
- **Em:** see above. Her hair is `part_head` and `part_ponytail` (the long hair hanging behind, still named that so the tool finds it) in `art/characters/her.svg`. Her clothes stay the story's teal raincoat, rust scarf and jeans; an at-home outfit could come later.

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
  - name: **Bodhi** (male)
  - coat color: red-apricot with a cream face and chest (matched to the real Bodhi)
  - curl type (wavy / curly / fleece)
  - approximate weight and height
  - ear shape and tail
  - collar
  - favorite toy
  - three signature habits (for example: greeting, head tilt, leaning in when scared)
- Her:
  - in-game name: **Em**
  - hair: honey-blonde, centre parting, straight, past the shoulders (matched to the real Em)
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
