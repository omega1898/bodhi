// companion_behavior.hpp
// Purpose: turns the dog's feelings (bond_state) into how the dog moves beside the human.
// Unreal's dog character reads these three numbers each moment; the core decides them.
// Example: right after the walkway collapses, fear is high, so the dog follows
// at about one metre, never runs ahead, and looks back every couple of seconds.
#pragma once

#include "bodhi/bond_state.hpp"

namespace bodhi {

// The fixed safe ranges. Every companion_behavior returned stays inside them,
// whatever numbers arrive in the bond_state (including NaN or infinity).
// "constexpr" means the value is fixed when the program is compiled and can never change.
constexpr double closest_follow_distance_meters = 1.0;   // When the dog is most anxious.
constexpr double farthest_follow_distance_meters = 6.0;  // When the dog is completely calm.
constexpr double shortest_check_back_interval_seconds = 2.0;   // Most anxious.
constexpr double longest_check_back_interval_seconds = 12.0;   // Completely calm.
// The dog may run ahead only while its anxiety is below this level (0.0 to 1.0).
constexpr double run_ahead_anxiety_limit = 0.5;

// How the dog should move relative to the human right now.
struct companion_behavior {
    double follow_distance_meters = farthest_follow_distance_meters;      // How far behind it trails.
    bool may_run_ahead = false;                                           // Whether it may explore ahead.
    double checks_back_interval_seconds = longest_check_back_interval_seconds;  // Time between glances back.
};

// Works out the dog's movement from its feelings. It changes nothing and has no memory,
// so the same bond always gives the same behavior.
// Rule: the higher closeness_need or fear is, the closer the dog stays and the more
// often it checks back. A broken number (NaN) is treated as the most anxious value,
// because a dog that stays close is the safe mistake; a dog that wanders off is not.
companion_behavior choose_companion_behavior(const bond_state& bond);

}  // namespace bodhi
