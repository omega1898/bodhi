// test_telemetry_log.cpp
// Purpose: proves the playtest log refuses broken times, caps its size,
// and cannot be tricked into writing fake fields.
#include "bodhi/telemetry_log.hpp"

#include <cmath>
#include <limits>
#include <string>

#include "test_harness.hpp"

using bodhi::escape_json_string;
using bodhi::telemetry_log;

void run_telemetry_log_tests() {
    // Normal entries are kept in order.
    {
        telemetry_log log;
        CHECK(log.record(0.0, "beat_entered", "together"));
        CHECK(log.record(2.5, "world_event", "walkway_collapsed"));
        CHECK(log.entries().size() == 2);
    }

    // Broken times are refused: backwards, negative, not-a-number, infinite, absurdly large.
    {
        telemetry_log log;
        log.record(10.0, "first", "");
        CHECK(!log.record(9.0, "backwards", ""));
        CHECK(!log.record(-1.0, "negative", ""));
        CHECK(!log.record(std::numeric_limits<double>::quiet_NaN(), "nan", ""));
        CHECK(!log.record(std::numeric_limits<double>::infinity(), "infinite", ""));
        CHECK(!log.record(1.0e300, "huge", ""));
        CHECK(log.rejected_entry_count() == 5);
        CHECK(log.entries().size() == 1);
    }

    // The size cap holds.
    {
        telemetry_log log(3);
        for (int index = 0; index < 10; ++index) {
            log.record(static_cast<double>(index), "tick", "");
        }
        CHECK(log.entries().size() == 3);
        CHECK(log.dropped_entry_count() == 7);
    }

    // Escaping: quotes, backslashes, newlines and control bytes become safe text.
    CHECK(escape_json_string("a\"b") == "a\\\"b");
    CHECK(escape_json_string("a\\b") == "a\\\\b");
    CHECK(escape_json_string("line\nbreak") == "line\\nbreak");
    CHECK(escape_json_string(std::string("bell\x07", 5)) == "bell\\u0007");

    // Break it: a detail that tries to close the string and add a fake field.
    // It must stay inside the "detail" value, so each line still has exactly
    // three fields and the attacker's "kind" never becomes a real field.
    {
        telemetry_log log;
        log.record(1.0, "world_event", "x\",\"kind\":\"admin");
        const std::string output = log.to_json_lines();
        CHECK(output ==
              "{\"time_seconds\":1.000,\"kind\":\"world_event\",\"detail\":\"x\\\",\\\"kind\\\":\\\"admin\"}\n");
    }
}
