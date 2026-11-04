# Build plan: from here to the whole game

This is the full path to the finished iOS chapter, then the PC game. Each milestone ends with a check you can run yourself, so "done" means evidence, not a feeling.

## Who can do what

| Who | Can do | Cannot do |
|---|---|---|
| Cloud sessions | Game rules in `core/`, tests, docs, specs, CI, the text version | Open Unreal, build for iPhone, make or judge art |
| You, on the Mac | Unreal Editor, Xcode, iPhone builds, playtests | — |
| An artist (later) | Dog and human models, fur, the station | — |

Most of the remaining work needs the Mac. Cloud sessions can't build "the whole game"; they build the parts that don't need an engine and write precise steps for the parts that do.

## Where things stand

- **Done, on `main`:** the chapter's rules: story beats, links between the two routes, the dog's feelings, the playtest log.
- **Open pull requests, waiting for your review:** CI (#1), how the dog follows (#2), a code walkthrough (#3), save files (#4).
- **This pull request:** a playable text version of the chapter (`core/tools/text_chapter.cpp`).

## Milestones for the iOS chapter

| | Milestone | Who | Check that proves it |
|---|---|---|---|
| T | Text playtest | You + 3 people | Each plays `text_chapter` uncoached; you note their answers to the five questions below |
| M0 | Mac set up | You | Unreal 5 project `BodhiGame` opens (steps in `unreal/README.md`); Xcode installed; `ctest` passes on the Mac |
| M1 | Rules inside Unreal | Cloud writes the spec (prompt E in `docs/cloud_sessions.md`); you build it | In the editor, pulling a test lever changes the beat printed on screen |
| M2 | Grey-box chapter | You | The whole chapter is playable on the Mac with boxes for rooms and capsules for the pair; layout follows the `places` and `paths` tables in `text_chapter.cpp` |
| M3 | First iPhone build | You | The grey-box chapter runs on your iPhone with touch controls; you write down the frame time |
| M4 | Grey-box playtest | You + 5 people | Uncoached play; telemetry log saved; five questions answered |
| M5 | Hardest-scene test on iPhone | You (+ artist) | One scene with curly fur, her face, water and both characters holds frame rate through a 15-minute heat test (`docs/characters.md`) |
| M6 | Real art and sound | Artist + you | Dog and human models in; whistle, bark and rain sounds in; written likeness agreement signed |
| M7 | App Store | You | TestFlight build played by testers; App Review passed |

**Why this order:** each milestone answers the riskiest open question as cheaply as possible.
- T and M4 answer "does anyone care?" before art money is spent.
- M3 and M5 answer "does it run on a phone?" before the level is finished.

**The five playtest questions** (from `docs/design.md`):
1. When did you first care about the dog?
2. When separated, did you want to reunite or just finish the puzzle?
3. Did playing as the dog feel different?
4. Was anything confusing rather than emotional?
5. Did the reunion feel earned?

## Details only you can supply

These are listed in `docs/characters.md` and still blank: the dog's name, coat, size and three habits; her in-game name, hair and clothes; how she calls the dog. The text version says "your dog" until they're filled in.

## Cloud-session work that can run in parallel

- Merge or fix the open pull requests #1 to #4 first. Unreviewed code piles up.
- Then, one session each:
  - the Unreal adapter spec (prompt E),
  - scent and whistle rules in `core/` (how far a whistle carries, where the scent is lost),
  - the playtest-log analysis script for M4.

## After the iOS chapter: the PC game

- More chapters (the design document lists a hospital, a school and an evacuation shelter), richer lighting and fur.
- Customization of the human and dog, which `docs/characters.md` deliberately left out of the iOS release.
- The `core/` rules carry over unchanged. That's why they're kept engine-independent.
