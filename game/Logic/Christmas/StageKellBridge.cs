// Purpose: how Chapter Three of the Christmas story, "Kell Bridge", looks: the road down to
// Strand, Nell's cafe and woodshed, the snowplough depot, the bridge where the lorry stopped and
// the frozen lake, and what each story rule does on screen. The story is
// rules/Runtime/Christmas/Christmas03KellBridge.cs.
//
// Left to right, in layout units: the road to Strand (0 to 800), the cafe (800 to 2000), the depot
// (2000 to 3200), the bridge (3200 to 4200) and the lake shore (4200 to 5000). The floor is at
// y = 520. Everything is a grey box until the 3D scenery is built.
namespace Bodhi.Game
{
    public static partial class ChristmasStages
    {
        // Chapter Three: Kell Bridge.
        public static ChapterStage kell_bridge()
        {
            var plough_yellow = StageSetting.of_text("color", "#d8b030");
            return new ChapterStage
            {
                id = "kell_bridge",
                width = 5000,
                height = 600,
                mood = "calm",
                scenery = new StageSetting[]
                {
                    StageSetting.of_text("sky_top", "#a9c4de"), StageSetting.of_text("sky_bottom", "#f3ead2"),
                    StageSetting.of_text("far", "hills"), StageSetting.of_text("far_color", "#c9d4df"),
                    StageSetting.of_text("mid", "trees"), StageSetting.of_text("mid_color", "#4a5a52"),
                    StageSetting.of_text("ground_top", "#f4f6f9"), StageSetting.of_text("ground_body", "#b5c4d6"),
                    StageSetting.of_text("wall", "#6d5a4a"), StageSetting.of_text("room_wall", "#8a7058"),
                    StageSetting.of_number("darkness", 0.08), StageSetting.of_text("darkness_color", "rgb(40,48,70)"),
                    StageSetting.of_text("darkness_floor", "#8a97a6"), StageSetting.of_number("snow", 0),
                    StageSetting.of_number("wind", 0.1), StageSetting.of_text("mist", "rgba(250,245,230,0.08)"),
                },
                human_start_x = 3650, human_start_feet_y = 520,
                dog_start_x = 3500, dog_start_feet_y = 520,
                guest = new StageGuest { x = 3440, feet_y = 520, size = 0.8, mode = "still", pose = "sleep" },
                solids = new StageSolid[]
                {
                    new StageSolid { kind = "floor", x = 0, y = 520, width = 5000, height = 100 },
                    new StageSolid { kind = "invisible", x = 0, y = 0, width = 10, height = 520 },
                    new StageSolid { kind = "invisible", x = 4990, y = 0, width = 10, height = 520 },
                },
                props = new StageProp[]
                {
                    // The road down to Strand.
                    new StageProp { kind = "road_sign", x = 450, y = 520 },
                    new StageProp { kind = "sign", x = 400, y = 330, settings = new StageSetting[] { StageSetting.of_text("text", "Strand 18") } },
                    // Nell's cafe, its bins and its woodshed.
                    new StageProp { kind = "house", x = 850, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 560), StageSetting.of_text("color", "#6d5a4a") } },
                    new StageProp { kind = "window", x = 1040, y = 330, settings = new StageSetting[] { StageSetting.of_number("w", 140), StageSetting.of_number("h", 80), StageSetting.of_flag("glow", true), StageSetting.of_number("radius", 240), StageSetting.of_text("color", "rgba(255,214,160,0.18)") } },
                    new StageProp { kind = "sign", x = 1230, y = 300, settings = new StageSetting[] { StageSetting.of_text("text", "OPE") } },
                    new StageProp { kind = "crate", x = 1480, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 70), StageSetting.of_number("h", 60), StageSetting.of_text("color", "#3e4a40") } },
                    new StageProp { kind = "house", x = 1700, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 220), StageSetting.of_number("h", 140), StageSetting.of_text("color", "#5a4632") } },
                    // The snowplough depot.
                    new StageProp { kind = "sign", x = 2080, y = 300, settings = new StageSetting[] { StageSetting.of_text("text", "Kell Bridge Depot") } },
                    new StageProp { kind = "lorry", x = 2200, y = 520, until = new string[] { "crew_gone" }, settings = new StageSetting[] { StageSetting.of_number("w", 280), StageSetting.of_number("h", 150), plough_yellow } },
                    new StageProp { kind = "bench", x = 2440, y = 520 },
                    new StageProp { kind = "lorry", x = 2700, y = 520, until = new string[] { "crew_gone" }, settings = new StageSetting[] { StageSetting.of_number("w", 280), StageSetting.of_number("h", 150), plough_yellow } },
                    // The bridge, Dougie's lorry and Ama's car.
                    new StageProp { kind = "rail", x = 3200, y = 500, settings = new StageSetting[] { StageSetting.of_number("w", 900) } },
                    new StageProp { kind = "lorry", x = 3300, y = 520, until = new string[] { "em_arrived" }, settings = new StageSetting[] { StageSetting.of_number("w", 420), StageSetting.of_number("h", 150), StageSetting.of_text("color", "#2f5a3a") } },
                    new StageProp { kind = "car", x = 3700, y = 520, after = "em_arrived", settings = new StageSetting[] { StageSetting.of_text("color", "#7a3a30") } },
                    // The frozen lake, stretching away up the valley.
                    new StageProp { kind = "ice", x = 4200, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 800), StageSetting.of_number("h", 12), StageSetting.of_text("color", "#cfe3f0") } },
                    new StageProp { kind = "snow_drift", x = 4500, y = 520 },
                },
                things = new StageThingLook[]
                {
                    new StageThingLook { event_name = "off_the_lorry", sound = "thud" },
                    new StageThingLook { event_name = "sausage_found", sound = "eat", pose = "sniff" },
                    new StageThingLook { event_name = "keepsake_dog_bed", sprite = "keepsake", sound = "sniff" },
                    new StageThingLook { event_name = "prints_read", pose = "kneel" },
                    new StageThingLook { event_name = "bowl_read", pose = "kneel" },
                    new StageThingLook { event_name = "curl_found", sound = "pickup" },
                    new StageThingLook { event_name = "keepsake_noticeboard", sprite = "keepsake" },
                    new StageThingLook { event_name = "keepsake_class_card", sprite = "keepsake" },
                    new StageThingLook { event_name = "talked_to_nell" },
                    new StageThingLook { event_name = "poster_put_up", sound = "pickup" },
                },
                // One entry per story rule, in the same order; the comment names the rule.
                rules = new RuleStaging[]
                {
                    new RuleStaging { hide = new[] { new StageHiding { who = StageWho.human, hidden = true } }, camera = CameraChange.hold, camera_x = 3450, camera_y = 420, camera_zoom = 1.0, caption = "The fifteenth of December" },  // 0 level_started
                    new RuleStaging { pose = new[] { new StagePose { who = StageWho.dog, pose = "sniff", seconds = 2 } } },  // 1 wake_1
                    new RuleStaging { camera = CameraChange.release, caption = "" },  // 2 woke
                    new RuleStaging { guest_mode = "follow" },  // 3 off_the_lorry
                    new RuleStaging { sound = "gate" },  // 4 nell_bowl: the cafe door
                    new RuleStaging { },  // 5 sausage_found
                    new RuleStaging { sound = "thud", mood = "tense" },  // 6 crew_out
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.guest, x = 1840, feet_y = 520 } }, guest_mode = "still", pose = new[] { new StagePose { who = StageWho.dog, pose = "sit", seconds = 6 } }, camera = CameraChange.hold, camera_x = 1850, camera_y = 430, camera_zoom = 1.1 },  // 7 hid_in_woodshed
                    new RuleStaging { guest_mode = "follow", camera = CameraChange.release, mood = "calm" },  // 8 crew_gone
                    new RuleStaging { face = new[] { new StageFacing { who = StageWho.dog, direction = 1 } }, pose = new[] { new StagePose { who = StageWho.dog, pose = "sniff", seconds = 3 } } },  // 9 home_smelled
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.guest, x = 700, feet_y = 520 } }, guest_mode = "still", face = new[] { new StageFacing { who = StageWho.guest, direction = -1 } } },  // 10 biscuit_went_down
                    new RuleStaging { camera = CameraChange.hold, camera_x = 400, camera_y = 420, camera_zoom = 0.95, mood = "tender" },  // 11 followed_biscuit
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.human, x = 3650, feet_y = 520 } }, hide = new[] { new StageHiding { who = StageWho.human, hidden = false }, new StageHiding { who = StageWho.dog, hidden = true }, new StageHiding { who = StageWho.guest, hidden = true } }, camera = CameraChange.release, mood = "quiet", fade_in = true, caption = "An hour later" },  // 12 em_arrived
                    new RuleStaging { caption = "" },  // 13 the_drive
                    new RuleStaging { },  // 14 prints_read
                    new RuleStaging { },  // 15 bowl_read
                    new RuleStaging { },  // 16 curl_found
                    new RuleStaging { },  // 17 all three traces read
                    new RuleStaging { camera = CameraChange.hold, camera_x = 1200, camera_y = 420, camera_zoom = 1.1 },  // 18 talked_to_nell
                    new RuleStaging { },  // 19 nell_2
                    new RuleStaging { camera = CameraChange.release },  // 20 nell_done
                    new RuleStaging { },  // 21 poster_put_up
                    new RuleStaging { },  // 22 poster_up
                    new RuleStaging { camera = CameraChange.hold, camera_x = 350, camera_y = 420, camera_zoom = 0.95, mood = "tender" },  // 23 end_of_prints
                    new RuleStaging { darkness = 0.2 },  // 24 closing
                    new RuleStaging { },  // 25 chapter_done
                },
                aftermath = new StageAftermath { scene = "road_dawn", mood = "quiet", rain = 0 },
            };
        }
    }
}
