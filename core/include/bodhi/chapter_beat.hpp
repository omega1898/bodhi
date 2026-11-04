// chapter_beat.hpp
// Purpose: names every story beat in a chapter and says which beat may follow which.
// This file only declares things; the matching chapter_beat.cpp defines them.
#pragma once

#include <cstddef>

namespace bodhi {

// The story beats of one chapter, in the order the design document lists them.
enum class chapter_beat {
    together,        // Human and dog explore side by side.
    separation,      // An event splits them (the walkway collapses).
    human_section,   // The player controls the human.
    dog_section,     // The player controls the dog.
    near_contact,    // They see or touch each other across a barrier.
    final_obstacle,  // Actions on both routes are needed to finish.
    reunion,         // Physical contact is restored.
    aftermath,       // The quiet ending.
};

// How many beats exist. Used to size arrays that hold one value per beat.
constexpr std::size_t chapter_beat_count = 8;

// Returns a fixed lowercase name for logs, for example "dog_section".
const char* beat_name(chapter_beat beat);

// True when the story may move from `from_beat` straight to `to_beat`.
bool is_allowed_transition(chapter_beat from_beat, chapter_beat to_beat);

}  // namespace bodhi
