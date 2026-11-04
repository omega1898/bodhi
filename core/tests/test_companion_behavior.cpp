// test_companion_behavior.cpp
// Purpose: proves anxious dogs stay close and check back often, and that no input,
// however broken, can push the behavior outside its fixed safe ranges.
#include "bodhi/companion_behavior.hpp"

#include <cmath>
#include <limits>

#include "test_harness.hpp"

using bodhi::bond_state;
using bodhi::choose_companion_behavior;
using bodhi::companion_behavior;

namespace {

// True when every number in the behavior is real and inside the documented ranges.
bool is_inside_safe_ranges(const companion_behavior& behavior) {
    return std::isfinite(behavior.follow_distance_meters) &&
           behavior.follow_distance_meters >= bodhi::closest_follow_distance_meters &&
           behavior.follow_distance_meters <= bodhi::farthest_follow_distance_meters &&
           std::isfinite(behavior.checks_back_interval_seconds) &&
           behavior.checks_back_interval_seconds >= bodhi::shortest_check_back_interval_seconds &&
           behavior.checks_back_interval_seconds <= bodhi::longest_check_back_interval_seconds;
}

// Makes a bond with the given feelings, so each test reads in one line.
bond_state make_bond(double closeness_need, double fear, double trust) {
    bond_state bond;
    bond.closeness_need = closeness_need;
    bond.fear = fear;
    bond.trust = trust;
    return bond;
}

}  // namespace

void run_companion_behavior_tests() {
    // A completely calm dog trails at the farthest distance, explores, and rarely looks back.
    {
        companion_behavior calm = choose_companion_behavior(make_bond(0.0, 0.0, 0.8));
        CHECK(calm.follow_distance_meters == bodhi::farthest_follow_distance_meters);
        CHECK(calm.checks_back_interval_seconds == bodhi::longest_check_back_interval_seconds);
        CHECK(calm.may_run_ahead);
    }

    // A terrified dog stays as close as allowed, never runs ahead, and looks back most often.
    {
        companion_behavior terrified = choose_companion_behavior(make_bond(0.0, 1.0, 0.8));
        CHECK(terrified.follow_distance_meters == bodhi::closest_follow_distance_meters);
        CHECK(terrified.checks_back_interval_seconds == bodhi::shortest_check_back_interval_seconds);
        CHECK(!terrified.may_run_ahead);
    }

    // High closeness need alone is enough to keep the dog close.
    {
        companion_behavior clingy = choose_companion_behavior(make_bond(0.9, 0.0, 0.8));
        CHECK(clingy.follow_distance_meters < 2.0);
        CHECK(!clingy.may_run_ahead);
    }

    // More fear never means farther away or fewer check-backs.
    {
        double previous_distance = bodhi::farthest_follow_distance_meters;
        double previous_interval = bodhi::longest_check_back_interval_seconds;
        for (int step = 0; step <= 100; ++step) {
            companion_behavior behavior = choose_companion_behavior(make_bond(0.2, step / 100.0, 0.8));
            CHECK(behavior.follow_distance_meters <= previous_distance);
            CHECK(behavior.checks_back_interval_seconds <= previous_interval);
            previous_distance = behavior.follow_distance_meters;
            previous_interval = behavior.checks_back_interval_seconds;
        }
    }

    // The default bond from bond_state.hpp lets the dog explore.
    {
        CHECK(choose_companion_behavior(bond_state{}).may_run_ahead);
    }

    // After the walkway collapse in the real rules, the dog stays close.
    {
        bond_state bond;
        companion_behavior before = choose_companion_behavior(bond);
        bodhi::apply_bond_event(bond, "walkway_collapsed");
        companion_behavior after = choose_companion_behavior(bond);
        CHECK(!after.may_run_ahead);
        CHECK(after.follow_distance_meters < before.follow_distance_meters);
        CHECK(after.checks_back_interval_seconds < before.checks_back_interval_seconds);
    }

    // Break it: NaN in a feeling must not let the dog roam; it is treated as most anxious.
    {
        const double not_a_number = std::numeric_limits<double>::quiet_NaN();
        companion_behavior from_nan_fear = choose_companion_behavior(make_bond(0.0, not_a_number, 0.8));
        CHECK(is_inside_safe_ranges(from_nan_fear));
        CHECK(!from_nan_fear.may_run_ahead);
        CHECK(from_nan_fear.follow_distance_meters == bodhi::closest_follow_distance_meters);

        companion_behavior from_nan_closeness =
            choose_companion_behavior(make_bond(not_a_number, 0.0, 0.8));
        CHECK(is_inside_safe_ranges(from_nan_closeness));
        CHECK(!from_nan_closeness.may_run_ahead);

        // NaN trust is ignored: trust does not drive movement.
        CHECK(is_inside_safe_ranges(choose_companion_behavior(make_bond(0.0, 0.0, not_a_number))));
    }

    // Break it: every combination of hostile numbers stays inside the safe ranges.
    {
        const double infinity = std::numeric_limits<double>::infinity();
        const double hostile_values[] = {
            std::numeric_limits<double>::quiet_NaN(),
            infinity,
            -infinity,
            std::numeric_limits<double>::max(),
            std::numeric_limits<double>::lowest(),
            std::numeric_limits<double>::denorm_min(),
            -0.0,
            -1.0,
            0.5,
            2.0,
            1e308,
        };
        for (double closeness_need : hostile_values) {
            for (double fear : hostile_values) {
                for (double trust : hostile_values) {
                    CHECK(is_inside_safe_ranges(
                        choose_companion_behavior(make_bond(closeness_need, fear, trust))));
                }
            }
        }
    }

    // Break it: huge negative feelings are clamped to calm, not to "beyond calm".
    {
        const double infinity = std::numeric_limits<double>::infinity();
        companion_behavior behavior = choose_companion_behavior(make_bond(-infinity, -infinity, 0.8));
        CHECK(behavior.follow_distance_meters == bodhi::farthest_follow_distance_meters);
        CHECK(behavior.may_run_ahead);
    }
}
