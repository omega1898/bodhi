// Purpose: how Chapter Twelve of the Christmas story, "Christmas Day", looks: the cabin on
// Christmas morning with the stocking, the lopsided tree lit, the table laid for lunch and the
// lamp in the window; and the clearing at night where the snowman of the title screen goes, the
// sled by the porch; and what each story rule does on screen. The story is
// rules/Runtime/Christmas/Christmas12ChristmasDay.cs.
//
// Left to right, in layout units: the cabin (0 to 1500) and the clearing (1500 to 2600). The
// floor is at y = 520. Everything is a grey box until the 3D scenery is built; the night scene is
// meant to become the title screen exactly.
namespace Bodhi.Game
{
    public static partial class ChristmasStages
    {
        // Chapter Twelve: Christmas Day.
        public static ChapterStage christmas_day()
        {
            var warm = StageSetting.of_text("color", "rgba(255,206,150,0.2)");
            return new ChapterStage
            {
                id = "christmas_day",
                width = 2600,
                height = 600,
                mood = "hopeful",
                scenery = new StageSetting[]
                {
                    StageSetting.of_text("sky_top", "#78a8e0"), StageSetting.of_text("sky_bottom", "#f0f6fc"),
                    StageSetting.of_text("far", "hills"), StageSetting.of_text("far_color", "#cfdced"),
                    StageSetting.of_text("mid", "trees"), StageSetting.of_text("mid_color", "#3f4f4a"),
                    StageSetting.of_text("ground_top", "#f6f9fc"), StageSetting.of_text("ground_body", "#b8c8da"),
                    StageSetting.of_text("wall", "#6b5a4a"), StageSetting.of_text("room_wall", "#8a6f55"),
                    StageSetting.of_number("darkness", 0.05), StageSetting.of_text("darkness_color", "rgb(20,30,50)"),
                    StageSetting.of_text("darkness_floor", "#7d8790"), StageSetting.of_number("snow", 0.2),
                    StageSetting.of_number("wind", 0.05), StageSetting.of_text("mist", "rgba(255,250,240,0.05)"),
                },
                human_start_x = 160, human_start_feet_y = 520,
                dog_start_x = 200, dog_start_feet_y = 520,
                ball_x = 560, ball_y = 510,
                guest = new StageGuest { x = 1150, feet_y = 520, size = 0.8, mode = "gone", pose = "sit" },
                solids = new StageSolid[]
                {
                    new StageSolid { kind = "floor", x = 0, y = 520, width = 2600, height = 100 },
                    new StageSolid { kind = "invisible", x = 0, y = 0, width = 10, height = 520 },
                    new StageSolid { kind = "invisible", x = 2590, y = 0, width = 10, height = 520 },
                },
                props = new StageProp[]
                {
                    // The cabin on Christmas morning.
                    new StageProp { kind = "room_wall", x = 0, y = 200, settings = new StageSetting[] { StageSetting.of_number("w", 1500), StageSetting.of_number("h", 320) } },
                    new StageProp { kind = "double_bed", x = 60, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#4d6a86") } },
                    new StageProp { kind = "stove", x = 440, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#2b2b2e") } },
                    new StageProp { kind = "sign", x = 450, y = 300, settings = new StageSetting[] { StageSetting.of_text("text", "BODHI") } },
                    new StageProp { kind = "tree", x = 800, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#5f8a5a"), StageSetting.of_number("w", 70), StageSetting.of_number("h", 170) } },
                    new StageProp { kind = "lamp", x = 800, y = 520, settings = new StageSetting[] { StageSetting.of_number("h", 170), StageSetting.of_number("radius", 260), warm } },
                    new StageProp { kind = "bench", x = 1020, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 220), StageSetting.of_text("color", "#6b4a32") } },
                    new StageProp { kind = "window", x = 1360, y = 280, settings = new StageSetting[] { StageSetting.of_number("w", 80), StageSetting.of_number("h", 90) } },
                    new StageProp { kind = "lamp", x = 1400, y = 380, after = "last_lamp", settings = new StageSetting[] { StageSetting.of_number("h", 30), StageSetting.of_number("radius", 320), StageSetting.of_text("color", "rgba(255,206,150,0.26)") } },
                    // The clearing: the sled by the porch, and the snowman of the title screen.
                    new StageProp { kind = "sled", x = 1580, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 130), StageSetting.of_number("h", 28), StageSetting.of_text("color", "#7a4a2a") } },
                    new StageProp { kind = "snowman", x = 1960, y = 520, after = "snowman_base", until = new string[] { "arms_fetched" }, settings = new StageSetting[] { StageSetting.of_number("w", 70), StageSetting.of_number("h", 90), StageSetting.of_text("color", "#f4f7fa") } },
                    new StageProp { kind = "snowman", x = 1960, y = 520, after = "arms_fetched", settings = new StageSetting[] { StageSetting.of_number("w", 80), StageSetting.of_number("h", 160), StageSetting.of_text("color", "#f4f7fa") } },
                    new StageProp { kind = "lamp", x = 1820, y = 520, after = "snowman_base", settings = new StageSetting[] { StageSetting.of_number("h", 30), StageSetting.of_number("radius", 200), warm } },
                    new StageProp { kind = "tree", x = 2400, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#2f3d38") } },
                },
                things = new StageThingLook[]
                {
                    new StageThingLook { event_name = "stocking_opened", sound = "pickup" },
                    new StageThingLook { event_name = "tag_on", sound = "chime", pose = "kneel" },
                    new StageThingLook { event_name = "towelled", sound = "towel", pose = "kneel" },
                    new StageThingLook { event_name = "christmas_dinner", sprite = "bowl", sound = "eat" },
                    new StageThingLook { event_name = "keepsake_bothy_stick", sprite = "keepsake", sound = "sniff" },
                    new StageThingLook { event_name = "presents_opened", sound = "pickup" },
                    new StageThingLook { event_name = "keepsake_minas_drawing", sprite = "keepsake" },
                    new StageThingLook { event_name = "keepsake_tams_card", sprite = "keepsake" },
                    new StageThingLook { event_name = "sprout_stolen", sound = "sniff", pose = "sniff" },
                    new StageThingLook { event_name = "snowman_base", sound = "thud", pose = "kneel" },
                    new StageThingLook { event_name = "arms_fetched", sound = "pickup" },
                    new StageThingLook { event_name = "last_lamp", sound = "pickup" },
                },
                // One entry per story rule, in the same order; the comment names the rule.
                rules = new RuleStaging[]
                {
                    new RuleStaging { pose = new[] { new StagePose { who = StageWho.dog, pose = "sit", seconds = 8 } }, camera = CameraChange.hold, camera_x = 250, camera_y = 420, camera_zoom = 1.1, darkness = 0.15, caption = "Christmas Day" },  // 0 level_started
                    new RuleStaging { },  // 1 wake_1
                    new RuleStaging { },  // 2 wake_2
                    new RuleStaging { camera = CameraChange.release, darkness = 0.05, caption = "" },  // 3 christmas_morning
                    new RuleStaging { },  // 4 stocking_opened
                    new RuleStaging { },  // 5 tag_on (hers)
                    new RuleStaging { },  // 6 tag_on (the tag itself)
                    new RuleStaging { },  // 7 ball_fetched
                    new RuleStaging { },  // 8 towelled
                    new RuleStaging { },  // 9 christmas_dinner
                    new RuleStaging { },  // 10 dog_petted
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.guest, x = 1150, feet_y = 520 } }, guest_mode = "still", pose = new[] { new StagePose { who = StageWho.guest, pose = "bark", seconds = 1 } }, camera = CameraChange.hold, camera_x = 1100, camera_y = 420, camera_zoom = 1.0, sound = "bark", mood = "tender" },  // 11 the visitors arrive
                    new RuleStaging { camera = CameraChange.release },  // 12 lunch
                    new RuleStaging { },  // 13 presents_opened
                    new RuleStaging { sound = "gate" },  // 14 dougie_came: a knock at the door
                    new RuleStaging { },  // 15 tam_rang
                    new RuleStaging { },  // 16 sprout_time
                    new RuleStaging { },  // 17 sprout_stolen (his)
                    new RuleStaging { guest_mode = "follow", darkness = 0.5, mood = "quiet", fade_in = true, caption = "Christmas night" },  // 18 sprout_stolen (hers): night falls
                    new RuleStaging { caption = "" },  // 19 snowman_base
                    new RuleStaging { },  // 20 arms_fetched (his)
                    new RuleStaging { },  // 21 arms_fetched (hers)
                    new RuleStaging { sound = "chime", mood = "hopeful" },  // 22 the whistle answered: his bell
                    new RuleStaging { camera = CameraChange.hold, camera_x = 1500, camera_y = 410, camera_zoom = 0.9, mood = "tender" },  // 23 last_lamp
                    new RuleStaging { },  // 24 last_lines
                    new RuleStaging { darkness = 0.55 },  // 25 here_is_the_place
                    new RuleStaging { },  // 26 chapter_done
                },
                aftermath = new StageAftermath { scene = "farm_fire", mood = "tender", rain = 0 },
            };
        }
    }
}
