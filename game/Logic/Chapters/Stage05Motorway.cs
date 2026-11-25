// Purpose: how the chapter "The Motorway" looks: its floors, water, props and staging.
// First generated from the old browser game; since it was removed, this file is the original: edit it here.
namespace Bodhi.Game
{
    public static partial class ChapterStages
    {
        // Chapter 5: The Motorway.
        public static ChapterStage motorway()
        {
            return new ChapterStage
            {
                id = "motorway",
                width = 4200,
                height = 600,
                mood = "calm",
                scenery = new StageSetting[] { StageSetting.of_text("sky_top", "#5f6f86"), StageSetting.of_text("sky_bottom", "#c8b8a4"), StageSetting.of_text("far", "hills"), StageSetting.of_nothing("mid"), StageSetting.of_text("far_color", "#7a808a"), StageSetting.of_text("mid_color", "#5d646c"), StageSetting.of_text("ground_top", "#55595e"), StageSetting.of_text("ground_body", "#2f3236"), StageSetting.of_text("wall", "#5a5f66"), StageSetting.of_text("room_wall", "#3e4349"), StageSetting.of_text("near", "near_hedges"), StageSetting.of_text("near_color", "#3f4a3a"), StageSetting.of_number("darkness", 0.12), StageSetting.of_text("darkness_color", "rgb(30,32,44)"), StageSetting.of_number("rain", 0.25), StageSetting.of_number("wind", 0.3), StageSetting.of_text("mist", "rgba(210,214,220,0.07)"), StageSetting.of_flag("wet", true) },
                human_start_x = 430, human_start_feet_y = 300,
                dog_start_x = 510, dog_start_feet_y = 300,
                solids = new StageSolid[]
                {
                    new StageSolid { kind = "floor", x = 0, y = 300, width = 700, height = 320 },
                    new StageSolid { kind = "floor", x = 700, y = 300, width = 600, height = 30 },
                    new StageSolid { kind = "plank", id = "deck_gap", x = 1300, y = 300, width = 300, height = 12 },
                    new StageSolid { kind = "invisible", id = "edge_rail", x = 1294, y = 120, width = 6, height = 180, blocks = SolidBlocks.human, hidden = true },
                    new StageSolid { kind = "plank", id = "ladder_bridge", x = 1300, y = 300, width = 300, height = 10, hidden = true },
                    new StageSolid { kind = "floor", x = 1600, y = 300, width = 1450, height = 30 },
                    new StageSolid { kind = "floor", x = 700, y = 520, width = 3500, height = 100 },
                    new StageSolid { kind = "invisible", x = 1660, y = 480, width = 140, height = 40 },
                    new StageSolid { kind = "invisible", x = 2260, y = 480, width = 140, height = 40 },
                    new StageSolid { kind = "invisible", x = 2660, y = 480, width = 140, height = 40 },
                    new StageSolid { kind = "bars", id = "slip_gate", x = 3000, y = 330, width = 24, height = 190 },
                },
                water = new StageWater[]
                {
                    new StageWater { id = "flood", x = 1300, y = 500, width = 1500, height = 20, flow = 0.5 },
                },
                props = new StageProp[]
                {
                    new StageProp { kind = "crash_barrier", x = 700, y = 300, settings = new StageSetting[] { StageSetting.of_number("w", 600) } },
                    new StageProp { kind = "car", x = 760, y = 300, settings = new StageSetting[] { StageSetting.of_text("color", "#7a2f2a"), StageSetting.of_flag("wreck", true) } },
                    new StageProp { kind = "chalk_heart", x = 810, y = 268, settings = new StageSetting[] { StageSetting.of_number("arrow", 1) } },
                    new StageProp { kind = "car", x = 950, y = 300, settings = new StageSetting[] { StageSetting.of_text("color", "#c9a23c") } },
                    new StageProp { kind = "scrawl", x = 975, y = 220, settings = new StageSetting[] { StageSetting.of_text("text", "BREAKDOWN"), StageSetting.of_text("color", "rgba(40,40,40,0.6)") } },
                    new StageProp { kind = "crash_barrier", x = 1600, y = 300, settings = new StageSetting[] { StageSetting.of_number("w", 1450) } },
                    new StageProp { kind = "car", x = 1700, y = 300, settings = new StageSetting[] { StageSetting.of_text("color", "#3f5a6e"), StageSetting.of_flag("wreck", true) } },
                    new StageProp { kind = "chalk_heart", x = 1750, y = 268, settings = new StageSetting[] { StageSetting.of_number("arrow", 1) } },
                    new StageProp { kind = "road_sign", x = 1950, y = 300, settings = new StageSetting[] { StageSetting.of_text("text", "WEST\nRiver crossing  40") } },
                    new StageProp { kind = "car", x = 2350, y = 300, settings = new StageSetting[] { StageSetting.of_text("color", "#5a6a4a"), StageSetting.of_flag("wreck", true) } },
                    new StageProp { kind = "chalk_heart", x = 2400, y = 268, settings = new StageSetting[] { StageSetting.of_number("arrow", 1) } },
                    new StageProp { kind = "car", x = 2700, y = 300, settings = new StageSetting[] { StageSetting.of_text("color", "#6a5a7a"), StageSetting.of_flag("wreck", true) } },
                    new StageProp { kind = "room_wall", x = 700, y = 330, settings = new StageSetting[] { StageSetting.of_number("w", 2300), StageSetting.of_number("h", 190), StageSetting.of_text("color", "#2b2f34") } },
                    new StageProp { kind = "pillar", x = 1000, y = 330, settings = new StageSetting[] { StageSetting.of_number("w", 26), StageSetting.of_number("h", 190) } },
                    new StageProp { kind = "pillar", x = 2000, y = 330, settings = new StageSetting[] { StageSetting.of_number("w", 26), StageSetting.of_number("h", 190) } },
                    new StageProp { kind = "car", x = 1650, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#6a3f35"), StageSetting.of_flag("wreck", true) } },
                    new StageProp { kind = "car", x = 2250, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#4a5a6a"), StageSetting.of_flag("wreck", true) } },
                    new StageProp { kind = "car", x = 2650, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#5b5236"), StageSetting.of_flag("wreck", true) } },
                    new StageProp { kind = "lamp", x = 3400, y = 400, settings = new StageSetting[] { StageSetting.of_number("radius", 200) } },
                    new StageProp { kind = "sign", x = 3500, y = 340, settings = new StageSetting[] { StageSetting.of_text("text", "WEST  >"), StageSetting.of_text("color", "#1f3a4f") } },
                    new StageProp { kind = "tree", x = 3800, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#4b5a42") } },
                },
                things = new StageThingLook[]
                {
                    new StageThingLook { event_name = "ladder_taken", sprite = "", sound = "pickup", pose = "" },
                    new StageThingLook { event_name = "ladder_laid", sprite = "", sound = "thud", pose = "" },
                    new StageThingLook { event_name = "mina_note", sprite = "note", sound = "", pose = "" },
                    new StageThingLook { event_name = "car_food", sprite = "", sound = "pickup", pose = "" },
                    new StageThingLook { event_name = "dog_barked", sprite = "", sound = "bark", pose = "bark" },
                    new StageThingLook { event_name = "barrier_touched", sprite = "", sound = "", pose = "" },
                    new StageThingLook { event_name = "crank_turned", sprite = "crank", sound = "lever", pose = "" },
                    new StageThingLook { event_name = "gravel_dug", sprite = "debris", sound = "thud", pose = "sniff" },
                    new StageThingLook { event_name = "keepsake_toy_car", sprite = "keepsake", sound = "", pose = "" },
                    new StageThingLook { event_name = "keepsake_dog_bed", sprite = "keepsake", sound = "", pose = "" },
                    new StageThingLook { event_name = "keepsake_windscreen", sprite = "keepsake", sound = "", pose = "" },
                },
                rules = new RuleStaging[]
                {
                    new RuleStaging {  },
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.dog, x = 1460, feet_y = 520 }, new StagePlacement { who = StageWho.human, x = 1260, feet_y = 300 } }, mood = "tense", sound = "collapse", shake_seconds = 1.2, effect = new StageEffect { kind = "debris", x = 1450, y = 300, has_place = true, width = 300 } },
                    new RuleStaging {  },
                    new RuleStaging {  },
                    new RuleStaging { scent = ScentChange.point, scent_x = 2960, scent_y = 500 },
                    new RuleStaging {  },
                    new RuleStaging {  },
                    new RuleStaging {  },
                    new RuleStaging { scent = ScentChange.clear },
                    new RuleStaging {  },
                    new RuleStaging {  },
                    new RuleStaging {  },
                    new RuleStaging {  },
                    new RuleStaging { sound = "gate" },
                    new RuleStaging {  },
                    new RuleStaging { mood = "calm" },
                    new RuleStaging {  },
                    new RuleStaging {  },
                },
                aftermath = new StageAftermath { scene = "camper_van", mood = "quiet", rain = 0.2 },
            };
        }
    }
}
