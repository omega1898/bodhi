// test_event_bus.cpp
// Purpose: proves events reach the right listeners, and that loops are stopped.
#include "bodhi/event_bus.hpp"

#include <string>
#include <vector>

#include "test_harness.hpp"

using bodhi::event_bus;
using bodhi::game_event;

void run_event_bus_tests() {
    // A listener hears only the event it subscribed to.
    {
        event_bus bus;
        int lever_count = 0;
        bus.subscribe("lever_pulled", [&lever_count](const game_event&) { ++lever_count; });
        bus.publish({"lever_pulled"});
        bus.publish({"door_opened"});
        CHECK(lever_count == 1);
    }

    // Publishing with no listeners is allowed.
    {
        event_bus bus;
        CHECK(bus.publish({"nobody_listens"}));
    }

    // "Listen to everything" runs before name-specific listeners.
    {
        event_bus bus;
        std::vector<std::string> order;
        bus.subscribe("bark", [&order](const game_event&) { order.push_back("named"); });
        bus.subscribe_to_all([&order](const game_event&) { order.push_back("all"); });
        bus.publish({"bark"});
        CHECK(order.size() == 2 && order[0] == "all" && order[1] == "named");
    }

    // A listener that subscribes more listeners while running must not crash.
    // (Run the sanitizer build to prove no memory error happens here.)
    {
        event_bus bus;
        int added_listener_runs = 0;
        bus.subscribe("grow", [&bus, &added_listener_runs](const game_event&) {
            for (int index = 0; index < 100; ++index) {
                bus.subscribe("grow", [&added_listener_runs](const game_event&) { ++added_listener_runs; });
            }
        });
        bus.publish({"grow"});
        CHECK(added_listener_runs == 0);  // New listeners start with the next publish.
    }

    // Break it: A causes B, B causes A, forever. The guard must stop it.
    {
        event_bus bus;
        bus.subscribe("ping", [&bus](const game_event&) { bus.publish({"pong"}); });
        bus.subscribe("pong", [&bus](const game_event&) { bus.publish({"ping"}); });
        bus.publish({"ping"});
        CHECK(bus.dropped_event_count() == 1);
    }
}
