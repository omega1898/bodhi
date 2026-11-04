// telemetry_log.hpp
// Purpose: records what happened during play, with times, for playtest analysis.
// Example: how long a player stood at the fence before moving on.
#pragma once

#include <cstddef>
#include <string>
#include <vector>

namespace bodhi {

// Times above this (about 115 days) are treated as broken input.
constexpr double maximum_time_seconds = 1.0e7;

struct telemetry_entry {
    double time_seconds;  // Seconds since the chapter started.
    std::string kind;     // What sort of entry, e.g. "beat_entered".
    std::string detail;   // Specifics, e.g. "dog_section".
};

class telemetry_log {
public:
    // `entry_limit` caps memory use: a runaway loop cannot fill the device's memory.
    explicit telemetry_log(std::size_t entry_limit = 100000);

    // Adds an entry. Returns false and stores nothing if the time is broken
    // (not a real number, negative, too large, or earlier than the last entry)
    // or if the log is full.
    bool record(double time_seconds, const std::string& kind, const std::string& detail);

    const std::vector<telemetry_entry>& entries() const;
    std::size_t rejected_entry_count() const;  // Refused because of a broken time.
    std::size_t dropped_entry_count() const;   // Refused because the log was full.

    // One JSON object per line ("JSON Lines"), the format most analysis tools read.
    std::string to_json_lines() const;

private:
    std::size_t maximum_entries;
    std::vector<telemetry_entry> recorded_entries;
    std::size_t rejected_entries = 0;
    std::size_t dropped_entries = 0;
};

// Makes any text safe to place inside a JSON string. Without this, a name containing
// a quote mark could end the string early and inject fake fields into the log.
std::string escape_json_string(const std::string& text);

}  // namespace bodhi
