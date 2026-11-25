// Purpose: how the chapter "High Farm" looks: its floors, water, props and staging.
// First generated from the old browser game; since it was removed, this file is the original: edit it here.
namespace Bodhi.Game
{
    public static partial class ChapterStages
    {
        // Chapter 11: High Farm.
        public static ChapterStage high_farm()
        {
            return new ChapterStage
            {
                id = "high_farm",
                width = 4000,
                height = 600,
                mood = "hopeful",
                scenery = new StageSetting[] { StageSetting.of_text("sky_top", "#0b0f1a"), StageSetting.of_text("sky_bottom", "#2a2a3a"), StageSetting.of_text("far", "river_hills"), StageSetting.of_nothing("mid"), StageSetting.of_text("far_color", "#151a24"), StageSetting.of_text("mid_color", "#1b2028"), StageSetting.of_text("ground_top", "#4a4a36"), StageSetting.of_text("ground_body", "#26241a"), StageSetting.of_text("wall", "#3a3428"), StageSetting.of_text("room_wall", "#2e2a22"), StageSetting.of_text("near", "near_hedges"), StageSetting.of_text("near_color", "#151a14"), StageSetting.of_text("horizon_glow", "rgba(255,170,100,0.25)"), StageSetting.of_number("darkness", 0.62), StageSetting.of_text("darkness_color", "rgb(6,7,14)"), StageSetting.of_number("rain", 0), StageSetting.of_number("wind", 0.3), StageSetting.of_flag("fireflies", true), StageSetting.of_text("darkness_floor", "#1c1a12"), StageSetting.of_text("foreground", "grass"), StageSetting.of_text("foreground_color", "rgba(30,32,20,0.85)") },
                human_start_x = 200, human_start_feet_y = 520,
                dog_start_x = 280, dog_start_feet_y = 520,
                guest = new StageGuest { x = 3150, feet_y = 400, size = 0.86, mode = "still", pose = "sleep" },
                solids = new StageSolid[]
                {
                    new StageSolid { kind = "floor", x = 0, y = 520, width = 1500, height = 100 },
                    new StageSolid { kind = "gate", id = "farm_gate", x = 1480, y = 410, width = 16, height = 110, blocks = SolidBlocks.human },
                    new StageSolid { kind = "floor", x = 1500, y = 490, width = 300, height = 130 },
                    new StageSolid { kind = "floor", x = 1800, y = 460, width = 300, height = 160 },
                    new StageSolid { kind = "floor", x = 2100, y = 430, width = 300, height = 190 },
                    new StageSolid { kind = "floor", x = 2400, y = 400, width = 1600, height = 220 },
                    new StageSolid { kind = "wall", id = "byre_beam", x = 3060, y = 330, width = 50, height = 70 },
                },
                water = new StageWater[]
                {
                },
                props = new StageProp[]
                {
                    new StageProp { kind = "tent", x = 260, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#5a6a4a") } },
                    new StageProp { kind = "scrawl", x = 420, y = 430, settings = new StageSetting[] { StageSetting.of_text("text", "HIGH FARM  -  all welcome  -  dogs too"), StageSetting.of_text("color", "rgba(239,230,212,0.55)") } },
                    new StageProp { kind = "people", x = 640, y = 520, settings = new StageSetting[] { StageSetting.of_number("count", 4) } },
                    new StageProp { kind = "fire", x = 860, y = 520, settings = new StageSetting[] { StageSetting.of_number("size", 1.6), StageSetting.of_number("radius", 320) } },
                    new StageProp { kind = "people", x = 960, y = 520, settings = new StageSetting[] { StageSetting.of_number("count", 3) } },
                    new StageProp { kind = "tent", x = 1150, y = 520, settings = new StageSetting[] { StageSetting.of_text("color", "#6b5a4a") } },
                    new StageProp { kind = "lamp", x = 1350, y = 400, settings = new StageSetting[] { StageSetting.of_number("radius", 160) } },
                    new StageProp { kind = "tree", x = 1700, y = 490, settings = new StageSetting[] { StageSetting.of_text("color", "#1c251c") } },
                    new StageProp { kind = "tree", x = 2300, y = 430, settings = new StageSetting[] { StageSetting.of_text("color", "#202a20") } },
                    new StageProp { kind = "room_wall", x = 2700, y = 220, settings = new StageSetting[] { StageSetting.of_number("w", 900), StageSetting.of_number("h", 180), StageSetting.of_text("color", "#2e2a22") } },
                    new StageProp { kind = "tree", x = 3800, y = 400, settings = new StageSetting[] { StageSetting.of_text("color", "#1c251c") } },
                },
                things = new StageThingLook[]
                {
                    new StageThingLook { event_name = "stew_shared", sprite = "bowl", sound = "", pose = "" },
                    new StageThingLook { event_name = "barked_for_help", sprite = "", sound = "bark", pose = "bark" },
                    new StageThingLook { event_name = "gate_opened", sprite = "", sound = "gate", pose = "" },
                    new StageThingLook { event_name = "beam_levered", sprite = "lever", sound = "lever", pose = "" },
                    new StageThingLook { event_name = "biscuit_dug", sprite = "debris", sound = "thud", pose = "sniff" },
                    new StageThingLook { event_name = "keepsake_welcome_board", sprite = "keepsake", sound = "", pose = "" },
                    new StageThingLook { event_name = "keepsake_old_kennel", sprite = "keepsake", sound = "", pose = "" },
                    new StageThingLook { event_name = "keepsake_hilltop", sprite = "keepsake", sound = "", pose = "" },
                },
                rules = new RuleStaging[]
                {
                    new RuleStaging { walk = new[] { new StageWalk { who = StageWho.human, x = 600 }, new StageWalk { who = StageWho.dog, x = 660 } }, camera = CameraChange.hold, camera_x = 760, camera_y = 430, camera_zoom = 0.95 },
                    new RuleStaging {  },
                    new RuleStaging {  },
                    new RuleStaging { pose = new[] { new StagePose { who = StageWho.human, pose = "kneel", seconds = 3 } } },
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.human, x = 600, feet_y = 520 }, new StagePlacement { who = StageWho.dog, x = 660, feet_y = 520 } }, camera = CameraChange.release },
                    new RuleStaging {  },
                    new RuleStaging { mood = "tense", scent = ScentChange.point, scent_x = 3120, scent_y = 380 },
                    new RuleStaging {  },
                    new RuleStaging { scent = ScentChange.clear },
                    new RuleStaging {  },
                    new RuleStaging {  },
                    new RuleStaging {  },
                    new RuleStaging {  },
                    new RuleStaging { pose = new[] { new StagePose { who = StageWho.guest, pose = "stand", seconds = 0.1 } }, mood = "tender", guest_mode = "follow", sound = "chime" },
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.guest, x = 900, feet_y = 520 } }, pose = new[] { new StagePose { who = StageWho.guest, pose = "lean", seconds = 9999 } }, guest_mode = "still" },
                    new RuleStaging {  },
                    new RuleStaging {  },
                },
                aftermath = new StageAftermath { scene = "farm_fire", mood = "tender", rain = 0 },
            };
        }
    }
}
