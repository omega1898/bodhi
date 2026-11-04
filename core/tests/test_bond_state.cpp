// test_bond_state.cpp
// Purpose: proves story events move the dog's feelings as designed and never out of range.
#include "bodhi/bond_state.hpp"

#include "test_harness.hpp"

using bodhi::apply_bond_event;
using bodhi::bond_state;

void run_bond_state_tests() {
    // The collapse frightens the dog and makes it want to stay close.
    {
        bond_state bond;
        apply_bond_event(bond, "walkway_collapsed");
        CHECK(bond.fear > 0.5);
        CHECK(bond.closeness_need > 0.4);
    }

    // After the reunion the dog stays close, whatever came before.
    {
        bond_state bond;
        bond.closeness_need = 0.0;
        apply_bond_event(bond, "reunion_contact_made");
        CHECK(bond.closeness_need >= 0.9);
    }

    // Break it: repeat a frightening event many times; feelings stay between 0 and 1.
    {
        bond_state bond;
        for (int repeat = 0; repeat < 1000; ++repeat) {
            apply_bond_event(bond, "walkway_collapsed");
        }
        CHECK(bond.fear <= 1.0 && bond.closeness_need <= 1.0);
        for (int repeat = 0; repeat < 1000; ++repeat) {
            apply_bond_event(bond, "rested_together");
        }
        CHECK(bond.fear >= 0.0 && bond.closeness_need >= 0.0);
    }

    // Unknown events change nothing.
    {
        bond_state bond;
        apply_bond_event(bond, "no_such_event");
        CHECK(bond.fear == 0.0 && bond.trust == 0.8);
    }
}
