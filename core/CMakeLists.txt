# CMakeLists.txt
# Purpose: tells CMake how to build the core library, its tests and the demo.
# Build and test from the repository root:
#   cmake -S core -B core/build -G Ninja
#   cmake --build core/build
#   ctest --test-dir core/build --output-on-failure
cmake_minimum_required(VERSION 3.20)
project(bodhi_core LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

add_library(bodhi_core
    src/chapter_beat.cpp
    src/event_bus.cpp
    src/bond_state.cpp
    src/companion_behavior.cpp
    src/telemetry_log.cpp
    src/railway_chapter.cpp
    src/railway_save.cpp
)
target_include_directories(bodhi_core PUBLIC include)

if (NOT MSVC)
    # Unreal Engine builds game modules with C++ exceptions and RTTI (runtime type
    # information) switched off by default. Building the core the same way here means
    # code that depends on them fails in the cloud today, not later on the Mac.
    # -Werror turns every warning into an error so warnings cannot pile up unread.
    target_compile_options(bodhi_core PUBLIC
        -fno-exceptions -fno-rtti
        -Wall -Wextra -Wpedantic -Wshadow -Werror)
endif()

# Optional: build with sanitizers, which watch the running program for memory and
# arithmetic errors. Use:  cmake -S core -B core/build-sanitize -DBODHI_SANITIZE=ON
option(BODHI_SANITIZE "Build with AddressSanitizer and UndefinedBehaviorSanitizer" OFF)
if (BODHI_SANITIZE AND NOT MSVC)
    target_compile_options(bodhi_core PUBLIC -fsanitize=address,undefined -fno-sanitize-recover=all -g)
    target_link_options(bodhi_core PUBLIC -fsanitize=address,undefined)
endif()

enable_testing()

add_executable(core_tests
    tests/test_main.cpp
    tests/test_chapter_beat.cpp
    tests/test_event_bus.cpp
    tests/test_bond_state.cpp
    tests/test_companion_behavior.cpp
    tests/test_telemetry_log.cpp
    tests/test_railway_chapter.cpp
    tests/test_railway_save.cpp
)
target_link_libraries(core_tests PRIVATE bodhi_core)
add_test(NAME core_tests COMMAND core_tests)

add_executable(chapter_demo tools/chapter_demo.cpp)
target_link_libraries(chapter_demo PRIVATE bodhi_core)
add_test(NAME chapter_demo_reaches_aftermath COMMAND chapter_demo)

# A playable text version of chapter one. It stands in for Unreal Engine:
#   ./core/build/text_chapter
add_executable(text_chapter tools/text_chapter.cpp)
target_link_libraries(text_chapter PRIVATE bodhi_core)

# Replays scripts of typed commands. The full playthrough and the hostile script (garbage
# mixed into a real playthrough) must both reach the ending, which gives exit code 0.
set(playthrough_folder ${CMAKE_CURRENT_SOURCE_DIR}/tests/playthroughs)
add_test(NAME text_chapter_full_playthrough COMMAND text_chapter ${playthrough_folder}/full_chapter.txt)
add_test(NAME text_chapter_hostile_input COMMAND text_chapter ${playthrough_folder}/hostile_input.txt)
# Break it: try every shortcut. The story must still be stuck right after the separation.
add_test(NAME text_chapter_cheat_attempts COMMAND text_chapter ${playthrough_folder}/cheat_attempts.txt)
set_tests_properties(text_chapter_cheat_attempts PROPERTIES
    PASS_REGULAR_EXPRESSION "stopped at beat: human_section"
    FAIL_REGULAR_EXPRESSION "AddressSanitizer|runtime error")
