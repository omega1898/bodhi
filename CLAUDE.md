# Bodhi game — instructions for Claude Code sessions

## What this is
A narrative game about a human and their dog who are separated and reunite. First release: an iOS app on the App Store: a native Swift app carrying the finished game (`prototype/web/`) inside it (decision 7 in `docs/architecture.md`, steps in `docs/app_store.md`). Later: a larger PC game, possibly in Unreal on top of `core/`. Design document: `docs/design.md`. Architecture and decisions: `docs/architecture.md`. The two main characters are based on real people and a real dog: read `docs/characters.md`, and never add photos of them to the repository.

## Who owns it
The owner is learning programming from zero. Every change must be readable by a beginner:
- Full-word snake_case names (`seconds_spent_in`, not `sSpent` or `t`).
- Every file starts with a `// Purpose:` comment; every function has a comment saying why it exists.
- Simple code over clever code. No templates, inheritance hierarchies or macros unless a comment justifies them.
- In the pull request description, explain in plain language what changed, why, and any new C++ syntax used.

## Layout
- `core/`: engine-independent C++17 game logic. Builds and tests anywhere, including cloud sessions.
- `unreal/`: the Unreal Engine project. Created and edited only on the owner's Mac. Cloud sessions cannot run Unreal.
- `prototype/web/`: the whole story as a playable browser game (canvas, no libraries). The art, story and pacing blueprint for the Unreal version. Test it with `node prototype/web/tests/playtest.js`.
- `ios/`: the iOS app. `project.yml` (turned into an Xcode project on the Mac by XcodeGen), two Swift files, the app icon, and `bundle_game.sh`, which copies the game in. Cloud sessions can't build it; the Mac does.
- `art/`: the game's art as editable SVG files: painted character parts, frame-by-frame animations (`art/animations/`, drawn by `art/tools/draw_frames.js`), scenery layers. See `art/README.md`. Check frames with `node prototype/web/tests/frames_check.js`.
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
- If a task needs Unreal, the Mac or art, stop and write down what the owner must do instead of guessing.
