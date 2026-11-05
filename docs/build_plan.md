# Build plan: from here to the App Store

This is the path to the iOS release, then the PC game. Each milestone ends with a check you can run yourself, so "done" means evidence, not a feeling.

## The route

The iOS release is a native Swift app that carries the browser game inside it and runs offline (decision 8 in `docs/architecture.md`; steps in `docs/app_store.md`). Unreal Engine and the C++ rules in `core/` are kept for the later PC game; they are not in the iPhone app.

## Who can do what

| Who | Can do | Cannot do |
|---|---|---|
| Cloud sessions | The game in `prototype/web/` (story, chapters, mechanics, art drawn in code), the robot tests, `core/`, docs | Build or run the iPhone app, judge how it feels on a phone, sign anything with Apple |
| You, on the Mac | Xcode, the iPhone build, TestFlight, App Store Connect, playtests with people | — |
| An artist (later) | Hand-painted scenery, a polished dog and human | — |

## Where things stand (5 November 2026)

- **The story:** twelve chapters covering Bodhi's first year, from the puppy coming home to the two of them asleep in their bed. A mystery runs under it: the lost dog next door, Biscuit, and the chalk hearts his girl draws so he can follow her (`docs/story.md`).
- **Systems:** Bodhi grows chapter by chapter (puppy drawings, then the grown build drawn smaller), feeding and cleaning, mid-chapter saving with Continue, a pause menu, film scenes with Skip, and a third character (Biscuit) who can follow Bodhi.
- **Tests that pass in the cloud:**
  - `playtest.js`: the robot plays all twelve chapters.
  - `systems_check.js`: 47 checks, including break-it checks on saves, care, films and Biscuit.
  - `frames_check.js`, `filmstrip.js`, `frame_rate.js`: about 57 to 60 frames a second at iPhone size, in every chapter measured.
- **Not tested anywhere yet:** the Swift app itself, and the game on a real iPhone (memory, heat, touch feel).
- **Play time:** the robot's shortest run is 10.8 minutes. A first-time player is estimated at 45 to 75 minutes, a guess until measured. The target is at least five hours; the plan is in `docs/story.md`.

## Milestones

| | Milestone | Who | Check that proves it |
|---|---|---|---|
| M1 | First iPhone build | You | `docs/app_store.md` parts A and B: the game opens on your iPhone, fills the screen sideways, plays sound after a tap, and works with Wi-Fi off. Play Coming Home to the end. Write down anything that stutters or looks wrong |
| T | First playtest | You + 3 people | Each plays on your phone, uncoached. Time how long they take, and ask the five questions below |
| M2 | TestFlight | You | Apple Developer Program, `docs/app_store.md` parts C to F; five testers play the build |
| M3 | Toward five hours | Cloud + you | One deepened or new chapter at a time (`docs/story.md`), each played by the robot and by at least one person, whose time you write down. Total measured time reaches five hours |
| M4 | Art and sound polish | Artist + you | Painted scenery per chapter; whistle, bark and rain recorded or replaced |
| M5 | App Store | You | App Review passed |

**Why this order:**
- **M1 first:** the Swift app has never been compiled, so the first build is its real test. Everything built since PR #6 sits on it.
- **T before M3:** five hours of a game people don't care about is the most expensive mistake available. Thirty minutes that people love can grow; five hours they quit after twenty minutes can't be fixed by adding more.

**The five playtest questions** (from `docs/design.md`):
1. When did you first care about the dog?
2. When separated, did you want to reunite or just finish the puzzle?
3. Did playing as the dog feel different?
4. Was anything confusing rather than emotional?
5. Did the reunion feel earned?

## Details only you can supply

In `docs/characters.md`: Bodhi's real coat colour and three habits, her in-game name (if she should have one) and how she calls the dog. The game uses a golden-apricot coat from goldendoodle breed descriptions; tell a session if Bodhi's is cream, red or another shade.

## After the iOS release: the PC game

- The design document's PC plans: more chapters, richer lighting and fur, customization of the human and dog.
- `core/` (engine-independent C++) and the Unreal adapter spec are the foundation for that version.
