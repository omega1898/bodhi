# Bodhi game (`com.bodhi.game`)

The Unity side of the game: everything between the rules (`rules/`, which decide what happens) and the scenes (which draw it and read the player's touches).

## What's in here

- `Logic/`: plain C# helpers that use the rules but not Unity, so they're tested anywhere with `dotnet test rules_dotnet/Bodhi.Rules.Tests`, like the rules.
  - `SubtitlePacer`: which story line is on screen and for how long, ported from the browser game.
  - `ZoneOccupancy`: who is standing in a story zone, counting each character's colliders.
  - `SaveFiles`: the saves on the phone, written so a phone dying mid-save never loses the last good one.
  - `GameSession`: the one object the scene talks to: New Game, Continue, autosave and the next chapter.
  - `LayoutSpace`: chapter layout positions (the browser game's units, y down) as Unity metres (y up), and back.
  - `CharacterMotion`: how she and Bodhi walk, jog, turn, jump and fall, from the browser game's numbers.
  - `CompanionSteering`: where the one you aren't playing walks: beside you, to you in a reunion, where the story sends him, after the ball and back, or a little ahead.
  - `ChapterStage`: what a chapter looks like: its floors, water, props, where they start, and what each story rule does on screen.
  - `Chapters/`: the twelve chapters' stages, generated from `prototype/web/js/levels.js` by `node rules_dotnet/tools/export_chapters.js`. Don't edit them by hand.
  - `PromptPicker`: what the "use" button offers right now (the reunion, the ball, the nearest thing, feeding, towelling, petting) and what pressing it does.
  - `StageLook`: reads the chapters' colours, says which floors, walls and props show right now, and the rough box each prop fills (for the grey-box scene).
  - `BallFlight`: the ball in the fetch chapters: thrown, bouncing, rolling, carried and dropped at her feet.
  - `StageTracker`: keeps the scene in step with the story (darkness, floods, the camera, who's hidden), including after Continue.
  - `CharacterMover`: moves her or Bodhi through a chapter and keeps them out of walls and floors (steps, ceilings, the chapter's ends, falling out of the world). Unity's own physics isn't used for them, so the game feels the same on every phone.
- Coming next: the Unity scripts that build a chapter and play it.

## Adding it to the Unity project

Same as the rules package: once the project exists, in the Unity Editor choose **Window**, **Package Manager**, **+**, **Install package from disk...**, and pick `rules/package.json` first, then `game/package.json`. Commit the `.meta` files Unity adds.
