// Purpose: how Chapter Seven of the Christmas story, "Tam's Sled", looks: Tam's kitchen with its
// stove and wall phone, the workshop with his father's sled on the wall, the barn, the practice
// field and the herd, the farm gate, and the drovers' path climbing to the first cairn under a
// lid of cloud; and what each story rule does on screen. The story is
// rules/Runtime/Christmas/Christmas07TamsSled.cs.
//
// Left to right, in layout units: the kitchen (0 to 900), the yard, workshop and barn (900 to
// 1800), the field (1800 to 2600), the gate (2600 to 2800) and the path (2800 to 4000). The floor
// is at y = 520. Everything is a grey box until the 3D scenery is built.
namespace Bodhi.Game
{
    public static partial class ChristmasStages
    {
        // Chapter Seven: Tam's Sled.
        public static ChapterStage tams_sled()
        {
            var sled_wood = StageSetting.of_text("color", "#7a4a2a");
            var cairn = StageSetting.of_text("color", "#8a8a84");
            var reindeer = StageSetting.of_text("color", "#4a3a2e");
            return new ChapterStage
            {
                id = "tams_sled",
                width = 4000,
                height = 600,
                mood = "tender",
                scenery = new StageSetting[]
                {
                    StageSetting.of_text("sky_top", "#6fa0d8"), StageSetting.of_text("sky_bottom", "#e8f0f8"),
                    StageSetting.of_text("far", "hills"), StageSetting.of_text("far_color", "#c8d6e6"),
                    StageSetting.of_text("ground_top", "#f6f9fc"), StageSetting.of_text("ground_body", "#b8c8da"),
                    StageSetting.of_text("wall", "#6a6460"), StageSetting.of_text("room_wall", "#8a7058"),
                    StageSetting.of_number("darkness", 0.05), StageSetting.of_text("darkness_color", "rgb(40,48,70)"),
                    StageSetting.of_text("darkness_floor", "#8a97a6"), StageSetting.of_number("snow", 0),
                    StageSetting.of_number("wind", 0.1), StageSetting.of_text("mist", "rgba(255,255,255,0.06)"),
                },
                human_start_x = 380, human_start_feet_y = 520,
                dog_start_x = 420, dog_start_feet_y = 520,
                guest = new StageGuest { x = 220, feet_y = 520, size = 0.8, mode = "still", pose = "sleep" },
                solids = new StageSolid[]
                {
                    new StageSolid { kind = "floor", x = 0, y = 520, width = 4000, height = 100 },
                    new StageSolid { kind = "invisible", x = 0, y = 0, width = 10, height = 520 },
                    new StageSolid { kind = "invisible", x = 3990, y = 0, width = 10, height = 520 },
                },
                props = new StageProp[]
                {
                    // Tam's kitchen.
                    new StageProp { kind = "room_wall", x = 0, y = 200, settings = new StageSetting[] { StageSetting.of_number("w", 900), StageSetting.of_number("h", 320) } },
                    new StageProp { kind = "stove", x = 220, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#2b2b2e") } },
                    new StageProp { kind = "lamp", x = 260, y = 520, settings = new StageSetting[] { StageSetting.of_number("h", 60), StageSetting.of_number("radius", 260), StageSetting.of_text("color", "rgba(255,190,120,0.2)") } },
                    new StageProp { kind = "sign", x = 490, y = 330, settings = new StageSetting[] { StageSetting.of_text("text", "Phone") } },
                    new StageProp { kind = "sign", x = 620, y = 300, settings = new StageSetting[] { StageSetting.of_text("text", "Hay. Lichen. Ring Jean") } },
                    new StageProp { kind = "window", x = 760, y = 300, settings = new StageSetting[] { StageSetting.of_number("w", 80), StageSetting.of_number("h", 80) } },
                    // The workshop, the sled and the barn.
                    new StageProp { kind = "house", x = 950, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 420), StageSetting.of_number("h", 220), StageSetting.of_text("color", "#5a4632") } },
                    new StageProp { kind = "sled", x = 1060, y = 400, until = new string[] { "sled_down" }, settings = new StageSetting[] { StageSetting.of_number("w", 130), StageSetting.of_number("h", 28), sled_wood } },
                    new StageProp { kind = "sled", x = 1080, y = 520, after = "sled_down", settings = new StageSetting[] { StageSetting.of_number("w", 130), StageSetting.of_number("h", 28), sled_wood } },
                    new StageProp { kind = "bale", x = 1360, y = 520 },
                    new StageProp { kind = "house", x = 1500, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 280), StageSetting.of_number("h", 200), StageSetting.of_text("color", "#5a3a2a") } },
                    // The field and the herd grazing lichen.
                    new StageProp { kind = "deer", x = 1950, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 80), StageSetting.of_number("h", 75), reindeer } },
                    new StageProp { kind = "calf", x = 2120, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 45), StageSetting.of_number("h", 50), StageSetting.of_text("color", "#e8e0d0") } },
                    new StageProp { kind = "deer", x = 2450, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 80), StageSetting.of_number("h", 75), reindeer } },
                    new StageProp { kind = "garden_fence", x = 2600, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 60), StageSetting.of_text("color", "#5a4630") } },
                    // The drovers' path and its cairns.
                    new StageProp { kind = "cairn", x = 3000, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 50), StageSetting.of_number("h", 70), cairn } },
                    new StageProp { kind = "snow_drift", x = 3300, y = 520 },
                    new StageProp { kind = "cairn", x = 3800, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 60), StageSetting.of_number("h", 90), cairn } },
                },
                things = new StageThingLook[]
                {
                    new StageThingLook { event_name = "towelled", sound = "towel", pose = "kneel" },
                    new StageThingLook { event_name = "paws_brushed", pose = "kneel" },
                    new StageThingLook { event_name = "breakfast", sprite = "bowl", sound = "eat" },
                    new StageThingLook { event_name = "keepsake_kitchen_list", sprite = "keepsake" },
                    new StageThingLook { event_name = "keepsake_calf_bell", sprite = "keepsake", sound = "chime" },
                    new StageThingLook { event_name = "phone_answered", sound = "pickup" },
                    new StageThingLook { event_name = "asked_tam" },
                    new StageThingLook { event_name = "pin_found", sound = "sniff", pose = "sniff" },
                    new StageThingLook { event_name = "keepsake_sled_initials", sprite = "keepsake", pose = "kneel" },
                    new StageThingLook { event_name = "pin_fitted", sound = "thud", pose = "kneel" },
                    new StageThingLook { event_name = "harness_on", sound = "pickup", pose = "kneel" },
                },
                // One entry per story rule, in the same order; the comment names the rule.
                rules = new RuleStaging[]
                {
                    new RuleStaging { pose = new[] { new StagePose { who = StageWho.dog, pose = "sleep", seconds = 8 } }, camera = CameraChange.hold, camera_x = 350, camera_y = 420, camera_zoom = 1.1, darkness = 0.2, caption = "The nineteenth of December" },  // 0 level_started
                    new RuleStaging { },  // 1 sleep_1
                    new RuleStaging { },  // 2 sleep_2
                    new RuleStaging { camera = CameraChange.release, darkness = 0.05, caption = "" },  // 3 morning
                    new RuleStaging { },  // 4 towelled
                    new RuleStaging { },  // 5 paws_brushed
                    new RuleStaging { },  // 6 breakfast
                    new RuleStaging { },  // 7 dog_petted
                    new RuleStaging { },  // 8 all four done
                    new RuleStaging { camera = CameraChange.hold, camera_x = 500, camera_y = 420, camera_zoom = 1.15, mood = "quiet" },  // 9 phone_answered
                    new RuleStaging { },  // 10 mina_on_phone
                    new RuleStaging { },  // 11 mina_lamp
                    new RuleStaging { camera = CameraChange.release },  // 12 phone_done
                    new RuleStaging { camera = CameraChange.hold, camera_x = 280, camera_y = 420, camera_zoom = 1.1 },  // 13 asked_tam
                    new RuleStaging { },  // 14 moss_1
                    new RuleStaging { },  // 15 moss_2
                    new RuleStaging { mood = "tender" },  // 16 moss_3
                    new RuleStaging { },  // 17 moss_4
                    new RuleStaging { camera = CameraChange.release, mood = "hopeful" },  // 18 sled_down
                    new RuleStaging { },  // 19 pin_found
                    new RuleStaging { },  // 20 pin_fitted
                    new RuleStaging { },  // 21 harness_on
                    new RuleStaging { },  // 22 practice_pull
                    new RuleStaging { },  // 23 tam_pretended
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.guest, x = 2600, feet_y = 520 } }, guest_mode = "follow", darkness = 0.12, wind = 0.25 },  // 24 farm_gate: a grey lid on the mountains
                    new RuleStaging { },  // 25 first_slope
                    new RuleStaging { face = new[] { new StageFacing { who = StageWho.human, direction = -1 } }, camera = CameraChange.hold, camera_x = 3500, camera_y = 420, camera_zoom = 0.8, mood = "quiet" },  // 26 first_cairn: she looks back
                    new RuleStaging { darkness = 0.3, wind = 0.6 },  // 27 snow_came
                    new RuleStaging { },  // 28 chapter_done
                },
                aftermath = new StageAftermath { scene = "snow_hut", mood = "quiet", rain = 0 },
            };
        }
    }
}
