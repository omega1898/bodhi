// Purpose: how Chapter One of the Christmas story, "First Snow", looks: its floors and doors, the
// cabin, the clearing, the lane and the timber yard, and what each story rule does on screen.
// The story itself is rules/Runtime/Christmas/Christmas01FirstSnow.cs; this file only stages it.
//
// Left to right, in layout units: the pine woods behind the cabin (0 to 1500, shut until the
// night search), the cabin (1500 to 3000), the clearing (3000 to 4200), the lane (4200 to 6000)
// and the timber yard (6000 to 7600). The floor is at y = 520. Everything is a grey box until
// the 3D scenery is built.
namespace Bodhi.Game
{
    public static partial class ChristmasStages
    {
        // Chapter One: First Snow.
        public static ChapterStage first_snow()
        {
            var warm_lamp = StageSetting.of_text("color", "rgba(255,206,150,0.18)");
            return new ChapterStage
            {
                id = "first_snow",
                width = 7600,
                height = 600,
                mood = "quiet",
                scenery = new StageSetting[]
                {
                    StageSetting.of_text("sky_top", "#9fb3c8"), StageSetting.of_text("sky_bottom", "#e6edf2"),
                    StageSetting.of_text("far", "hills"), StageSetting.of_text("far_color", "#c4ced8"),
                    StageSetting.of_text("mid", "trees"), StageSetting.of_text("mid_color", "#3f4f4a"),
                    StageSetting.of_text("near", "near_trees"), StageSetting.of_text("near_color", "#2f3d38"),
                    StageSetting.of_text("ground_top", "#f2f5f8"), StageSetting.of_text("ground_body", "#b8c6d4"),
                    StageSetting.of_text("wall", "#6b5a4a"), StageSetting.of_text("room_wall", "#8a6f55"),
                    StageSetting.of_number("darkness", 0.08), StageSetting.of_text("darkness_color", "rgb(40,48,70)"),
                    StageSetting.of_text("darkness_floor", "#7d8790"), StageSetting.of_number("snow", 0.6),
                    StageSetting.of_number("wind", 0.2), StageSetting.of_text("mist", "rgba(235,240,248,0.10)"),
                },
                human_start_x = 1640, human_start_feet_y = 520,
                dog_start_x = 2740, dog_start_feet_y = 520,
                guest = new StageGuest { x = 3760, feet_y = 520, size = 0.8, mode = "gone", pose = "sit" },
                solids = new StageSolid[]
                {
                    new StageSolid { kind = "floor", x = 0, y = 520, width = 7600, height = 100 },
                    new StageSolid { id = "back_door", kind = "invisible", x = 1490, y = 0, width = 10, height = 520 },
                    new StageSolid { id = "front_door", kind = "invisible", x = 2990, y = 0, width = 10, height = 520 },
                    new StageSolid { kind = "invisible", x = 7590, y = 0, width = 10, height = 520 },
                },
                props = new StageProp[]
                {
                    // The pine woods behind the cabin.
                    new StageProp { kind = "tree", x = 160, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#2f3d38") } },
                    new StageProp { kind = "snow_drift", x = 260, y = 520 },
                    new StageProp { kind = "tree", x = 520, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#2f3d38") } },
                    new StageProp { kind = "tree", x = 880, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#2f3d38") } },
                    new StageProp { kind = "snow_drift", x = 1000, y = 520 },
                    new StageProp { kind = "tree", x = 1260, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#2f3d38") } },
                    // The cabin.
                    new StageProp { kind = "room_wall", x = 1500, y = 200, settings = new StageSetting[] { StageSetting.of_number("w", 1500), StageSetting.of_number("h", 320) } },
                    new StageProp { kind = "double_bed", x = 1540, y = 520, until = new string[] { "went_to_bed" }, settings = new StageSetting[] { StageSetting.of_text("color", "#4d6a86") } },
                    new StageProp { kind = "double_bed", x = 1540, y = 520, after = "went_to_bed", until = new string[] { "hammering" }, settings = new StageSetting[] { StageSetting.of_text("color", "#4d6a86"), StageSetting.of_flag("occupied", true) } },
                    new StageProp { kind = "double_bed", x = 1540, y = 520, after = "hammering", settings = new StageSetting[] { StageSetting.of_text("color", "#4d6a86") } },
                    new StageProp { kind = "stove", x = 1860, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#2b2b2e") } },
                    new StageProp { kind = "lamp", x = 1900, y = 200, settings = new StageSetting[] { StageSetting.of_flag("hanging", true), StageSetting.of_number("radius", 220), warm_lamp } },
                    new StageProp { kind = "kitchen", x = 2060, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 180) } },
                    new StageProp { kind = "sign", x = 2240, y = 330, settings = new StageSetting[] { StageSetting.of_text("text", "Puppy book") } },
                    new StageProp { kind = "tree", x = 2450, y = 520, after = "home_with_tree", until = new string[] { "tree_decorated" }, settings = new StageSetting[] { StageSetting.of_text("color", "#3f6b4a"), StageSetting.of_number("w", 70), StageSetting.of_number("h", 170) } },
                    new StageProp { kind = "tree", x = 2450, y = 520, after = "tree_decorated", settings = new StageSetting[] { StageSetting.of_text("color", "#5f8a5a"), StageSetting.of_number("w", 70), StageSetting.of_number("h", 170) } },
                    new StageProp { kind = "sofa", x = 2540, y = 520 },
                    new StageProp { kind = "sign", x = 1960, y = 300, after = "stocking_hung", settings = new StageSetting[] { StageSetting.of_text("text", "BODHI") } },
                    new StageProp { kind = "window", x = 2720, y = 280, settings = new StageSetting[] { StageSetting.of_number("w", 80), StageSetting.of_number("h", 90) } },
                    new StageProp { kind = "lamp", x = 2760, y = 380, after = "lamp_lit", settings = new StageSetting[] { StageSetting.of_number("h", 30), StageSetting.of_number("radius", 260), warm_lamp } },
                    // The clearing, the lane and the timber yard.
                    new StageProp { kind = "sled", x = 3080, y = 520, until = new string[] { "tree_tied_on" }, settings = new StageSetting[] { StageSetting.of_number("w", 110), StageSetting.of_number("h", 25), StageSetting.of_text("color", "#7a4a2a") } },
                    new StageProp { kind = "snowman", x = 3560, y = 520, after = "snowman_base_rolled", settings = new StageSetting[] { StageSetting.of_number("w", 80), StageSetting.of_number("h", 150), StageSetting.of_text("color", "#f4f7fa") } },
                    new StageProp { kind = "tree", x = 4300, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#2f3d38") } },
                    new StageProp { kind = "tree", x = 4800, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#2f3d38") } },
                    new StageProp { kind = "tree", x = 5400, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#2f3d38") } },
                    new StageProp { kind = "sign", x = 6020, y = 300, settings = new StageSetting[] { StageSetting.of_text("text", "Hollin Timber") } },
                    new StageProp { kind = "lorry", x = 6300, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 320), StageSetting.of_number("h", 150), StageSetting.of_text("color", "#2f5a3a") } },
                    new StageProp { kind = "lamp", x = 6900, y = 520, settings = new StageSetting[] { StageSetting.of_number("radius", 240), StageSetting.of_text("color", "rgba(255,170,90,0.16)") } },
                    new StageProp { kind = "tree", x = 7000, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#45664c"), StageSetting.of_number("w", 60), StageSetting.of_number("h", 160) } },
                    new StageProp { kind = "tree", x = 7120, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#45664c"), StageSetting.of_number("w", 60), StageSetting.of_number("h", 160) } },
                    new StageProp { kind = "tree", x = 7300, y = 520, until = new string[] { "tree_tied_on" }, settings = new StageSetting[] { StageSetting.of_text("color", "#3f6b4a"), StageSetting.of_number("w", 70), StageSetting.of_number("h", 170) } },
                    new StageProp { kind = "tree", x = 7460, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#45664c"), StageSetting.of_number("w", 60), StageSetting.of_number("h", 160) } },
                },
                things = new StageThingLook[]
                {
                    new StageThingLook { event_name = "front_door_opened", sound = "gate" },
                    new StageThingLook { event_name = "snowball_thrown", sound = "pickup", pose = "turn" },
                    new StageThingLook { event_name = "snowball_dug_out", sound = "sniff", pose = "sniff" },
                    new StageThingLook { event_name = "snowman_base_rolled", sound = "thud", pose = "kneel" },
                    new StageThingLook { event_name = "stick_fetched", sound = "pickup" },
                    new StageThingLook { event_name = "keepsake_minas_card", sprite = "keepsake" },
                    new StageThingLook { event_name = "sandwich_sniffed", sound = "sniff", pose = "sniff" },
                    new StageThingLook { event_name = "tree_chosen", pose = "sit" },
                    new StageThingLook { event_name = "tree_tied_on", sound = "pickup", pose = "kneel" },
                    new StageThingLook { event_name = "towelled_dry", sound = "towel", pose = "kneel" },
                    new StageThingLook { event_name = "dinner_given", sprite = "bowl", sound = "eat" },
                    new StageThingLook { event_name = "tree_decorated", sound = "chime" },
                    new StageThingLook { event_name = "keepsake_stolen_bauble", sprite = "keepsake", sound = "sniff" },
                    new StageThingLook { event_name = "stocking_hung" },
                    new StageThingLook { event_name = "keepsake_puppy_book", sprite = "keepsake" },
                    new StageThingLook { event_name = "lamp_lit", sound = "pickup" },
                    new StageThingLook { event_name = "went_to_bed" },
                    new StageThingLook { event_name = "night_door_opened", sound = "gate" },
                    new StageThingLook { event_name = "lantern_taken", sound = "pickup" },
                },
                // One entry per story rule, in the same order; the comment names the rule.
                rules = new RuleStaging[]
                {
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.human, x = 1640, feet_y = 520 }, new StagePlacement { who = StageWho.dog, x = 2740, feet_y = 520 } }, pose = new[] { new StagePose { who = StageWho.dog, pose = "sit", seconds = 12 } }, camera = CameraChange.hold, camera_x = 2200, camera_y = 420, camera_zoom = 0.95, darkness = 0.25, caption = "The fourteenth of December" },  // 0 level_started
                    new RuleStaging { face = new[] { new StageFacing { who = StageWho.dog, direction = 1 } }, pose = new[] { new StagePose { who = StageWho.dog, pose = "sniff", seconds = 4 } }, sound = "whine" },  // 1 wake_1
                    new RuleStaging { walk = new[] { new StageWalk { who = StageWho.human, x = 2660 } }, darkness = 0.15, caption = "" },  // 2 wake_2
                    new RuleStaging { camera = CameraChange.release },  // 3 woke_up
                    new RuleStaging { dog_runs_to = 3300, darkness = 0.06, mood = "hopeful" },  // 4 front_door_opened
                    new RuleStaging { pose = new[] { new StagePose { who = StageWho.dog, pose = "land", seconds = 1 } } },  // 5 first_steps
                    new RuleStaging { pose = new[] { new StagePose { who = StageWho.dog, pose = "sniff", seconds = 2 } } },  // 6 first_bites
                    new RuleStaging { dog_runs_to = 3750, sound = "thud" },  // 7 snowball_thrown
                    new RuleStaging { scent = ScentChange.point, scent_x = 3750, scent_y = 500 },  // 8 snowball_gone
                    new RuleStaging { scent = ScentChange.clear },  // 9 snowball_dug_out
                    new RuleStaging { dog_runs_to = 3420 },  // 10 snowball_returned
                    new RuleStaging { sound = "chime" },  // 11 human_called: the little brass bell
                    new RuleStaging { },  // 12 snowball returned and whistled for
                    new RuleStaging { },  // 13 snowman_base_rolled
                    new RuleStaging { dog_runs_to = 3620 },  // 14 stick_fetched
                    new RuleStaging { pose = new[] { new StagePose { who = StageWho.dog, pose = "sit", seconds = 3 } } },  // 15 carrot_stolen
                    new RuleStaging { },  // 16 nose_gone
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.guest, x = 3760, feet_y = 520 } }, guest_mode = "still", camera = CameraChange.hold, camera_x = 3550, camera_y = 420, camera_zoom = 0.95, sound = "bark" },  // 17 mina_came
                    new RuleStaging { walk = new[] { new StageWalk { who = StageWho.dog, x = 3700 } }, pose = new[] { new StagePose { who = StageWho.dog, pose = "lean", seconds = 3 } } },  // 18 mina_2
                    new RuleStaging { },  // 19 mina_3
                    new RuleStaging { face = new[] { new StageFacing { who = StageWho.guest, direction = -1 } }, pose = new[] { new StagePose { who = StageWho.guest, pose = "sit", seconds = 6 } } },  // 20 mina_4: Biscuit stares at the hill
                    new RuleStaging { sound = "bell" },  // 21 mina_5: Mercy rings twelve
                    new RuleStaging { guest_mode = "gone", camera = CameraChange.release },  // 22 mina_done
                    new RuleStaging { mood = "calm" },  // 23 reached_timber_yard
                    new RuleStaging { },  // 24 sandwich_sniffed
                    new RuleStaging { },  // 25 dougie_caught_him
                    new RuleStaging { pose = new[] { new StagePose { who = StageWho.dog, pose = "sit", seconds = 6 } } },  // 26 tree_chosen
                    new RuleStaging { },  // 27 dougie_agreed
                    new RuleStaging { mood = "hopeful" },  // 28 tree_tied_on
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.human, x = 2860, feet_y = 520 }, new StagePlacement { who = StageWho.dog, x = 2800, feet_y = 520 } }, darkness = 0.35, mood = "tender", fade_in = true },  // 29 home_with_tree
                    new RuleStaging { },  // 30 towelled_dry
                    new RuleStaging { },  // 31 dinner_given
                    new RuleStaging { },  // 32 tree_decorated
                    new RuleStaging { dog_runs_to = 2600 },  // 33 bauble_missing
                    new RuleStaging { },  // 34 keepsake_stolen_bauble
                    new RuleStaging { },  // 35 stocking_hung
                    new RuleStaging { darkness = 0.55 },  // 36 lamp_lit
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.dog, x = 1660, feet_y = 500 } }, pose = new[] { new StagePose { who = StageWho.dog, pose = "sleep", seconds = 9999 } }, hide = new[] { new StageHiding { who = StageWho.human, hidden = true } }, camera = CameraChange.hold, camera_x = 1700, camera_y = 430, camera_zoom = 1.0, darkness = 0.75, mood = "quiet" },  // 37 went_to_bed
                    new RuleStaging { },  // 38 bed_2
                    new RuleStaging { pose = new[] { new StagePose { who = StageWho.dog, pose = "stop", seconds = 3 } }, shake_seconds = 0.6, mood = "tense" },  // 39 midnight
                    new RuleStaging { shake_seconds = 1.2 },  // 40 midnight_2
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.human, x = 1720, feet_y = 520 }, new StagePlacement { who = StageWho.dog, x = 1780, feet_y = 520 } }, hide = new[] { new StageHiding { who = StageWho.human, hidden = false } }, camera = CameraChange.release, sound = "thud" },  // 41 hammering
                    new RuleStaging { camera = CameraChange.hold, camera_x = 2900, camera_y = 420, camera_zoom = 1.0 },  // 42 night_door_opened
                    new RuleStaging { },  // 43 door_2
                    new RuleStaging { camera = CameraChange.release },  // 44 set_out_ready
                    new RuleStaging { darkness = 0.8 },  // 45 lantern_taken
                    new RuleStaging { shake_seconds = 0.8 },  // 46 into_the_woods
                    new RuleStaging { scent = ScentChange.point, scent_x = 420, scent_y = 480 },  // 47 scent_caught
                    new RuleStaging { },  // 48 lead_sang
                    new RuleStaging { },  // 49 under_it_all
                    new RuleStaging { walk = new[] { new StageWalk { who = StageWho.dog, x = 0 } }, camera = CameraChange.hold, camera_x = 450, camera_y = 430, camera_zoom = 1.1, sound = "bark" },  // 50 the_lunge
                    new RuleStaging { hide = new[] { new StageHiding { who = StageWho.dog, hidden = true } } },  // 51 lead_went_light
                    new RuleStaging { hide = new[] { new StageHiding { who = StageWho.dog, hidden = true } }, scent = ScentChange.clear, camera = CameraChange.release, sound = "chime" },  // 52 lead_came_back: the bell in her hand
                    new RuleStaging { camera = CameraChange.hold, camera_x = 450, camera_y = 430, camera_zoom = 1.15, darkness = 0.88 },  // 53 her whistle, unanswered
                    new RuleStaging { darkness = 0.95, mood = "quiet" },  // 54 into_the_dark
                    new RuleStaging { },  // 55 chapter_done
                },
                aftermath = new StageAftermath { scene = "snow_hut", mood = "quiet", rain = 0 },
            };
        }
    }
}
