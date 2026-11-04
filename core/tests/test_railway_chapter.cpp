// test_railway_chapter.cpp
// Purpose: plays chapter one from start to finish with no graphics, then tries to break it.
#include "bodhi/railway_chapter.hpp"

#include <string>

#include "test_harness.hpp"

using bodhi::chapter_beat;
using bodhi::character;
using bodhi::railway_chapter;

namespace {

// Plays the chapter up to the start of the final obstacle, as the level design intends.
void play_until_final_obstacle(railway_chapter& chapter) {
    chapter.report_world_event("walkway_collapsed", 60.0);
    chapter.report_world_event("separation_scene_finished", 75.0);
    chapter.report_world_event("maintenance_power_restored", 140.0);
    chapter.report_world_event("glove_found", 200.0);
    chapter.report_world_event("dog_barked_from_window", 230.0);
    chapter.report_world_event("pair_met_at_fence", 300.0);
    chapter.report_world_event("fence_touch_finished", 345.0);
}

}  // namespace

void run_railway_chapter_tests() {
    // The full designed path, checking story, control and feelings at each stage.
    {
        railway_chapter chapter;
        CHECK(chapter.current_beat() == chapter_beat::together);
        CHECK(chapter.controlled_character() == character::human);

        chapter.report_world_event("walkway_collapsed", 60.0);
        CHECK(chapter.current_beat() == chapter_beat::separation);
        CHECK(chapter.bond().fear > 0.5);

        chapter.report_world_event("separation_scene_finished", 75.0);
        CHECK(chapter.current_beat() == chapter_beat::human_section);

        // The human's action on one route opens the dog's route and hands control to the dog.
        chapter.report_world_event("maintenance_power_restored", 140.0);
        CHECK(chapter.current_beat() == chapter_beat::dog_section);
        CHECK(chapter.controlled_character() == character::dog);

        // The dog's bark reveals the human's way through and hands control back.
        chapter.report_world_event("dog_barked_from_window", 230.0);
        CHECK(chapter.current_beat() == chapter_beat::human_section);
        CHECK(chapter.controlled_character() == character::human);

        chapter.report_world_event("pair_met_at_fence", 300.0);
        CHECK(chapter.current_beat() == chapter_beat::near_contact);
        chapter.report_world_event("fence_touch_finished", 345.0);
        CHECK(chapter.current_beat() == chapter_beat::final_obstacle);

        // Playtest measure: how long the player lingered at the fence.
        CHECK(chapter.seconds_spent_in(chapter_beat::near_contact, 345.0) == 45.0);

        // Both routes must act: lever as the human, switch, clear debris as the dog.
        chapter.report_world_event("jammed_gate_lever_pulled", 380.0);
        CHECK(chapter.current_beat() == chapter_beat::final_obstacle);
        CHECK(chapter.request_character_switch(381.0));
        CHECK(chapter.controlled_character() == character::dog);
        chapter.report_world_event("debris_cleared", 420.0);
        CHECK(chapter.current_beat() == chapter_beat::reunion);
        CHECK(chapter.controlled_character() == character::human);

        chapter.report_world_event("reunion_contact_made", 430.0);
        CHECK(chapter.bond().closeness_need >= 0.9);
        chapter.report_world_event("reunion_scene_finished", 470.0);
        CHECK(chapter.current_beat() == chapter_beat::aftermath);
        CHECK(chapter.refused_input_count() == 0);
    }

    // Break it: an event that belongs later in the story does nothing early on.
    {
        railway_chapter chapter;
        chapter.report_world_event("reunion_scene_finished", 5.0);
        CHECK(chapter.current_beat() == chapter_beat::together);
    }

    // Break it: announcing the final obstacle's result directly is refused.
    {
        railway_chapter chapter;
        play_until_final_obstacle(chapter);
        CHECK(!chapter.report_world_event("shared_route_opened", 350.0));
        CHECK(chapter.current_beat() == chapter_beat::final_obstacle);
    }

    // Break it: one route's action alone is not enough, even if repeated.
    {
        railway_chapter chapter;
        play_until_final_obstacle(chapter);
        chapter.report_world_event("jammed_gate_lever_pulled", 360.0);
        chapter.report_world_event("jammed_gate_lever_pulled", 361.0);
        CHECK(chapter.current_beat() == chapter_beat::final_obstacle);
    }

    // Break it: the final action done before the final obstacle does not count later.
    {
        railway_chapter chapter;
        chapter.report_world_event("debris_cleared", 10.0);
        play_until_final_obstacle(chapter);
        chapter.report_world_event("jammed_gate_lever_pulled", 360.0);
        CHECK(chapter.current_beat() == chapter_beat::final_obstacle);
    }

    // Break it: switching characters outside the final obstacle is refused.
    {
        railway_chapter chapter;
        CHECK(!chapter.request_character_switch(1.0));
        CHECK(chapter.controlled_character() == character::human);
    }

    // Break it: bad names and bad times are refused and counted, not acted on.
    {
        railway_chapter chapter;
        chapter.report_world_event("walkway_collapsed", 60.0);
        CHECK(!chapter.report_world_event("", 61.0));
        CHECK(!chapter.report_world_event("Walkway_Collapsed", 61.0));
        CHECK(!chapter.report_world_event("x\",\"kind\":\"admin", 61.0));
        CHECK(!chapter.report_world_event(std::string(100000, 'a'), 61.0));
        CHECK(!chapter.report_world_event("separation_scene_finished", 30.0));  // Time went backwards.
        CHECK(chapter.current_beat() == chapter_beat::separation);
        CHECK(chapter.refused_input_count() == 5);

        // The huge name was cut to 64 characters before it reached the log.
        bool every_detail_is_short = true;
        for (const bodhi::telemetry_entry& entry : chapter.telemetry().entries()) {
            if (entry.detail.size() > 100) {
                every_detail_is_short = false;
            }
        }
        CHECK(every_detail_is_short);
    }

    // Break it: a beat value that is not a real beat cannot read outside memory.
    {
        railway_chapter chapter;
        CHECK(chapter.seconds_spent_in(static_cast<chapter_beat>(99), 10.0) == 0.0);
    }
}
