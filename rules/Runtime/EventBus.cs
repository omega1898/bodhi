// Purpose: lets one part of the game announce "this happened" without knowing who reacts.
// Example: the lever announces "maintenance_power_restored"; Bodhi's gate reacts.
// A port of core/src/event_bus.cpp. Plain C#: no Unity code, so it can be tested anywhere.
using System;
using System.Collections.Generic;

namespace Bodhi.Rules
{
    // One thing that happened in the game world, such as "lever_pulled".
    // "readonly struct" means a small value that can't be changed after it is made.
    public readonly struct GameEvent
    {
        public readonly string name;

        // Makes an event with the given name.
        public GameEvent(string name)
        {
            this.name = name;
        }
    }

    public class EventBus
    {
        // Deepest allowed chain of "a listener published another event".
        const int maximum_publish_depth = 16;

        // Action<GameEvent> is C#'s name for "a function that takes an event and returns nothing".
        readonly Dictionary<string, List<Action<GameEvent>>> listeners_by_event_name = new Dictionary<string, List<Action<GameEvent>>>();
        readonly List<Action<GameEvent>> listeners_for_all_events = new List<Action<GameEvent>>();
        int current_publish_depth = 0;

        // How many events the loop guard has dropped so far.
        public int dropped_event_count { get; private set; }

        // How many events were refused because they had no name.
        public int refused_event_count { get; private set; }

        // Calls `on_event` every time an event called `event_name` is published.
        public void subscribe(string event_name, Action<GameEvent> on_event)
        {
            if (event_name == null || on_event == null)
            {
                return;  // Nothing sensible to listen for: ignore rather than crash.
            }
            if (!listeners_by_event_name.TryGetValue(event_name, out var listeners))
            {
                listeners = new List<Action<GameEvent>>();
                listeners_by_event_name[event_name] = listeners;
            }
            listeners.Add(on_event);
        }

        // Calls `on_event` for every event, whatever its name. These run before the
        // name-specific listeners, so a log sees the cause before its effects.
        public void subscribe_to_all(Action<GameEvent> on_event)
        {
            if (on_event != null)
            {
                listeners_for_all_events.Add(on_event);
            }
        }

        // Sends `game_event` to its listeners. Returns false if it was refused (no name) or
        // dropped because listeners kept publishing inside each other (an endless loop).
        public bool publish(GameEvent game_event)
        {
            if (game_event.name == null)
            {
                refused_event_count += 1;
                return false;
            }
            // A chain this deep is almost certainly a loop (A causes B causes A), so drop the
            // event and count it instead of freezing the game.
            if (current_publish_depth >= maximum_publish_depth)
            {
                dropped_event_count += 1;
                return false;
            }

            // Copy the listener lists first: a listener may subscribe new listeners while we
            // loop, and changing a list while walking through it is an error in C#.
            var general_listeners = listeners_for_all_events.ToArray();
            var named_listeners = listeners_by_event_name.TryGetValue(game_event.name, out var found)
                ? found.ToArray()
                : Array.Empty<Action<GameEvent>>();

            current_publish_depth += 1;
            foreach (var on_event in general_listeners)
            {
                on_event(game_event);
            }
            foreach (var on_event in named_listeners)
            {
                on_event(game_event);
            }
            current_publish_depth -= 1;
            return true;
        }
    }
}
