// bond_state.cpp
// Purpose: the rules for how story events change the dog's feelings.
#include "bodhi/bond_state.hpp"

#include <algorithm>
#include <cmath>

namespace bodhi {
namespace {

// How one story event shifts the dog's feelings. Positive numbers raise a feeling.
struct bond_rule {
    const char* event_name;
    double fear_change;
    double closeness_need_change;
    double trust_change;
    double minimum_closeness_need;  // After this event, closeness need is at least this high.
};

// The whole rule set in one table, so a designer can tune it without reading logic.
const bond_rule bond_rules[] = {
    {"walkway_collapsed",     0.6,  0.3,  0.0,  0.0},
    {"whistle_heard",        -0.1,  0.0,  0.05, 0.0},
    {"glove_found",          -0.1,  0.0,  0.0,  0.0},
    {"fence_touch_finished", -0.2,  0.0,  0.1,  0.0},
    {"reunion_contact_made", -0.5,  0.0,  0.1,  0.9},
    {"rested_together",      -0.2, -0.2,  0.0,  0.0},
};

// Keeps a feeling inside 0.0 to 1.0 however many events pile up.
// A broken number (NaN, "not a number") becomes 0.0 so it cannot spread.
double keep_in_unit_range(double value) {
    if (std::isnan(value)) {
        return 0.0;
    }
    return std::clamp(value, 0.0, 1.0);
}

}  // namespace

void apply_bond_event(bond_state& bond, const std::string& event_name) {
    for (const bond_rule& rule : bond_rules) {
        if (event_name != rule.event_name) {
            continue;
        }
        bond.fear = keep_in_unit_range(bond.fear + rule.fear_change);
        bond.closeness_need = keep_in_unit_range(
            std::max(bond.closeness_need + rule.closeness_need_change, rule.minimum_closeness_need));
        bond.trust = keep_in_unit_range(bond.trust + rule.trust_change);
        return;
    }
}

}  // namespace bodhi
