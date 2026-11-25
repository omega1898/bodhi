// Purpose: how Chapter Two of the Christmas story, "The Lamp in the Window", looks: the cabin
// porch, the pines in the blizzard, the fallen spruce, the slope down to the timber yard, the
// fence only a dog can get under, the yard lamp and Dougie's lorry, and what each story rule does
// on screen. The story is rules/Runtime/Christmas/Christmas02LampInWindow.cs.
//
// Left to right, in layout units: the porch (0 to 900, walled off from the woods until dawn), the
// pines (900 to 3400), the lower pines (3400 to 4800), the yard fence (4800) and the yard (4800 to
// 6400). The floor is at y = 520. Everything is a grey box until the 3D scenery is built.
namespace Bodhi.Game
{
    public static partial class ChristmasStages
    {
        // Chapter Two: The Lamp in the Window.
        public static ChapterStage lamp_in_window()
        {
            var pine = StageSetting.of_text("color", "#1f2b2a");
            return new ChapterStage
            {
                id = "lamp_in_window",
                width = 6400,
                height = 600,
                mood = "tense",
                scenery = new StageSetting[]
                {
                    StageSetting.of_text("sky_top", "#1d2433"), StageSetting.of_text("sky_bottom", "#3a4558"),
                    StageSetting.of_text("far", "hills"), StageSetting.of_text("far_color", "#2c3646"),
                    StageSetting.of_text("mid", "trees"), StageSetting.of_text("mid_color", "#1f2b2a"),
                    StageSetting.of_text("near", "near_trees"), StageSetting.of_text("near_color", "#18211f"),
                    StageSetting.of_text("ground_top", "#d9e2ec"), StageSetting.of_text("ground_body", "#8e9fb3"),
                    StageSetting.of_text("wall", "#5a4a3c"), StageSetting.of_text("room_wall", "#6e5844"),
                    StageSetting.of_number("darkness", 0.75), StageSetting.of_text("darkness_color", "rgb(20,26,44)"),
                    StageSetting.of_text("darkness_floor", "#5d6b7c"), StageSetting.of_number("snow", 1),
                    StageSetting.of_number("wind", 1.0), StageSetting.of_text("mist", "rgba(220,230,245,0.18)"),
                },
                human_start_x = 400, human_start_feet_y = 520,
                dog_start_x = 1000, dog_start_feet_y = 520,
                guest = new StageGuest { x = 2650, feet_y = 520, size = 0.8, mode = "gone", pose = "sleep" },
                solids = new StageSolid[]
                {
                    new StageSolid { kind = "floor", x = 0, y = 520, width = 6400, height = 100 },
                    new StageSolid { id = "night_wall", kind = "invisible", x = 900, y = 0, width = 10, height = 520 },
                    new StageSolid { id = "yard_fence", kind = "gate", x = 4800, y = 430, width = 12, height = 90, blocks = SolidBlocks.human },
                    new StageSolid { kind = "invisible", x = 6390, y = 0, width = 10, height = 520 },
                },
                props = new StageProp[]
                {
                    // The cabin porch and its window lamp.
                    new StageProp { kind = "house", x = 0, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 320), StageSetting.of_text("color", "#4a3b2e") } },
                    new StageProp { kind = "window", x = 200, y = 300, settings = new StageSetting[] { StageSetting.of_number("w", 80), StageSetting.of_number("h", 80) } },
                    new StageProp { kind = "lamp", x = 240, y = 380, until = new string[] { "lamp_turned_up" }, settings = new StageSetting[] { StageSetting.of_number("h", 30), StageSetting.of_number("radius", 200), StageSetting.of_text("color", "rgba(255,206,150,0.16)") } },
                    new StageProp { kind = "lamp", x = 240, y = 380, after = "lamp_turned_up", settings = new StageSetting[] { StageSetting.of_number("h", 30), StageSetting.of_number("radius", 340), StageSetting.of_text("color", "rgba(255,206,150,0.24)") } },
                    // The pines and the fallen spruce over the hollow.
                    new StageProp { kind = "tree", x = 1100, y = 520, settings = new StageSetting[] { pine } },
                    new StageProp { kind = "snow_drift", x = 1300, y = 520 },
                    new StageProp { kind = "tree", x = 1650, y = 520, settings = new StageSetting[] { pine } },
                    new StageProp { kind = "tree", x = 2100, y = 520, settings = new StageSetting[] { pine } },
                    new StageProp { kind = "debris", x = 2540, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 240), StageSetting.of_number("h", 60), StageSetting.of_text("color", "#2a2f2a") } },
                    new StageProp { kind = "tree", x = 2950, y = 520, settings = new StageSetting[] { pine } },
                    new StageProp { kind = "tree", x = 3350, y = 520, settings = new StageSetting[] { pine } },
                    new StageProp { kind = "snow_drift", x = 3600, y = 520 },
                    new StageProp { kind = "tree", x = 3850, y = 520, settings = new StageSetting[] { pine } },
                    new StageProp { kind = "tree", x = 4400, y = 520, settings = new StageSetting[] { pine } },
                    // The timber yard.
                    new StageProp { kind = "sign", x = 4960, y = 300, settings = new StageSetting[] { StageSetting.of_text("text", "Hollin Timber") } },
                    new StageProp { kind = "lamp", x = 5100, y = 520, settings = new StageSetting[] { StageSetting.of_number("radius", 260), StageSetting.of_text("color", "rgba(255,160,80,0.18)") } },
                    new StageProp { kind = "crate", x = 5330, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 130), StageSetting.of_number("h", 55), StageSetting.of_text("color", "#6b5233") } },
                    new StageProp { kind = "lorry", x = 5500, y = 520, until = new string[] { "lorry_gone" }, settings = new StageSetting[] { StageSetting.of_number("w", 520), StageSetting.of_number("h", 150), StageSetting.of_text("color", "#2f5a3a") } },
                },
                things = new StageThingLook[]
                {
                    new StageThingLook { event_name = "scent_1", sound = "sniff", pose = "sniff" },
                    new StageThingLook { event_name = "scent_2", sound = "sniff", pose = "sniff" },
                    new StageThingLook { event_name = "scent_3", sound = "sniff", pose = "sniff" },
                    new StageThingLook { event_name = "biscuit_dug_out", sound = "sniff", pose = "sniff" },
                    new StageThingLook { event_name = "lay_close", pose = "sleep" },
                    new StageThingLook { event_name = "lamp_turned_up", sound = "pickup" },
                    new StageThingLook { event_name = "biscuit_nudged", pose = "lean" },
                    new StageThingLook { event_name = "climbed_on_lorry", sound = "thud" },
                    new StageThingLook { event_name = "keepsake_dougies_note", sprite = "keepsake", sound = "sniff" },
                    new StageThingLook { event_name = "sandwich_shared", sound = "pickup" },
                    new StageThingLook { event_name = "stayed", pose = "sleep" },
                    new StageThingLook { event_name = "keepsake_bird_feeder", sprite = "keepsake" },
                    new StageThingLook { event_name = "keepsake_yard_mitten", sprite = "keepsake" },
                    new StageThingLook { event_name = "collar_found", sound = "pickup", pose = "kneel" },
                },
                // One entry per story rule, in the same order; the comment names the rule.
                rules = new RuleStaging[]
                {
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.dog, x = 1000, feet_y = 520 } }, hide = new[] { new StageHiding { who = StageWho.human, hidden = true } }, camera = CameraChange.hold, camera_x = 1100, camera_y = 420, camera_zoom = 1.0, caption = "The same night" },  // 0 level_started
                    new RuleStaging { },  // 1 open_1
                    new RuleStaging { camera = CameraChange.release, caption = "" },  // 2 opened
                    new RuleStaging { scent = ScentChange.point, scent_x = 1900, scent_y = 500 },  // 3 scent_1
                    new RuleStaging { scent = ScentChange.point, scent_x = 2300, scent_y = 500 },  // 4 scent_2
                    new RuleStaging { scent = ScentChange.point, scent_x = 2650, scent_y = 500 },  // 5 scent_3
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.guest, x = 2650, feet_y = 520 } }, guest_mode = "still", scent = ScentChange.clear },  // 6 reached_hollow
                    new RuleStaging { },  // 7 biscuit_dug_out
                    new RuleStaging { pose = new[] { new StagePose { who = StageWho.dog, pose = "sleep", seconds = 14 } }, camera = CameraChange.hold, camera_x = 2650, camera_y = 450, camera_zoom = 1.2, mood = "tender" },  // 8 lay_close
                    new RuleStaging { sound = "heartbeat" },  // 9 warming_2
                    new RuleStaging { },  // 10 warming_3
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.human, x = 400, feet_y = 520 } }, hide = new[] { new StageHiding { who = StageWho.human, hidden = false } }, camera = CameraChange.release, darkness = 0.7, mood = "quiet", fade_in = true },  // 11 porch
                    new RuleStaging { sound = "chime" },  // 12 lamp_turned_up: the bell in her pocket
                    new RuleStaging { walk = new[] { new StageWalk { who = StageWho.human, x = 620 } }, camera = CameraChange.hold, camera_x = 450, camera_y = 420, camera_zoom = 1.1, sound = "whistle" },  // 13 the hours
                    new RuleStaging { darkness = 0.8 },  // 14 hours_2: the stove goes out
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.dog, x = 2720, feet_y = 520 } }, hide = new[] { new StageHiding { who = StageWho.human, hidden = true } }, guest_mode = "follow", camera = CameraChange.release, darkness = 0.65, wind = 0.15, mood = "quiet", fade_in = true },  // 15 wind_dropped
                    new RuleStaging { },  // 16 yard_light_seen
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.guest, x = 4150, feet_y = 520 } }, guest_mode = "still", pose = new[] { new StagePose { who = StageWho.guest, pose = "sit", seconds = 30 } } },  // 17 biscuit_sat
                    new RuleStaging { guest_mode = "follow" },  // 18 biscuit_nudged
                    new RuleStaging { },  // 19 under_the_fence
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.dog, x = 5700, feet_y = 520 }, new StagePlacement { who = StageWho.guest, x = 5640, feet_y = 520 } }, guest_mode = "still", pose = new[] { new StagePose { who = StageWho.guest, pose = "sleep", seconds = 9999 } }, darkness = 0.6 },  // 20 climbed_on_lorry
                    new RuleStaging { },  // 21 sandwich_shared
                    new RuleStaging { sound = "eat" },  // 22 sandwich_halved
                    new RuleStaging { face = new[] { new StageFacing { who = StageWho.dog, direction = -1 } }, camera = CameraChange.hold, camera_x = 5400, camera_y = 400, camera_zoom = 0.85 },  // 23 her_light
                    new RuleStaging { camera = CameraChange.release },  // 24 her_light_seen
                    new RuleStaging { },  // 25 tried_to_leave
                    new RuleStaging { pose = new[] { new StagePose { who = StageWho.dog, pose = "sleep", seconds = 9999 } }, camera = CameraChange.hold, camera_x = 5700, camera_y = 430, camera_zoom = 1.1, mood = "tender" },  // 26 stayed
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.human, x = 400, feet_y = 520 } }, hide = new[] { new StageHiding { who = StageWho.human, hidden = false }, new StageHiding { who = StageWho.dog, hidden = true }, new StageHiding { who = StageWho.guest, hidden = true } }, camera = CameraChange.release, darkness = 0.25, wind = 0, mood = "quiet", fade_in = true, caption = "First light" },  // 27 dawn
                    new RuleStaging { camera = CameraChange.hold, camera_x = 5000, camera_y = 420, camera_zoom = 0.85, caption = "" },  // 28 collar_found
                    new RuleStaging { },  // 29 the_promise
                    new RuleStaging { shake_seconds = 0.4 },  // 30 engine_started
                    new RuleStaging { darkness = 0.2 },  // 31 lorry_gone
                    new RuleStaging { },  // 32 chapter_done
                },
                aftermath = new StageAftermath { scene = "road_dawn", mood = "quiet", rain = 0 },
            };
        }
    }
}
