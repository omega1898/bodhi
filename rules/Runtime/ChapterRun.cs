// Purpose: plays any chapter written as a ChapterScript. The Unity scene reports what the player
// did (used a thing, walked into a zone, whistled, sniffed); this class decides what happens:
// which story rules run, the beat, who the player controls, timers, film scenes, the reunion,
// and how Bodhi feels. A port of the story layer of prototype/web/js/game.js (fire_event,
// apply_effects, the reunion and film scenes), so the twelve browser chapters behave the same.
using System;
using System.Collections.Generic;

namespace Bodhi.Rules
{
    // Where the reunion is.
    public enum ReunionStage
    {
        none,      // Not started.
        approach,  // He runs to her; the player chooses when to kneel and hold him.
        embrace,   // Holding each other; nothing else happens for 6.5 seconds.
        done,      // Over; he follows her again.
    }

    // One story line waiting to be shown, still with its {dog} placeholders.
    public readonly struct StoryLine
    {
        public readonly string text;
        public readonly StoryVoice voice;

        // Makes a line.
        public StoryLine(string text, StoryVoice voice)
        {
            this.text = text;
            this.voice = voice;
        }
    }

    // A story rule that has just been carried out, for the scene to stage (see take_applied_rules).
    public readonly struct AppliedRule
    {
        public readonly int rule_number;   // Its place in ChapterScript.rules.
        public readonly bool replayed;     // True while a save was being replayed (Continue).
        public readonly int events_so_far; // Events that had happened when it ran, counting the one that set it off.

        // Makes a record of one applied rule.
        public AppliedRule(int rule_number, bool replayed, int events_so_far)
        {
            this.rule_number = rule_number;
            this.replayed = replayed;
            this.events_so_far = events_so_far;
        }
    }

    // "partial": this class is written across two files; saving and resuming are in ChapterRunSave.cs.
    public partial class ChapterRun
    {
        public const int maximum_event_name_length = 64;
        public const double embrace_seconds = 6.5;          // How long the embrace lasts.
        public const double answer_delay_seconds = 0.75;    // The other one answers a call after this.
        public const double call_rest_seconds = 1.4;        // The shortest gap between two calls.
        public const double sniff_reach_across = 300;       // How far his nose finds hidden things, sideways...
        public const double sniff_reach_up_down = 120;      // ...and up or down, in layout units.
        const double longest_step_seconds = 3600;           // A bigger time step is treated as this.
        const int most_timers = 256;                        // A runaway script can't schedule more.

        // Events only this class announces; the scene can't report them.
        static readonly string[] engine_event_names =
        {
            "level_started", "cutscene_skipped", "reunion_embrace", "reunion_complete",
            "dog_petted", "dog_leaned_in", "knelt_by_dog", "dog_sniffed", "dog_fed", "dog_cleaned",
        };

        // A timer: after seconds_left, event_name fires.
        sealed class Timer
        {
            public double seconds_left;
            public string event_name;
        }

        readonly ChapterScript script;
        readonly HashSet<string> happened = new HashSet<string>(StringComparer.Ordinal);
        readonly List<string> order_of_events = new List<string>();
        readonly HashSet<int> fired_rules = new HashSet<int>();
        readonly HashSet<string> revealed = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> keepsakes = new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<string, bool> solid_changes = new Dictionary<string, bool>(StringComparer.Ordinal);
        readonly List<Timer> timers = new List<Timer>();
        readonly List<AppliedRule> applied_rules = new List<AppliedRule>();
        readonly List<StoryLine> waiting_lines = new List<StoryLine>();
        string cutscene_skip_to = null;
        double embrace_seconds_so_far = 0;
        double call_rest_left = 0;
        double answer_seconds_left = 0;
        Character? waiting_answerer = null;
        bool replaying = false;  // True while a save is replayed: lasting changes only, no lines.

        public ChapterBeat current_beat { get; private set; } = ChapterBeat.together;
        public Character controlled_character { get; private set; } = Character.human;
        public bool switching_allowed { get; private set; } = false;
        public bool dog_follows { get; private set; } = true;
        public bool fetch_enabled { get; private set; }
        public string objective { get; private set; } = "";
        public bool cutscene_playing { get; private set; } = false;
        public ReunionStage reunion_stage { get; private set; } = ReunionStage.none;
        public bool is_complete { get; private set; } = false;
        public BondState bond { get; } = new BondState();
        public CareState care { get; private set; }

        // Every event that has happened, in order (for saving and for tests).
        public IReadOnlyList<string> event_order => order_of_events;

        // The keepsakes found in this chapter, by name.
        public IReadOnlyCollection<string> found_keepsakes => keepsakes;

        // The chapter being played.
        public ChapterScript chapter => script;

        // True while the player's buttons do nothing: a film scene, the embrace, or the end.
        public bool controls_locked => cutscene_playing || reunion_stage == ReunionStage.embrace || is_complete;

        // Gets a chapter ready. Care carries on from the last chapter (or a new game) unless the
        // chapter sets its own starting values. Call start() once the scene is listening.
        public ChapterRun(ChapterScript script, CareState carried_care = null)
        {
            this.script = script ?? throw new ArgumentNullException(nameof(script));
            bond.fear = clamp_unit(script.start_fear);
            bond.closeness_need = clamp_unit(script.start_closeness);
            fetch_enabled = script.has_ball;
            var before = carried_care ?? new CareState();
            care = new CareState(script.start_hunger ?? before.hunger, script.start_dirt ?? before.dirt, script.start_food ?? before.food);
        }

        // Starts the chapter: fires level_started.
        public void start()
        {
            fire_event("level_started");
        }

        // True if this event has happened.
        public bool has_happened(string event_name)
        {
            return event_name != null && happened.Contains(event_name);
        }

        // True if this solid thing has been taken away by the story.
        public bool solid_is_removed(string solid_id)
        {
            return solid_id != null && solid_changes.TryGetValue(solid_id, out bool active) && !active;
        }

        // True if this solid thing has been put in place by the story.
        public bool solid_is_added(string solid_id)
        {
            return solid_id != null && solid_changes.TryGetValue(solid_id, out bool active) && active;
        }

        // Hands over the rules carried out since the last call, oldest first, so the scene can
        // stage them (dim the lights, raise a flood). Rules replayed from a save say so: the scene
        // keeps their lasting changes but plays none of their sounds. Each rule is handed over once.
        public List<AppliedRule> take_applied_rules()
        {
            var taken = new List<AppliedRule>(applied_rules);
            applied_rules.Clear();
            return taken;
        }

        // Hands over the story lines waiting to be shown, oldest first, and forgets them.
        public List<StoryLine> take_lines()
        {
            var lines = new List<StoryLine>(waiting_lines);
            waiting_lines.Clear();
            return lines;
        }

        // True if the controlled character could use this thing now. dog_is_near is the scene's
        // answer to "is Bodhi right there?", for things that need him close.
        public bool can_use(string event_name, bool dog_is_near)
        {
            var thing = find_thing(event_name);
            return thing != null && thing_available(thing, dog_is_near);
        }

        // The controlled character uses a thing. Returns false, changing nothing, if they can't.
        // Once a reunion has started nothing can be used, as in the browser game.
        public bool use(string event_name, bool dog_is_near)
        {
            var thing = find_thing(event_name);
            if (thing == null || !thing_available(thing, dog_is_near))
            {
                return false;
            }
            if (!string.IsNullOrEmpty(thing.keepsake))
            {
                keepsakes.Add(thing.keepsake);
                if (!string.IsNullOrEmpty(thing.memory_text))
                {
                    waiting_lines.Add(new StoryLine(thing.memory_text, StoryVoice.her));
                }
            }
            if (thing.food > 0)
            {
                care.pick_up_food(thing.food);
            }
            if (thing.care == CareKind.meal)
            {
                care.feed(false, bond);
            }
            else if (thing.care == CareKind.treat)
            {
                care.eat_treat();
            }
            else if (thing.care == CareKind.bath)
            {
                care.clean(true);
            }
            fire_event(thing.event_name);
            return true;
        }

        // The scene says who is inside a zone. Fires its event if the zone is open and the right
        // character is there. Zones still work during a film scene, so a scripted walk counts.
        public bool report_zone(string event_name, bool human_inside, bool dog_inside)
        {
            if (is_complete || string.IsNullOrEmpty(event_name) || happened.Contains(event_name))
            {
                return false;
            }
            foreach (var zone in script.zones ?? Array.Empty<ChapterZone>())
            {
                if (zone == null || zone.event_name != event_name || !all_happened(zone.requires) || any_happened(zone.unless))
                {
                    continue;
                }
                bool triggered = zone.who == ZoneWho.both
                    ? human_inside && dog_inside
                    : zone.who == ZoneWho.human
                        ? human_inside && controlled_character == Character.human
                        : dog_inside && controlled_character == Character.dog;
                if (triggered)
                {
                    return fire_event(event_name);
                }
            }
            return false;
        }

        // The controlled character calls (she whistles, he barks). Calming, a little. The other
        // one answers a moment later.
        public bool call()
        {
            if (controls_locked || call_rest_left > 0)
            {
                return false;
            }
            call_rest_left = call_rest_seconds;
            bond.fear = Math.Max(0, bond.fear - 0.03);
            Character caller = controlled_character;
            waiting_answerer = caller == Character.human ? Character.dog : Character.human;
            answer_seconds_left = answer_delay_seconds;
            fire_event(caller + "_called");
            fire_event(caller + "_called_during_" + current_beat);
            return true;
        }

        // Bodhi sniffs with his feet at (dog_x, dog_feet_y) in the chapter's layout. Hidden things
        // near his nose show themselves. Only he can sniff.
        public bool sniff(double dog_x, double dog_feet_y)
        {
            if (controls_locked || controlled_character != Character.dog)
            {
                return false;
            }
            foreach (var thing in script.things ?? Array.Empty<ChapterThing>())
            {
                if (thing != null && thing.hidden && Math.Abs(thing.x - dog_x) < sniff_reach_across && Math.Abs(thing.y - dog_feet_y) < sniff_reach_up_down)
                {
                    revealed.Add(thing.reveal_name ?? "");
                }
            }
            fire_event("dog_sniffed");
            fire_event("dog_sniffed_during_" + current_beat);
            return true;
        }

        // She pets him, or (if the player controls him) he leans into her. The scene checks
        // they're close. It calms him.
        public bool pet()
        {
            if (controls_locked || reunion_stage != ReunionStage.none)
            {
                return false;
            }
            bond.fear = Math.Max(0, bond.fear - 0.08);
            fire_event(controlled_character == Character.human ? "dog_petted" : "dog_leaned_in");
            return true;
        }

        // She looks after him when the game offers it (CareState.offered_care): feeds him a meal
        // from the bag, or towels most of the mud off. `together` is the scene's answer to "are
        // they side by side?". Returns false, changing nothing, unless that care is on offer now.
        public bool give_care(CareOffer offer, bool together)
        {
            if (offer == CareOffer.none || controls_locked || reunion_stage != ReunionStage.none
                || care.offered_care(together, controlled_character, cutscene_playing) != offer)
            {
                return false;
            }
            if (offer == CareOffer.feed)
            {
                care.feed(true, bond);
                bond.closeness_need = clamp_unit(bond.closeness_need + 0.05);
                fire_event("dog_fed");  // Counts once; later meals still feed him.
            }
            else
            {
                care.clean(false);
                bond.closeness_need = clamp_unit(bond.closeness_need + 0.03);
                fire_event("dog_cleaned");
            }
            return true;
        }

        // She kneels. If he's close and there's no reunion, he leans in (knelt_by_dog).
        public bool kneel(bool dog_is_near)
        {
            if (controls_locked || controlled_character != Character.human)
            {
                return false;
            }
            if (dog_is_near && reunion_stage == ReunionStage.none)
            {
                fire_event("knelt_by_dog");
            }
            return true;
        }

        // In the reunion, once he has reached her: she holds him and the embrace begins.
        public bool hold_dog(bool dog_is_near)
        {
            if (is_complete || reunion_stage != ReunionStage.approach || !dog_is_near)
            {
                return false;
            }
            reunion_stage = ReunionStage.embrace;
            embrace_seconds_so_far = 0;
            bond.fear = 0;
            bond.closeness_need = 0.95;
            fire_event("reunion_embrace");
            return true;
        }

        // Swaps which character the player controls, if the story allows it now.
        public bool switch_character()
        {
            if (controls_locked || !switching_allowed)
            {
                return false;
            }
            controlled_character = controlled_character == Character.human ? Character.dog : Character.human;
            return true;
        }

        // Skip: drops the film scene's timers and jumps to its end. False if no scene is playing.
        public bool skip_cutscene()
        {
            if (!cutscene_playing || is_complete)
            {
                return false;
            }
            string skip_to = cutscene_skip_to;
            timers.Clear();
            fire_event("cutscene_skipped");
            if (!string.IsNullOrEmpty(skip_to))
            {
                fire_event(skip_to);
            }
            end_cutscene();  // In case the end of the scene forgot to.
            return true;
        }

        // The scene reports something only it can see, for example "ball_fetched". Refused for
        // a thing's or a zone's event (use those methods, which check the rules) and for events
        // only this class announces.
        public bool report_event(string event_name)
        {
            if (string.IsNullOrEmpty(event_name) || is_engine_event(event_name) || find_thing(event_name) != null || is_zone_event(event_name))
            {
                return false;
            }
            return fire_event(event_name);
        }

        // Moves time on: timers, the answering call and the embrace. Bad times are ignored.
        public void pass_time(double delta_seconds)
        {
            if (is_complete || !(delta_seconds > 0) || double.IsInfinity(delta_seconds))
            {
                return;
            }
            delta_seconds = Math.Min(delta_seconds, longest_step_seconds);
            call_rest_left = Math.Max(0, call_rest_left - delta_seconds);
            // Like the browser game: only timers that existed before this step can fire in it.
            foreach (var timer in timers.ToArray())
            {
                timer.seconds_left -= delta_seconds;
                if (timer.seconds_left <= 0 && timers.Remove(timer))
                {
                    fire_event(timer.event_name);
                }
            }
            update_answer(delta_seconds);
            update_embrace(delta_seconds);
        }

        // Plays the answering call once its short delay has passed (not during the embrace).
        void update_answer(double delta_seconds)
        {
            if (waiting_answerer == null)
            {
                return;
            }
            answer_seconds_left -= delta_seconds;
            if (answer_seconds_left > 0)
            {
                return;
            }
            Character answerer = waiting_answerer.Value;
            waiting_answerer = null;
            if (reunion_stage != ReunionStage.embrace)
            {
                fire_event(answerer + "_answered");
            }
        }

        // Counts down the embrace; when it ends he follows her again.
        void update_embrace(double delta_seconds)
        {
            if (reunion_stage != ReunionStage.embrace)
            {
                return;
            }
            embrace_seconds_so_far += delta_seconds;
            if (embrace_seconds_so_far > embrace_seconds)
            {
                reunion_stage = ReunionStage.done;
                dog_follows = true;
                fire_event("reunion_complete");
            }
        }

        // Records that something happened and runs every rule waiting for it, in order. Each
        // event counts once and each rule runs once. A rule blocked by "unless" is still used up.
        bool fire_event(string event_name)
        {
            if (is_complete || string.IsNullOrEmpty(event_name) || event_name.Length > maximum_event_name_length || happened.Contains(event_name))
            {
                return false;
            }
            happened.Add(event_name);
            order_of_events.Add(event_name);
            var rules = script.rules ?? Array.Empty<StoryRule>();
            for (int index = 0; index < rules.Length; index++)
            {
                var rule = rules[index];
                if (rule == null || fired_rules.Contains(index))
                {
                    continue;
                }
                bool by_this_event = rule.on == event_name;
                bool all_done = rule.when_all != null && rule.when_all.Length > 0 && all_happened(rule.when_all);
                if (!by_this_event && !all_done)
                {
                    continue;
                }
                fired_rules.Add(index);
                if (!any_happened(rule.unless))
                {
                    applied_rules.Add(new AppliedRule(index, replaying, order_of_events.Count));
                    apply(rule);
                }
                if (is_complete)
                {
                    break;
                }
            }
            return true;
        }

        // Carries out a rule's changes, in the browser game's order.
        void apply(StoryRule rule)
        {
            foreach (var line in replaying ? Array.Empty<string>() : rule.lines ?? Array.Empty<string>())
            {
                waiting_lines.Add(new StoryLine(line ?? "", rule.voice));
            }
            foreach (var solid_id in rule.remove ?? Array.Empty<string>())
            {
                if (solid_id != null) solid_changes[solid_id] = false;
            }
            foreach (var solid_id in rule.add ?? Array.Empty<string>())
            {
                if (solid_id != null) solid_changes[solid_id] = true;
            }
            if (rule.beat != null) current_beat = rule.beat.Value;
            if (rule.control != null) controlled_character = rule.control.Value;
            if (rule.switching != null) switching_allowed = rule.switching.Value;
            if (rule.follow != null) dog_follows = rule.follow.Value;
            if (rule.fetch != null) fetch_enabled = rule.fetch.Value;
            bond.fear = clamp_unit(bond.fear + finite_or_zero(rule.fear_change));
            bond.closeness_need = clamp_unit(bond.closeness_need + finite_or_zero(rule.closeness_change));
            if (rule.objective != null) objective = rule.objective;
            if (!string.IsNullOrEmpty(rule.schedule_event) && rule.schedule_seconds >= 0 && !double.IsInfinity(rule.schedule_seconds) && timers.Count < most_timers)
            {
                timers.Add(new Timer { seconds_left = rule.schedule_seconds, event_name = rule.schedule_event });
            }
            if (rule.reunion) start_reunion();
            if (rule.cutscene == CutsceneChange.start)
            {
                cutscene_playing = true;
                cutscene_skip_to = rule.cutscene_skip_to;
            }
            else if (rule.cutscene == CutsceneChange.end)
            {
                end_cutscene();
            }
            if (rule.care_hunger != null || rule.care_dirt != null || rule.care_food != null)
            {
                care = new CareState(rule.care_hunger ?? care.hunger, rule.care_dirt ?? care.dirt, rule.care_food ?? care.food);
            }
            if (rule.complete) is_complete = true;
        }

        // Starts the reunion: he runs to her, so the player controls her and can't switch.
        void start_reunion()
        {
            reunion_stage = ReunionStage.approach;
            controlled_character = Character.human;
            switching_allowed = false;
            dog_follows = false;
            objective = "";
        }

        // Ends the film scene and gives the controls back.
        void end_cutscene()
        {
            cutscene_playing = false;
            cutscene_skip_to = null;
        }

        // True if the thing can be used now by the controlled character.
        bool thing_available(ChapterThing thing, bool dog_is_near)
        {
            return !controls_locked
                && reunion_stage == ReunionStage.none
                && thing.who == controlled_character
                && !happened.Contains(thing.event_name)
                && (!thing.hidden || revealed.Contains(thing.reveal_name ?? ""))
                && (!thing.needs_dog || dog_is_near)
                && all_happened(thing.requires)
                && !any_happened(thing.until);
        }

        // The thing that fires this event, or null.
        ChapterThing find_thing(string event_name)
        {
            if (string.IsNullOrEmpty(event_name))
            {
                return null;
            }
            foreach (var thing in script.things ?? Array.Empty<ChapterThing>())
            {
                if (thing != null && thing.event_name == event_name)
                {
                    return thing;
                }
            }
            return null;
        }

        // True if a zone fires this event.
        bool is_zone_event(string event_name)
        {
            foreach (var zone in script.zones ?? Array.Empty<ChapterZone>())
            {
                if (zone != null && zone.event_name == event_name)
                {
                    return true;
                }
            }
            return false;
        }

        // True for events this class announces itself: calls, answers, sniffs and the fixed list.
        static bool is_engine_event(string event_name)
        {
            return Array.IndexOf(engine_event_names, event_name) >= 0
                || event_name.EndsWith("_called", StringComparison.Ordinal)
                || event_name.Contains("_called_during_")
                || event_name.EndsWith("_answered", StringComparison.Ordinal)
                || event_name.StartsWith("dog_sniffed_during_", StringComparison.Ordinal);
        }

        // True if every named event has happened (an empty or missing list counts as true).
        bool all_happened(string[] names)
        {
            foreach (var name in names ?? Array.Empty<string>())
            {
                if (name == null || !happened.Contains(name)) return false;
            }
            return true;
        }

        // True if any named event has happened.
        bool any_happened(string[] names)
        {
            foreach (var name in names ?? Array.Empty<string>())
            {
                if (name != null && happened.Contains(name)) return true;
            }
            return false;
        }

        // Keeps a feeling between 0 and 1; a value that isn't a number becomes 0.
        static double clamp_unit(double value)
        {
            return double.IsNaN(value) ? 0 : Math.Max(0, Math.Min(1, value));
        }

        // A change that isn't a finite number counts as no change.
        static double finite_or_zero(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) ? 0 : value;
        }
    }
}
