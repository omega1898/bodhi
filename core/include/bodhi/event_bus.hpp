// event_bus.hpp
// Purpose: lets one part of the game announce "this happened" without knowing who reacts.
// Example: the lever announces "maintenance_power_restored"; the dog's gate reacts.
#pragma once

#include <functional>
#include <string>
#include <unordered_map>
#include <vector>

namespace bodhi {

// One thing that happened in the game world, such as "lever_pulled".
struct game_event {
    std::string name;
};

class event_bus {
public:
    // A listener is any function that takes an event and returns nothing.
    using listener = std::function<void(const game_event&)>;

    // Calls `on_event` every time an event called `event_name` is published.
    void subscribe(const std::string& event_name, listener on_event);

    // Calls `on_event` for every event, whatever its name. These listeners run
    // before the name-specific ones, so a log sees the cause before its effects.
    void subscribe_to_all(listener on_event);

    // Sends `event` to its listeners. Returns false if the event was dropped
    // because listeners kept publishing inside each other (an endless loop).
    bool publish(const game_event& event);

    // How many events the loop guard has dropped so far.
    int dropped_event_count() const;

private:
    // Deepest allowed chain of "a listener published another event".
    static constexpr int maximum_publish_depth = 16;

    std::unordered_map<std::string, std::vector<listener>> listeners_by_event_name;
    std::vector<listener> listeners_for_all_events;
    int current_publish_depth = 0;
    int dropped_events = 0;
};

}  // namespace bodhi
