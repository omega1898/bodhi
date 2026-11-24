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
  - `FrameSheets`: every animation drawing in `Resources/Frames`: its box, where its feet are, and where a carried thing is held. The numbers are in `FrameSheets.Data.cs`, written by `art/tools/export_sprites.py`.
  - `FlipbookChooser`: which drawing to show each frame, ported from the browser game: walking drawings move on by distance so feet stay planted; jumps follow the arc; landings, turns, stops, skids and getting up play over the rest.
  - `FramePacing`: runs the game at the screen's own frame rate (120 on ProMotion iPhones).
  - `CharacterMover`: moves her or Bodhi through a chapter and keeps them out of walls and floors (steps, ceilings, the chapter's ends, falling out of the world). Unity's own physics isn't used for them, so the game feels the same on every phone.
- `Runtime/`: the Unity scripts. They decide nothing themselves; they ask `Logic/` and draw the answer.
  - `ChapterDirector`: the one script the scene needs. It makes everything else and plays every chapter in turn.
  - `ChapterLevel`: builds a chapter's place (sky, floors, water, props, markers) from its stage, and keeps it in step with the story.
  - `CharacterBody`: draws her, Bodhi or the visiting dog where `CharacterMover` says, with the hand-drawn animation frames.
  - `SpriteLibrary`: loads the animation pictures when a chapter loads (never mid-chapter) and lets go of the ones it doesn't need.
  - `CameraRig`: follows the player smoothly, a little ahead, inside the chapter; holds film shots; shakes.
  - `GameHud`, `TouchButton`: subtitles, the goal, the button prompt, captions, darkness, fades, touch buttons and the title, pause, after-chapter and end-of-year screens.
  - `PlayerControls`: keyboard, game controller and touch buttons, read the same way.
  - `GreyBox`: plain coloured boxes for everything not drawn yet (and for a character whose pictures are missing).
- `Editor/`: scripts that run only in the Unity Editor. `FrameImport` sets how the animation pictures are imported (exact size, no shrunken copies, iPhone ASTC squeeze); `HighFrameRateBuild` lets the iPhone build run at 120 frames a second on ProMotion screens.
- `Resources/Frames/`: the animation pictures, made by `art/tools/export_sprites.py`. Don't edit them by hand.

The scripts in `Runtime/` can only really be compiled by Unity. In the cloud, `dotnet build rules_dotnet/Bodhi.Game.RuntimeStubCheck` compiles them against stand-ins for Unity's types, which catches typos and wrong calls but isn't proof.

## Trying it in Unity (on the Mac)

1. Make a Unity 6 project with the **Universal 2D** template.
2. Add the two packages (below). Unity also installs the Input System and Unity UI they ask for.
3. When Unity asks to switch to the new Input System, say yes (or set **Project Settings**, **Player**, **Active Input Handling** to **Input System Package**).
4. In an empty scene, add an empty GameObject, add the `ChapterDirector` component to it, and press Play.

Every sound effect, the music for each mood, and the rain and wind are made in code (`Logic/SoundSynth.cs` and `Logic/MusicSynth.cs`, played by `Runtime/GameAudio.cs`), so the game has sound with no audio files at all. To replace one with a recorded file, put a clip with the same name in `Assets/Resources/Sounds/` (like `bark`, `whistle`, `thunder`) or `Assets/Resources/Music/` (named after the mood, like `calm` or `tense`); the file wins.

To listen to the made sounds without Unity, run this from the repository folder. It writes one `.wav` file per sound into the folder you name:

```
dotnet run --project rules_dotnet/Bodhi.Audio.Preview -- audio_preview
```

## Adding it to the Unity project

Same as the rules package: once the project exists, in the Unity Editor choose **Window**, **Package Manager**, **+**, **Install package from disk...**, and pick `rules/package.json` first, then `game/package.json`. Commit the `.meta` files Unity adds.
