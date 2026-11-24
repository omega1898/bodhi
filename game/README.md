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
  - `CompanionSteering`: where the one you aren't playing walks: beside you, to you in a reunion, or a little ahead.
- Coming next: the scripts that sit on objects in the scenes and connect them to the rules.

## Adding it to the Unity project

Same as the rules package: once the project exists, in the Unity Editor choose **Window**, **Package Manager**, **+**, **Install package from disk...**, and pick `rules/package.json` first, then `game/package.json`. Commit the `.meta` files Unity adds.
