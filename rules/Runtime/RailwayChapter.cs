// Purpose: the rules of the station chapter ("The Other Side"), with no graphics.
// The Unity scene only reports what happened ("walkway_collapsed" at 60 seconds); this class
// decides where the story is, who the player controls, and how Bodhi feels.
// Ported line for line from core/src/railway_chapter.cpp so its tested behaviour carries over.
using System;

namespace Bodhi.Rules
{
    // Who the player is controlling.
    public enum Character
    {
        human,  // Her.
        dog,    // Bodhi.
    }

    public partial class RailwayChapter
    {
        // One story step: in this beat, this event moves the story to that beat.
        readonly struct BeatTrigger
        {
            public readonly ChapterBeat from_beat;
            public readonly string event_name;
            public readonly ChapterBeat to_beat;

            // Makes a story step.
            public BeatTrigger(ChapterBeat from_beat, string event_name, ChapterBeat to_beat)
            {
                this.from_beat = from_beat;
                this.event_name = event_name;
                this.to_beat = to_beat;
            }
        }

        static readonly BeatTrigger[] beat_triggers =
        {
            new BeatTrigger(ChapterBeat.together, "walkway_collapsed", ChapterBeat.separation),
            new BeatTrigger(ChapterBeat.separation, "separation_scene_finished", ChapterBeat.human_section),
            new BeatTrigger(ChapterBeat.human_section, "dog_gate_opened", ChapterBeat.dog_section),
            new BeatTrigger(ChapterBeat.dog_section, "human_route_revealed", ChapterBeat.human_section),
            new BeatTrigger(ChapterBeat.human_section, "pair_met_at_fence", ChapterBeat.near_contact),
            new BeatTrigger(ChapterBeat.dog_section, "pair_met_at_fence", ChapterBeat.near_contact),
            new BeatTrigger(ChapterBeat.near_contact, "fence_touch_finished", ChapterBeat.final_obstacle),
            new BeatTrigger(ChapterBeat.final_obstacle, "shared_route_opened", ChapterBeat.reunion),
            new BeatTrigger(ChapterBeat.reunion, "reunion_scene_finished", ChapterBeat.aftermath),
        };

        // Route links: an action on one route announces its effect on the other route.
        // Each pair is (what the player did, what it causes on the other side).
        static readonly string[,] route_links =
        {
            { "maintenance_power_restored", "dog_gate_opened" },       // Her power switch opens his gate.
            { "dog_barked_from_window", "human_route_revealed" },     // His bark shows her a way.
        };

        const string human_final_action = "jammed_gate_lever_pulled";
        const string dog_final_action = "debris_cleared";

        // Events only the chapter itself may announce; a scene reporting one is refused.
        static readonly string[] internal_event_names = { "dog_gate_opened", "human_route_revealed", "shared_route_opened" };

        const int maximum_event_name_length = 64;

        readonly EventBus bus = new EventBus();
        readonly double[] finished_seconds_by_beat = new double[ChapterBeats.beat_count];
        double beat_started_at_seconds = 0.0;
        double latest_time_seconds = 0.0;
        bool human_final_action_done = false;
        bool dog_final_action_done = false;

        // Where the story is now.
        public ChapterBeat current_beat { get; private set; } = ChapterBeat.together;

        // Who the player controls now.
        public Character controlled_character { get; private set; } = Character.human;

        // How Bodhi feels; changes as events happen.
        public BondState bond { get; } = new BondState();

        // The playtest log for this chapter.
        public TelemetryLog telemetry { get; } = new TelemetryLog();

        // How many reports were refused (bad names, bad times, internal events, bad switches).
        public int refused_input_count { get; private set; }

        // Starts the chapter at "together", with the player as her.
        public RailwayChapter()
        {
            wire_event_rules();
            telemetry.record(0.0, "beat_entered", ChapterBeats.name_of(current_beat));
        }

        // Connects the chapter's rules to its event bus.
        void wire_event_rules()
        {
            // Every event is logged, applied to Bodhi's feelings, then checked against the story.
            bus.subscribe_to_all(game_event =>
            {
                telemetry.record(latest_time_seconds, "world_event", game_event.name);
                bond.apply_event(game_event.name);
                advance_story(game_event.name);
            });

            for (int link_index = 0; link_index < route_links.GetLength(0); link_index += 1)
            {
                // Copy into a local so each listener keeps its own effect (a C# closure rule).
                string effect_event = route_links[link_index, 1];
                bus.subscribe(route_links[link_index, 0], _ => bus.publish(new GameEvent(effect_event)));
            }

            // The final obstacle: count each route's action, but only during that beat.
            bus.subscribe(human_final_action, _ => handle_final_action(true));
            bus.subscribe(dog_final_action, _ => handle_final_action(false));
        }

        // The scene reports something that happened. Returns false if the report was refused.
        public bool report_world_event(string event_name, double time_seconds)
        {
            if (!is_valid_event_name(event_name))
            {
                // Only the first 64 characters are logged, so a huge name cannot flood the log.
                string shown_name = event_name ?? "";
                if (shown_name.Length > maximum_event_name_length)
                {
                    shown_name = shown_name.Substring(0, maximum_event_name_length);
                }
                refuse_input("bad_event_name", shown_name);
                return false;
            }
            if (Array.IndexOf(internal_event_names, event_name) >= 0)
            {
                refuse_input("internal_event_from_outside", event_name);
                return false;
            }
            if (!accept_time(time_seconds))
            {
                refuse_input("bad_time", event_name);
                return false;
            }
            return bus.publish(new GameEvent(event_name));
        }

        // The player asks to switch between her and Bodhi. Only allowed at the final obstacle.
        public bool request_character_switch(double time_seconds)
        {
            if (!accept_time(time_seconds))
            {
                refuse_input("bad_time", "character_switch");
                return false;
            }
            if (current_beat != ChapterBeat.final_obstacle)
            {
                refuse_input("switch_not_allowed_now", ChapterBeats.name_of(current_beat));
                return false;
            }
            take_control_of(controlled_character == Character.human ? Character.dog : Character.human);
            return true;
        }

        // Total seconds spent in a beat so far, including the current visit. A forced bad value gives 0.
        public double seconds_spent_in(ChapterBeat beat, double now_seconds)
        {
            if (!ChapterBeats.is_real_beat(beat))
            {
                return 0.0;
            }
            double total_seconds = finished_seconds_by_beat[(int)beat];
            if (beat == current_beat && now_seconds > beat_started_at_seconds)
            {
                total_seconds += now_seconds - beat_started_at_seconds;
            }
            return total_seconds;
        }

        // True for 1 to 64 characters of a-z, 0-9 and underscore. Anything else is refused,
        // so a typo ("Walkway_Collapsed") or junk text can never move the story.
        static bool is_valid_event_name(string event_name)
        {
            if (event_name == null || event_name.Length == 0 || event_name.Length > maximum_event_name_length)
            {
                return false;
            }
            foreach (char character in event_name)
            {
                bool allowed = (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') || character == '_';
                if (!allowed)
                {
                    return false;
                }
            }
            return true;
        }

        // Takes a new time if it is a real number, not going backwards, and not absurdly large.
        bool accept_time(double time_seconds)
        {
            bool usable = double.IsFinite(time_seconds)
                && time_seconds >= latest_time_seconds
                && time_seconds <= TelemetryLog.maximum_time_seconds;
            if (!usable)
            {
                return false;
            }
            latest_time_seconds = time_seconds;
            return true;
        }

        // Counts a refused report and logs why.
        void refuse_input(string reason, string detail)
        {
            refused_input_count += 1;
            telemetry.record(latest_time_seconds, "input_refused", reason + ":" + detail);
        }

        // Takes at most one story step for this event. Looking up once (and returning) means
        // a beat that changes mid-loop can't make the story skip two beats.
        void advance_story(string event_name)
        {
            foreach (var trigger in beat_triggers)
            {
                if (trigger.from_beat == current_beat && trigger.event_name == event_name)
                {
                    enter_beat(trigger.to_beat);
                    return;
                }
            }
        }

        // Moves the story into a new beat, records the time spent, and sets who is controlled.
        void enter_beat(ChapterBeat next_beat)
        {
            // Second safety check: the story order table has the final say.
            if (!ChapterBeats.is_allowed_transition(current_beat, next_beat))
            {
                return;
            }
            finished_seconds_by_beat[(int)current_beat] += latest_time_seconds - beat_started_at_seconds;
            current_beat = next_beat;
            beat_started_at_seconds = latest_time_seconds;
            telemetry.record(latest_time_seconds, "beat_entered", ChapterBeats.name_of(next_beat));

            switch (next_beat)
            {
                case ChapterBeat.dog_section:
                    take_control_of(Character.dog);
                    break;
                case ChapterBeat.near_contact:
                case ChapterBeat.final_obstacle:
                    break;  // Keep whoever the player had.
                default:
                    take_control_of(Character.human);
                    break;
            }
        }

        // Hands control to a character and logs it, if it isn't already them.
        void take_control_of(Character who)
        {
            if (controlled_character == who)
            {
                return;
            }
            controlled_character = who;
            telemetry.record(latest_time_seconds, "control_changed", who.ToString());
        }

        // Counts one route's final action; when both are done, the shared route opens.
        void handle_final_action(bool is_human_action)
        {
            if (current_beat != ChapterBeat.final_obstacle)
            {
                return;  // The same lever pulled earlier in the chapter does not count.
            }
            if (is_human_action)
            {
                human_final_action_done = true;
            }
            else
            {
                dog_final_action_done = true;
            }
            if (human_final_action_done && dog_final_action_done)
            {
                bus.publish(new GameEvent("shared_route_opened"));
            }
        }
    }
}
