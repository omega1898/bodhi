// Purpose: checks a puzzle by trying every order of actions (a breadth-first search: all
// states one move away, then two, and so on). It proves the puzzle is solvable, finds every
// soft lock (a state the player can reach from which the goal can no longer be reached),
// counts wrong moves, and checks whether one character could solve it alone (spec R6.1 to R6.3).
using System.Collections.Generic;

namespace Bodhi.Rules
{
    // What the solver found.
    public class PuzzleReport
    {
        public bool is_solvable;
        public int reachable_state_count;
        public string[] shortest_solution = new string[0];
        public int soft_lock_state_count;
        public string soft_lock_example = "";
        public int wrong_move_count;          // Moves that take the player farther from the goal.
        public bool solvable_by_human_alone;
        public bool solvable_by_dog_alone;
    }

    public static class PuzzleSolver
    {
        // One move between two states, kept so the search can be walked backwards.
        readonly struct Move
        {
            public readonly string from_key;
            public readonly string to_key;
            public readonly string action_name;

            // Records a move.
            public Move(string from_key, string to_key, string action_name)
            {
                this.from_key = from_key;
                this.to_key = to_key;
                this.action_name = action_name;
            }
        }

        // Checks every reachable state of the puzzle and reports what it found.
        public static PuzzleReport solve(Puzzle puzzle)
        {
            var report = new PuzzleReport();
            if (puzzle == null || !puzzle.is_valid)
            {
                return report;
            }

            // 1. Forward search from the start, with both characters able to act.
            var states_by_key = new Dictionary<string, int[]>();
            var first_move_into = new Dictionary<string, Move>();
            var moves = new List<Move>();
            var goal_keys = new List<string>();
            explore(puzzle, null, states_by_key, first_move_into, moves, goal_keys);
            report.reachable_state_count = states_by_key.Count;
            report.is_solvable = goal_keys.Count > 0;

            // 2. Backward search from every goal: how many moves each state is from the goal.
            var moves_into = new Dictionary<string, List<string>>();
            foreach (var move in moves)
            {
                if (!moves_into.TryGetValue(move.to_key, out var sources))
                {
                    sources = new List<string>();
                    moves_into[move.to_key] = sources;
                }
                sources.Add(move.from_key);
            }
            var distance_to_goal = new Dictionary<string, int>();
            var queue = new Queue<string>();
            foreach (var goal_key in goal_keys)
            {
                distance_to_goal[goal_key] = 0;
                queue.Enqueue(goal_key);
            }
            while (queue.Count > 0)
            {
                string key = queue.Dequeue();
                if (!moves_into.TryGetValue(key, out var sources))
                {
                    continue;
                }
                foreach (var source in sources)
                {
                    if (!distance_to_goal.ContainsKey(source))
                    {
                        distance_to_goal[source] = distance_to_goal[key] + 1;
                        queue.Enqueue(source);
                    }
                }
            }

            // 3. Soft locks: reachable states with no way back to the goal.
            foreach (var entry in states_by_key)
            {
                if (!distance_to_goal.ContainsKey(entry.Key))
                {
                    if (report.soft_lock_state_count == 0)
                    {
                        report.soft_lock_example = puzzle.describe(entry.Value);
                    }
                    report.soft_lock_state_count += 1;
                }
            }

            // 4. Wrong moves: moves that leave the player farther from the goal (or soft-locked).
            foreach (var move in moves)
            {
                bool from_can_finish = distance_to_goal.TryGetValue(move.from_key, out int from_distance);
                bool to_can_finish = distance_to_goal.TryGetValue(move.to_key, out int to_distance);
                if (from_can_finish && (!to_can_finish || to_distance > from_distance))
                {
                    report.wrong_move_count += 1;
                }
            }

            // 5. The shortest solution: walk back from the first goal found to the start.
            if (report.is_solvable)
            {
                var steps = new List<string>();
                string key = goal_keys[0];
                while (first_move_into.TryGetValue(key, out var move))
                {
                    steps.Add(move.action_name);
                    key = move.from_key;
                }
                steps.Reverse();
                report.shortest_solution = steps.ToArray();
            }

            // 6. Could one character solve it alone? A good two-character puzzle says no to both.
            report.solvable_by_human_alone = can_solve_alone(puzzle, Character.human);
            report.solvable_by_dog_alone = can_solve_alone(puzzle, Character.dog);
            return report;
        }

        // Every state the player can reach from the start, for tests that check each one.
        internal static List<int[]> reachable_states(Puzzle puzzle)
        {
            var states_by_key = new Dictionary<string, int[]>();
            if (puzzle != null && puzzle.is_valid)
            {
                explore(puzzle, null, states_by_key, new Dictionary<string, Move>(), new List<Move>(), new List<string>());
            }
            return new List<int[]>(states_by_key.Values);
        }

        // True if the goal can be reached with only this character acting.
        static bool can_solve_alone(Puzzle puzzle, Character who)
        {
            var goal_keys = new List<string>();
            explore(puzzle, who, new Dictionary<string, int[]>(), new Dictionary<string, Move>(), new List<Move>(), goal_keys);
            return goal_keys.Count > 0;
        }

        // Breadth-first search from the start. `only_who` null lets both characters act.
        // The goal ends a playthrough, so goal states are not explored further.
        static void explore(Puzzle puzzle, Character? only_who, Dictionary<string, int[]> states_by_key,
            Dictionary<string, Move> first_move_into, List<Move> moves, List<string> goal_keys)
        {
            var start = puzzle.initial_state();
            string start_key = key_of(start);
            states_by_key[start_key] = start;
            var queue = new Queue<int[]>();
            queue.Enqueue(start);
            var characters = only_who.HasValue ? new[] { only_who.Value } : new[] { Character.human, Character.dog };
            while (queue.Count > 0)
            {
                var state = queue.Dequeue();
                string state_key = key_of(state);
                if (puzzle.is_goal(state))
                {
                    goal_keys.Add(state_key);
                    continue;
                }
                foreach (var action in puzzle.actions)
                {
                    bool someone_can = false;
                    foreach (var who in characters)
                    {
                        someone_can |= puzzle.can_do(state, action, who);
                    }
                    if (!someone_can)
                    {
                        continue;
                    }
                    var next_state = Puzzle.after(state, action);
                    string next_key = key_of(next_state);
                    var move = new Move(state_key, next_key, action.name);
                    moves.Add(move);
                    if (!states_by_key.ContainsKey(next_key))
                    {
                        states_by_key[next_key] = next_state;
                        first_move_into[next_key] = move;
                        queue.Enqueue(next_state);
                    }
                }
            }
        }

        // A state as text, so it can be used as a dictionary key: "0,2,1".
        static string key_of(int[] state)
        {
            return string.Join(",", state);
        }
    }
}
