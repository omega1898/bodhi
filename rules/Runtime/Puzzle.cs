// Purpose: a puzzle as data (spec 3.4, decision D5). Elements have states (a gate: shut or
// open); actions belong to her, to Bodhi, or to either, and change states when their
// conditions hold. Because the puzzle is only data, PuzzleSolver can try every order of
// actions and prove it can't soft-lock. The Unity scene only reports actions by name.
using System;
using System.Collections.Generic;

namespace Bodhi.Rules
{
    // One thing in the puzzle and the states it can be in. The first state is not special:
    // `initial_state_index` says which one it starts in.
    public class PuzzleElement
    {
        public readonly string name;
        public readonly string[] states;
        public readonly int initial_state_index;

        // Describes an element, for example ("gate", {"shut", "open"}, 0).
        public PuzzleElement(string name, string[] states, int initial_state_index)
        {
            this.name = name;
            this.states = states ?? new string[0];
            this.initial_state_index = initial_state_index;
        }
    }

    // "This element is in this state", used both as a requirement and as an effect.
    public readonly struct PuzzleCondition
    {
        public readonly string element_name;
        public readonly string state_name;

        // Makes a condition, for example ("gate", "open").
        public PuzzleCondition(string element_name, string state_name)
        {
            this.element_name = element_name;
            this.state_name = state_name;
        }
    }

    // Something a character can do. `who` null means either of them can.
    public class PuzzleAction
    {
        public readonly string name;
        public readonly Character? who;
        public readonly PuzzleCondition[] requires;
        public readonly PuzzleCondition[] sets;

        // Describes an action: its name, who may do it, what must be true, and what it changes.
        public PuzzleAction(string name, Character? who, PuzzleCondition[] requires, PuzzleCondition[] sets)
        {
            this.name = name;
            this.who = who;
            this.requires = requires ?? new PuzzleCondition[0];
            this.sets = sets ?? new PuzzleCondition[0];
        }
    }

    public class Puzzle
    {
        // Above this many possible states the solver can't check every one quickly, so the
        // puzzle is refused rather than left to slow the tests down.
        public const long maximum_state_count = 1000000;

        // An action with its names turned into numbers, so checking it is fast.
        internal readonly struct ResolvedAction
        {
            public readonly string name;
            public readonly Character? who;
            public readonly int[] required_elements;
            public readonly int[] required_states;
            public readonly int[] set_elements;
            public readonly int[] set_states;

            // Stores an action's resolved numbers.
            public ResolvedAction(string name, Character? who, int[] required_elements, int[] required_states, int[] set_elements, int[] set_states)
            {
                this.name = name;
                this.who = who;
                this.required_elements = required_elements;
                this.required_states = required_states;
                this.set_elements = set_elements;
                this.set_states = set_states;
            }
        }

        readonly List<string> found_problems = new List<string>();

        public readonly string name;
        internal readonly PuzzleElement[] elements;
        internal readonly ResolvedAction[] actions;
        internal readonly int[] goal_elements;
        internal readonly int[] goal_states;

        // Everything wrong with the data, in plain words. Empty means the puzzle is usable.
        public IReadOnlyList<string> problems => found_problems;

        // True when the data has no problems. An invalid puzzle accepts no actions.
        public bool is_valid => found_problems.Count == 0;

        // Builds a puzzle and checks its data. Mistakes are listed in `problems`, never thrown.
        public Puzzle(string name, PuzzleElement[] elements, PuzzleAction[] actions, PuzzleCondition[] goal)
        {
            this.name = name ?? "";
            this.elements = elements ?? new PuzzleElement[0];
            check_elements();

            var resolved_actions = new List<ResolvedAction>();
            var seen_action_names = new HashSet<string>();
            foreach (var action in actions ?? new PuzzleAction[0])
            {
                if (action == null)
                {
                    found_problems.Add("an action is missing (null)");
                    continue;
                }
                if (string.IsNullOrEmpty(action.name) || !seen_action_names.Add(action.name))
                {
                    found_problems.Add("action name '" + action.name + "' is empty or used twice");
                }
                if (action.who.HasValue && !Enum.IsDefined(typeof(Character), action.who.Value))
                {
                    found_problems.Add("action '" + action.name + "' belongs to a character that doesn't exist");
                }
                if (action.sets.Length == 0)
                {
                    found_problems.Add("action '" + action.name + "' changes nothing");
                }
                resolve(action.requires, "action '" + action.name + "' requires", out int[] required_elements, out int[] required_states);
                resolve(action.sets, "action '" + action.name + "' sets", out int[] set_elements, out int[] set_states);
                resolved_actions.Add(new ResolvedAction(action.name, action.who, required_elements, required_states, set_elements, set_states));
            }
            this.actions = resolved_actions.ToArray();

            var goal_conditions = goal ?? new PuzzleCondition[0];
            if (goal_conditions.Length == 0)
            {
                found_problems.Add("the puzzle has no goal");
            }
            resolve(goal_conditions, "the goal", out goal_elements, out goal_states);
        }

        // The starting state: each element's state number, in element order.
        public int[] initial_state()
        {
            var state = new int[elements.Length];
            for (int index = 0; index < elements.Length; index += 1)
            {
                state[index] = elements[index].initial_state_index;
            }
            return state;
        }

        // True if this state meets the goal.
        public bool is_goal(int[] state)
        {
            return matches(state, goal_elements, goal_states);
        }

        // True if this character may do this action in this state, and it would change something.
        internal bool can_do(int[] state, ResolvedAction action, Character who)
        {
            if (action.who.HasValue && action.who.Value != who)
            {
                return false;
            }
            if (!matches(state, action.required_elements, action.required_states))
            {
                return false;
            }
            return !matches(state, action.set_elements, action.set_states);
        }

        // The state after an action. The original state is left as it was.
        internal static int[] after(int[] state, ResolvedAction action)
        {
            var next_state = (int[])state.Clone();
            for (int index = 0; index < action.set_elements.Length; index += 1)
            {
                next_state[action.set_elements[index]] = action.set_states[index];
            }
            return next_state;
        }

        // A state in words, for test messages and the playtest log: "gate=open, water=low".
        public string describe(int[] state)
        {
            var parts = new string[elements.Length];
            for (int index = 0; index < elements.Length; index += 1)
            {
                parts[index] = elements[index].name + "=" + elements[index].states[state[index]];
            }
            return string.Join(", ", parts);
        }

        // True if every listed element is in its listed state.
        static bool matches(int[] state, int[] element_indexes, int[] state_indexes)
        {
            for (int index = 0; index < element_indexes.Length; index += 1)
            {
                if (state[element_indexes[index]] != state_indexes[index])
                {
                    return false;
                }
            }
            return true;
        }

        // Checks element names, states and size, adding any problems.
        void check_elements()
        {
            var seen_names = new HashSet<string>();
            long state_count = 1;
            foreach (var element in elements)
            {
                if (element == null)
                {
                    found_problems.Add("an element is missing (null)");
                    continue;
                }
                if (string.IsNullOrEmpty(element.name) || !seen_names.Add(element.name))
                {
                    found_problems.Add("element name '" + element.name + "' is empty or used twice");
                }
                if (element.states.Length == 0)
                {
                    found_problems.Add("element '" + element.name + "' has no states");
                }
                else if (element.initial_state_index < 0 || element.initial_state_index >= element.states.Length)
                {
                    found_problems.Add("element '" + element.name + "' starts in a state it doesn't have");
                }
                state_count *= Math.Max(1, element.states.Length);
                if (state_count > maximum_state_count)
                {
                    found_problems.Add("the puzzle has more than " + maximum_state_count + " possible states, too many to check");
                    break;
                }
            }
        }

        // Turns conditions' names into element and state numbers, adding a problem for any name
        // that doesn't exist. Unknown conditions are dropped, so a broken puzzle can't crash.
        void resolve(PuzzleCondition[] conditions, string where, out int[] element_indexes, out int[] state_indexes)
        {
            var found_elements = new List<int>();
            var found_states = new List<int>();
            foreach (var condition in conditions)
            {
                int element_index = Array.FindIndex(elements, element => element != null && element.name == condition.element_name);
                int state_index = element_index < 0 ? -1 : Array.IndexOf(elements[element_index].states, condition.state_name);
                if (state_index < 0)
                {
                    found_problems.Add(where + " '" + condition.element_name + "=" + condition.state_name + "', which doesn't exist");
                    continue;
                }
                found_elements.Add(element_index);
                found_states.Add(state_index);
            }
            element_indexes = found_elements.ToArray();
            state_indexes = found_states.ToArray();
        }
    }

    // One playthrough of a puzzle: the scene reports actions, this keeps the state.
    public class PuzzleRun
    {
        readonly Puzzle puzzle;
        int[] state;

        // Reports refused as broken input: unknown names, missing names, characters that don't exist.
        public int refused_action_count { get; private set; }

        // True once the goal is met.
        public bool is_solved => puzzle.is_valid && puzzle.is_goal(state);

        // Starts a playthrough from the puzzle's starting state.
        public PuzzleRun(Puzzle puzzle)
        {
            this.puzzle = puzzle;
            state = puzzle.initial_state();
        }

        // The current state in words.
        public string describe() => puzzle.describe(state);

        // The scene reports that a character tried an action. Returns true if it happened.
        // Trying something whose conditions don't hold yet is normal play, not refused.
        public bool report_action(string action_name, Character who)
        {
            if (!puzzle.is_valid || is_solved)
            {
                return false;
            }
            int action_index = Array.FindIndex(puzzle.actions, action => action.name == action_name);
            if (action_name == null || action_index < 0 || !Enum.IsDefined(typeof(Character), who))
            {
                refused_action_count += 1;
                return false;
            }
            var action = puzzle.actions[action_index];
            if (!puzzle.can_do(state, action, who))
            {
                return false;
            }
            state = Puzzle.after(state, action);
            return true;
        }

        // The names of the actions this character could do right now ("has a move", spec R5.1).
        public string[] available_actions(Character who)
        {
            var names = new List<string>();
            if (puzzle.is_valid && !is_solved)
            {
                foreach (var action in puzzle.actions)
                {
                    if (puzzle.can_do(state, action, who))
                    {
                        names.Add(action.name);
                    }
                }
            }
            return names.ToArray();
        }
    }
}
