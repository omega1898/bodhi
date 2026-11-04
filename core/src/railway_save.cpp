// railway_save.cpp
// Purpose: turns chapter one's progress into a small text file and back again.
// The loader treats the file as hostile: a player can edit it, a crash can cut it short,
// and an older or newer game version can leave a file this code does not understand.
//
// Example of a version 1 save file (one "name value" pair per line):
//   bodhi_railway_save 1
//   beat final_obstacle
//   controlled_character dog
//   latest_time_ms 381000
//   ...
#include "bodhi/railway_chapter.hpp"

#include <algorithm>
#include <cmath>
#include <cstdint>

namespace bodhi {
namespace {

const char* const save_header = "bodhi_railway_save";
constexpr std::int64_t supported_version = 1;
constexpr std::size_t maximum_save_bytes = 4096;
constexpr std::int64_t maximum_time_milliseconds =
    static_cast<std::int64_t>(maximum_time_seconds) * 1000;
constexpr std::int64_t feeling_scale = 1000000;  // Feelings are saved in millionths.

// Every field after the header line, in the order save_progress writes them.
// The eight "finished_ms_" names follow the order of chapter_beat.
const char* const field_names[] = {
    "beat", "controlled_character", "latest_time_ms", "beat_started_at_ms",
    "finished_ms_together", "finished_ms_separation", "finished_ms_human_section",
    "finished_ms_dog_section", "finished_ms_near_contact", "finished_ms_final_obstacle",
    "finished_ms_reunion", "finished_ms_aftermath",
    "human_final_action_done", "dog_final_action_done",
    "closeness_need_millionths", "fear_millionths", "trust_millionths",
};
constexpr std::size_t field_count = 17;
// static_assert stops the build if the list above and field_count ever disagree.
static_assert(sizeof(field_names) / sizeof(field_names[0]) == field_count, "field list size");

// Positions in field_names, so code reads "beat_field" instead of a bare number.
constexpr std::size_t beat_field = 0;
constexpr std::size_t controlled_field = 1;
constexpr std::size_t latest_time_field = 2;
constexpr std::size_t beat_started_field = 3;
constexpr std::size_t first_finished_field = 4;
constexpr std::size_t human_action_field = 12;
constexpr std::size_t dog_action_field = 13;
constexpr std::size_t closeness_field = 14;
constexpr std::size_t fear_field = 15;
constexpr std::size_t trust_field = 16;

// The loaded values, held here until every check passes. Only then are they copied
// into the chapter, which is how a refused file leaves the chapter untouched.
struct railway_progress {
    chapter_beat beat = chapter_beat::together;
    character controlled = character::human;
    std::int64_t latest_time_ms = 0;
    std::int64_t beat_started_at_ms = 0;
    std::array<std::int64_t, chapter_beat_count> finished_ms_by_beat{};
    bool human_final_action_done = false;
    bool dog_final_action_done = false;
    std::int64_t closeness_need_millionths = 0;
    std::int64_t fear_millionths = 0;
    std::int64_t trust_millionths = 0;
};

// Seconds to whole milliseconds, rounded down so a saved time is never later than the
// real one (a later time would make the next real event look like it went backwards).
std::int64_t to_milliseconds(double seconds) {
    const double clamped = std::clamp(seconds, 0.0, maximum_time_seconds);
    return static_cast<std::int64_t>(std::floor(clamped * 1000.0));
}

// A feeling (0.0 to 1.0) to the nearest millionth.
std::int64_t to_millionths(double feeling) {
    const double clamped = std::clamp(feeling, 0.0, 1.0);
    return static_cast<std::int64_t>(std::llround(clamped * feeling_scale));
}

// Only these characters may appear. Refusing everything else up front means later
// steps never meet a carriage return, a zero byte, a minus sign or a decimal point.
bool is_allowed_save_character(char letter) {
    return (letter >= 'a' && letter <= 'z') || (letter >= '0' && letter <= '9') ||
           letter == '_' || letter == ' ' || letter == '\n';
}

// Splits "name value" at its single space. Refuses empty parts and extra spaces.
bool split_line(const std::string& line, std::string& name, std::string& value) {
    const std::size_t space = line.find(' ');
    if (space == std::string::npos || space == 0 || space + 1 == line.size() ||
        line.find(' ', space + 1) != std::string::npos) {
        return false;
    }
    name = line.substr(0, space);
    value = line.substr(space + 1);
    return true;
}

// Reads digits only (no sign, no decimal point) as a number from 0 to `maximum`.
// At most 12 digits, so the number cannot overflow before the range check.
bool parse_whole_number(const std::string& text, std::int64_t maximum, std::int64_t& number) {
    if (text.empty() || text.size() > 12) {
        return false;
    }
    std::int64_t result = 0;
    for (const char digit : text) {
        if (digit < '0' || digit > '9') {
            return false;
        }
        result = result * 10 + (digit - '0');
    }
    if (result > maximum) {
        return false;
    }
    number = result;
    return true;
}

// Finds a field's position in field_names. Returns false for an unknown name.
bool find_field(const std::string& name, std::size_t& index) {
    for (std::size_t candidate = 0; candidate < field_count; ++candidate) {
        if (name == field_names[candidate]) {
            index = candidate;
            return true;
        }
    }
    return false;
}

// Step 1: checks the file's shape and collects each field's text, without judging values.
save_load_error read_fields(const std::string& save_text,
                            std::array<std::string, field_count>& values) {
    if (save_text.size() > maximum_save_bytes) {
        return save_load_error::file_too_large;
    }
    for (const char letter : save_text) {
        if (!is_allowed_save_character(letter)) {
            return save_load_error::bad_character;
        }
    }
    if (save_text.empty() || save_text.back() != '\n') {
        return save_load_error::bad_line;  // Empty, or cut short mid-line.
    }
    std::array<bool, field_count> seen{};
    bool is_header_line = true;
    std::size_t line_start = 0;
    while (line_start < save_text.size()) {
        // Always found, because the text was checked to end with a newline.
        const std::size_t line_end = save_text.find('\n', line_start);
        const std::string line = save_text.substr(line_start, line_end - line_start);
        line_start = line_end + 1;

        std::string name;
        std::string value;
        if (!split_line(line, name, value)) {
            return save_load_error::bad_line;
        }
        if (is_header_line) {
            is_header_line = false;
            std::int64_t version = 0;
            if (name != save_header) {
                return save_load_error::missing_header;
            }
            if (!parse_whole_number(value, 1000000, version) || version != supported_version) {
                return save_load_error::unknown_version;
            }
            continue;
        }
        std::size_t index = 0;
        if (!find_field(name, index)) {
            return save_load_error::unknown_field;
        }
        if (seen[index]) {
            return save_load_error::duplicate_field;
        }
        seen[index] = true;
        values[index] = value;
    }
    for (const bool was_seen : seen) {
        if (!was_seen) {
            return save_load_error::missing_field;  // Also catches a header-only file.
        }
    }
    return save_load_error::none;
}

// Step 2: turns each field's text into a value, checking each one's own range.
save_load_error convert_fields(const std::array<std::string, field_count>& values,
                               railway_progress& progress) {
    bool beat_found = false;
    for (std::size_t index = 0; index < chapter_beat_count; ++index) {
        const chapter_beat candidate = static_cast<chapter_beat>(index);
        if (values[beat_field] == beat_name(candidate)) {
            progress.beat = candidate;
            beat_found = true;
        }
    }
    if (!beat_found) {
        return save_load_error::unknown_beat;
    }
    if (values[controlled_field] == "human") {
        progress.controlled = character::human;
    } else if (values[controlled_field] == "dog") {
        progress.controlled = character::dog;
    } else {
        return save_load_error::bad_value;
    }
    std::int64_t human_done = 0;
    std::int64_t dog_done = 0;
    bool all_numbers_fit =
        parse_whole_number(values[latest_time_field], maximum_time_milliseconds,
                           progress.latest_time_ms) &&
        parse_whole_number(values[beat_started_field], maximum_time_milliseconds,
                           progress.beat_started_at_ms) &&
        parse_whole_number(values[human_action_field], 1, human_done) &&
        parse_whole_number(values[dog_action_field], 1, dog_done) &&
        parse_whole_number(values[closeness_field], feeling_scale,
                           progress.closeness_need_millionths) &&
        parse_whole_number(values[fear_field], feeling_scale, progress.fear_millionths) &&
        parse_whole_number(values[trust_field], feeling_scale, progress.trust_millionths);
    for (std::size_t index = 0; index < chapter_beat_count; ++index) {
        all_numbers_fit = all_numbers_fit &&
                          parse_whole_number(values[first_finished_field + index],
                                             maximum_time_milliseconds,
                                             progress.finished_ms_by_beat[index]);
    }
    if (!all_numbers_fit) {
        return save_load_error::bad_value;
    }
    progress.human_final_action_done = human_done == 1;
    progress.dog_final_action_done = dog_done == 1;
    return save_load_error::none;
}

// Step 3: refuses combinations real play can never produce, because the story rules
// could not continue from them (for example, the dog in control during the reunion).
bool is_reachable(const railway_progress& progress) {
    if (progress.beat_started_at_ms > progress.latest_time_ms) {
        return false;
    }
    for (const std::int64_t finished_ms : progress.finished_ms_by_beat) {
        if (finished_ms > progress.latest_time_ms) {
            return false;
        }
    }
    // Who is in control must match the beat, as enter_beat in railway_chapter.cpp decides.
    const bool either_allowed = progress.beat == chapter_beat::near_contact ||
                                progress.beat == chapter_beat::final_obstacle;
    const character expected =
        progress.beat == chapter_beat::dog_section ? character::dog : character::human;
    if (!either_allowed && progress.controlled != expected) {
        return false;
    }
    // The final actions only count during the final obstacle, both are needed to leave
    // it, and leaving it is the only way to reach the reunion.
    const bool human_done = progress.human_final_action_done;
    const bool dog_done = progress.dog_final_action_done;
    if (progress.beat == chapter_beat::final_obstacle) {
        return !(human_done && dog_done);
    }
    if (progress.beat == chapter_beat::reunion || progress.beat == chapter_beat::aftermath) {
        return human_done && dog_done;
    }
    return !human_done && !dog_done;
}

// Adds one "name value" line to a save file being written.
void append_field(std::string& text, const char* name, const std::string& value) {
    text += name;
    text += ' ';
    text += value;
    text += '\n';
}

}  // namespace

const char* save_load_error_name(save_load_error error) {
    switch (error) {
        case save_load_error::none:                   return "none";
        case save_load_error::file_too_large:         return "file_too_large";
        case save_load_error::bad_character:          return "bad_character";
        case save_load_error::bad_line:               return "bad_line";
        case save_load_error::missing_header:         return "missing_header";
        case save_load_error::unknown_version:        return "unknown_version";
        case save_load_error::unknown_field:          return "unknown_field";
        case save_load_error::duplicate_field:        return "duplicate_field";
        case save_load_error::missing_field:          return "missing_field";
        case save_load_error::unknown_beat:           return "unknown_beat";
        case save_load_error::bad_value:              return "bad_value";
        case save_load_error::impossible_combination: return "impossible_combination";
    }
    return "unknown";
}

std::string railway_chapter::save_progress() const {
    const std::int64_t latest_ms = to_milliseconds(latest_time_seconds);
    std::string text = std::string(save_header) + " " + std::to_string(supported_version) + "\n";
    append_field(text, field_names[beat_field], beat_name(beat));
    append_field(text, field_names[controlled_field], character_name(controlled));
    append_field(text, field_names[latest_time_field], std::to_string(latest_ms));
    // std::min keeps rounding from ever writing a time later than latest_ms,
    // which the loader would refuse as impossible.
    append_field(text, field_names[beat_started_field],
                 std::to_string(std::min(to_milliseconds(beat_started_at_seconds), latest_ms)));
    for (std::size_t index = 0; index < chapter_beat_count; ++index) {
        const std::int64_t finished_ms =
            std::min(to_milliseconds(finished_seconds_by_beat[index]), latest_ms);
        append_field(text, field_names[first_finished_field + index], std::to_string(finished_ms));
    }
    append_field(text, field_names[human_action_field], human_final_action_done ? "1" : "0");
    append_field(text, field_names[dog_action_field], dog_final_action_done ? "1" : "0");
    append_field(text, field_names[closeness_field],
                 std::to_string(to_millionths(dog_bond.closeness_need)));
    append_field(text, field_names[fear_field], std::to_string(to_millionths(dog_bond.fear)));
    append_field(text, field_names[trust_field], std::to_string(to_millionths(dog_bond.trust)));
    return text;
}

save_load_error railway_chapter::load_progress(const std::string& save_text) {
    std::array<std::string, field_count> values;
    save_load_error error = read_fields(save_text, values);
    if (error != save_load_error::none) {
        return error;
    }
    railway_progress progress;
    error = convert_fields(values, progress);
    if (error != save_load_error::none) {
        return error;
    }
    if (!is_reachable(progress)) {
        return save_load_error::impossible_combination;
    }

    // Every check passed: only now does anything in the chapter change.
    beat = progress.beat;
    controlled = progress.controlled;
    latest_time_seconds = static_cast<double>(progress.latest_time_ms) / 1000.0;
    beat_started_at_seconds = static_cast<double>(progress.beat_started_at_ms) / 1000.0;
    for (std::size_t index = 0; index < chapter_beat_count; ++index) {
        finished_seconds_by_beat[index] =
            static_cast<double>(progress.finished_ms_by_beat[index]) / 1000.0;
    }
    human_final_action_done = progress.human_final_action_done;
    dog_final_action_done = progress.dog_final_action_done;
    dog_bond.closeness_need = static_cast<double>(progress.closeness_need_millionths) / feeling_scale;
    dog_bond.fear = static_cast<double>(progress.fear_millionths) / feeling_scale;
    dog_bond.trust = static_cast<double>(progress.trust_millionths) / feeling_scale;
    log.record(latest_time_seconds, "progress_loaded", beat_name(beat));
    return save_load_error::none;
}

}  // namespace bodhi
