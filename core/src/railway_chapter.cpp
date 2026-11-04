// railway_chapter.cpp
// Purpose: wires chapter one's rules together: story beats, links between the two routes,
// the dog's feelings, control switching and the playtest log.
#include "bodhi/railway_chapter.hpp"

#include <cmath>

namespace bodhi {
namespace {

// Which event moves the story from which beat to which.
struct beat_trigger {
    chapter_beat from_beat;
    const char* event_name;
    chapter_beat to_beat;
};

const beat_trigger beat_triggers[] = {
    {chapter_beat::together, "walkway_collapsed", chapter_beat::separation},
    {chapter_beat::separation, "separation_scene_finished", chapter_beat::human_section},
    {chapter_beat::human_section, "dog_gate_opened", chapter_beat::dog_section},
    {chapter_beat::dog_section, "human_route_revealed", chapter_beat::human_section},
    {chapter_beat::human_section, "pair_met_at_fence", chapter_beat::near_contact},
    {chapter_beat::dog_section, "pair_met_at_fence", chapter_beat::near_contact},
    {chapter_beat::near_contact, "fence_touch_finished", chapter_beat::final_obstacle},
    {chapter_beat::final_obstacle, "shared_route_opened", chapter_beat::reunion},
    {chapter_beat::reunion, "reunion_scene_finished", chapter_beat::aftermath},
};

// An action on one route that causes an effect on the other route.
struct route_link {
    const char* trigger_event;
    const char* effect_event;
};

const route_link route_links[] = {
    // The human restores power, which opens the gate on the dog's route.
    {"maintenance_power_restored", "dog_gate_opened"},
    // The dog barks from a window, which shows the human a new way through.
    {"dog_barked_from_window", "human_route_revealed"},
};

// The two actions the final obstacle needs, one per route.
const char* const human_final_action = "jammed_gate_lever_pulled";
const char* const dog_final_action = "debris_cleared";

// Events only the core may announce. They are the effects above.
const char* const internal_event_names[] = {
    "dog_gate_opened",
    "human_route_revealed",
    "shared_route_opened",
};

// Turns a beat into an array position, refusing anything outside the array.
bool beat_to_index(chapter_beat beat, std::size_t& index) {
    const std::size_t candidate = static_cast<std::size_t>(beat);
    if (candidate >= chapter_beat_count) {
        return false;
    }
    index = candidate;
    return true;
}

}  // namespace

const char* character_name(character who) {
    return who == character::dog ? "dog" : "human";
}

bool is_valid_event_name(const std::string& event_name) {
    if (event_name.empty() || event_name.size() > 64) {
        return false;
    }
    for (const char letter : event_name) {
        const bool allowed =
            (letter >= 'a' && letter <= 'z') || (letter >= '0' && letter <= '9') || letter == '_';
        if (!allowed) {
            return false;
        }
    }
    return true;
}

bool is_internal_event_name(const std::string& event_name) {
    for (const char* internal_name : internal_event_names) {
        if (event_name == internal_name) {
            return true;
        }
    }
    return false;
}

railway_chapter::railway_chapter() {
    wire_event_rules();
    log.record(0.0, "beat_entered", beat_name(beat));
}

void railway_chapter::wire_event_rules() {
    // Every event is logged, applied to the dog's feelings, then checked against the story.
    bus.subscribe_to_all([this](const game_event& event) {
        log.record(latest_time_seconds, "world_event", event.name);
        apply_bond_event(dog_bond, event.name);
        advance_story(event.name);
    });

    // Route links: an action on one route announces its effect on the other route.
    for (const route_link& link : route_links) {
        const std::string effect_event = link.effect_event;
        bus.subscribe(link.trigger_event, [this, effect_event](const game_event&) {
            bus.publish({effect_event});
        });
    }

    // The final obstacle: count each route's action, but only during that beat.
    bus.subscribe(human_final_action, [this](const game_event&) { handle_final_action(true); });
    bus.subscribe(dog_final_action, [this](const game_event&) { handle_final_action(false); });
}

bool railway_chapter::report_world_event(const std::string& event_name, double time_seconds) {
    if (!is_valid_event_name(event_name)) {
        // Only the first 64 characters are logged, so a huge name cannot flood the log.
        refuse_input("bad_event_name", event_name.substr(0, 64));
        return false;
    }
    if (is_internal_event_name(event_name)) {
        refuse_input("internal_event_from_outside", event_name);
        return false;
    }
    if (!accept_time(time_seconds)) {
        refuse_input("bad_time", event_name);
        return false;
    }
    return bus.publish({event_name});
}

bool railway_chapter::request_character_switch(double time_seconds) {
    if (!accept_time(time_seconds)) {
        refuse_input("bad_time", "character_switch");
        return false;
    }
    if (beat != chapter_beat::final_obstacle) {
        refuse_input("switch_not_allowed_now", beat_name(beat));
        return false;
    }
    take_control_of(controlled == character::human ? character::dog : character::human);
    return true;
}

bool railway_chapter::accept_time(double time_seconds) {
    const bool usable = std::isfinite(time_seconds) && time_seconds >= latest_time_seconds &&
                        time_seconds <= maximum_time_seconds;
    if (!usable) {
        return false;
    }
    latest_time_seconds = time_seconds;
    return true;
}

void railway_chapter::refuse_input(const std::string& reason, const std::string& detail) {
    ++refused_inputs;
    log.record(latest_time_seconds, "input_refused", reason + ":" + detail);
}

void railway_chapter::advance_story(const std::string& event_name) {
    // Look up the step for the current beat once, then take at most one step.
    // Checking all triggers against a beat that changes mid-loop could skip two beats.
    for (const beat_trigger& trigger : beat_triggers) {
        if (trigger.from_beat == beat && event_name == trigger.event_name) {
            enter_beat(trigger.to_beat);
            return;
        }
    }
}

void railway_chapter::enter_beat(chapter_beat next_beat) {
    // Second safety check: the story order table has the final say.
    if (!is_allowed_transition(beat, next_beat)) {
        return;
    }
    std::size_t finished_index = 0;
    if (beat_to_index(beat, finished_index)) {
        finished_seconds_by_beat[finished_index] += latest_time_seconds - beat_started_at_seconds;
    }
    beat = next_beat;
    beat_started_at_seconds = latest_time_seconds;
    log.record(latest_time_seconds, "beat_entered", beat_name(next_beat));

    // Who the player controls follows the story, except where the player may choose.
    switch (next_beat) {
        case chapter_beat::dog_section:
            take_control_of(character::dog);
            break;
        case chapter_beat::near_contact:
        case chapter_beat::final_obstacle:
            break;  // Keep whoever the player had.
        default:
            take_control_of(character::human);
            break;
    }
}

void railway_chapter::take_control_of(character who) {
    if (controlled == who) {
        return;
    }
    controlled = who;
    log.record(latest_time_seconds, "control_changed", character_name(who));
}

void railway_chapter::handle_final_action(bool is_human_action) {
    if (beat != chapter_beat::final_obstacle) {
        return;  // The same lever pulled earlier in the chapter does not count.
    }
    if (is_human_action) {
        human_final_action_done = true;
    } else {
        dog_final_action_done = true;
    }
    if (human_final_action_done && dog_final_action_done) {
        bus.publish({"shared_route_opened"});
    }
}

chapter_beat railway_chapter::current_beat() const {
    return beat;
}

character railway_chapter::controlled_character() const {
    return controlled;
}

const bond_state& railway_chapter::bond() const {
    return dog_bond;
}

const telemetry_log& railway_chapter::telemetry() const {
    return log;
}

double railway_chapter::seconds_spent_in(chapter_beat which_beat, double now_seconds) const {
    std::size_t index = 0;
    if (!beat_to_index(which_beat, index)) {
        return 0.0;  // Not a real beat: refuse rather than read outside the array.
    }
    double total = finished_seconds_by_beat[index];
    if (which_beat == beat && now_seconds > beat_started_at_seconds) {
        total += now_seconds - beat_started_at_seconds;
    }
    return total;
}

int railway_chapter::refused_input_count() const {
    return refused_inputs;
}

}  // namespace bodhi
