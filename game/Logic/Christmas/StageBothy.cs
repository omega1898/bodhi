// Purpose: how Chapter Nine of the Christmas story, "The Bothy", looks: one stone room with its
// hearth, sleeping platform, drying line and shelf with the visitors' book, its snow-jammed door,
// and outside, the storm and the woodpile under the drift; and what each story rule does on
// screen. The story is rules/Runtime/Christmas/Christmas09Bothy.cs.
//
// Left to right, in layout units: the room (300 to 1260), the door (1260) and outside (1300 to
// 2400). The floor is at y = 520. Everything is a grey box until the 3D scenery is built.
namespace Bodhi.Game
{
    public static partial class ChristmasStages
    {
        // Chapter Nine: The Bothy.
        public static ChapterStage bothy()
        {
            return new ChapterStage
            {
                id = "bothy",
                width = 2400,
                height = 600,
                mood = "quiet",
                scenery = new StageSetting[]
                {
                    StageSetting.of_text("sky_top", "#1a2230"), StageSetting.of_text("sky_bottom", "#2c3a4e"),
                    StageSetting.of_text("far", "hills"), StageSetting.of_text("far_color", "#243042"),
                    StageSetting.of_text("ground_top", "#d6dfe8"), StageSetting.of_text("ground_body", "#8a9aac"),
                    StageSetting.of_text("wall", "#5a5450"), StageSetting.of_text("room_wall", "#5a5450"),
                    StageSetting.of_number("darkness", 0.6), StageSetting.of_text("darkness_color", "rgb(22,26,38)"),
                    StageSetting.of_text("darkness_floor", "#4a4644"), StageSetting.of_number("snow", 1),
                    StageSetting.of_number("wind", 1.0), StageSetting.of_text("mist", "rgba(220,228,240,0.12)"),
                },
                human_start_x = 900, human_start_feet_y = 520,
                dog_start_x = 950, dog_start_feet_y = 520,
                guest = new StageGuest { x = 720, feet_y = 520, size = 0.8, mode = "still", pose = "sleep" },
                solids = new StageSolid[]
                {
                    new StageSolid { kind = "floor", x = 0, y = 520, width = 2400, height = 100 },
                    new StageSolid { kind = "invisible", x = 290, y = 0, width = 10, height = 520 },
                    new StageSolid { id = "bothy_door", kind = "gate", x = 1260, y = 330, width = 14, height = 190 },
                    new StageSolid { kind = "invisible", x = 2390, y = 0, width = 10, height = 520 },
                },
                props = new StageProp[]
                {
                    // The room: stone walls, the hearth and its fire, the platform, the line, the shelf.
                    new StageProp { kind = "room_wall", x = 300, y = 200, settings = new StageSetting[] { StageSetting.of_number("w", 960), StageSetting.of_number("h", 320) } },
                    new StageProp { kind = "fire", x = 480, y = 520, after = "fire_lit" },
                    new StageProp { kind = "lamp", x = 480, y = 520, after = "fire_lit", settings = new StageSetting[] { StageSetting.of_number("h", 60), StageSetting.of_number("radius", 360), StageSetting.of_text("color", "rgba(255,170,90,0.24)") } },
                    new StageProp { kind = "bed", x = 640, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 220), StageSetting.of_text("color", "#5a4632") } },
                    new StageProp { kind = "rail", x = 520, y = 300, settings = new StageSetting[] { StageSetting.of_number("w", 300), StageSetting.of_number("h", 6), StageSetting.of_text("color", "#8a7a66") } },
                    new StageProp { kind = "sign", x = 940, y = 330, settings = new StageSetting[] { StageSetting.of_text("text", "Visitors' book") } },
                    new StageProp { kind = "window", x = 1100, y = 300, settings = new StageSetting[] { StageSetting.of_number("w", 70), StageSetting.of_number("h", 70) } },
                    // Outside: the storm, and the woodpile under the drift.
                    new StageProp { kind = "snow_drift", x = 1500, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 220), StageSetting.of_number("h", 70) } },
                    new StageProp { kind = "crate", x = 1940, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 140), StageSetting.of_number("h", 50), StageSetting.of_text("color", "#5a4630") } },
                    new StageProp { kind = "snow_drift", x = 1900, y = 520, until = new string[] { "logs_dug" }, settings = new StageSetting[] { StageSetting.of_number("w", 240), StageSetting.of_number("h", 90) } },
                },
                things = new StageThingLook[]
                {
                    new StageThingLook { event_name = "fire_lit", sound = "pickup", pose = "kneel" },
                    new StageThingLook { event_name = "biscuit_wont_eat", sprite = "bowl", pose = "kneel" },
                    new StageThingLook { event_name = "lay_along_biscuit", pose = "sleep" },
                    new StageThingLook { event_name = "door_shouldered", sound = "thud" },
                    new StageThingLook { event_name = "logs_dug", sound = "sniff", pose = "sniff" },
                    new StageThingLook { event_name = "logs_carried", sound = "thud" },
                    new StageThingLook { event_name = "towelled", sound = "towel", pose = "kneel" },
                    new StageThingLook { event_name = "book_read" },
                    new StageThingLook { event_name = "keepsake_book_peaches", sprite = "keepsake" },
                    new StageThingLook { event_name = "keepsake_book_shortbread", sprite = "keepsake" },
                    new StageThingLook { event_name = "keepsake_moss_doorframe", sprite = "keepsake", sound = "sniff" },
                    new StageThingLook { event_name = "stick_brought", sound = "pickup" },
                },
                // One entry per story rule, in the same order; the comment names the rule.
                rules = new RuleStaging[]
                {
                    new RuleStaging { camera = CameraChange.hold, camera_x = 800, camera_y = 420, camera_zoom = 1.0, caption = "The twenty-first of December" },  // 0 level_started
                    new RuleStaging { },  // 1 open_1
                    new RuleStaging { },  // 2 open_2
                    new RuleStaging { camera = CameraChange.release, caption = "" },  // 3 inside
                    new RuleStaging { darkness = 0.4, mood = "tender" },  // 4 fire_lit: the warmest light in the game
                    new RuleStaging { mood = "quiet" },  // 5 biscuit_wont_eat
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.dog, x = 745, feet_y = 520 } }, pose = new[] { new StagePose { who = StageWho.dog, pose = "sleep", seconds = 12 } }, camera = CameraChange.hold, camera_x = 730, camera_y = 440, camera_zoom = 1.25, sound = "heartbeat" },  // 6 lay_along_biscuit
                    new RuleStaging { camera = CameraChange.release, darkness = 0.6 },  // 7 fire_low
                    new RuleStaging { shake_seconds = 0.5 },  // 8 door_shouldered: the storm comes in
                    new RuleStaging { darkness = 0.75, mood = "tense" },  // 9 out_in_the_storm
                    new RuleStaging { },  // 10 logs_dug
                    new RuleStaging { darkness = 0.4, mood = "quiet" },  // 11 logs_carried
                    new RuleStaging { },  // 12 towelled
                    new RuleStaging { camera = CameraChange.hold, camera_x = 1000, camera_y = 420, camera_zoom = 1.1 },  // 13 book_read
                    new RuleStaging { mood = "tender" },  // 14 book_1974
                    new RuleStaging { },  // 15 tams_bothy
                    new RuleStaging { camera = CameraChange.release, darkness = 0.55 },  // 16 second_night
                    new RuleStaging { },  // 17 stick_brought
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.human, x = 880, feet_y = 520 }, new StagePlacement { who = StageWho.dog, x = 930, feet_y = 520 } }, camera = CameraChange.hold, camera_x = 880, camera_y = 420, camera_zoom = 1.15 },  // 18 present
                    new RuleStaging { pose = new[] { new StagePose { who = StageWho.guest, pose = "bark", seconds = 1 } }, darkness = 0.25, wind = 0.2, mood = "hopeful", sound = "bark" },  // 19 morning: one polite bark
                    new RuleStaging { },  // 20 morning_2
                    new RuleStaging { camera = CameraChange.hold, camera_x = 1000, camera_y = 420, camera_zoom = 1.0 },  // 21 closing
                    new RuleStaging { },  // 22 chapter_done
                },
                aftermath = new StageAftermath { scene = "cabin_fire", mood = "hopeful", rain = 0 },
            };
        }
    }
}
