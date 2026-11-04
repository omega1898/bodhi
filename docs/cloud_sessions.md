# Running Claude Code cloud sessions on this repo

## What cloud sessions can and can't do here

**Can:** write, build and test everything in `core/`, write docs, open pull requests.

**Can't:** run Unreal Engine, build for iOS, or look at graphics. Those happen on the Mac.

## One-time setup

1. GitHub: install the Claude GitHub App on this repository: github.com/apps/claude, then **Configure**, then select `bodhi`. Private repositories need this step, or cloud sessions can't read or push to them.
2. Go to claude.ai/code, choose this repository, and use the **Default** environment.
3. Edit the environment and add this environment variable:
   `GIT_LFS_SKIP_SMUDGE=1`
   It tells git not to download large art files (the Git LFS files) when the session clones the repo. Cloud sessions never need the art, and it would fill the 30 GB disk.
4. No setup script is needed. GCC, Clang, CMake and Ninja are already installed in cloud sessions.

## How to run a session

1. Paste one prompt from below.
2. Let it work, then review the pull request on GitHub or in the session's **Changes** view.
3. Merge only what you can explain line by line. Ask the session to explain anything you can't.

**Cost control:**
- Each session spends credit while it works.
- Parallel sessions spend in parallel.
- Reviewing and reading cost nothing.
- Prefer one focused session per pull request.

## Ready-to-paste prompts

**A. Code walkthrough (learning, no code changes)**
> Read CLAUDE.md and every file in core/. Write docs/walkthrough.md: for each file, in reading order, explain what each block of code does and why, defining every C++ term the first time it appears (namespace, enum class, struct, class, std::string, std::vector, std::function, lambda, reference, const, #include, #pragma once). End with 10 questions I should be able to answer, with answers in a collapsed section. Open a pull request.

**B. Continuous integration (proves the build on macOS's compiler too)**
> Read CLAUDE.md. Add a GitHub Actions workflow at .github/workflows/core.yml that builds and tests core/ on ubuntu-latest (g++, plus the sanitizer build) and macos-latest (Apple clang), on every push and pull request. Comment every line of the workflow in plain language. Open a pull request.

**C. Companion behavior from the bond numbers**
> Read CLAUDE.md and docs/architecture.md. Add core/include/bodhi/companion_behavior.hpp and .cpp: a pure function that takes a bond_state and returns a companion_behavior (follow_distance_meters, may_run_ahead, checks_back_interval_seconds). Rule: high closeness_need or fear means staying closer and checking back more often. Keep every output within fixed safe ranges even for NaN or out-of-range input. Add tests, including break-it tests. Open a pull request.

**D. Save and load chapter progress (hostile-input lesson)**
> Read CLAUDE.md. Add saving and loading of railway_chapter progress as a small, versioned text format. The loader must treat the file as hostile: refuse unknown versions, unknown beats, oversized files (over 4 KB), duplicate fields, and missing fields, returning an error without changing the chapter. Add tests for each refusal, and a test that feeds 10,000 randomly mutated save files (fixed random seed) and checks that nothing crashes under the sanitizer build. Open a pull request.

**E. Unreal adapter (only after the Unreal project exists on the Mac)**
> Read CLAUDE.md and docs/architecture.md. Write a spec, not code, at docs/unreal_adapter_spec.md for a UActorComponent named UChapterRulesComponent that owns a bodhi::railway_chapter, exposes ReportWorldEvent and RequestCharacterSwitch to Blueprints, and broadcasts BeatChanged and ControlledCharacterChanged events. Include how to add core/ as an Unreal module, and the exact steps I must do on the Mac.
