// companion_behavior.cpp
// Purpose: the rule that turns the dog's feelings into follow distance and check-backs.
#include "bodhi/companion_behavior.hpp"

#include <algorithm>
#include <cmath>

namespace bodhi {
namespace {

// Brings one feeling into 0.0 to 1.0 before it is used.
// NaN ("not a number") becomes 1.0, the most anxious value, so a broken number makes
// the dog cautious instead of letting it roam. Infinity is clamped like any big number.
double anxiety_in_unit_range(double feeling) {
    if (std::isnan(feeling)) {
        return 1.0;
    }
    return std::clamp(feeling, 0.0, 1.0);
}

// Slides from calm_value (anxiety 0.0) to anxious_value (anxiety 1.0) in a straight line.
// anxiety must already be inside 0.0 to 1.0, so the answer stays between the two values.
double blend(double calm_value, double anxious_value, double anxiety) {
    return calm_value + (anxious_value - calm_value) * anxiety;
}

}  // namespace

companion_behavior choose_companion_behavior(const bond_state& bond) {
    // The stronger of the two feelings decides: a frightened dog stays close even if it
    // does not usually need closeness, and the reverse. Trust does not affect movement yet.
    const double anxiety = std::max(anxiety_in_unit_range(bond.closeness_need),
                                    anxiety_in_unit_range(bond.fear));

    companion_behavior behavior;
    behavior.follow_distance_meters =
        blend(farthest_follow_distance_meters, closest_follow_distance_meters, anxiety);
    behavior.checks_back_interval_seconds =
        blend(longest_check_back_interval_seconds, shortest_check_back_interval_seconds, anxiety);
    behavior.may_run_ahead = anxiety < run_ahead_anxiety_limit;
    return behavior;
}

}  // namespace bodhi
