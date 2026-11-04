// railway_chapter.hpp
// Purpose: the rules of chapter one ("The Other Side"), with no graphics, physics or input.
// The engine (Unreal) tells this object what happened in the world; this object decides
// what that means for the story, the dog's feelings, and which character the player controls.
#pragma once

#include <array>
#include <string>

#include "bodhi/bond_state.hpp"
#include "bodhi/chapter_beat.hpp"
#include "bodhi/event_bus.hpp"
#include "bodhi/telemetry_log.hpp"

namespace bodhi {

enum class character { human, dog };

// Returns "human" or "dog" for logs.
const char* character_name(character who);

// Why a save file was refused. `none` means it loaded.
enum class save_load_error {
    none,
    file_too_large,          // Over 4096 bytes.
    bad_character,           // Anything except a-z, 0-9, underscore, space and newline.
    bad_line,                // A line that is not exactly "name value", or no final newline.
    missing_header,          // The first line does not start with "bodhi_railway_save".
    unknown_version,         // A format version this game cannot read.
    unknown_field,
    duplicate_field,
    missing_field,
    unknown_beat,
    bad_value,               // A number out of range, or a word that is not allowed there.
    impossible_combination,  // Each value is fine alone, but the story cannot reach them together.
};

// Returns a fixed lowercase name for logs, for example "duplicate_field".
const char* save_load_error_name(save_load_error error);

// True when an event name is safe to accept from outside the core:
// 1 to 64 characters, only lowercase letters, digits and underscores.
bool is_valid_event_name(const std::string& event_name);

// True for events that only the core itself may announce, such as "shared_route_opened".
// Refusing them from outside stops a bug, or a modified game file, from skipping the
// final obstacle by announcing its result directly.
bool is_internal_event_name(const std::string& event_name);

class railway_chapter {
public:
    railway_chapter();

    // The event listeners inside this object point back at this exact object.
    // A copy would still point at the original, so copying is switched off.
    railway_chapter(const railway_chapter&) = delete;
    railway_chapter& operator=(const railway_chapter&) = delete;

    // The engine calls this when something happens, e.g. "maintenance_power_restored".
    // `time_seconds` counts from the start of the chapter and must never go backwards.
    // Returns false and changes nothing if the name or the time is refused.
    bool report_world_event(const std::string& event_name, double time_seconds);

    // The player asks to control the other character. Allowed only during the
    // final obstacle, where both routes need actions. Returns false if refused.
    bool request_character_switch(double time_seconds);

    chapter_beat current_beat() const;
    character controlled_character() const;
    const bond_state& bond() const;
    const telemetry_log& telemetry() const;

    // Total seconds spent in `which_beat` up to `now_seconds`, for playtest measures
    // such as "how long did players linger at the fence".
    double seconds_spent_in(chapter_beat which_beat, double now_seconds) const;

    // Writes the chapter's progress as a small text file (format version 1).
    // Times are rounded down to the millisecond and feelings to the millionth.
    // The playtest log and the refused-input count are not saved.
    std::string save_progress() const;

    // Replaces the chapter's progress with the progress in `save_text`.
    // The text is treated as hostile: on any problem this returns the reason and
    // changes nothing at all, so a broken file cannot leave the chapter half-loaded.
    // The caller decides what to do with a refusal (for example, count it and start fresh).
    save_load_error load_progress(const std::string& save_text);

    // How many inputs were refused because of a bad name, bad time or wrong moment.
    int refused_input_count() const;

private:
    void wire_event_rules();
    bool accept_time(double time_seconds);
    void refuse_input(const std::string& reason, const std::string& detail);
    void advance_story(const std::string& event_name);
    void enter_beat(chapter_beat next_beat);
    void take_control_of(character who);
    void handle_final_action(bool is_human_action);

    event_bus bus;
    telemetry_log log;
    bond_state dog_bond;
    chapter_beat beat = chapter_beat::together;
    character controlled = character::human;
    double latest_time_seconds = 0.0;
    double beat_started_at_seconds = 0.0;
    std::array<double, chapter_beat_count> finished_seconds_by_beat{};
    bool human_final_action_done = false;
    bool dog_final_action_done = false;
    int refused_inputs = 0;
};

}  // namespace bodhi
