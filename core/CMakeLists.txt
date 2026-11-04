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
)
target_link_libraries(core_tests PRIVATE bodhi_core)
add_test(NAME core_tests COMMAND core_tests)

add_executable(chapter_demo tools/chapter_demo.cpp)
target_link_libraries(chapter_demo PRIVATE bodhi_core)
add_test(NAME chapter_demo_reaches_aftermath COMMAND chapter_demo)
