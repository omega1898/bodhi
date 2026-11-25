// Purpose: how the chapter "The Other Side" looks: its floors, water, props and staging.
// First generated from the old browser game; since it was removed, this file is the original: edit it here.
namespace Bodhi.Game
{
    public static partial class ChapterStages
    {
        // Chapter 4: The Other Side.
        public static ChapterStage other_side()
        {
            return new ChapterStage
            {
                id = "other_side",
                width = 4000,
                height = 600,
                mood = "calm",
                scenery = new StageSetting[] { StageSetting.of_text("sky_top", "#0e1520"), StageSetting.of_text("sky_bottom", "#24303f"), StageSetting.of_text("far", "city"), StageSetting.of_text("mid", "station_roof"), StageSetting.of_text("art.far", "art/scenery/station_far.svg"), StageSetting.of_text("art.mid", "art/scenery/station_mid.svg"), StageSetting.of_text("art.near", "art/scenery/station_near.svg"), StageSetting.of_text("far_color", "#18202b"), StageSetting.of_text("mid_color", "#1d2632"), StageSetting.of_text("ground_top", "#4a4f57"), StageSetting.of_text("ground_body", "#22262c"), StageSetting.of_text("wall", "#2f3238"), StageSetting.of_text("room_wall", "#232830"), StageSetting.of_text("near", "near_columns"), StageSetting.of_text("near_color", "#121a25"), StageSetting.of_text("horizon_glow", "rgba(120,145,180,0.55)"), StageSetting.of_number("darkness", 0.64), StageSetting.of_text("darkness_color", "rgb(4,7,13)"), StageSetting.of_number("rain", 1), StageSetting.of_number("wind", 0.4), StageSetting.of_text("foreground", "pillars"), StageSetting.of_text("mist", "rgba(140,165,190,0.07)") },
                human_start_x = 560, human_start_feet_y = 300,
                dog_start_x = 640, dog_start_feet_y = 300,
                solids = new StageSolid[]
                {
                    new StageSolid { kind = "floor", x = 0, y = 300, width = 700, height = 320 },
                    new StageSolid { kind = "floor", x = 470, y = 262, width = 80, height = 38 },
                    new StageSolid { kind = "floor", x = 390, y = 222, width = 80, height = 78 },
                    new StageSolid { kind = "floor", x = 110, y = 182, width = 280, height = 118 },
                    new StageSolid { kind = "floor", x = 700, y = 300, width = 300, height = 30 },
                    new StageSolid { kind = "plank", id = "walkway", x = 1000, y = 300, width = 400, height = 12 },
                    new StageSolid { kind = "invisible", id = "edge_rail", x = 994, y = 120, width = 6, height = 180, blocks = SolidBlocks.human, hidden = true },
                    new StageSolid { kind = "plank", id = "gantry", x = 1000, y = 300, width = 400, height = 12, hidden = true },
                    new StageSolid { kind = "floor", x = 1400, y = 300, width = 1820, height = 30 },
                    new StageSolid { kind = "floor", x = 700, y = 520, width = 2500, height = 100 },
                    new StageSolid { kind = "floor", x = 3200, y = 520, width = 800, height = 100 },
                    new StageSolid { kind = "gate", id = "luggage_gate", x = 1500, y = 330, width = 20, height = 190 },
                    new StageSolid { kind = "suitcase", x = 1680, y = 488, width = 56, height = 32, color = "#6a3f35" },
                    new StageSolid { kind = "suitcase", x = 1736, y = 466, width = 44, height = 54, color = "#3f4f5a" },
                    new StageSolid { kind = "suitcase", x = 1920, y = 484, width = 64, height = 36, color = "#5b5236" },
                    new StageSolid { kind = "bars", id = "fence_gate", x = 3176, y = 330, width = 24, height = 190 },
                },
                water = new StageWater[]
                {
                    new StageWater { id = "flood", x = 2120, y = 494, width = 460, height = 26, flow = 1 },
                },
                props = new StageProp[]
                {
                    new StageProp { kind = "room_wall", x = 110, y = 60, settings = new StageSetting[] { StageSetting.of_number("w", 280), StageSetting.of_number("h", 122), StageSetting.of_text("color", "#2a2f2a") } },
                    new StageProp { kind = "sign", x = 160, y = 80, settings = new StageSetting[] { StageSetting.of_text("text", "SIGNALS"), StageSetting.of_text("color", "#3a2f22") } },
                    new StageProp { kind = "lamp", x = 250, y = 60, settings = new StageSetting[] { StageSetting.of_flag("hanging", true), StageSetting.of_flag("off", false), StageSetting.of_number("radius", 170), StageSetting.of_flag("flicker", true) } },
                    new StageProp { kind = "window", x = 1080, y = -20, settings = new StageSetting[] { StageSetting.of_flag("skylight", true), StageSetting.of_flag("glow", true), StageSetting.of_number("w", 70), StageSetting.of_number("h", 6), StageSetting.of_number("shaft_reach", 540), StageSetting.of_number("radius", 1), StageSetting.of_text("color", "rgba(150,175,210,0.04)") } },
                    new StageProp { kind = "window", x = 1900, y = -20, settings = new StageSetting[] { StageSetting.of_flag("skylight", true), StageSetting.of_flag("glow", true), StageSetting.of_number("w", 90), StageSetting.of_number("h", 6), StageSetting.of_number("shaft_reach", 320), StageSetting.of_number("radius", 1), StageSetting.of_text("color", "rgba(150,175,210,0.04)") } },
                    new StageProp { kind = "window", x = 2700, y = -20, settings = new StageSetting[] { StageSetting.of_flag("skylight", true), StageSetting.of_flag("glow", true), StageSetting.of_number("w", 60), StageSetting.of_number("h", 6), StageSetting.of_number("shaft_reach", 320), StageSetting.of_number("radius", 1), StageSetting.of_text("color", "rgba(150,175,210,0.04)") } },
                    new StageProp { kind = "sign", x = 560, y = 200, settings = new StageSetting[] { StageSetting.of_text("text", "PLATFORM 2"), StageSetting.of_text("color", "#1f3a4f") } },
                    new StageProp { kind = "lamp", x = 820, y = 150, settings = new StageSetting[] { StageSetting.of_number("radius", 200) } },
                    new StageProp { kind = "rail", x = 930, y = 300, settings = new StageSetting[] { StageSetting.of_number("w", 60), StageSetting.of_flag("front", true) } },
                    new StageProp { kind = "room_wall", x = 700, y = 330, settings = new StageSetting[] { StageSetting.of_number("w", 2500), StageSetting.of_number("h", 190), StageSetting.of_text("color", "#20252c") } },
                    new StageProp { kind = "train", x = 760, y = 520 },
                    new StageProp { kind = "sign", x = 1540, y = 350, settings = new StageSetting[] { StageSetting.of_text("text", "LEFT LUGGAGE"), StageSetting.of_text("color", "#3b3228") } },
                    new StageProp { kind = "lamp", x = 1800, y = 330, settings = new StageSetting[] { StageSetting.of_flag("hanging", true), StageSetting.of_number("radius", 150), StageSetting.of_flag("flicker", true) } },
                    new StageProp { kind = "pillar", x = 2100, y = 330, settings = new StageSetting[] { StageSetting.of_number("w", 22), StageSetting.of_number("h", 190) } },
                    new StageProp { kind = "pillar", x = 2600, y = 330, settings = new StageSetting[] { StageSetting.of_number("w", 22), StageSetting.of_number("h", 190) } },
                    new StageProp { kind = "chalk_heart", x = 2640, y = 440, settings = new StageSetting[] { StageSetting.of_number("arrow", 1) } },
                    new StageProp { kind = "sign", x = 2820, y = 350, settings = new StageSetting[] { StageSetting.of_text("text", "WAITING ROOM"), StageSetting.of_text("color", "#1f3a4f") } },
                    new StageProp { kind = "window", x = 2975, y = 345, settings = new StageSetting[] { StageSetting.of_number("w", 70), StageSetting.of_number("h", 80), StageSetting.of_flag("glow", true), StageSetting.of_number("radius", 160), StageSetting.of_text("color", "rgba(170,200,230,0.10)") } },
                    new StageProp { kind = "bench", x = 2840, y = 520 },
                    new StageProp { kind = "lamp", x = 1700, y = 160, settings = new StageSetting[] { StageSetting.of_number("radius", 220) } },
                    new StageProp { kind = "lamp", x = 2500, y = 160, settings = new StageSetting[] { StageSetting.of_number("radius", 220) } },
                    new StageProp { kind = "sign", x = 2950, y = 200, settings = new StageSetting[] { StageSetting.of_text("text", "MAINTENANCE  >"), StageSetting.of_text("color", "#3a2f22") } },
                    new StageProp { kind = "room_wall", x = 3700, y = 400, settings = new StageSetting[] { StageSetting.of_number("w", 260), StageSetting.of_number("h", 120), StageSetting.of_text("color", "#3b2f27") } },
                    new StageProp { kind = "window", x = 3760, y = 420, settings = new StageSetting[] { StageSetting.of_number("w", 50), StageSetting.of_number("h", 50), StageSetting.of_flag("lit", true), StageSetting.of_number("radius", 260) } },
                    new StageProp { kind = "lamp", x = 3480, y = 400, settings = new StageSetting[] { StageSetting.of_number("radius", 180) } },
                },
                things = new StageThingLook[]
                {
                    new StageThingLook { event_name = "power_restored", sprite = "breaker", sound = "lever", pose = "" },
                    new StageThingLook { event_name = "glove_found", sprite = "glove", sound = "pickup", pose = "" },
                    new StageThingLook { event_name = "dog_barked", sprite = "", sound = "bark", pose = "bark" },
                    new StageThingLook { event_name = "fence_touched", sprite = "", sound = "", pose = "" },
                    new StageThingLook { event_name = "lever_pulled", sprite = "lever", sound = "lever", pose = "" },
                    new StageThingLook { event_name = "debris_cleared", sprite = "debris", sound = "thud", pose = "sniff" },
                    new StageThingLook { event_name = "keepsake_ticket", sprite = "keepsake", sound = "", pose = "" },
                    new StageThingLook { event_name = "keepsake_luggage_tag", sprite = "keepsake", sound = "", pose = "" },
                    new StageThingLook { event_name = "keepsake_bench_scratches", sprite = "keepsake", sound = "", pose = "" },
                },
                rules = new RuleStaging[]
                {
                    new RuleStaging {  },
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.dog, x = 1200, feet_y = 240 }, new StagePlacement { who = StageWho.human, x = 960, feet_y = 300 } }, mood = "tense", sound = "collapse", shake_seconds = 1.2, effect = new StageEffect { kind = "debris", x = 1200, y = 300, has_place = true, width = 400 } },
                    new RuleStaging {  },
                    new RuleStaging {  },
                    new RuleStaging { scent = ScentChange.point, scent_x = 2250, scent_y = 500, sound = "gate" },
                    new RuleStaging {  },
                    new RuleStaging { scent = ScentChange.clear },
                    new RuleStaging { scent = ScentChange.point, scent_x = 3010, scent_y = 480, carry = CarryChange.carry, carry_item = "glove" },
                    new RuleStaging { mood = "calm", scent = ScentChange.clear },
                    new RuleStaging {  },
                    new RuleStaging { carry = CarryChange.drop },
                    new RuleStaging {  },
                    new RuleStaging {  },
                    new RuleStaging { sound = "gate" },
                    new RuleStaging {  },
                    new RuleStaging { mood = "calm" },
                    new RuleStaging {  },
                    new RuleStaging {  },
                },
                aftermath = new StageAftermath { scene = "cabin_fire", mood = "quiet", rain = 0.5 },
            };
        }
    }
}
