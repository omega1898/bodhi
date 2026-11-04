// test_railway_save.cpp
// Purpose: checks that chapter progress survives a save and load, and that every kind
// of broken or hostile save file is refused without changing the chapter.
#include <cmath>
#include <cstdint>
#include <random>
#include <string>

#include "bodhi/railway_chapter.hpp"
#include "test_harness.hpp"

using bodhi::chapter_beat;
using bodhi::character;
using bodhi::railway_chapter;
using bodhi::save_load_error;

namespace {

// Plays into the final obstacle and pulls the lever, so the save holds a half-finished
// two-route puzzle with the dog in control: the hardest state to restore correctly.
void play_into_final_obstacle(railway_chapter& chapter) {
    chapter.report_world_event("walkway_collapsed", 60.0);
    chapter.report_world_event("separation_scene_finished", 75.0);
    chapter.report_world_event("maintenance_power_restored", 140.0);
    chapter.report_world_event("dog_barked_from_window", 230.0);
    chapter.report_world_event("pair_met_at_fence", 300.0);
    chapter.report_world_event("fence_touch_finished", 345.0);
    chapter.report_world_event("jammed_gate_lever_pulled", 380.0);
    chapter.request_character_switch(381.5);
}

// A known-good save file to break in different ways.
std::string make_valid_save() {
    railway_chapter chapter;
    play_into_final_obstacle(chapter);
    return chapter.save_progress();
}

// Replaces the whole line that starts with "name " by `new_line` (empty removes it).
std::string replace_line(const std::string& save_text, const std::string& name,
                         const std::string& new_line) {
    const std::size_t start = save_text.find("\n" + name + " ") + 1;
    const std::size_t end = save_text.find('\n', start) + 1;
    return save_text.substr(0, start) + new_line + save_text.substr(end);
}

// Loads `save_text` into a chapter with progress, checks the expected refusal, and
// checks the chapter is exactly as it was: same progress, no new log entries.
void check_refused(const std::string& save_text, save_load_error expected_error) {
    railway_chapter chapter;
    chapter.report_world_event("walkway_collapsed", 60.0);
    const std::string progress_before = chapter.save_progress();
    const std::size_t log_size_before = chapter.telemetry().entries().size();

    const save_load_error error = chapter.load_progress(save_text);
    CHECK(error == expected_error);
    if (error != expected_error) {
        std::printf("  expected %s, got %s\n", bodhi::save_load_error_name(expected_error),
                    bodhi::save_load_error_name(error));
    }
    CHECK(chapter.save_progress() == progress_before);
    CHECK(chapter.telemetry().entries().size() == log_size_before);
}

}  // namespace

void run_railway_save_tests() {
    const std::string valid_save = make_valid_save();

    // Round trip: a loaded chapter matches the saved one and can be finished from there.
    {
        railway_chapter original;
        play_into_final_obstacle(original);
        railway_chapter restored;
        CHECK(restored.load_progress(valid_save) == save_load_error::none);
        CHECK(restored.save_progress() == valid_save);
        CHECK(restored.current_beat() == chapter_beat::final_obstacle);
        CHECK(restored.controlled_character() == character::dog);
        CHECK(std::fabs(restored.bond().fear - original.bond().fear) < 0.000001);
        CHECK(restored.seconds_spent_in(chapter_beat::near_contact, 400.0) == 45.0);
        // The lever pull was saved, so the dog's action alone now finishes the puzzle.
        restored.report_world_event("debris_cleared", 420.0);
        CHECK(restored.current_beat() == chapter_beat::reunion);
        CHECK(restored.refused_input_count() == 0);
        // Time cannot go backwards past the loaded time.
        CHECK(!restored.report_world_event("glove_found", 100.0));
    }

    // Each refusal the loader promises, one at a time.
    check_refused(valid_save.substr(valid_save.find('\n') + 1), save_load_error::missing_header);
    check_refused("bodhi_railway_save 2\n" + valid_save.substr(valid_save.find('\n') + 1),
                  save_load_error::unknown_version);
    check_refused("bodhi_railway_save 0\n" + valid_save.substr(valid_save.find('\n') + 1),
                  save_load_error::unknown_version);
    check_refused("bodhi_railway_save 99999999999999999999\n", save_load_error::unknown_version);
    check_refused(replace_line(valid_save, "beat", "beat secret_ending\n"),
                  save_load_error::unknown_beat);
    check_refused(replace_line(valid_save, "beat", "beat \n"), save_load_error::bad_line);
    check_refused(valid_save + std::string(4096, 'a'), save_load_error::file_too_large);
    check_refused(valid_save + "fear_millionths 0\n", save_load_error::duplicate_field);
    check_refused(valid_save + "beat together\n", save_load_error::duplicate_field);
    check_refused(replace_line(valid_save, "trust_millionths", ""), save_load_error::missing_field);
    check_refused("bodhi_railway_save 1\n", save_load_error::missing_field);
    check_refused(valid_save + "skip_to_reunion 1\n", save_load_error::unknown_field);

    // Break it: a file of exactly 4096 bytes is not refused for its size (the limit is
    // "over 4 KB"), only for its broken content.
    {
        railway_chapter chapter;
        const std::string exactly_limit = std::string(4095, 'a') + "\n";
        CHECK(chapter.load_progress(exactly_limit) != save_load_error::file_too_large);
    }

    // Break it: characters and shapes the format never writes.
    check_refused("", save_load_error::bad_line);
    check_refused(valid_save.substr(0, valid_save.size() - 1), save_load_error::bad_line);
    check_refused(replace_line(valid_save, "fear_millionths", "fear_millionths -5\n"),
                  save_load_error::bad_character);
    check_refused(replace_line(valid_save, "fear_millionths", "fear_millionths 0.5\n"),
                  save_load_error::bad_character);
    check_refused(replace_line(valid_save, "beat", "beat Final_Obstacle\n"),
                  save_load_error::bad_character);
    check_refused(valid_save + "\r\n", save_load_error::bad_character);
    check_refused(std::string("bodhi_railway_save 1\0\n", 22), save_load_error::bad_character);
    check_refused(replace_line(valid_save, "beat", "beat  final_obstacle\n"),
                  save_load_error::bad_line);
    check_refused(valid_save + "\n", save_load_error::bad_line);

    // Break it: values out of range, including numbers too long to fit in memory.
    check_refused(replace_line(valid_save, "fear_millionths", "fear_millionths 1000001\n"),
                  save_load_error::bad_value);
    check_refused(replace_line(valid_save, "latest_time_ms", "latest_time_ms 99999999999999\n"),
                  save_load_error::bad_value);
    check_refused(replace_line(valid_save, "human_final_action_done", "human_final_action_done 2\n"),
                  save_load_error::bad_value);
    check_refused(replace_line(valid_save, "controlled_character", "controlled_character cat\n"),
                  save_load_error::bad_value);

    // Break it: values that are fine alone but impossible together.
    check_refused(replace_line(valid_save, "beat_started_at_ms", "beat_started_at_ms 999999\n"),
                  save_load_error::impossible_combination);
    check_refused(replace_line(valid_save, "dog_final_action_done", "dog_final_action_done 1\n"),
                  save_load_error::impossible_combination);
    check_refused(replace_line(valid_save, "beat", "beat reunion\n"),
                  save_load_error::impossible_combination);
    {
        // The dog in control during the human's section.
        const std::string human_section = replace_line(
            replace_line(replace_line(valid_save, "beat", "beat human_section\n"),
                         "human_final_action_done", "human_final_action_done 0\n"),
            "controlled_character", "controlled_character dog\n");
        check_refused(human_section, save_load_error::impossible_combination);
    }

    // Break it: 10,000 randomly damaged save files. Each must either load (then the
    // chapter must save cleanly again) or be refused with the chapter unchanged.
    // The fixed seed makes every run test the same files, so a failure can be replayed.
    // The sanitizer build turns any out-of-bounds read or overflow here into a failure.
    {
        std::mt19937 random_numbers(20261104);
        int loaded_count = 0;
        int refused_count = 0;
        bool all_refusals_left_chapter_unchanged = true;
        bool all_loads_save_again = true;
        for (int attempt = 0; attempt < 10000; ++attempt) {
            std::string damaged = valid_save;
            const int damage_count = 1 + static_cast<int>(random_numbers() % 4);
            for (int damage = 0; damage < damage_count; ++damage) {
                const std::size_t position =
                    damaged.empty() ? 0 : random_numbers() % damaged.size();
                const char random_byte = static_cast<char>(random_numbers() % 256);
                switch (random_numbers() % 6) {
                    case 0:  // Overwrite one byte with any byte value.
                        if (!damaged.empty()) damaged[position] = random_byte;
                        break;
                    case 1:  // Insert one byte.
                        damaged.insert(position, 1, random_byte);
                        break;
                    case 2:  // Delete one byte.
                        if (!damaged.empty()) damaged.erase(position, 1);
                        break;
                    case 3:  // Swap in a digit, which often still parses.
                        if (!damaged.empty()) damaged[position] = static_cast<char>('0' + random_numbers() % 10);
                        break;
                    case 4:  // Copy a chunk of the file to another place (duplicates lines).
                        if (!damaged.empty()) {
                            damaged.insert(position, damaged.substr(random_numbers() % damaged.size(), 40));
                        }
                        break;
                    default:  // Cut the file short.
                        damaged.resize(position);
                        break;
                }
            }
            railway_chapter chapter;
            chapter.report_world_event("walkway_collapsed", 60.0);
            const std::string progress_before = chapter.save_progress();
            if (chapter.load_progress(damaged) == save_load_error::none) {
                ++loaded_count;
                railway_chapter reloaded;
                all_loads_save_again = all_loads_save_again &&
                    reloaded.load_progress(chapter.save_progress()) == save_load_error::none;
            } else {
                ++refused_count;
                all_refusals_left_chapter_unchanged = all_refusals_left_chapter_unchanged &&
                    chapter.save_progress() == progress_before;
            }
        }
        CHECK(all_refusals_left_chapter_unchanged);
        CHECK(all_loads_save_again);
        CHECK(loaded_count + refused_count == 10000);
        CHECK(refused_count > 0);
        std::printf("save damage test: %d loaded, %d refused\n", loaded_count, refused_count);
    }
}
