// text_chapter.cpp
// Purpose: a playable text version of chapter one ("The Other Side").
// It stands in for Unreal Engine: it knows the station's places and what the player typed,
// reports world events by name to bodhi::railway_chapter, and reads back the story beat and
// which character the player controls. That lets anyone play the whole chapter before a 3D
// level exists, and the place and path tables below are the blueprint for the first level.
//
// Play:    ./core/build/text_chapter
// Replay:  ./core/build/text_chapter core/tests/playthroughs/full_chapter.txt
#include <cstdio>
#include <set>
#include <string>

#include "bodhi/railway_chapter.hpp"

namespace {

using bodhi::chapter_beat;
using bodhi::character;

// Longest command line kept. Longer lines are skipped rather than stored, so a script
// file with one enormous line cannot use up memory.
constexpr std::size_t maximum_line_length = 200;

// How many seconds of play each typed command, and each short scene, stands for.
constexpr double seconds_per_command = 10.0;
constexpr double seconds_per_scene = 15.0;

struct place {
    const char* name;              // What the player types after "go".
    const char* description;       // What "look" prints.
    const char* event_on_arrival;  // Reported the first time anyone arrives, or nullptr.
};

const place places[] = {
    {"station_entrance", "The station entrance at dusk. Floodwater laps at the bottom step.", nullptr},
    {"upper_walkway", "A rusted walkway high above the flooded lower platform.", nullptr},
    {"signal_room", "The signal room. A breaker panel marked MAINTENANCE POWER hangs open.", nullptr},
    {"fence", "The footbridge ends at an old iron fence. Beyond it, a path to the cabin.",
     "pair_met_at_fence"},
    {"lower_platform", "The lower platform. Wet concrete, rust and diesel. A gate to the luggage room.",
     nullptr},
    {"luggage_room", "Toppled suitcases and a long-dead conveyor belt.", nullptr},
    {"flooded_passage", "Water rushes through a broken wall, loud enough to drown out everything.",
     nullptr},
    {"railing_walk", "A narrow ledge beside a railing, above the rushing water.", nullptr},
    {"waiting_room", "An old waiting room. A tall window looks up toward the station above.", nullptr},
    {"fence_dog_side", "Your side of the iron fence. Boards and debris jam the bottom of the gate.",
     nullptr},
    {"cabin_path", "The path beyond the gate, wet and quiet.", nullptr},
    {"maintenance_cabin", "The maintenance cabin. Dry walls, a stove, rain on the roof.", nullptr},
};

// A path works in both directions, and stays closed until its required event has happened.
struct path {
    const char* first_place;
    const char* second_place;
    const char* required_event;  // nullptr means always open.
};

const path paths[] = {
    {"station_entrance", "upper_walkway", nullptr},
    {"upper_walkway", "signal_room", "walkway_collapsed"},
    {"signal_room", "fence", "dog_barked_from_window"},
    {"lower_platform", "luggage_room", "maintenance_power_restored"},
    {"luggage_room", "flooded_passage", nullptr},
    {"flooded_passage", "railing_walk", nullptr},
    // The scent is lost at the water. Only the glove picks it up again.
    {"railing_walk", "waiting_room", "glove_found"},
};

// Something the controlled character can do in one place. Doing it reports one event.
struct action {
    const char* place_name;
    const char* command;          // What the player types.
    const char* event_name;       // What the engine reports to the core.
    const char* required_event;   // Hidden until this has happened; nullptr means never hidden.
    bool repeatable;              // false: hidden once done.
    const char* response;         // What the player sees.
    const char* follow_up_event;  // A short scene reported straight after, or nullptr.
    bool ends_chapter;
};

const action actions[] = {
    {"station_entrance", "pet", "dog_petted", nullptr, true,
     "Your dog leans into your hand, then trots ahead and looks back to check you're coming.",
     nullptr, false},
    {"station_entrance", "whistle", "whistle_heard", nullptr, true,
     "Ears lift. Paws splash straight back to your side.", nullptr, false},
    {"upper_walkway", "cross", "walkway_collapsed", nullptr, false,
     "You step out onto the walkway, your dog just ahead of you.", nullptr, false},
    {"signal_room", "restore_power", "maintenance_power_restored", nullptr, false,
     "You throw the breaker. Somewhere below, a motor whines.", nullptr, false},
    {"signal_room", "whistle", "whistle_heard", nullptr, true,
     "You whistle. From somewhere below, a bark answers.", nullptr, false},
    {"lower_platform", "listen", "whistle_heard", nullptr, true,
     "High above, a whistle. It comes from beyond the luggage room.", nullptr, false},
    {"lower_platform", "sniff", "scent_sniffed", nullptr, true,
     "Your human's scent, rain and soap, drifts toward the luggage room.", nullptr, false},
    {"luggage_room", "sniff", "scent_sniffed", nullptr, true,
     "The scent weaves between the suitcases, toward the sound of water.", nullptr, false},
    {"flooded_passage", "sniff", "scent_sniffed", nullptr, true,
     "Only water. The scent is gone. You whine and cast about.", nullptr, false},
    {"railing_walk", "sniff", "scent_sniffed", nullptr, true,
     "Something familiar is caught on the railing.", nullptr, false},
    {"railing_walk", "take_glove", "glove_found", nullptr, false,
     "A glove. Your human's glove. The scent is back, and it leads to the waiting room.", nullptr,
     false},
    {"waiting_room", "bark", "dog_barked_from_window", nullptr, false,
     "Paws on the sill, you bark and bark at the window.", nullptr, false},
    {"fence", "touch", "fence_touch_finished", "pair_met_at_fence", false,
     "You reach through the bars. A warm nose pushes into your palm.", nullptr, false},
    {"fence", "pull_lever", "jammed_gate_lever_pulled", "fence_touch_finished", false,
     "You throw your weight on the lever. It grinds down and locks.", nullptr, false},
    {"fence_dog_side", "clear_debris", "debris_cleared", "fence_touch_finished", false,
     "You dig and drag the boards clear with teeth and paws.", nullptr, false},
    {"cabin_path", "approach", "human_approached", nullptr, false,
     "You walk toward each other, slowly at first.", nullptr, false},
    {"cabin_path", "kneel", "human_knelt", "human_approached", false,
     "You kneel on the wet path. Your dog stops a step away, watching your face.", nullptr, false},
    {"cabin_path", "hold", "reunion_contact_made", "human_knelt", false,
     "You open your arms. Your dog presses into your chest and stays there, shaking, then still.",
     "reunion_scene_finished", false},
    {"maintenance_cabin", "rest", "rested_together", nullptr, false,
     "Rain on the roof. Your dog falls asleep against your leg. The glove dries beside the stove.",
     nullptr, true},
};

// What happens on screen when the story moves from one beat to another, like a short cutscene.
struct scene {
    chapter_beat from_beat;
    chapter_beat to_beat;
    const char* text;
    const char* human_moves_to;   // nullptr means the human stays put.
    const char* dog_moves_to;     // nullptr means the dog stays put.
    const char* follow_up_event;  // Reported when the scene ends, or nullptr.
};

const scene scenes[] = {
    {chapter_beat::together, chapter_beat::separation,
     "The walkway groans and gives way. Your dog drops with the boards onto the lower platform.\n"
     "You cling to the railing above. Below, your dog scrambles up, barking.",
     nullptr, "lower_platform", "separation_scene_finished"},
    {chapter_beat::separation, chapter_beat::human_section,
     "You are alone. The way down is gone, but the signal room door hangs open.", nullptr, nullptr,
     nullptr},
    {chapter_beat::human_section, chapter_beat::dog_section,
     "Below, lights flicker on and a gate rattles open.\n-- You are now your dog. --", nullptr,
     nullptr, nullptr},
    {chapter_beat::dog_section, chapter_beat::human_section,
     "Across the station, a bark rings out from a window and keeps going. Following it,\n"
     "you spot a footbridge you missed.\n-- You are the human again. --",
     nullptr, "fence_dog_side", nullptr},
    {chapter_beat::human_section, chapter_beat::near_contact,
     "Your dog is on the other side of the fence, tail going, your lost glove dropped at the bars.",
     nullptr, nullptr, nullptr},
    {chapter_beat::near_contact, chapter_beat::final_obstacle,
     "The gate won't open: a jammed lever on your side, debris on your dog's side.\n"
     "Both sides need work. Type switch to change who you control.",
     nullptr, nullptr, nullptr},
    {chapter_beat::final_obstacle, chapter_beat::reunion,
     "The gate shudders and swings free. Nothing between you now.", "cabin_path", "cabin_path",
     nullptr},
    {chapter_beat::reunion, chapter_beat::aftermath, "Together, you walk the last stretch to the cabin.",
     "maintenance_cabin", "maintenance_cabin", nullptr},
};

// Everything the stand-in engine remembers between commands.
struct text_game {
    bodhi::railway_chapter chapter;
    std::string human_place = "station_entrance";
    std::string dog_place = "station_entrance";
    std::set<std::string> events_that_happened;  // Only events the core accepted.
    double clock_seconds = 0.0;
    bool chapter_ended = false;
};

// The place of whichever character the player controls right now.
std::string& controlled_place(text_game& game) {
    return game.chapter.controlled_character() == character::dog ? game.dog_place : game.human_place;
}

// True when `event_name` is nullptr (no requirement) or the core has accepted that event.
bool has_happened(const text_game& game, const char* event_name) {
    return event_name == nullptr || game.events_that_happened.count(event_name) > 0;
}

// Finds a place by its typed name. Returns nullptr when no such place exists.
const place* find_place(const std::string& place_name) {
    for (const place& candidate : places) {
        if (place_name == candidate.name) {
            return &candidate;
        }
    }
    return nullptr;
}

// True when an open path joins the two places, in either direction.
bool path_is_open(const text_game& game, const std::string& from, const std::string& to) {
    for (const path& candidate : paths) {
        const bool joins_them = (from == candidate.first_place && to == candidate.second_place) ||
                                (from == candidate.second_place && to == candidate.first_place);
        if (joins_them && has_happened(game, candidate.required_event)) {
            return true;
        }
    }
    return false;
}

// True when the action is in this place and neither hidden by its requirement nor already done.
bool action_is_available(const text_game& game, const action& candidate, const std::string& here) {
    return here == candidate.place_name && has_happened(game, candidate.required_event) &&
           (candidate.repeatable || !has_happened(game, candidate.event_name));
}

// Makes typed text safe to print back. Invisible control bytes can move the cursor or clear
// the screen ("terminal escape codes"), so anything outside plain printable ASCII becomes '?'.
std::string printable(const std::string& text) {
    std::string safe_text;
    for (const char text_byte : text) {
        const unsigned char byte_value = static_cast<unsigned char>(text_byte);
        safe_text += (byte_value >= 0x20 && byte_value < 0x7f) ? text_byte : '?';
    }
    return safe_text;
}

// Prints where the controlled character is, the open paths and the available actions.
void print_surroundings(text_game& game) {
    const std::string& here = controlled_place(game);
    const place* current = find_place(here);
    std::printf("\n[%s] %s: %s\n", bodhi::character_name(game.chapter.controlled_character()),
                here.c_str(), current != nullptr ? current->description : "");
    std::printf("Paths:");
    for (const place& other : places) {
        if (path_is_open(game, here, other.name)) {
            std::printf(" %s", other.name);
        }
    }
    std::printf("\nYou can:");
    for (const action& candidate : actions) {
        if (action_is_available(game, candidate, here)) {
            std::printf(" %s", candidate.command);
        }
    }
    if (game.chapter.current_beat() == chapter_beat::final_obstacle) {
        std::printf(" switch");
    }
    std::printf("\n");
}

// Reports one event to the core. If the story beat changes, plays the matching scene,
// which may report a follow-up event. That calls this same function again ("recursion");
// it always stops, because each scene table row can only run on its own beat change.
void report(text_game& game, const char* event_name) {
    const chapter_beat beat_before = game.chapter.current_beat();
    if (!game.chapter.report_world_event(event_name, game.clock_seconds)) {
        std::printf("(the chapter rules refused %s)\n", event_name);
        return;
    }
    game.events_that_happened.insert(event_name);
    const chapter_beat beat_after = game.chapter.current_beat();
    if (beat_after == beat_before) {
        return;
    }
    for (const scene& candidate : scenes) {
        if (candidate.from_beat != beat_before || candidate.to_beat != beat_after) {
            continue;
        }
        std::printf("\n%s\n", candidate.text);
        if (candidate.human_moves_to != nullptr) {
            game.human_place = candidate.human_moves_to;
        }
        if (candidate.dog_moves_to != nullptr) {
            game.dog_place = candidate.dog_moves_to;
        }
        if (candidate.follow_up_event != nullptr) {
            game.clock_seconds += seconds_per_scene;
            report(game, candidate.follow_up_event);
        }
        return;
    }
}

// Moves the controlled character, if an open path leads there.
void go_to(text_game& game, const std::string& destination) {
    if (find_place(destination) == nullptr || !path_is_open(game, controlled_place(game), destination)) {
        std::printf("You can't get to '%s' from here.\n", printable(destination).c_str());
        return;
    }
    controlled_place(game) = destination;
    if (game.chapter.current_beat() == chapter_beat::together) {
        game.dog_place = destination;  // Before the separation, the dog follows you.
    }
    const char* arrival_event = find_place(destination)->event_on_arrival;
    if (arrival_event != nullptr && !has_happened(game, arrival_event)) {
        report(game, arrival_event);
    }
}

// Turns a typed line into lowercase with no spaces at either end, so "  Look\r" means "look".
std::string normalize(const std::string& line) {
    std::string lowered;
    for (const char letter : line) {
        lowered += (letter >= 'A' && letter <= 'Z') ? static_cast<char>(letter - 'A' + 'a') : letter;
    }
    const char* blank_characters = " \t\r";
    const std::size_t first = lowered.find_first_not_of(blank_characters);
    if (first == std::string::npos) {
        return "";
    }
    const std::size_t last = lowered.find_last_not_of(blank_characters);
    return lowered.substr(first, last - first + 1);
}

// Carries out one typed command. Returns false when the player asked to quit.
// Typed text is only ever compared with the tables above, never passed to the core,
// so typing an internal event name such as "shared_route_opened" does nothing.
bool run_command(text_game& game, const std::string& line) {
    const std::string command = normalize(line);
    if (command.empty()) {
        return true;
    }
    game.clock_seconds += seconds_per_command;
    const std::string place_before = controlled_place(game);
    const chapter_beat beat_before = game.chapter.current_beat();
    const character controlled_before = game.chapter.controlled_character();

    if (command == "quit") {
        return false;
    } else if (command == "help") {
        std::printf("Type look, go <place>, help, quit, or anything listed after 'You can:'.\n");
    } else if (command == "look") {
        print_surroundings(game);
    } else if (command == "switch") {
        if (!game.chapter.request_character_switch(game.clock_seconds)) {
            std::printf("You can only switch when both sides of the gate need you.\n");
        }
    } else if (command.compare(0, 3, "go ") == 0) {
        go_to(game, normalize(command.substr(3)));
    } else {
        const action* chosen = nullptr;
        for (const action& candidate : actions) {
            if (command == candidate.command && action_is_available(game, candidate, place_before)) {
                chosen = &candidate;
                break;
            }
        }
        if (chosen == nullptr) {
            std::printf("There's no '%s' to do here. Type help.\n", printable(command).c_str());
            return true;
        }
        std::printf("%s\n", chosen->response);
        report(game, chosen->event_name);
        if (chosen->follow_up_event != nullptr) {
            game.clock_seconds += seconds_per_scene;
            report(game, chosen->follow_up_event);
        }
        game.chapter_ended = chosen->ends_chapter;
    }

    const bool something_changed = controlled_place(game) != place_before ||
                                   game.chapter.current_beat() != beat_before ||
                                   game.chapter.controlled_character() != controlled_before;
    if (something_changed && !game.chapter_ended) {
        print_surroundings(game);
    }
    return true;
}

// Reads one line, keeping at most maximum_line_length characters and dropping the rest.
// Returns false at the end of the input. Sets `was_too_long` when characters were dropped.
bool read_line(std::FILE* input, std::string& line, bool& was_too_long) {
    line.clear();
    was_too_long = false;
    int next_character = std::fgetc(input);
    if (next_character == EOF) {
        return false;
    }
    while (next_character != EOF && next_character != '\n') {
        if (line.size() < maximum_line_length) {
            line += static_cast<char>(next_character);
        } else {
            was_too_long = true;
        }
        next_character = std::fgetc(input);
    }
    return true;
}

}  // namespace

// Plays from the keyboard, or replays a file of commands (one per line) if one is given.
// Exit code: 0 when the chapter is finished, 1 when play stopped early, 2 for a bad argument.
int main(int argument_count, char* arguments[]) {
    if (argument_count > 2) {
        std::fprintf(stderr, "usage: text_chapter [file_of_commands]\n");
        return 2;
    }
    std::FILE* input = stdin;
    const bool replaying_file = argument_count == 2;
    if (replaying_file) {
        input = std::fopen(arguments[1], "rb");
        if (input == nullptr) {
            std::fprintf(stderr, "could not open %s\n", arguments[1]);
            return 2;
        }
    }

    text_game game;
    std::printf("THE OTHER SIDE\nNightfall. Beyond the flooded station is a cabin where you can shelter.\n");
    print_surroundings(game);

    std::string line;
    bool was_too_long = false;
    while (!game.chapter_ended) {
        if (!replaying_file) {
            std::printf("> ");
            std::fflush(stdout);
        }
        if (!read_line(input, line, was_too_long)) {
            break;
        }
        if (replaying_file) {
            std::printf("> %s\n", printable(line).c_str());
        }
        if (was_too_long) {
            std::printf("(line longer than %zu characters ignored)\n", maximum_line_length);
            continue;
        }
        if (!run_command(game, line)) {
            break;
        }
    }
    if (replaying_file) {
        std::fclose(input);
    }

    if (game.chapter_ended) {
        std::printf("\nchapter finished | seconds at the fence: %.0f | refused inputs: %d\n",
                    game.chapter.seconds_spent_in(chapter_beat::near_contact, game.clock_seconds),
                    game.chapter.refused_input_count());
        return 0;
    }
    std::printf("\nstopped at beat: %s\n", bodhi::beat_name(game.chapter.current_beat()));
    return 1;
}
