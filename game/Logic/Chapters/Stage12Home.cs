// Purpose: how the chapter "Home" looks: its floors, water, props and staging.
// First generated from the old browser game; since it was removed, this file is the original: edit it here.
namespace Bodhi.Game
{
    public static partial class ChapterStages
    {
        // Chapter 12: Home.
        public static ChapterStage home()
        {
            return new ChapterStage
            {
                id = "home",
                width = 4400,
                height = 600,
                mood = "hopeful",
                scenery = new StageSetting[] { StageSetting.of_text("sky_top", "#5c6b8a"), StageSetting.of_text("sky_bottom", "#f0c08a"), StageSetting.of_text("far", "hills"), StageSetting.of_nothing("mid"), StageSetting.of_flag("sun", true), StageSetting.of_text("far_color", "#7c7f86"), StageSetting.of_text("mid_color", "#5d6a52"), StageSetting.of_text("ground_top", "#6f7a4c"), StageSetting.of_text("ground_body", "#4a4532"), StageSetting.of_text("wall", "#6d6356"), StageSetting.of_text("room_wall", "#a8987e"), StageSetting.of_text("near", "near_hedges"), StageSetting.of_text("near_color", "#4d5638"), StageSetting.of_number("darkness", 0), StageSetting.of_number("rain", 0), StageSetting.of_number("wind", 0.2), StageSetting.of_flag("fireflies", true), StageSetting.of_text("darkness_floor", "#2b2a1c"), StageSetting.of_text("foreground", "grass"), StageSetting.of_text("foreground_color", "rgba(46,52,30,0.8)"), StageSetting.of_text("mist", "rgba(255,228,190,0.07)") },
                human_start_x = 160, human_start_feet_y = 520,
                dog_start_x = 240, dog_start_feet_y = 520,
                solids = new StageSolid[]
                {
                    new StageSolid { kind = "floor", x = 0, y = 520, width = 4400, height = 100 },
                    new StageSolid { kind = "floor", x = 600, y = 508, width = 220, height = 12 },
                    new StageSolid { kind = "floor", x = 1500, y = 508, width = 300, height = 12 },
                    new StageSolid { kind = "invisible", x = 2210, y = 0, width = 10, height = 520 },
                    new StageSolid { kind = "invisible", x = 2590, y = 0, width = 10, height = 520 },
                    new StageSolid { kind = "invisible", x = 3908, y = 480, width = 162, height = 40 },
                },
                water = new StageWater[]
                {
                },
                props = new StageProp[]
                {
                    new StageProp { kind = "tree", x = 300, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#5d6a42") } },
                    new StageProp { kind = "flowers", x = 500, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 300) } },
                    new StageProp { kind = "tree", x = 1100, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#56664a") } },
                    new StageProp { kind = "flowers", x = 1300, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 200) } },
                    new StageProp { kind = "tree", x = 1800, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#4f6a3a") } },
                    new StageProp { kind = "house", x = 1900, y = 520, settings = new StageSetting[] { StageSetting.of_flag("weathered", true) } },
                    new StageProp { kind = "room_wall", x = 2600, y = 200, settings = new StageSetting[] { StageSetting.of_number("w", 1800), StageSetting.of_number("h", 320), StageSetting.of_text("color", "#a8987e") } },
                    new StageProp { kind = "scrawl", x = 2650, y = 420, settings = new StageSetting[] { StageSetting.of_text("text", "- - - - - - - - - - - - - - - - - - -"), StageSetting.of_text("color", "rgba(80,60,40,0.35)") } },
                    new StageProp { kind = "doorframe", x = 2900, y = 520, until = new string[] { "height_marked_again" }, settings = new StageSetting[] { StageSetting.of_number("marks.0.0", 18), StageSetting.of_text("marks.0.1", "{dog}, 8 weeks") } },
                    new StageProp { kind = "doorframe", x = 2900, y = 520, after = "height_marked_again", settings = new StageSetting[] { StageSetting.of_number("marks.0.0", 18), StageSetting.of_text("marks.0.1", "{dog}, 8 weeks"), StageSetting.of_number("marks.1.0", 52), StageSetting.of_text("marks.1.1", "{dog}, one year") } },
                    new StageProp { kind = "kitchen", x = 3060, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 300) } },
                    new StageProp { kind = "lamp", x = 3200, y = 200, settings = new StageSetting[] { StageSetting.of_flag("hanging", true), StageSetting.of_number("radius", 220), StageSetting.of_text("color", "rgba(255,214,170,0.14)") } },
                    new StageProp { kind = "sofa", x = 3460, y = 520 },
                    new StageProp { kind = "window", x = 3700, y = 280, settings = new StageSetting[] { StageSetting.of_number("w", 90), StageSetting.of_number("h", 100), StageSetting.of_flag("glow", true), StageSetting.of_number("radius", 220), StageSetting.of_text("color", "rgba(255,214,170,0.12)") } },
                    new StageProp { kind = "double_bed", x = 3900, y = 520, until = new string[] { "lay_down" }, settings = new StageSetting[] { StageSetting.of_text("blanket", "#7a5a8a") } },
                    new StageProp { kind = "double_bed", x = 3900, y = 520, after = "lay_down", settings = new StageSetting[] { StageSetting.of_text("blanket", "#7a5a8a"), StageSetting.of_flag("occupied", true) } },
                    new StageProp { kind = "tucked_blanket", x = 3900, y = 520, after = "tucked_in", settings = new StageSetting[] { StageSetting.of_text("blanket", "#7a5a8a"), StageSetting.of_flag("front", true) } },
                    new StageProp { kind = "lamp", x = 4050, y = 200, settings = new StageSetting[] { StageSetting.of_flag("hanging", true), StageSetting.of_number("radius", 230), StageSetting.of_text("color", "rgba(255,200,150,0.16)") } },
                },
                things = new StageThingLook[]
                {
                    new StageThingLook { event_name = "home_door_opened", sprite = "", sound = "gate", pose = "" },
                    new StageThingLook { event_name = "height_marked_again", sprite = "doorframe", sound = "", pose = "" },
                    new StageThingLook { event_name = "last_meal", sprite = "bowl", sound = "", pose = "" },
                    new StageThingLook { event_name = "towelled_dry", sprite = "", sound = "", pose = "" },
                    new StageThingLook { event_name = "tucked_in", sprite = "", sound = "", pose = "" },
                    new StageThingLook { event_name = "keepsake_best_stick", sprite = "keepsake", sound = "", pose = "" },
                    new StageThingLook { event_name = "keepsake_apple", sprite = "keepsake", sound = "", pose = "" },
                    new StageThingLook { event_name = "keepsake_other_slipper", sprite = "keepsake", sound = "", pose = "" },
                },
                rules = new RuleStaging[]
                {
                    new RuleStaging {  },
                    new RuleStaging {  },
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.human, x = 2680, feet_y = 520 }, new StagePlacement { who = StageWho.dog, x = 2740, feet_y = 520 } }, mood = "calm", fade_in = true },
                    new RuleStaging { mood = "tender" },
                    new RuleStaging {  },
                    new RuleStaging {  },
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.dog, x = 3990, feet_y = 480 } }, face = new[] { new StageFacing { who = StageWho.dog, direction = -1 } }, pose = new[] { new StagePose { who = StageWho.dog, pose = "sleep", seconds = 9999 }, new StagePose { who = StageWho.human, pose = "kneel", seconds = 5 } }, darkness = 0.45 },
                    new RuleStaging { hide = new[] { new StageHiding { who = StageWho.human, hidden = true } }, camera = CameraChange.hold, camera_x = 3990, camera_y = 440, camera_zoom = 1.15, darkness = 0.7, mood = "quiet" },
                    new RuleStaging {  },
                },
                aftermath = new StageAftermath { scene = "bedtime", mood = "hopeful", rain = 0.2 },
            };
        }
    }
}
