# Bodhi rules (`com.bodhi.rules`)

The game's rules in plain C#: what the story remembers, how Bodhi feels (the bond), and later his care needs, the puzzles, who the player controls, and saves. Nothing in here uses Unity, so the rules can be tested on any computer in seconds, including GitHub's after every push.

The Unity project will show the rules on screen. It reads them, and never the other way round (`docs/unity_rebuild_spec.md`, section 3).

## What's in here

- `EventBus`: passes named events ("walkway_collapsed") from the scene to the rules.
- `BondState`: how Bodhi feels (fear, how much he wants to be near her, trust).
- `ChapterBeat`: a chapter's story stages and the order they may come in.
- `TelemetryLog`: the playtest log, written as JSON Lines.
- `RailwayChapter`: the station chapter's rules: story, who the player controls, and the final obstacle that needs both of them.
- `CareState`, `BondEffects`, `CareMoment`: hunger, dirt, food, and the feeding, drying, bathing and grooming moments.
- `Puzzle`, `PuzzleSolver`, `StationPuzzles`: puzzles as data, and a solver that proves each one can be finished and finds dead ends.
- `ControlArbiter`: switching the player between Bodhi and her without lost or doubled button presses.
- `RailwaySave`: saving and loading the chapter, refusing damaged files whole.
- `CompanionBehavior`: how far behind her Bodhi trails, how often he looks back, and whether he is wary of water.
- `StoryMemory`: facts one chapter records for later ones, such as being swept away by the river.
- `HintCoach`: after 3 minutes without progress, a gentle hint, then a plain one, always toward a step that really leads to the goal.
- `ChapterScript`, `ChapterRun`: any chapter written as data (things to use, zones, story rules), and the one engine that plays it: rules, timers, film scenes, calls, sniffing, the reunion.

## Why it is a package and not a folder inside the Unity project

The Unity project doesn't exist yet. Only Unity Hub, on the Mac, can create it. Keeping the rules here means the work can go ahead now, and nothing collides when the project is created later. Unity reads a folder like this as a **package**: a bundle of code with a `package.json` that names it.

Once the Unity project exists, add it in the Unity Editor: **Window**, **Package Manager**, the **+** button, **Install package from disk...**, then pick `rules/package.json`. Unity then writes a `.meta` file next to each file. Commit those, because they are how Unity keeps track of each file.

## Running the tests

With the .NET 8 SDK installed (on a Mac: `brew install dotnet@8`):

```
dotnet test rules_dotnet/Bodhi.Rules.Tests
```

Expect: `Passed!  - Failed: 0`. To check that the rules still compile with the older .NET that Unity uses:

```
dotnet build rules_dotnet/Bodhi.Rules.UnityCheck
```

## Rules for this code

- No `using UnityEngine`. `Runtime/Bodhi.Rules.asmdef` sets `noEngineReferences`, so Unity refuses to compile Unity code here.
- C# 9 at most, and only functions in .NET Standard 2.1, the version Unity 6 uses. The UnityCheck build enforces both.
- Names follow C#'s rule for types and the repository's rule for everything else. Types are `PascalCase` (`EventBus`, `BondState`), because Unity needs a script's file name to match its type name. Fields, functions and variables are full-word `snake_case` (`closeness_need`, `apply_event`).
- Tests come first. Write a failing test in `rules_dotnet/Bodhi.Rules.Tests`, then the code that makes it pass, and add at least one "break it" test that tries to misuse the new code.
- Treat anything from outside (event names, saved files) as hostile. Refuse it and count it, and never crash.
