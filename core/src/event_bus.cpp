// event_bus.cpp
// Purpose: delivers published events to listeners, and stops endless event loops.
#include "bodhi/event_bus.hpp"

#include <utility>

namespace bodhi {

void event_bus::subscribe(const std::string& event_name, listener on_event) {
    listeners_by_event_name[event_name].push_back(std::move(on_event));
}

void event_bus::subscribe_to_all(listener on_event) {
    listeners_for_all_events.push_back(std::move(on_event));
}

bool event_bus::publish(const game_event& event) {
    // A listener may publish another event while this call is still running.
    // A chain that goes this deep is almost certainly a loop (A causes B causes A),
    // so drop the event and count it instead of freezing or crashing the game.
    if (current_publish_depth >= maximum_publish_depth) {
        ++dropped_events;
        return false;
    }

    // Copy the listener lists first. A listener may subscribe new listeners
    // while we loop, and changing a list while walking through it is unsafe.
    const std::vector<listener> general_listeners = listeners_for_all_events;
    std::vector<listener> named_listeners;
    const auto found = listeners_by_event_name.find(event.name);
    if (found != listeners_by_event_name.end()) {
        named_listeners = found->second;
    }

    // Copy the event too: a listener could change the original while we use it.
    const game_event event_copy = event;

    ++current_publish_depth;
    for (const listener& on_event : general_listeners) {
        on_event(event_copy);
    }
    for (const listener& on_event : named_listeners) {
        on_event(event_copy);
    }
    --current_publish_depth;
    return true;
}

int event_bus::dropped_event_count() const {
    return dropped_events;
}

}  // namespace bodhi
