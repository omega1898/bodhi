// chapter_demo.cpp
// Purpose: plays chapter one with scripted events and prints the playtest log,
// so you can see the core working without any game engine.
// Run it after building:  ./core/build/chapter_demo
#include <cstdio>
#include <cstring>

#include "bodhi/railway_chapter.hpp"

int main() {
    bodhi::railway_chapter chapter;

    // Each pair is (seconds since the chapter started, what happened in the world).
    struct scripted_event {
        double time_seconds;
        const char* event_name;
    };
    const scripted_event script[] = {
        {60.0, "walkway_collapsed"},
        {75.0, "separation_scene_finished"},
        {140.0, "maintenance_power_restored"},
        {170.0, "whistle_heard"},
        {200.0, "glove_found"},
        {230.0, "dog_barked_from_window"},
        {300.0, "pair_met_at_fence"},
        {345.0, "fence_touch_finished"},
        {380.0, "jammed_gate_lever_pulled"},
        {381.0, "switch_character"},  // Not a world event: the player presses "switch".
        {420.0, "debris_cleared"},
        {430.0, "reunion_contact_made"},
        {470.0, "reunion_scene_finished"},
        {520.0, "rested_together"},
    };

    for (const scripted_event& step : script) {
        if (std::strcmp(step.event_name, "switch_character") == 0) {
            chapter.request_character_switch(step.time_seconds);
        } else {
            chapter.report_world_event(step.event_name, step.time_seconds);
        }
    }

    std::fputs(chapter.telemetry().to_json_lines().c_str(), stdout);
    std::fprintf(stderr, "final beat: %s | fence seconds: %.1f | fear %.2f closeness %.2f trust %.2f\n",
                 bodhi::beat_name(chapter.current_beat()),
                 chapter.seconds_spent_in(bodhi::chapter_beat::near_contact, 520.0), chapter.bond().fear,
                 chapter.bond().closeness_need, chapter.bond().trust);
    return chapter.current_beat() == bodhi::chapter_beat::aftermath ? 0 : 1;
}
