// chapter_beat.cpp
// Purpose: the fixed story order of a chapter, written as data that tests can check.
#include "bodhi/chapter_beat.hpp"

namespace bodhi {

const char* beat_name(chapter_beat beat) {
    switch (beat) {
        case chapter_beat::together:       return "together";
        case chapter_beat::separation:     return "separation";
        case chapter_beat::human_section:  return "human_section";
        case chapter_beat::dog_section:    return "dog_section";
        case chapter_beat::near_contact:   return "near_contact";
        case chapter_beat::final_obstacle: return "final_obstacle";
        case chapter_beat::reunion:        return "reunion";
        case chapter_beat::aftermath:      return "aftermath";
    }
    // Reached only if code forces a number that is not a real beat into the type.
    return "unknown";
}

bool is_allowed_transition(chapter_beat from_beat, chapter_beat to_beat) {
    // Every legal step in the chapter. Anything not listed is refused,
    // so a stray event can never skip the player straight to the reunion.
    struct beat_step {
        chapter_beat from_beat;
        chapter_beat to_beat;
    };
    static const beat_step allowed_steps[] = {
        {chapter_beat::together, chapter_beat::separation},
        {chapter_beat::separation, chapter_beat::human_section},
        {chapter_beat::human_section, chapter_beat::dog_section},
        {chapter_beat::dog_section, chapter_beat::human_section},
        {chapter_beat::human_section, chapter_beat::near_contact},
        {chapter_beat::dog_section, chapter_beat::near_contact},
        {chapter_beat::near_contact, chapter_beat::final_obstacle},
        {chapter_beat::final_obstacle, chapter_beat::reunion},
        {chapter_beat::reunion, chapter_beat::aftermath},
    };
    for (const beat_step& step : allowed_steps) {
        if (step.from_beat == from_beat && step.to_beat == to_beat) {
            return true;
        }
    }
    return false;
}

}  // namespace bodhi
