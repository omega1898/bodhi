// Purpose: how the chapter "Visiting Hours" looks: its floors, water, props and staging.
// First generated from the old browser game; since it was removed, this file is the original: edit it here.
namespace Bodhi.Game
{
    public static partial class ChapterStages
    {
        // Chapter 6: Visiting Hours.
        public static ChapterStage visiting_hours()
        {
            return new ChapterStage
            {
                id = "visiting_hours",
                width = 3800,
                height = 600,
                mood = "quiet",
                scenery = new StageSetting[] { StageSetting.of_text("sky_top", "#0b1018"), StageSetting.of_text("sky_bottom", "#1a2230"), StageSetting.of_text("far", "hospital_blocks"), StageSetting.of_text("mid", "corridor"), StageSetting.of_text("far_color", "#141b24"), StageSetting.of_text("mid_color", "#1b232c"), StageSetting.of_text("window_color", "rgba(170,220,210,0.25)"), StageSetting.of_text("ground_top", "#7d8a86"), StageSetting.of_text("ground_body", "#2b302f"), StageSetting.of_text("wall", "#3a4442"), StageSetting.of_text("room_wall", "#26302f"), StageSetting.of_text("near", "near_doorways"), StageSetting.of_text("near_color", "#131b1d"), StageSetting.of_text("horizon_glow", "rgba(110,150,150,0.4)"), StageSetting.of_number("darkness", 0.68), StageSetting.of_text("darkness_color", "rgb(3,9,11)"), StageSetting.of_number("rain", 0), StageSetting.of_number("rain_sound", 0.35), StageSetting.of_number("wind", 0.1), StageSetting.of_flag("dust", true), StageSetting.of_text("foreground", "beams") },
                human_start_x = 420, human_start_feet_y = 300,
                dog_start_x = 500, dog_start_feet_y = 300,
                solids = new StageSolid[]
                {
                    new StageSolid { kind = "floor", x = 0, y = 300, width = 600, height = 320 },
                    new StageSolid { kind = "floor", x = 600, y = 300, width = 2200, height = 30 },
                    new StageSolid { kind = "floor", x = 2880, y = 300, width = 920, height = 30 },
                    new StageSolid { kind = "gate", id = "fire_door", x = 1300, y = 150, width = 20, height = 150, hidden = true },
                    new StageSolid { kind = "gate", id = "lift_door_upper", x = 2300, y = 150, width = 20, height = 150 },
                    new StageSolid { kind = "floor", x = 600, y = 520, width = 2160, height = 100 },
                    new StageSolid { kind = "floor", x = 2760, y = 520, width = 1040, height = 100 },
                    new StageSolid { kind = "gate", id = "basement_door", x = 1000, y = 330, width = 20, height = 190 },
                    new StageSolid { kind = "wall", x = 1600, y = 330, width = 40, height = 136 },
                    new StageSolid { kind = "glass", id = "lift_glass", x = 2740, y = 330, width = 20, height = 190 },
                },
                water = new StageWater[]
                {
                },
                props = new StageProp[]
                {
                    new StageProp { kind = "room_wall", x = 0, y = 120, settings = new StageSetting[] { StageSetting.of_number("w", 2800), StageSetting.of_number("h", 180), StageSetting.of_text("color", "#28322f") } },
                    new StageProp { kind = "sign", x = 140, y = 150, settings = new StageSetting[] { StageSetting.of_text("text", "CHILDREN'S WING"), StageSetting.of_text("color", "#2f4a5a") } },
                    new StageProp { kind = "lamp", x = 300, y = 120, settings = new StageSetting[] { StageSetting.of_flag("hanging", true), StageSetting.of_number("radius", 190), StageSetting.of_flag("flicker", true), StageSetting.of_text("color", "rgba(170,230,220,0.10)") } },
                    new StageProp { kind = "bed", x = 640, y = 300 },
                    new StageProp { kind = "drawing", x = 780, y = 200 },
                    new StageProp { kind = "lamp", x = 1000, y = 120, settings = new StageSetting[] { StageSetting.of_flag("hanging", true), StageSetting.of_number("radius", 190), StageSetting.of_text("color", "rgba(170,230,220,0.10)") } },
                    new StageProp { kind = "bed", x = 1450, y = 300 },
                    new StageProp { kind = "lamp", x = 1700, y = 120, settings = new StageSetting[] { StageSetting.of_flag("hanging", true), StageSetting.of_number("radius", 190), StageSetting.of_flag("flicker", true), StageSetting.of_text("color", "rgba(170,230,220,0.10)") } },
                    new StageProp { kind = "sign", x = 1830, y = 160, settings = new StageSetting[] { StageSetting.of_text("text", "SECURITY"), StageSetting.of_text("color", "#4a2f2f") } },
                    new StageProp { kind = "window", x = 2050, y = 170, settings = new StageSetting[] { StageSetting.of_number("w", 70), StageSetting.of_number("h", 80), StageSetting.of_flag("glow", true), StageSetting.of_number("radius", 140), StageSetting.of_text("color", "rgba(170,200,230,0.10)") } },
                    new StageProp { kind = "sign", x = 2340, y = 160, settings = new StageSetting[] { StageSetting.of_text("text", "LIFTS"), StageSetting.of_text("color", "#2f4a5a") } },
                    new StageProp { kind = "room_wall", x = 600, y = 330, settings = new StageSetting[] { StageSetting.of_number("w", 3200), StageSetting.of_number("h", 190), StageSetting.of_text("color", "#1f2524") } },
                    new StageProp { kind = "sign", x = 640, y = 350, settings = new StageSetting[] { StageSetting.of_text("text", "STAIRS  B"), StageSetting.of_text("color", "#2f4a5a") } },
                    new StageProp { kind = "lamp", x = 1300, y = 330, settings = new StageSetting[] { StageSetting.of_flag("hanging", true), StageSetting.of_number("radius", 140), StageSetting.of_flag("flicker", true), StageSetting.of_text("color", "rgba(170,230,220,0.08)") } },
                    new StageProp { kind = "sign", x = 1700, y = 350, settings = new StageSetting[] { StageSetting.of_text("text", "STORES"), StageSetting.of_text("color", "#3b3228") } },
                    new StageProp { kind = "drawing", x = 2200, y = 470, settings = new StageSetting[] { StageSetting.of_text("color", "#3c6fa8") } },
                    new StageProp { kind = "lamp", x = 2450, y = 330, settings = new StageSetting[] { StageSetting.of_flag("hanging", true), StageSetting.of_number("radius", 160), StageSetting.of_text("color", "rgba(170,230,220,0.10)") } },
                    new StageProp { kind = "sign", x = 2540, y = 350, settings = new StageSetting[] { StageSetting.of_text("text", "LIFT  B"), StageSetting.of_text("color", "#2f4a5a") } },
                    new StageProp { kind = "lamp", x = 3200, y = 330, settings = new StageSetting[] { StageSetting.of_flag("hanging", true), StageSetting.of_number("radius", 200) } },
                    new StageProp { kind = "sign", x = 3450, y = 360, settings = new StageSetting[] { StageSetting.of_text("text", "CHILDREN'S WARD  >"), StageSetting.of_text("color", "#2f4a5a") } },
                },
                things = new StageThingLook[]
                {
                    new StageThingLook { event_name = "note_read", sprite = "note", sound = "", pose = "" },
                    new StageThingLook { event_name = "basement_unlocked", sprite = "button", sound = "chime", pose = "" },
                    new StageThingLook { event_name = "lift_called", sprite = "button", sound = "chime", pose = "jump" },
                    new StageThingLook { event_name = "glass_touched", sprite = "", sound = "", pose = "" },
                    new StageThingLook { event_name = "crank_turned", sprite = "crank", sound = "lever", pose = "" },
                    new StageThingLook { event_name = "cord_pulled", sprite = "cord", sound = "lever", pose = "" },
                    new StageThingLook { event_name = "keepsake_get_well_card", sprite = "keepsake", sound = "", pose = "" },
                    new StageThingLook { event_name = "keepsake_pet_bracelet", sprite = "keepsake", sound = "", pose = "" },
                    new StageThingLook { event_name = "keepsake_crayon_dog", sprite = "keepsake", sound = "", pose = "" },
                },
                rules = new RuleStaging[]
                {
                    new RuleStaging {  },
                    new RuleStaging {  },
                    new RuleStaging { place = new[] { new StagePlacement { who = StageWho.dog, x = 820, feet_y = 520 } }, mood = "tense", sound = "thud", shake_seconds = 0.6 },
                    new RuleStaging {  },
                    new RuleStaging { scent = ScentChange.point, scent_x = 2500, scent_y = 500, sound = "gate" },
                    new RuleStaging { carry = CarryChange.carry, carry_item = "rabbit" },
                    new RuleStaging { scent = ScentChange.clear },
                    new RuleStaging {  },
                    new RuleStaging {  },
                    new RuleStaging { sound = "gate" },
                    new RuleStaging {  },
                    new RuleStaging { mood = "calm" },
                    new RuleStaging {  },
                    new RuleStaging {  },
                    new RuleStaging {  },
                },
                aftermath = new StageAftermath { scene = "ward_stars", mood = "quiet", rain = 0.3 },
            };
        }
    }
}
