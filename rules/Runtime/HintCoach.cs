// Purpose: the hint rule (spec R6.4). If the player makes no progress for 3 minutes, the game
// nudges them toward the next step on the shortest path: first gently (Bodhi looks at the key),
// then, 90 seconds later, plainly (Bodhi paws at the vent). A dead end is offered a restart at
// once, because getting stuck for good is the game's fault, not the player's.
// It uses the puzzle solver, so a hint is always a step that really leads to the goal.
using System;

namespace Bodhi.Rules
{
    // What kind of hint to show. none: nothing to show yet.
    public enum HintKind { none, look_at, do_action, start_over }

    // One hint for the scene to show. The scene decides how: a glance, a whine, a glow.
    public readonly struct Hint
    {
        public readonly HintKind kind;
        public readonly string element_name;   // The thing to look at, such as "key".
        public readonly string action_name;    // The step to take, such as "fetch_key_through_vent".
        public readonly Character? who;        // Who can take it; null means either of them.

        // Makes one hint.
        public Hint(HintKind kind, string element_name, string action_name, Character? who)
        {
            this.kind = kind;
            this.element_name = element_name ?? "";
            this.action_name = action_name ?? "";
            this.who = who;
        }
    }

    public class HintCoach
    {
        // Seconds without progress before the first, gentle hint.
        public const double gentle_hint_after_seconds = 180.0;
        // Seconds after a hint before the next, plainer one.
        public const double plain_hint_after_seconds = 90.0;

        readonly PuzzleRun run;
        int[] last_seen_state;            // To notice when the player changed something.
        int closest_distance_so_far;      // Fewest steps from the goal the player has been.
        int current_distance;             // Steps from the goal now; -1 for a dead end.
        string next_action_name;          // The step to hint at now.
        int hints_since_progress;         // 0: next hint is gentle. 1 or more: plain.
        double seconds_since_last_hint;

        // Seconds since the player last got closer to the goal.
        public double seconds_without_progress { get; private set; }
        // How many hints have been shown, for the playtest log.
        public int hints_given { get; private set; }

        // Watches one playthrough. A missing run or a broken puzzle never hints.
        public HintCoach(PuzzleRun run)
        {
            if (run == null || !run.played_puzzle.is_valid)
            {
                return;
            }
            this.run = run;
            look_at_the_puzzle();
            closest_distance_so_far = current_distance;
        }

        // True when a hint should be shown now.
        public bool hint_due
        {
            get
            {
                if (run == null || run.is_solved)
                {
                    return false;
                }
                if (current_distance < 0)
                {
                    return true;  // A dead end: offer to start over straight away.
                }
                if (hints_since_progress == 0)
                {
                    return seconds_without_progress >= gentle_hint_after_seconds;
                }
                return seconds_since_last_hint >= plain_hint_after_seconds;
            }
        }

        // Lets time pass, and notices whether the player has moved closer to the goal.
        // Broken, negative or endless times are ignored.
        public void pass_time(double delta_seconds)
        {
            if (run == null || !double.IsFinite(delta_seconds) || delta_seconds < 0)
            {
                return;
            }
            if (!same_state(run.current_state(), last_seen_state))
            {
                look_at_the_puzzle();
                if (current_distance >= 0 && current_distance < closest_distance_so_far)
                {
                    // Real progress: closer than ever before. Start the clock and hints again.
                    closest_distance_so_far = current_distance;
                    seconds_without_progress = 0.0;
                    hints_since_progress = 0;
                    return;
                }
            }
            seconds_without_progress += delta_seconds;
            seconds_since_last_hint += delta_seconds;
        }

        // Gives the hint that is due, or a "none" hint if none is due yet.
        public Hint take_hint()
        {
            if (!hint_due)
            {
                return new Hint(HintKind.none, null, null, null);
            }
            hints_given += 1;
            seconds_since_last_hint = 0.0;
            if (current_distance < 0)
            {
                return new Hint(HintKind.start_over, null, null, null);
            }
            var puzzle = run.played_puzzle;
            int action_index = Array.FindIndex(puzzle.actions, action => action.name == next_action_name);
            if (action_index < 0)
            {
                return new Hint(HintKind.none, null, null, null);  // Can't happen; never crash.
            }
            var next_action = puzzle.actions[action_index];
            // The thing to look at is the first thing the step changes.
            string element_name = puzzle.elements[next_action.set_elements[0]].name;
            var kind = hints_since_progress == 0 ? HintKind.look_at : HintKind.do_action;
            hints_since_progress += 1;
            return new Hint(kind, element_name, next_action.name, next_action.who);
        }

        // Works out how far the goal is from where the player is now, and the next step.
        void look_at_the_puzzle()
        {
            last_seen_state = run.current_state();
            var step = PuzzleSolver.next_step(run.played_puzzle, last_seen_state);
            current_distance = step.distance;
            next_action_name = step.action_name;
        }

        // True if two states are the same, element by element.
        static bool same_state(int[] first, int[] second)
        {
            if (first == null || second == null || first.Length != second.Length)
            {
                return false;
            }
            for (int index = 0; index < first.Length; index += 1)
            {
                if (first[index] != second[index])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
