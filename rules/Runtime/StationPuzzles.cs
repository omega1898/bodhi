// Purpose: the station chapter's main puzzle as data (spec section 2, "Main puzzle: both
// routes, water level, floating platform, counterweight"). She is on the near side of the
// flooded tracks, Bodhi on the far side. She has the power and the pump; he can squeeze
// through the vent to the valve and jump onto the platform. Neither can get him across alone.
// The solver test (StationPuzzleTests) proves it can't soft-lock.

namespace Bodhi.Rules
{
    public static class StationPuzzles
    {
        // The puzzle's parts and their starting states. A fresh copy every call.
        public static PuzzleElement[] elements()
        {
            return new[]
            {
                new PuzzleElement("power", new[] { "off", "on" }, 0),
                new PuzzleElement("valve", new[] { "closed", "open" }, 0),
                new PuzzleElement("water", new[] { "high", "mid", "low" }, 0),
                new PuzzleElement("platform", new[] { "far_side", "near_side" }, 0),
                new PuzzleElement("counterweight", new[] { "up", "down" }, 0),
                new PuzzleElement("bodhi", new[] { "far_side", "on_platform", "near_side" }, 0),
            };
        }

        // What each of them can do. A fresh copy every call.
        public static PuzzleAction[] actions()
        {
            return new[]
            {
                // Her route: the fuse box, the pump and the sluice are on her side.
                action("restore_power", Character.human, new[] { is_("power", "off") }, new[] { is_("power", "on") }),
                action("run_pump", Character.human, new[] { is_("power", "on"), is_("valve", "open"), is_("water", "high") }, new[] { is_("water", "mid") }),
                action("run_pump_again", Character.human, new[] { is_("power", "on"), is_("valve", "open"), is_("water", "mid") }, new[] { is_("water", "low") }),
                action("open_sluice_from_low", Character.human, new[] { is_("water", "low") }, new[] { is_("water", "mid") }),
                action("open_sluice_from_mid", Character.human, new[] { is_("water", "mid") }, new[] { is_("water", "high") }),
                // The platform only floats free at the middle level: too fast when high, aground when low.
                action("haul_platform_across", Character.human, new[] { is_("platform", "far_side"), is_("water", "mid") }, new[] { is_("platform", "near_side") }),
                action("haul_platform_back", Character.human, new[] { is_("platform", "near_side"), is_("water", "mid") }, new[] { is_("platform", "far_side") }),
                action("release_counterweight", Character.human, new[] { is_("counterweight", "up") }, new[] { is_("counterweight", "down") }),

                // His route: only he fits through the vent to the valve, and only he can jump.
                action("nose_open_valve", Character.dog, new[] { is_("bodhi", "far_side"), is_("valve", "closed") }, new[] { is_("valve", "open") }),
                action("jump_onto_platform", Character.dog, new[] { is_("bodhi", "far_side"), is_("platform", "far_side"), is_("water", "mid") }, new[] { is_("bodhi", "on_platform") }),
                action("jump_back_to_far_side", Character.dog, new[] { is_("bodhi", "on_platform"), is_("platform", "far_side") }, new[] { is_("bodhi", "far_side") }),
                action("climb_the_ramp", Character.dog, new[] { is_("bodhi", "on_platform"), is_("platform", "near_side"), is_("counterweight", "down") }, new[] { is_("bodhi", "near_side") }),
            };
        }

        // Solved when Bodhi is on her side.
        public static PuzzleCondition[] goal()
        {
            return new[] { is_("bodhi", "near_side") };
        }

        // The whole puzzle, ready to play or to check.
        public static Puzzle main_puzzle()
        {
            return new Puzzle("station_main_puzzle", elements(), actions(), goal());
        }

        // Short helpers so the table above reads like a sentence. The trailing underscore
        // is there because "is" is a reserved word in C#.
        static PuzzleCondition is_(string element_name, string state_name)
        {
            return new PuzzleCondition(element_name, state_name);
        }

        // Makes one action.
        static PuzzleAction action(string name, Character who, PuzzleCondition[] requires, PuzzleCondition[] sets)
        {
            return new PuzzleAction(name, who, requires, sets);
        }
    }
}
