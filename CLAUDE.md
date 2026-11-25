# Bodhi game — instructions for Claude Code sessions

## What this is
A narrative game about a human and their dog who are separated and reunite. First release: a Unity game for iPhone and iPad on the App Store (decision 11 in `docs/architecture.md`, plan in `docs/unity_rebuild_spec.md`). The old browser version and its Swift wrapper app were removed on 24 November 2026; the Unity packages are now the only game. Later: possibly a larger PC game on top of `core/`. Design document: `docs/design.md`. Architecture and decisions: `docs/architecture.md`. The two main characters are based on real people and a real dog: read `docs/characters.md`, and never add photos of them to the repository.

## Who owns it
The owner is learning programming from zero. Every change must be readable by a beginner:
- Full-word snake_case names (`seconds_spent_in`, not `sSpent` or `t`).
- Every file starts with a `// Purpose:` comment; every function has a comment saying why it exists.
- Simple code over clever code. No templates, inheritance hierarchies or macros unless a comment justifies them.
- In the pull request description, explain in plain language what changed, why, and any new C++ syntax used.

## Layout
- `core/`: engine-independent C++17 game logic. Builds and tests anywhere, including cloud sessions.
- `unreal/`: the Unreal Engine project. Created and edited only on the owner's Mac. Cloud sessions cannot run Unreal.
- `rules/`: Unity package `com.bodhi.rules`: the game's rules in plain C# (story, bond, care, puzzles, saves). No Unity code.
- `look/`: Unity package `com.bodhi.look`: palette and motion maths in plain C#. No Unity code.
- `game/`: Unity package `com.bodhi.game`: `Logic/` (plain C#, tested with dotnet), `Runtime/` (scene scripts; `World/`, `Title/` and `Characters/` are the 3D scenes), `Editor/` (build settings: iPhone + iPad, landscape, 120 Hz), `Resources/Frames/` (animation pictures), `Resources/Shaders/` (3D shaders), `Plugins/iOS/` (Apple privacy manifest).
- To look at the 3D title scene from a session on the Mac (Unity installed, project not open in the Editor), render stills with `unity/Assets/Editor/TitleSnapshot.cs` (command in its first lines) and read the PNGs. Use `-snapshotFollow em|bodhi` for close-ups.
- `rules_dotnet/`: builds and tests `rules/`, `look/` and `game/Logic/` without Unity: `dotnet test rules_dotnet/Bodhi.Rules.Tests`.
- `unity/`: the Unity project (Unity 6000.6, URP 3D template), made on the Mac. It adds the three packages from disk; `unity/Assets/Editor/` holds the setup, build and snapshot scripts. Never hand-edit `.unity`, `.prefab` or `.asset` files.
- `art/`: the game's art as editable SVG files: painted character parts, frame-by-frame animations (`art/animations/`, drawn by `art/tools/draw_frames.js`), scenery layers. See `art/README.md`. Check the drawing tool with `node art/tools/draw_frames_test.js`; turn the drawings into Unity's pictures with `python3 art/tools/export_sprites.py`. The app icon is `art/app_icon/app_icon_1024.png`.
- `look/`: the Unity version's palette and motion maths in plain C#, from `docs/unity_art_bible.md`, tested along with the rules.
- `rules/`: the Unity version's game rules in plain C# (no Unity code), as a Unity package. Tested anywhere with `dotnet test rules_dotnet/Bodhi.Rules.Tests`; see `rules/README.md` and `docs/unity_rebuild_spec.md`.
- `game/`: the Unity version's game scripts, as a Unity package beside `rules/`. Its `Logic/` folder is plain C# tested with the rules; see `game/README.md`.
- `docs/`: design, story, art and production, architecture, cloud session guide.

## Hard rules for `core/`
1. C++17 standard library only. Never include Unreal, Apple or other engine headers.
2. No exceptions (`throw`, `try`) and no RTTI (`dynamic_cast`, `typeid`): Unreal builds with both off. The build flags enforce this.
3. Treat everything arriving from outside the core (event names, times, saved files) as hostile: validate it at the boundary, refuse and count bad input, never crash.
4. Every behavior change comes with a test in `core/tests/`. Add at least one "break it" test that tries to misuse the new code.
5. Before committing, all of these must pass:
   ```
   cmake -S core -B core/build -G Ninja && cmake --build core/build && ctest --test-dir core/build --output-on-failure
   cmake -S core -B core/build-sanitize -G Ninja -DCMAKE_CXX_COMPILER=g++ -DBODHI_SANITIZE=ON && cmake --build core/build-sanitize && ctest --test-dir core/build-sanitize --output-on-failure
   ```
6. Never edit or create `.uasset` or `.umap` files. They are binary files only the Unreal Editor can safely write.

## Working style
- Work on a branch and open a pull request; never push to `main` directly.
- Keep each pull request small enough to read in one sitting (roughly under 300 changed lines).
- If a task needs Unity, Unreal, the Mac or art, stop and write down what the owner must do instead of guessing.
