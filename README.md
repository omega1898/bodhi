# Bodhi game

A narrative game about a human and their dog: separated by a disaster, each works toward the other, and the player earns the reunion.

**Current stage:** the chapter's rules run without any game engine. Graphics, controls and the Unreal project come next, on a Mac.

## Layout

| Folder | What it holds | Where it's edited |
|---|---|---|
| `core/` | Game rules in plain C++17: story beats, links between the two routes, the dog's feelings, the playtest log | Anywhere, including cloud sessions |
| `unreal/` | The Unreal Engine project (not created yet) | Only on the Mac |
| `docs/` | Design, architecture and decisions, cloud session guide | Anywhere |

## Build and test the core

You need CMake, Ninja and a C++ compiler. Cloud sessions have all three. On a Mac, install Xcode, then `brew install cmake ninja`.

```
cmake -S core -B core/build -G Ninja
cmake --build core/build
ctest --test-dir core/build --output-on-failure
```

Expected result: `100% tests passed`.

To see the chapter play out as a log:

```
./core/build/chapter_demo
```

Each output line is one event, as JSON. The last line on screen shows the final story beat and the dog's feelings.

## Reading order for the code

1. `core/include/bodhi/chapter_beat.hpp`: the story beats
2. `core/include/bodhi/event_bus.hpp`: how parts announce events without knowing who listens
3. `core/include/bodhi/bond_state.hpp`: the dog's feelings as numbers
4. `core/include/bodhi/telemetry_log.hpp`: the playtest log
5. `core/include/bodhi/railway_chapter.hpp`: chapter one, combining all of the above
6. `core/tests/test_railway_chapter.cpp`: the whole chapter played as a test, then attacked
