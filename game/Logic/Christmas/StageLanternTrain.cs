// Purpose: how Chapter Five of the Christmas story, "The Lantern Train", looks: Strand station
// and the train, the line through the birches and pines with a fallen tree across it, the stone
// viaduct over Skelder Gorge with its frozen waterfall and the recess in its wall, and the halt
// beyond; and what each story rule does on screen. The story is
// rules/Runtime/Christmas/Christmas05LanternTrain.cs.
//
// Left to right, in layout units: the station (0 to 800), the line (800 to 2600), the viaduct
// (2600 to 4200) and the halt (4200 to 5000). The floor is at y = 520. Everything is a grey box
// until the 3D scenery is built.
namespace Bodhi.Game
{
    public static partial class ChristmasStages
    {
        // Chapter Five: The Lantern Train.
        public static ChapterStage lantern_train()
        {
            var birch = StageSetting.of_text("color", "#c8c4b8");
            var pine = StageSetting.of_text("color", "#24302c");
            var train_red = StageSetting.of_text("color", "#9a2a24");
            var stone = StageSetting.of_text("color", "#6a6660");
            return new ChapterStage
            {
                id = "lantern_train",
                width = 5000,
                height = 600,
                mood = "calm",
                scenery = new StageSetting[]
                {
                    StageSetting.of_text("sky_top", "#2a2850"), StageSetting.of_text("sky_bottom", "#8a7aa8"),
                    StageSetting.of_text("far", "hills"), StageSetting.of_text("far_color", "#3e3a66"),
                    StageSetting.of_text("mid", "trees"), StageSetting.of_text("mid_color", "#28302c"),
                    StageSetting.of_text("ground_top", "#e8ecf4"), StageSetting.of_text("ground_body", "#a2aec4"),
                    StageSetting.of_text("wall", "#6a6660"), StageSetting.of_text("room_wall", "#7a6a5a"),
                    StageSetting.of_number("darkness", 0.15), StageSetting.of_text("darkness_color", "rgb(30,26,60)"),
                    StageSetting.of_text("darkness_floor", "#7d8790"), StageSetting.of_number("snow", 0),
                    StageSetting.of_number("wind", 0.05), StageSetting.of_text("mist", "rgba(200,190,240,0.06)"),
                },
                human_start_x = 500, human_start_feet_y = 520,
                dog_start_x = 400, dog_start_feet_y = 520,
                guest = new StageGuest { x = 340, feet_y = 520, size = 0.8, mode = "follow", pose = "" },
                solids = new StageSolid[]
                {
                    new StageSolid { kind = "floor", x = 0, y = 520, width = 5000, height = 100 },
                    new StageSolid { kind = "invisible", x = 0, y = 0, width = 10, height = 520 },
                    new StageSolid { kind = "invisible", x = 4990, y = 0, width = 10, height = 520 },
                },
                props = new StageProp[]
                {
                    // Strand station and the Lantern Train waiting at it.
                    new StageProp { kind = "sign", x = 120, y = 300, settings = new StageSetting[] { StageSetting.of_text("text", "Strand") } },
                    new StageProp { kind = "bench", x = 640, y = 520 },
                    new StageProp { kind = "train", x = 60, y = 520, after = "kofis_mum_rang", until = new string[] { "boarded" }, settings = new StageSetting[] { train_red } },
                    new StageProp { kind = "lamp", x = 360, y = 520, after = "kofis_mum_rang", until = new string[] { "boarded" }, settings = new StageSetting[] { StageSetting.of_number("h", 180), StageSetting.of_number("radius", 320), StageSetting.of_text("color", "rgba(255,200,120,0.18)") } },
                    // The line through the birches and pines, and the fallen tree.
                    new StageProp { kind = "rail", x = 800, y = 510, settings = new StageSetting[] { StageSetting.of_number("w", 4200), StageSetting.of_number("h", 10), StageSetting.of_text("color", "#4a4440") } },
                    new StageProp { kind = "tree", x = 950, y = 520, settings = new StageSetting[] { birch, StageSetting.of_number("w", 40) } },
                    new StageProp { kind = "tree", x = 1200, y = 520, settings = new StageSetting[] { pine } },
                    new StageProp { kind = "tree", x = 1550, y = 520, settings = new StageSetting[] { birch, StageSetting.of_number("w", 40) } },
                    new StageProp { kind = "debris", x = 1700, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 220), StageSetting.of_number("h", 50), StageSetting.of_text("color", "#3a3028") } },
                    new StageProp { kind = "tree", x = 2100, y = 520, settings = new StageSetting[] { pine } },
                    new StageProp { kind = "tree", x = 2450, y = 520, settings = new StageSetting[] { birch, StageSetting.of_number("w", 40) } },
                    // The viaduct, its frozen waterfall and the recess in its wall.
                    new StageProp { kind = "rail", x = 2600, y = 480, settings = new StageSetting[] { StageSetting.of_number("w", 1600), StageSetting.of_number("h", 40), stone } },
                    new StageProp { kind = "pillar", x = 2800, y = 820, settings = new StageSetting[] { stone } },
                    new StageProp { kind = "glass", x = 3050, y = 880, settings = new StageSetting[] { StageSetting.of_number("w", 60), StageSetting.of_number("h", 360), StageSetting.of_text("color", "rgba(190,230,240,0.5)") } },
                    new StageProp { kind = "pillar", x = 3600, y = 820, settings = new StageSetting[] { stone } },
                    new StageProp { kind = "doorframe", x = 3370, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 70), StageSetting.of_number("h", 110), StageSetting.of_text("color", "#4e4a46") } },
                    new StageProp { kind = "pillar", x = 4000, y = 820, settings = new StageSetting[] { stone } },
                    // The train crossing, then stopped beyond the viaduct with every window gold.
                    new StageProp { kind = "train", x = 2900, y = 520, after = "train_passes", until = new string[] { "brake" }, settings = new StageSetting[] { train_red } },
                    new StageProp { kind = "train", x = 4250, y = 520, after = "brake", settings = new StageSetting[] { train_red } },
                    new StageProp { kind = "lamp", x = 4550, y = 520, after = "brake", settings = new StageSetting[] { StageSetting.of_number("h", 180), StageSetting.of_number("radius", 320), StageSetting.of_text("color", "rgba(255,200,120,0.18)") } },
                    // The halt.
                    new StageProp { kind = "house", x = 4560, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 160), StageSetting.of_number("h", 140), StageSetting.of_text("color", "#5a4a3a") } },
                    new StageProp { kind = "sign", x = 4560, y = 330, settings = new StageSetting[] { StageSetting.of_text("text", "Skelder Halt") } },
                },
                things = new StageThingLook[]
                {
                    new StageThingLook { event_name = "keepsake_rail_crumbs", sprite = "keepsake", sound = "sniff" },
                    new StageThingLook { event_name = "round_the_tree", pose = "lean" },
                    new StageThingLook { event_name = "keepsake_lantern_tag", sprite = "keepsake" },
                    new StageThingLook { event_name = "boarded", sound = "gate" },
                    new StageThingLook { event_name = "biscuit_in_recess", sound = "thud", pose = "lean" },
                    new StageThingLook { event_name = "keepsake_tea_flask", sprite = "keepsake" },
                },
                // One entry per story rule, in the same order; the comment names the rule.
                rules = new RuleStaging[]
                {
                    new RuleStaging { hide = new[] { new StageHiding { who = StageWho.human, hidden = true } }, camera = CameraChange.hold, camera_x = 450, camera_y = 420, camera_zoom = 1.0, caption = "The seventeenth of December" },  // 0 level_started
                    new RuleStaging { },  // 1 morning_1
                    new RuleStaging { camera = CameraChange.release, caption = "" },  // 2 on_the_line
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.guest, x = 1690, feet_y = 520 } }, guest_mode = "still" },  // 3 fallen_tree
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.guest, x = 1940, feet_y = 520 } }, guest_mode = "follow" },  // 4 round_the_tree
                    new RuleStaging { darkness = 0.4, mood = "quiet" },  // 5 line_climbing
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.human, x = 500, feet_y = 520 } }, hide = new[] { new StageHiding { who = StageWho.human, hidden = false }, new StageHiding { who = StageWho.dog, hidden = true }, new StageHiding { who = StageWho.guest, hidden = true } }, darkness = 0.45, mood = "hopeful", fade_in = true, caption = "Strand, four o'clock" },  // 6 kofis_mum_rang
                    new RuleStaging { hide = new[] { new StageHiding { who = StageWho.human, hidden = true } }, camera = CameraChange.hold, camera_x = 360, camera_y = 380, camera_zoom = 0.8, sound = "chime", caption = "" },  // 7 boarded
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.dog, x = 2700, feet_y = 520 }, new StagePlacement { who = StageWho.guest, x = 2640, feet_y = 520 } }, hide = new[] { new StageHiding { who = StageWho.dog, hidden = false }, new StageHiding { who = StageWho.guest, hidden = false } }, guest_mode = "follow", camera = CameraChange.release, darkness = 0.55, mood = "tense", fade_in = true },  // 8 viaduct
                    new RuleStaging { shake_seconds = 1.0 },  // 9 rails_hum
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.dog, x = 3410, feet_y = 520 }, new StagePlacement { who = StageWho.guest, x = 3380, feet_y = 520 } }, guest_mode = "still", camera = CameraChange.hold, camera_x = 3400, camera_y = 400, camera_zoom = 0.85 },  // 10 biscuit_in_recess
                    new RuleStaging { shake_seconds = 2.0, sound = "thud" },  // 11 train_passes
                    new RuleStaging { },  // 12 her_through_glass
                    new RuleStaging { hide = new[] { new StageHiding { who = StageWho.dog, hidden = true }, new StageHiding { who = StageWho.guest, hidden = true } } },  // 13 bolted
                    new RuleStaging { shake_seconds = 0.6, sound = "thud" },  // 14 brake
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.human, x = 4480, feet_y = 520 } }, hide = new[] { new StageHiding { who = StageWho.human, hidden = false }, new StageHiding { who = StageWho.dog, hidden = true }, new StageHiding { who = StageWho.guest, hidden = true } }, camera = CameraChange.release, darkness = 0.7, mood = "quiet" },  // 15 ran_back
                    new RuleStaging { },  // 16 recess_reached
                    new RuleStaging { camera = CameraChange.hold, camera_x = 3400, camera_y = 440, camera_zoom = 1.0 },  // 17 her whistle into the forest
                    new RuleStaging { sound = "bark", mood = "hopeful" },  // 18 one_bark
                    new RuleStaging { },  // 19 chapter_done
                },
                aftermath = new StageAftermath { scene = "snow_hut", mood = "hopeful", rain = 0 },
            };
        }
    }
}
