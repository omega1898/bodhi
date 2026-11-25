// Purpose: how Chapter Eight of the Christmas story, "Blackwater", looks: the shore buried by a
// fallen cornice, the white ice close in, the fishing hut on stilts with its pulley, and the black
// ice too thin for Em, where the sled skids; and what each story rule does on screen. The story is
// rules/Runtime/Christmas/Christmas08Blackwater.cs.
//
// Left to right, in layout units: the shore (0 to 600), the white ice and the hut (600 to 2350)
// and the black ice (2350 to 3200). The floor is at y = 520. Everything is a grey box until the
// 3D scenery is built.
namespace Bodhi.Game
{
    public static partial class ChristmasStages
    {
        // Chapter Eight: Blackwater.
        public static ChapterStage blackwater()
        {
            return new ChapterStage
            {
                id = "blackwater",
                width = 3200,
                height = 600,
                mood = "quiet",
                scenery = new StageSetting[]
                {
                    StageSetting.of_text("sky_top", "#a8b0b8"), StageSetting.of_text("sky_bottom", "#dde2e6"),
                    StageSetting.of_text("far", "hills"), StageSetting.of_text("far_color", "#c4ccd2"),
                    StageSetting.of_text("ground_top", "#e8eef2"), StageSetting.of_text("ground_body", "#a6b4c0"),
                    StageSetting.of_text("wall", "#5a4a3a"), StageSetting.of_text("room_wall", "#6e5a46"),
                    StageSetting.of_number("darkness", 0.15), StageSetting.of_text("darkness_color", "rgb(40,48,62)"),
                    StageSetting.of_text("darkness_floor", "#7d8790"), StageSetting.of_number("snow", 0.2),
                    StageSetting.of_number("wind", 0.3), StageSetting.of_text("mist", "rgba(230,236,240,0.16)"),
                },
                human_start_x = 300, human_start_feet_y = 520,
                dog_start_x = 360, dog_start_feet_y = 520,
                guest = new StageGuest { x = 240, feet_y = 520, size = 0.8, mode = "follow", pose = "" },
                solids = new StageSolid[]
                {
                    new StageSolid { kind = "floor", x = 0, y = 520, width = 3200, height = 100 },
                    new StageSolid { kind = "invisible", x = 0, y = 0, width = 10, height = 520 },
                    new StageSolid { id = "black_ice_edge", kind = "invisible", x = 2350, y = 0, width = 10, height = 520, blocks = SolidBlocks.human },
                    new StageSolid { kind = "invisible", x = 3190, y = 0, width = 10, height = 520 },
                },
                props = new StageProp[]
                {
                    // The fallen cornice over the shore path.
                    new StageProp { kind = "snow_drift", x = 320, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 280), StageSetting.of_number("h", 90) } },
                    // The white ice close in, and the black ice beyond.
                    new StageProp { kind = "ice", x = 600, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 1750), StageSetting.of_number("h", 10), StageSetting.of_text("color", "#e4ecf0") } },
                    new StageProp { kind = "ice", x = 2350, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 850), StageSetting.of_number("h", 10), StageSetting.of_text("color", "#1c2830") } },
                    new StageProp { kind = "hay", x = 1200, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 60), StageSetting.of_number("h", 40), StageSetting.of_text("color", "#8a8460") } },
                    // The fishing hut on stilts, its pulley, and the candle in its window.
                    new StageProp { kind = "house", x = 1680, y = 500, settings = new StageSetting[] { StageSetting.of_number("w", 220), StageSetting.of_number("h", 150), StageSetting.of_text("color", "#5a4a3a") } },
                    new StageProp { kind = "sign", x = 1860, y = 320, settings = new StageSetting[] { StageSetting.of_text("text", "Pulley") } },
                    new StageProp { kind = "lamp", x = 1760, y = 420, after = "candle", settings = new StageSetting[] { StageSetting.of_number("h", 20), StageSetting.of_number("radius", 180), StageSetting.of_text("color", "rgba(255,200,130,0.2)") } },
                    // The sled, out on the black ice until they are hauled in.
                    new StageProp { kind = "sled", x = 2640, y = 520, after = "halfway", until = new string[] { "reunion_complete" }, settings = new StageSetting[] { StageSetting.of_number("w", 130), StageSetting.of_number("h", 28), StageSetting.of_text("color", "#7a4a2a") } },
                },
                things = new StageThingLook[]
                {
                    new StageThingLook { event_name = "keepsake_fishing_log", sprite = "keepsake" },
                    new StageThingLook { event_name = "keepsake_biscuit_tin", sprite = "keepsake", pose = "kneel" },
                    new StageThingLook { event_name = "keepsake_child_skate", sprite = "keepsake", sound = "sniff" },
                    new StageThingLook { event_name = "rope_held", pose = "stop" },
                    new StageThingLook { event_name = "pulley_freed", sound = "lever" },
                    new StageThingLook { event_name = "crank_turned", sound = "lever" },
                },
                // One entry per story rule, in the same order; the comment names the rule.
                rules = new RuleStaging[]
                {
                    new RuleStaging { camera = CameraChange.hold, camera_x = 600, camera_y = 420, camera_zoom = 0.9, caption = "The twentieth of December" },  // 0 level_started
                    new RuleStaging { },  // 1 tarn_1
                    new RuleStaging { },  // 2 tarn_2
                    new RuleStaging { camera = CameraChange.release, caption = "" },  // 3 at_the_tarn
                    new RuleStaging { pose = new[] { new StagePose { who = StageWho.dog, pose = "land", seconds = 1 } }, mood = "hopeful" },  // 4 on_the_ice
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.guest, x = 2700, feet_y = 520 } }, guest_mode = "still", pose = new[] { new StagePose { who = StageWho.guest, pose = "sleep", seconds = 9999 } }, camera = CameraChange.hold, camera_x = 2400, camera_y = 420, camera_zoom = 0.85, wind = 0.95, shake_seconds = 0.8, mood = "tense" },  // 5 halfway: the gust
                    new RuleStaging { camera = CameraChange.release, wind = 0.35 },  // 6 too_heavy
                    new RuleStaging { pose = new[] { new StagePose { who = StageWho.dog, pose = "sleep", seconds = 2 } } },  // 7 crawled_out: belly low
                    new RuleStaging { },  // 8 rope_held
                    new RuleStaging { },  // 9 pulley_freed
                    new RuleStaging { },  // 10 crank_turned
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.guest, x = 2120, feet_y = 520 } }, guest_mode = "follow", sound = "collapse", shake_seconds = 1.0, mood = "tender" },  // 11 hauled in: the crack holds
                    new RuleStaging { },  // 12 reunion_embrace
                    new RuleStaging { darkness = 0.45, mood = "quiet" },  // 13 reunion_complete
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.human, x = 1780, feet_y = 500 }, new StagePlacement { who = StageWho.dog, x = 1830, feet_y = 500 }, new StagePlacement { who = StageWho.guest, x = 1730, feet_y = 500 } }, guest_mode = "still", camera = CameraChange.hold, camera_x = 1790, camera_y = 420, camera_zoom = 1.15, darkness = 0.6, wind = 0.15 },  // 14 into_the_hut
                    new RuleStaging { mood = "tender" },  // 15 candle
                    new RuleStaging { shake_seconds = 0.4, darkness = 0.75 },  // 16 booming
                    new RuleStaging { },  // 17 chapter_done
                },
                aftermath = new StageAftermath { scene = "snow_hut", mood = "quiet", rain = 0 },
            };
        }
    }
}
