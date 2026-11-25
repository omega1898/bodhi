// Purpose: how Chapter Four of the Christmas story, "Strand", looks: Kirk Wynd and its red door,
// the road and the bus, the market square at dusk with its huts, chestnut brazier, carousel,
// nativity stable and town tree, the town hall, and Santa's grotto with the bus shelter facing
// it; and what each story rule does on screen. The story is
// rules/Runtime/Christmas/Christmas04Strand.cs.
//
// Left to right, in layout units: Kirk Wynd (0 to 1200), the road (1200 to 1600), the market
// (1600 to 3900), the town hall (3900 to 4350) and the grotto (4350 to 5400). The floor is at
// y = 520. Everything is a grey box until the 3D scenery is built.
namespace Bodhi.Game
{
    public static partial class ChristmasStages
    {
        // Chapter Four: Strand.
        public static ChapterStage strand()
        {
            var hut = StageSetting.of_text("color", "#6b4a32");
            var bulbs = StageSetting.of_text("color", "rgba(255,236,200,0.14)");
            return new ChapterStage
            {
                id = "strand",
                width = 5400,
                height = 600,
                mood = "calm",
                scenery = new StageSetting[]
                {
                    StageSetting.of_text("sky_top", "#1e2a4a"), StageSetting.of_text("sky_bottom", "#5a5f8a"),
                    StageSetting.of_text("far", "hills"), StageSetting.of_text("far_color", "#2e3858"),
                    StageSetting.of_text("ground_top", "#e6ebf2"), StageSetting.of_text("ground_body", "#9aa8bc"),
                    StageSetting.of_text("wall", "#6a6258"), StageSetting.of_text("room_wall", "#7a6a5a"),
                    StageSetting.of_number("darkness", 0.45), StageSetting.of_text("darkness_color", "rgb(24,30,56)"),
                    StageSetting.of_text("darkness_floor", "#7d8790"), StageSetting.of_number("snow", 0.3),
                    StageSetting.of_number("wind", 0.1), StageSetting.of_text("mist", "rgba(255,236,210,0.06)"),
                },
                human_start_x = 1400, human_start_feet_y = 520,
                dog_start_x = 360, dog_start_feet_y = 520,
                guest = new StageGuest { x = 300, feet_y = 520, size = 0.8, mode = "follow", pose = "" },
                solids = new StageSolid[]
                {
                    new StageSolid { kind = "floor", x = 0, y = 520, width = 5400, height = 100 },
                    new StageSolid { kind = "invisible", x = 0, y = 0, width = 10, height = 520 },
                    new StageSolid { kind = "invisible", x = 5390, y = 0, width = 10, height = 520 },
                },
                props = new StageProp[]
                {
                    // Kirk Wynd: the red door, and Kofi's door next to it.
                    new StageProp { kind = "sign", x = 120, y = 300, settings = new StageSetting[] { StageSetting.of_text("text", "Kirk Wynd") } },
                    new StageProp { kind = "house", x = 380, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 600), StageSetting.of_text("color", "#6a6258") } },
                    new StageProp { kind = "doorframe", x = 590, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#8a2a22") } },
                    new StageProp { kind = "doorframe", x = 860, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#2a4a6a") } },
                    // The road and the bus.
                    new StageProp { kind = "road_sign", x = 1250, y = 520 },
                    new StageProp { kind = "lorry", x = 1460, y = 520, after = "kerb", until = new string[] { "biscuit_shoved" }, settings = new StageSetting[] { StageSetting.of_number("w", 380), StageSetting.of_number("h", 170), StageSetting.of_text("color", "#a03028") } },
                    new StageProp { kind = "lorry", x = 1200, y = 520, after = "em_bus", until = new string[] { "asked_at_stall" }, settings = new StageSetting[] { StageSetting.of_number("w", 380), StageSetting.of_number("h", 170), StageSetting.of_text("color", "#a03028") } },
                    // The market square.
                    new StageProp { kind = "tent", x = 1700, y = 520, settings = new StageSetting[] { hut } },
                    new StageProp { kind = "fire", x = 2060, y = 520 },
                    new StageProp { kind = "tent", x = 2150, y = 520, settings = new StageSetting[] { hut } },
                    new StageProp { kind = "lamp", x = 2250, y = 220, settings = new StageSetting[] { StageSetting.of_flag("hanging", true), StageSetting.of_number("radius", 260), bulbs } },
                    new StageProp { kind = "tent", x = 2550, y = 520, settings = new StageSetting[] { hut } },
                    new StageProp { kind = "carousel", x = 2780, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 220), StageSetting.of_number("h", 190), StageSetting.of_text("color", "#c09a5a") } },
                    new StageProp { kind = "lamp", x = 2900, y = 220, settings = new StageSetting[] { StageSetting.of_flag("hanging", true), StageSetting.of_number("radius", 260), bulbs } },
                    new StageProp { kind = "house", x = 3150, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 260), StageSetting.of_number("h", 160), StageSetting.of_text("color", "#8a6a3a") } },
                    new StageProp { kind = "bale", x = 3200, y = 520 },
                    new StageProp { kind = "tree", x = 3700, y = 520, until = new string[] { "countdown" }, settings = new StageSetting[] { StageSetting.of_text("color", "#2f4a3a"), StageSetting.of_number("w", 130), StageSetting.of_number("h", 330) } },
                    new StageProp { kind = "tree", x = 3700, y = 520, after = "countdown", settings = new StageSetting[] { StageSetting.of_text("color", "#5f8a5a"), StageSetting.of_number("w", 130), StageSetting.of_number("h", 330) } },
                    new StageProp { kind = "lamp", x = 3700, y = 520, after = "countdown", settings = new StageSetting[] { StageSetting.of_number("h", 340), StageSetting.of_number("radius", 420), StageSetting.of_text("color", "rgba(255,214,150,0.20)") } },
                    // The town hall and its steps.
                    new StageProp { kind = "house", x = 3920, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 420), StageSetting.of_number("h", 300), StageSetting.of_text("color", "#7a7468") } },
                    new StageProp { kind = "stairs", x = 3940, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 200), StageSetting.of_number("h", 60) } },
                    // Santa's grotto, the box under its back window, and the bus shelter facing it.
                    new StageProp { kind = "crate", x = 4330, y = 520, until = new string[] { "crate_pushed" }, settings = new StageSetting[] { StageSetting.of_number("w", 70), StageSetting.of_number("h", 50) } },
                    new StageProp { kind = "crate", x = 4520, y = 520, after = "crate_pushed", settings = new StageSetting[] { StageSetting.of_number("w", 70), StageSetting.of_number("h", 50) } },
                    new StageProp { kind = "house", x = 4560, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 260), StageSetting.of_number("h", 180), StageSetting.of_text("color", "#7a3a30") } },
                    new StageProp { kind = "sign", x = 4600, y = 320, settings = new StageSetting[] { StageSetting.of_text("text", "Santa's Grotto") } },
                    new StageProp { kind = "bench", x = 4940, y = 520 },
                    new StageProp { kind = "sign", x = 4960, y = 330, settings = new StageSetting[] { StageSetting.of_text("text", "Bus") } },
                },
                things = new StageThingLook[]
                {
                    new StageThingLook { event_name = "biscuit_shoved", sound = "bark", pose = "bark" },
                    new StageThingLook { event_name = "pretzel_found", sound = "eat", pose = "sniff" },
                    new StageThingLook { event_name = "keepsake_railing_mitten", sprite = "keepsake", sound = "sniff" },
                    new StageThingLook { event_name = "asked_at_stall" },
                    new StageThingLook { event_name = "poster_carousel", sound = "pickup" },
                    new StageThingLook { event_name = "poster_stall", sound = "pickup" },
                    new StageThingLook { event_name = "keepsake_porch_photo", sprite = "keepsake" },
                    new StageThingLook { event_name = "keepsake_letter_board", sprite = "keepsake" },
                    new StageThingLook { event_name = "crate_pushed", sound = "thud" },
                    new StageThingLook { event_name = "last_poster", sound = "pickup" },
                },
                // One entry per story rule, in the same order; the comment names the rule.
                rules = new RuleStaging[]
                {
                    new RuleStaging { hide = new[] { new StageHiding { who = StageWho.human, hidden = true } }, camera = CameraChange.hold, camera_x = 400, camera_y = 420, camera_zoom = 1.0, caption = "The sixteenth of December" },  // 0 level_started
                    new RuleStaging { },  // 1 dusk_1
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.guest, x = 600, feet_y = 520 } }, guest_mode = "still", pose = new[] { new StagePose { who = StageWho.guest, pose = "sit", seconds = 30 } }, camera = CameraChange.release, caption = "" },  // 2 arrived: Biscuit goes ahead
                    new RuleStaging { },  // 3 red_door
                    new RuleStaging { },  // 4 door_stays_shut
                    new RuleStaging { camera = CameraChange.hold, camera_x = 720, camera_y = 420, camera_zoom = 1.1, sound = "gate", mood = "tender" },  // 5 kofi_came
                    new RuleStaging { sound = "eat" },  // 6 kofi_2
                    new RuleStaging { guest_mode = "follow", camera = CameraChange.release, mood = "calm" },  // 7 kofi_done
                    new RuleStaging { shake_seconds = 0.6, mood = "tense" },  // 8 kerb
                    new RuleStaging { mood = "calm" },  // 9 biscuit_shoved
                    new RuleStaging { },  // 10 market
                    new RuleStaging { },  // 11 pretzel_found
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.human, x = 1400, feet_y = 520 } }, hide = new[] { new StageHiding { who = StageWho.human, hidden = false }, new StageHiding { who = StageWho.dog, hidden = true }, new StageHiding { who = StageWho.guest, hidden = true } }, darkness = 0.55, fade_in = true, caption = "Half past five" },  // 12 em_bus
                    new RuleStaging { caption = "" },  // 13 asked_at_stall
                    new RuleStaging { },  // 14 poster_carousel
                    new RuleStaging { },  // 15 poster_stall
                    new RuleStaging { },  // 16 both posters up
                    new RuleStaging { },  // 17 steps_climbed
                    new RuleStaging { camera = CameraChange.hold, camera_x = 3500, camera_y = 380, camera_zoom = 0.8, sound = "chime", mood = "hopeful" },  // 18 countdown: the tree comes on
                    new RuleStaging { camera = CameraChange.release },  // 19 whistle_time
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.dog, x = 3260, feet_y = 520 }, new StagePlacement { who = StageWho.guest, x = 3210, feet_y = 520 } }, hide = new[] { new StageHiding { who = StageWho.dog, hidden = false }, new StageHiding { who = StageWho.guest, hidden = false } }, guest_mode = "still", camera = CameraChange.hold, camera_x = 3600, camera_y = 420, camera_zoom = 0.75, mood = "tender" },  // 20 her whistle, under the band
                    new RuleStaging { face = new[] { new StageFacing { who = StageWho.dog, direction = 1 } } },  // 21 under_the_trumpets
                    new RuleStaging { },  // 22 one_row
                    new RuleStaging { guest_mode = "follow", camera = CameraChange.release, darkness = 0.65, mood = "quiet" },  // 23 near_miss_done
                    new RuleStaging { },  // 24 crate_pushed
                    new RuleStaging { hide = new[] { new StageHiding { who = StageWho.guest, hidden = true } }, camera = CameraChange.hold, camera_x = 4650, camera_y = 430, camera_zoom = 1.1, mood = "tender" },  // 25 into_the_grotto
                    new RuleStaging { hide = new[] { new StageHiding { who = StageWho.dog, hidden = true } } },  // 26 grotto_sleep
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.human, x = 4200, feet_y = 520 } }, hide = new[] { new StageHiding { who = StageWho.dog, hidden = true }, new StageHiding { who = StageWho.guest, hidden = true } }, camera = CameraChange.release },  // 27 em_last_poster
                    new RuleStaging { camera = CameraChange.hold, camera_x = 4800, camera_y = 420, camera_zoom = 0.9 },  // 28 last_poster: it faces the grotto
                    new RuleStaging { },  // 29 chapter_done
                },
                aftermath = new StageAftermath { scene = "road_dawn", mood = "quiet", rain = 0 },
            };
        }
    }
}
