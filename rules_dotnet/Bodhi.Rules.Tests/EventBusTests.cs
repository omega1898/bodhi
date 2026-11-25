// Purpose: proves events reach the right listeners, and that endless event loops are stopped.
// Ported from core/tests/test_event_bus.cpp, so the C# rules behave like the tested C++ ones.
using System.Collections.Generic;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class EventBusTests
    {
        // A listener hears only the event it subscribed to.
        [Test]
        public void a_listener_hears_only_its_own_event()
        {
            var bus = new EventBus();
            int lever_count = 0;
            bus.subscribe("lever_pulled", game_event => lever_count += 1);
            bus.publish(new GameEvent("lever_pulled"));
            bus.publish(new GameEvent("door_opened"));
            Assert.That(lever_count, Is.EqualTo(1));
        }

        // Publishing with nobody listening is allowed.
        [Test]
        public void publishing_with_no_listeners_succeeds()
        {
            var bus = new EventBus();
            Assert.That(bus.publish(new GameEvent("nobody_listens")), Is.True);
        }

        // "Listen to everything" runs before name-specific listeners, so a log sees the cause first.
        [Test]
        public void listeners_for_all_events_run_first()
        {
            var bus = new EventBus();
            var order = new List<string>();
            bus.subscribe("bark", game_event => order.Add("named"));
            bus.subscribe_to_all(game_event => order.Add("all"));
            bus.publish(new GameEvent("bark"));
            Assert.That(order, Is.EqualTo(new List<string> { "all", "named" }));
        }

        // A listener that adds listeners while it runs must not crash; the new ones start next time.
        [Test]
        public void listeners_added_during_a_publish_wait_for_the_next_one()
        {
            var bus = new EventBus();
            int added_listener_runs = 0;
            bus.subscribe("grow", game_event =>
            {
                for (int index = 0; index < 100; index += 1)
                {
                    bus.subscribe("grow", inner_event => added_listener_runs += 1);
                }
            });
            bus.publish(new GameEvent("grow"));
            Assert.That(added_listener_runs, Is.EqualTo(0));
        }

        // Break it: A causes B, B causes A, forever. The guard must stop it and count one drop.
        [Test]
        public void break_it_an_endless_loop_is_stopped_and_counted()
        {
            var bus = new EventBus();
            bus.subscribe("ping", game_event => bus.publish(new GameEvent("pong")));
            bus.subscribe("pong", game_event => bus.publish(new GameEvent("ping")));
            bus.publish(new GameEvent("ping"));
            Assert.That(bus.dropped_event_count, Is.EqualTo(1));
        }

        // Break it: an event with no name (null) is refused and counted, never crashes.
        [Test]
        public void break_it_an_event_with_no_name_is_refused()
        {
            var bus = new EventBus();
            int heard = 0;
            bus.subscribe_to_all(game_event => heard += 1);
            Assert.That(bus.publish(new GameEvent(null)), Is.False);
            Assert.That(heard, Is.EqualTo(0));
            Assert.That(bus.refused_event_count, Is.EqualTo(1));
        }
    }
}
