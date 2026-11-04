// telemetry_log.cpp
// Purpose: stores timed play events safely and writes them out as JSON Lines.
#include "bodhi/telemetry_log.hpp"

#include <cmath>
#include <cstdio>

namespace bodhi {

telemetry_log::telemetry_log(std::size_t entry_limit) : maximum_entries(entry_limit) {}

bool telemetry_log::record(double time_seconds, const std::string& kind, const std::string& detail) {
    const bool time_is_usable =
        std::isfinite(time_seconds) && time_seconds >= 0.0 && time_seconds <= maximum_time_seconds &&
        (recorded_entries.empty() || time_seconds >= recorded_entries.back().time_seconds);
    if (!time_is_usable) {
        ++rejected_entries;
        return false;
    }
    if (recorded_entries.size() >= maximum_entries) {
        ++dropped_entries;
        return false;
    }
    recorded_entries.push_back({time_seconds, kind, detail});
    return true;
}

const std::vector<telemetry_entry>& telemetry_log::entries() const {
    return recorded_entries;
}

std::size_t telemetry_log::rejected_entry_count() const {
    return rejected_entries;
}

std::size_t telemetry_log::dropped_entry_count() const {
    return dropped_entries;
}

std::string telemetry_log::to_json_lines() const {
    std::string output;
    for (const telemetry_entry& entry : recorded_entries) {
        // Three decimals = millisecond precision. The time limit above keeps the
        // printed number far shorter than this buffer.
        char time_text[32];
        std::snprintf(time_text, sizeof(time_text), "%.3f", entry.time_seconds);
        output += "{\"time_seconds\":";
        output += time_text;
        output += ",\"kind\":\"";
        output += escape_json_string(entry.kind);
        output += "\",\"detail\":\"";
        output += escape_json_string(entry.detail);
        output += "\"}\n";
    }
    return output;
}

std::string escape_json_string(const std::string& text) {
    std::string escaped;
    escaped.reserve(text.size());
    for (const char text_byte : text) {
        switch (text_byte) {
            case '"':  escaped += "\\\""; break;
            case '\\': escaped += "\\\\"; break;
            case '\n': escaped += "\\n"; break;
            case '\r': escaped += "\\r"; break;
            case '\t': escaped += "\\t"; break;
            default: {
                const unsigned char byte_value = static_cast<unsigned char>(text_byte);
                if (byte_value < 0x20) {
                    // Other invisible control bytes become \u00XX, which JSON requires.
                    char code_text[8];
                    std::snprintf(code_text, sizeof(code_text), "\\u%04x", static_cast<unsigned int>(byte_value));
                    escaped += code_text;
                } else {
                    escaped += text_byte;
                }
            }
        }
    }
    return escaped;
}

}  // namespace bodhi
