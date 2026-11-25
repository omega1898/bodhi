// Purpose: how the chapter "The Storm" looks: its floors, water, props and staging.
// First generated from the old browser game; since it was removed, this file is the original: edit it here.
namespace Bodhi.Game
{
    public static partial class ChapterStages
    {
        // Chapter 2: The Storm.
        public static ChapterStage the_storm()
        {
            return new ChapterStage
            {
                id = "the_storm",
                width = 3600,
                height = 600,
                mood = "quiet",
                scenery = new StageSetting[] { StageSetting.of_text("sky_top", "#070a10"), StageSetting.of_text("sky_bottom", "#18202c"), StageSetting.of_text("far", "hills"), StageSetting.of_text("mid", "trees"), StageSetting.of_text("far_color", "#121924"), StageSetting.of_text("mid_color", "#17202a"), StageSetting.of_text("ground_top", "#4a4038"), StageSetting.of_text("ground_body", "#241f1a"), StageSetting.of_text("wall", "#3a3028"), StageSetting.of_text("room_wall", "#3b3128"), StageSetting.of_text("near", "near_hedges"), StageSetting.of_text("near_color", "#0e141b"), StageSetting.of_text("horizon_glow", "rgba(110,130,160,0.4)"), StageSetting.of_number("darkness", 0.6), StageSetting.of_text("darkness_color", "rgb(4,6,12)"), StageSetting.of_number("rain", 1.2), StageSetting.of_number("rain_sound", 0.9), StageSetting.of_number("wind", 0.8), StageSetting.of_number("wind_slant", -0.4), StageSetting.of_flag("storm", true), StageSetting.of_text("mist", "rgba(150,170,185,0.06)") },
                human_start_x = 300, human_start_feet_y = 300,
                dog_start_x = 400, dog_start_feet_y = 300,
                solids = new StageSolid[]
                {
                    new StageSolid { kind = "floor", x = 0, y = 300, width = 1000, height = 320 },
                    new StageSolid { kind = "floor", x = 1000, y = 300, width = 400, height = 30 },
                    new StageSolid { kind = "invisible", id = "edge_rail", x = 1394, y = 120, width = 6, height = 180, blocks = SolidBlocks.human },
                    new StageSolid { kind = "plank", id = "stair_1", x = 1400, y = 355, width = 60, height = 12 },
                    new StageSolid { kind = "plank", id = "stair_2", x = 1460, y = 410, width = 60, height = 12 },
                    new StageSolid { kind = "plank", id = "stair_3", x = 1520, y = 465, width = 60, height = 12 },
                    new StageSolid { kind = "floor", x = 1000, y = 520, width = 2600, height = 100 },
                    new StageSolid { kind = "invisible", x = 2160, y = 474, width = 160, height = 46 },
                    new StageSolid { kind = "invisible", x = 1874, y = 474, width = 186, height = 46 },
                    new StageSolid { kind = "suitcase", x = 1820, y = 400, width = 54, height = 120, color = "#cfcbc2" },
                    new StageSolid { kind = "plank", id = "shelf", x = 1600, y = 410, width = 220, height = 12 },
                    new StageSolid { kind = "gate", id = "front_door", x = 3480, y = 380, width = 16, height = 140 },
                },
                water = new StageWater[]
                {
                    new StageWater { id = "flood", x = 1000, y = 505, width = 2600, height = 15, flow = 0.4 },
                },
                props = new StageProp[]
                {
                    new StageProp { kind = "room_wall", x = 0, y = 110, settings = new StageSetting[] { StageSetting.of_number("w", 1400), StageSetting.of_number("h", 190), StageSetting.of_text("color", "#3b3128") } },
                    new StageProp { kind = "window", x = 60, y = 160, settings = new StageSetting[] { StageSetting.of_number("w", 80), StageSetting.of_number("h", 100), StageSetting.of_flag("glow", true), StageSetting.of_number("radius", 160), StageSetting.of_text("color", "rgba(150,175,210,0.08)") } },
                    new StageProp { kind = "double_bed", x = 180, y = 300, settings = new StageSetting[] { StageSetting.of_text("blanket", "#4d6a86") } },
                    new StageProp { kind = "lamp", x = 500, y = 110, settings = new StageSetting[] { StageSetting.of_flag("hanging", true), StageSetting.of_number("radius", 170), StageSetting.of_flag("flicker", true) } },
                    new StageProp { kind = "scrawl", x = 760, y = 200, settings = new StageSetting[] { StageSetting.of_text("text", "{dog}  4 months\n(sits!  sometimes)"), StageSetting.of_text("color", "rgba(239,230,212,0.4)") } },
                    new StageProp { kind = "room_wall", x = 1400, y = 110, settings = new StageSetting[] { StageSetting.of_number("w", 2200), StageSetting.of_number("h", 220), StageSetting.of_text("color", "#2a221c") } },
                    new StageProp { kind = "stairs", x = 1400, y = 520, until = new string[] { "storm_3", "storm_done" }, settings = new StageSetting[] { StageSetting.of_number("top", 300), StageSetting.of_number("w", 180), StageSetting.of_flag("down_right", true) } },
                    new StageProp { kind = "room_wall", x = 1000, y = 330, settings = new StageSetting[] { StageSetting.of_number("w", 2600), StageSetting.of_number("h", 190), StageSetting.of_text("color", "#2e261f") } },
                    new StageProp { kind = "kitchen", x = 1880, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 176) } },
                    new StageProp { kind = "sofa", x = 2160, y = 520 },
                    new StageProp { kind = "dog_basket", x = 2550, y = 520 },
                    new StageProp { kind = "lamp", x = 2200, y = 330, settings = new StageSetting[] { StageSetting.of_flag("hanging", true), StageSetting.of_number("radius", 150), StageSetting.of_flag("flicker", true) } },
                    new StageProp { kind = "window", x = 2700, y = 380, settings = new StageSetting[] { StageSetting.of_number("w", 70), StageSetting.of_number("h", 80), StageSetting.of_flag("glow", true), StageSetting.of_number("radius", 140), StageSetting.of_text("color", "rgba(150,175,210,0.06)") } },
                    new StageProp { kind = "lamp", x = 3100, y = 330, settings = new StageSetting[] { StageSetting.of_flag("hanging", true), StageSetting.of_number("radius", 150), StageSetting.of_flag("off", true) } },
                },
                things = new StageThingLook[]
                {
                    new StageThingLook { event_name = "sheets_taken", sprite = "", sound = "pickup", pose = "" },
                    new StageThingLook { event_name = "sheets_lowered", sprite = "", sound = "pickup", pose = "" },
                    new StageThingLook { event_name = "sheet_grabbed", sprite = "", sound = "", pose = "" },
                    new StageThingLook { event_name = "keepsake_frame", sprite = "keepsake", sound = "", pose = "" },
                    new StageThingLook { event_name = "keepsake_basket_toy", sprite = "keepsake", sound = "", pose = "" },
                    new StageThingLook { event_name = "keepsake_lead", sprite = "keepsake", sound = "", pose = "" },
                },
                rules = new RuleStaging[]
                {
                    new RuleStaging { pose = new[] { new StagePose { who = StageWho.human, pose = "sit", seconds = 6 }, new StagePose { who = StageWho.dog, pose = "sleep", seconds = 6 } }, camera = CameraChange.hold, camera_x = 520, camera_y = 240, camera_zoom = 1, caption = "Four months later" },
                    new RuleStaging { pose = new[] { new StagePose { who = StageWho.dog, pose = "bark", seconds = 0.8 } }, walk = new[] { new StageWalk { who = StageWho.dog, x = 1700 } }, camera = CameraChange.release, caption = "", shake_seconds = 0.8, effect = new StageEffect { kind = "lightning" } },
                    new RuleStaging { pose = new[] { new StagePose { who = StageWho.human, pose = "stand", seconds = 0.1 } }, walk = new[] { new StageWalk { who = StageWho.human, x = 1330 } } },
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.dog, x = 2420, feet_y = 520 } }, water = new[] { new StageWaterLevel { water_id = "flood", top = 498 } }, mood = "tense", sound = "collapse", shake_seconds = 1.4, effect = new StageEffect { kind = "debris", x = 1490, y = 400, has_place = true, width = 200 } },
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.human, x = 1330, feet_y = 300 }, new StagePlacement { who = StageWho.dog, x = 2420, feet_y = 520 } }, water = new[] { new StageWaterLevel { water_id = "flood", top = 498 } }, mood = "tense" },
                    new RuleStaging {  },
                    new RuleStaging {  },
                    new RuleStaging { scent = ScentChange.point, scent_x = 1385, scent_y = 280 },
                    new RuleStaging { scent = ScentChange.clear },
                    new RuleStaging {  },
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.dog, x = 1320, feet_y = 300 } }, sound = "pickup" },
                    new RuleStaging {  },
                    new RuleStaging { water = new[] { new StageWaterLevel { water_id = "flood", top = 335 } } },
                    new RuleStaging { darkness = 0.75 },
                    new RuleStaging {  },
                },
                aftermath = new StageAftermath { scene = "rescue_boat", mood = "quiet", rain = 1 },
            };
        }
    }
}
