// test_main.cpp
// Purpose: runs every test group and reports a single pass or fail.
#include "test_harness.hpp"

int main() {
    run_chapter_beat_tests();
    run_event_bus_tests();
    run_bond_state_tests();
    run_telemetry_log_tests();
    run_railway_chapter_tests();
    run_railway_save_tests();

    std::printf("%d checks, %d failed\n", test_harness::check_count, test_harness::failure_count);
    return test_harness::failure_count == 0 ? 0 : 1;
}
