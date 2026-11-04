// test_harness.hpp
// Purpose: a tiny testing tool with no downloads needed.
// CHECK(condition) prints the file, line and condition when the condition is false.
// The test program exits with a non-zero code if any CHECK failed, which is how
// CTest (and any automated pipeline) knows the tests failed.
#pragma once

#include <cstdio>

namespace test_harness {
inline int failure_count = 0;
inline int check_count = 0;
}  // namespace test_harness

// Macros are written in capitals by long-standing C++ convention, so they stand out:
// a macro is pasted into the code before compiling, unlike a normal function.
#define CHECK(condition)                                                                \
    do {                                                                                \
        ++test_harness::check_count;                                                    \
        if (!(condition)) {                                                             \
            std::printf("FAILED %s:%d: %s\n", __FILE__, __LINE__, #condition);          \
            ++test_harness::failure_count;                                              \
        }                                                                               \
    } while (0)

// Each test file provides one of these; test_main.cpp calls them all.
void run_chapter_beat_tests();
void run_event_bus_tests();
void run_bond_state_tests();
void run_telemetry_log_tests();
void run_railway_chapter_tests();
