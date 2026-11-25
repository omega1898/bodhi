// Purpose: how Chapter Six of the Christmas story, "The Reindeer Hill", looks: the forest climbing
// the hill, the iced field gate only a dog can get under, the reindeer pens and barn, the runaway
// calf, Tam's farmhouse with its lit window and yard light, and the moor under the aurora; and what
// each story rule does on screen. The story is rules/Runtime/Christmas/Christmas06ReindeerHill.cs.
//
// Left to right, in layout units: the forest (0 to 1700), the field gate (1780), the pens and barn
// (1900 to 2900), the farmhouse (2900 to 3600) and the moor (3600 to 4200). The floor is at
// y = 520. Everything is a grey box until the 3D scenery (and the aurora) is built.
namespace Bodhi.Game
{
    public static partial class ChristmasStages
    {
        // Chapter Six: The Reindeer Hill.
        public static ChapterStage reindeer_hill()
        {
            var pine = StageSetting.of_text("color", "#1c2a26");
            var pen_wood = StageSetting.of_text("color", "#5a4630");
            var reindeer = StageSetting.of_text("color", "#4a3a2e");
            return new ChapterStage
            {
                id = "reindeer_hill",
                width = 4200,
                height = 600,
                mood = "quiet",
                scenery = new StageSetting[]
                {
                    StageSetting.of_text("sky_top", "#0b1a24"), StageSetting.of_text("sky_bottom", "#1f4a3e"),
                    StageSetting.of_text("far", "hills"), StageSetting.of_text("far_color", "#18302c"),
                    StageSetting.of_text("mid", "trees"), StageSetting.of_text("mid_color", "#1c2a26"),
                    StageSetting.of_text("ground_top", "#dfe8ee"), StageSetting.of_text("ground_body", "#93a6b8"),
                    StageSetting.of_text("wall", "#6a6460"), StageSetting.of_text("room_wall", "#7a6450"),
                    StageSetting.of_number("darkness", 0.65), StageSetting.of_text("darkness_color", "rgb(14,32,36)"),
                    StageSetting.of_text("darkness_floor", "#6d7c8a"), StageSetting.of_number("snow", 0),
                    StageSetting.of_number("wind", 0.05), StageSetting.of_text("mist", "rgba(140,230,190,0.06)"),
                },
                human_start_x = 300, human_start_feet_y = 520,
                dog_start_x = 300, dog_start_feet_y = 520,
                guest = new StageGuest { x = 240, feet_y = 520, size = 0.8, mode = "follow", pose = "" },
                solids = new StageSolid[]
                {
                    new StageSolid { kind = "floor", x = 0, y = 520, width = 4200, height = 100 },
                    new StageSolid { kind = "invisible", x = 0, y = 0, width = 10, height = 520 },
                    new StageSolid { id = "field_gate", kind = "gate", x = 1780, y = 430, width = 12, height = 90, blocks = SolidBlocks.human },
                    new StageSolid { kind = "invisible", x = 4190, y = 0, width = 10, height = 520 },
                },
                props = new StageProp[]
                {
                    // The forest climbing the hill.
                    new StageProp { kind = "tree", x = 200, y = 520, settings = new StageSetting[] { pine } },
                    new StageProp { kind = "tree", x = 600, y = 520, settings = new StageSetting[] { pine } },
                    new StageProp { kind = "snow_drift", x = 800, y = 520 },
                    new StageProp { kind = "tree", x = 1050, y = 520, settings = new StageSetting[] { pine } },
                    new StageProp { kind = "tree", x = 1500, y = 520, settings = new StageSetting[] { pine } },
                    // The reindeer pens, the barn and the herd.
                    new StageProp { kind = "garden_fence", x = 1950, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 300), pen_wood } },
                    new StageProp { kind = "deer", x = 2000, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 80), StageSetting.of_number("h", 75), reindeer } },
                    new StageProp { kind = "deer", x = 2140, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 80), StageSetting.of_number("h", 75), reindeer } },
                    new StageProp { kind = "calf", x = 2340, y = 520, after = "round_the_calf", settings = new StageSetting[] { StageSetting.of_number("w", 45), StageSetting.of_number("h", 50), StageSetting.of_text("color", "#e8e0d0") } },
                    new StageProp { kind = "house", x = 2420, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 300), StageSetting.of_number("h", 200), StageSetting.of_text("color", "#5a3a2a") } },
                    new StageProp { kind = "garden_fence", x = 2560, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 340), pen_wood } },
                    new StageProp { kind = "deer", x = 2660, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 80), StageSetting.of_number("h", 75), reindeer } },
                    new StageProp { kind = "deer", x = 2800, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 80), StageSetting.of_number("h", 75), reindeer } },
                    // Tam's farmhouse, its yard light and its lit window.
                    new StageProp { kind = "lamp", x = 2950, y = 520, settings = new StageSetting[] { StageSetting.of_number("radius", 300), StageSetting.of_text("color", "rgba(255,206,150,0.18)") } },
                    new StageProp { kind = "house", x = 3000, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 420), StageSetting.of_number("h", 240), StageSetting.of_text("color", "#6a6460") } },
                    new StageProp { kind = "window", x = 3120, y = 340, settings = new StageSetting[] { StageSetting.of_number("w", 90), StageSetting.of_number("h", 70), StageSetting.of_flag("glow", true), StageSetting.of_number("radius", 260), StageSetting.of_text("color", "rgba(255,206,150,0.2)") } },
                    // The moor, and the calf wandering out onto it.
                    new StageProp { kind = "calf", x = 3500, y = 520, until = new string[] { "round_the_calf" }, settings = new StageSetting[] { StageSetting.of_number("w", 45), StageSetting.of_number("h", 50), StageSetting.of_text("color", "#e8e0d0") } },
                    new StageProp { kind = "snow_drift", x = 3800, y = 520 },
                },
                things = new StageThingLook[]
                {
                    new StageThingLook { event_name = "keepsake_antler", sprite = "keepsake", sound = "sniff" },
                    new StageThingLook { event_name = "calf_penned", sound = "gate" },
                    new StageThingLook { event_name = "field_gate_opened", sound = "thud" },
                    new StageThingLook { event_name = "keepsake_herd_book", sprite = "keepsake" },
                    new StageThingLook { event_name = "keepsake_moss_photo", sprite = "keepsake" },
                },
                // One entry per story rule, in the same order; the comment names the rule.
                rules = new RuleStaging[]
                {
                    new RuleStaging { hide = new[] { new StageHiding { who = StageWho.human, hidden = true } }, camera = CameraChange.hold, camera_x = 400, camera_y = 420, camera_zoom = 1.0, caption = "The same night" },  // 0 level_started
                    new RuleStaging { },  // 1 forest_1
                    new RuleStaging { camera = CameraChange.release, caption = "" },  // 2 climbing
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.guest, x = 2060, feet_y = 520 } }, guest_mode = "still", pose = new[] { new StagePose { who = StageWho.guest, pose = "sleep", seconds = 9999 } } },  // 3 reached_pens: Biscuit lies down
                    new RuleStaging { face = new[] { new StageFacing { who = StageWho.dog, direction = -1 } } },  // 4 round_the_calf
                    new RuleStaging { },  // 5 calf_penned
                    new RuleStaging { camera = CameraChange.hold, camera_x = 2600, camera_y = 420, camera_zoom = 0.95, sound = "bark" },  // 6 his bark brings Tam
                    new RuleStaging { mood = "tender" },  // 7 tam_found_them
                    new RuleStaging { hide = new[] { new StageHiding { who = StageWho.guest, hidden = true } } },  // 8 tam_carried
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.human, x = 300, feet_y = 520 }, new StagePlacement { who = StageWho.dog, x = 2300, feet_y = 520 } }, hide = new[] { new StageHiding { who = StageWho.human, hidden = false } }, camera = CameraChange.release, mood = "quiet", fade_in = true },  // 9 em_climbing
                    new RuleStaging { darkness = 0.55, mood = "hopeful" },  // 10 saw_yard_light: the aurora
                    new RuleStaging { },  // 11 field_gate_opened
                    new RuleStaging { camera = CameraChange.hold, camera_x = 2000, camera_y = 420, camera_zoom = 0.8 },  // 12 her whistle
                    new RuleStaging { camera = CameraChange.release, mood = "tender" },  // 13 the reunion
                    new RuleStaging { },  // 14 reunion_embrace
                    new RuleStaging { sound = "chime" },  // 15 reunion_complete: the collar back on, the bell
                    new RuleStaging { },  // 16 puppy_book_line
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.human, x = 3250, feet_y = 520 }, new StagePlacement { who = StageWho.dog, x = 3300, feet_y = 520 }, new StagePlacement { who = StageWho.guest, x = 3180, feet_y = 520 } }, hide = new[] { new StageHiding { who = StageWho.guest, hidden = false } }, guest_mode = "still", camera = CameraChange.hold, camera_x = 3220, camera_y = 420, camera_zoom = 1.1, darkness = 0.45 },  // 17 into_the_kitchen
                    new RuleStaging { },  // 18 kitchen_2
                    new RuleStaging { mood = "tense" },  // 19 radio
                    new RuleStaging { darkness = 0.7, mood = "quiet" },  // 20 closing
                    new RuleStaging { },  // 21 chapter_done
                },
                aftermath = new StageAftermath { scene = "farm_fire", mood = "tender", rain = 0 },
            };
        }
    }
}
