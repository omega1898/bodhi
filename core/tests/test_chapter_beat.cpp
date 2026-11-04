// test_chapter_beat.cpp
// Purpose: proves the story order allows the designed path and refuses shortcuts.
#include "bodhi/chapter_beat.hpp"

#include <cstring>

#include "test_harness.hpp"

using bodhi::chapter_beat;

void run_chapter_beat_tests() {
    // The designed path is allowed.
    CHECK(bodhi::is_allowed_transition(chapter_beat::together, chapter_beat::separation));
    CHECK(bodhi::is_allowed_transition(chapter_beat::human_section, chapter_beat::dog_section));
    CHECK(bodhi::is_allowed_transition(chapter_beat::dog_section, chapter_beat::human_section));
    CHECK(bodhi::is_allowed_transition(chapter_beat::reunion, chapter_beat::aftermath));

    // Shortcuts and backward steps are refused.
    CHECK(!bodhi::is_allowed_transition(chapter_beat::together, chapter_beat::reunion));
    CHECK(!bodhi::is_allowed_transition(chapter_beat::aftermath, chapter_beat::together));
    CHECK(!bodhi::is_allowed_transition(chapter_beat::separation, chapter_beat::separation));

    // Every beat has a real name; a forced bad value gets "unknown" instead of garbage.
    CHECK(std::strcmp(bodhi::beat_name(chapter_beat::dog_section), "dog_section") == 0);
    CHECK(std::strcmp(bodhi::beat_name(static_cast<chapter_beat>(99)), "unknown") == 0);
}
