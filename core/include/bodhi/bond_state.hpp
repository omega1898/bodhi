// bond_state.hpp
// Purpose: the dog's feelings toward the human, stored as numbers.
// Animation and companion behavior read these numbers; story events change them.
// Example: after a frightening separation, closeness_need stays high, so the dog stays close.
#pragma once

#include <string>

namespace bodhi {

// Each feeling is kept between 0.0 (none) and 1.0 (as strong as possible).
struct bond_state {
    double closeness_need = 0.2;  // How strongly the dog wants to stay near the human.
    double fear = 0.0;            // How frightened the dog is right now.
    double trust = 0.8;           // How readily the dog follows the human's calls.
};

// Changes the bond in response to a story event. Unknown event names change nothing.
void apply_bond_event(bond_state& bond, const std::string& event_name);

}  // namespace bodhi
