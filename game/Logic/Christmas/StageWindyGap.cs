// Purpose: how Chapter Ten of the Christmas story, "Windy Gap", looks: the climb with its cairn,
// memorial bench, rime-iced marker posts and frozen waterfall, the ledge, the gully (held shut to
// Bodhi until Em says go, and to Em until she has fixed her rope), the sheepfold at its foot, and
// the top of the pass; and what each story rule does on screen. The story is
// rules/Runtime/Christmas/Christmas10WindyGap.cs.
//
// Left to right, in layout units: the climb (0 to 1700), the ledge (1700 to 1950), the gully and
// sheepfold (1950 to 2800) and the haul to the top (2800 to 3800). The floor is at y = 520.
// Everything is a grey box until the 3D scenery is built.
namespace Bodhi.Game
{
    public static partial class ChristmasStages
    {
        // Chapter Ten: Windy Gap.
        public static ChapterStage windy_gap()
        {
            var rime = StageSetting.of_text("color", "#dfe8f0");
            var stone = StageSetting.of_text("color", "#8a8a84");
            return new ChapterStage
            {
                id = "windy_gap",
                width = 3800,
                height = 600,
                mood = "tense",
                scenery = new StageSetting[]
                {
                    StageSetting.of_text("sky_top", "#b88aa0"), StageSetting.of_text("sky_bottom", "#f4c890"),
                    StageSetting.of_text("far", "hills"), StageSetting.of_text("far_color", "#d8b8b8"),
                    StageSetting.of_text("ground_top", "#f4eef0"), StageSetting.of_text("ground_body", "#b8b0c8"),
                    StageSetting.of_text("wall", "#7a7470"), StageSetting.of_text("room_wall", "#7a6a5a"),
                    StageSetting.of_number("darkness", 0.1), StageSetting.of_text("darkness_color", "rgb(40,40,72)"),
                    StageSetting.of_text("darkness_floor", "#8a8aa0"), StageSetting.of_number("snow", 0.2),
                    StageSetting.of_number("wind", 0.7), StageSetting.of_text("mist", "rgba(255,230,220,0.08)"),
                },
                human_start_x = 300, human_start_feet_y = 520,
                dog_start_x = 360, dog_start_feet_y = 520,
                guest = new StageGuest { x = 240, feet_y = 520, size = 0.8, mode = "follow", pose = "" },
                solids = new StageSolid[]
                {
                    new StageSolid { kind = "floor", x = 0, y = 520, width = 3800, height = 100 },
                    new StageSolid { kind = "invisible", x = 0, y = 0, width = 10, height = 520 },
                    new StageSolid { id = "gully_edge", kind = "invisible", x = 1950, y = 0, width = 10, height = 520, blocks = SolidBlocks.dog },
                    new StageSolid { id = "waterfall_route", kind = "invisible", x = 1960, y = 0, width = 10, height = 520, blocks = SolidBlocks.human },
                    new StageSolid { kind = "invisible", x = 3790, y = 0, width = 10, height = 520 },
                },
                props = new StageProp[]
                {
                    // The climb.
                    new StageProp { kind = "pillar", x = 700, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 12), StageSetting.of_number("h", 120), rime } },
                    new StageProp { kind = "cairn", x = 1180, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 60), StageSetting.of_number("h", 80), stone } },
                    new StageProp { kind = "bench", x = 1260, y = 520 },
                    new StageProp { kind = "glass", x = 1440, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 140), StageSetting.of_number("h", 320), StageSetting.of_text("color", "rgba(200,232,244,0.6)") } },
                    new StageProp { kind = "pillar", x = 1640, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 12), StageSetting.of_number("h", 120), rime } },
                    new StageProp { kind = "ribbon", x = 1650, y = 390, settings = new StageSetting[] { StageSetting.of_text("color", "#e8e4dc") } },
                    // The ledge and the cornice over the gully.
                    new StageProp { kind = "snow_drift", x = 1780, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 200), StageSetting.of_number("h", 30) } },
                    // The gully: the sled, the shelf and the sheepfold.
                    new StageProp { kind = "sled", x = 2560, y = 520, after = "gust", until = new string[] { "reunion_complete" }, settings = new StageSetting[] { StageSetting.of_number("w", 130), StageSetting.of_number("h", 28), StageSetting.of_text("color", "#7a4a2a") } },
                    new StageProp { kind = "crate", x = 2620, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 90), StageSetting.of_number("h", 30), stone } },
                    new StageProp { kind = "garden_fence", x = 2700, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 140), stone } },
                    // The top of the pass.
                    new StageProp { kind = "pillar", x = 3300, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 12), StageSetting.of_number("h", 120), rime } },
                    new StageProp { kind = "sign", x = 3500, y = 320, settings = new StageSetting[] { StageSetting.of_text("text", "Windy Gap") } },
                },
                things = new StageThingLook[]
                {
                    new StageThingLook { event_name = "keepsake_cairn_bowl", sprite = "keepsake", sound = "sniff" },
                    new StageThingLook { event_name = "keepsake_memorial_bench", sprite = "keepsake" },
                    new StageThingLook { event_name = "keepsake_prayer_flags", sprite = "keepsake" },
                    new StageThingLook { event_name = "looked_at_her", pose = "turn" },
                    new StageThingLook { event_name = "biscuit_on_shelf", pose = "lean" },
                    new StageThingLook { event_name = "steps_cut", sound = "thud", pose = "kneel" },
                    new StageThingLook { event_name = "rope_fixed", sound = "pickup", pose = "kneel" },
                },
                // One entry per story rule, in the same order; the comment names the rule.
                rules = new RuleStaging[]
                {
                    new RuleStaging { camera = CameraChange.hold, camera_x = 500, camera_y = 420, camera_zoom = 0.9, caption = "The twenty-third of December" },  // 0 level_started
                    new RuleStaging { },  // 1 open_1
                    new RuleStaging { },  // 2 open_2
                    new RuleStaging { camera = CameraChange.release, caption = "" },  // 3 climbing
                    new RuleStaging { },  // 4 first_pitch
                    new RuleStaging { wind = 0.9 },  // 5 ledge
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.guest, x = 2600, feet_y = 520 } }, guest_mode = "still", camera = CameraChange.hold, camera_x = 2200, camera_y = 420, camera_zoom = 0.8, shake_seconds = 0.8, sound = "collapse" },  // 6 gust
                    new RuleStaging { face = new[] { new StageFacing { who = StageWho.dog, direction = -1 } }, camera = CameraChange.release, wind = 0.7 },  // 7 the_look
                    new RuleStaging { },  // 8 looked_at_her
                    new RuleStaging { mood = "hopeful" },  // 9 she_said_go
                    new RuleStaging { },  // 10 reached_sled
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.guest, x = 2650, feet_y = 490 } }, pose = new[] { new StagePose { who = StageWho.guest, pose = "sit", seconds = 30 } } },  // 11 biscuit_on_shelf
                    new RuleStaging { mood = "tense" },  // 12 em_climbs
                    new RuleStaging { },  // 13 steps_cut
                    new RuleStaging { },  // 14 rope_fixed
                    new RuleStaging { darkness = 0.25 },  // 15 across_the_gully: the snow goes blue
                    new RuleStaging { guest_mode = "follow", mood = "tender" },  // 16 the sheepfold
                    new RuleStaging { },  // 17 reunion_embrace
                    new RuleStaging { darkness = 0.35 },  // 18 reunion_complete
                    new RuleStaging { camera = CameraChange.hold, camera_x = 3500, camera_y = 380, camera_zoom = 0.7, darkness = 0.45, mood = "quiet" },  // 19 the_top
                    new RuleStaging { darkness = 0.55 },  // 20 dark_windows
                    new RuleStaging { },  // 21 chapter_done
                },
                aftermath = new StageAftermath { scene = "snow_hut", mood = "tense", rain = 0 },
            };
        }
    }
}
