// Purpose: how Chapter Eleven of the Christmas story, "Christmas Eve", looks: the high path in a
// white-out with no warm light at all once the lantern goes out, the slope where the storm thins
// over a village with every window lit, the iced footbridge over the burn, and the pines down to
// the cabin porch, Mina's torch and Em's lamp; and what each story rule does on screen. The story
// is rules/Runtime/Christmas/Christmas11ChristmasEve.cs.
//
// Left to right, in layout units: the high path (0 to 2400), the slope (2400 to 3200), the
// footbridge (3200 to 3600) and the pines and porch (3600 to 4400). The floor is at y = 520.
// Everything is a grey box until the 3D scenery is built.
namespace Bodhi.Game
{
    public static partial class ChristmasStages
    {
        // Chapter Eleven: Christmas Eve.
        public static ChapterStage christmas_eve()
        {
            var post = StageSetting.of_text("color", "#4a4a4e");
            var candle = StageSetting.of_text("color", "rgba(255,200,120,0.16)");
            var pine = StageSetting.of_text("color", "#1f2b2a");
            return new ChapterStage
            {
                id = "christmas_eve",
                width = 4400,
                height = 600,
                mood = "tense",
                scenery = new StageSetting[]
                {
                    StageSetting.of_text("sky_top", "#c8ccd2"), StageSetting.of_text("sky_bottom", "#e8eaee"),
                    StageSetting.of_text("far", "hills"), StageSetting.of_text("far_color", "#d4d8de"),
                    StageSetting.of_text("ground_top", "#eef0f2"), StageSetting.of_text("ground_body", "#c4c8d0"),
                    StageSetting.of_text("wall", "#5a4a3c"), StageSetting.of_text("room_wall", "#6e5844"),
                    StageSetting.of_number("darkness", 0.35), StageSetting.of_text("darkness_color", "rgb(30,34,48)"),
                    StageSetting.of_text("darkness_floor", "#8a8e96"), StageSetting.of_number("snow", 1),
                    StageSetting.of_number("wind", 1.0), StageSetting.of_text("mist", "rgba(240,242,246,0.3)"),
                },
                human_start_x = 300, human_start_feet_y = 520,
                dog_start_x = 380, dog_start_feet_y = 520,
                guest = new StageGuest { x = 240, feet_y = 520, size = 0.8, mode = "follow", pose = "" },
                solids = new StageSolid[]
                {
                    new StageSolid { kind = "floor", x = 0, y = 520, width = 4400, height = 100 },
                    new StageSolid { kind = "invisible", x = 0, y = 0, width = 10, height = 520 },
                    new StageSolid { id = "footbridge_ice", kind = "invisible", x = 3450, y = 0, width = 10, height = 520 },
                    new StageSolid { kind = "invisible", x = 4390, y = 0, width = 10, height = 520 },
                },
                props = new StageProp[]
                {
                    // The high path's marker posts, half buried.
                    new StageProp { kind = "pillar", x = 300, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 12), StageSetting.of_number("h", 110), post } },
                    new StageProp { kind = "pillar", x = 820, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 12), StageSetting.of_number("h", 110), post } },
                    new StageProp { kind = "pillar", x = 1200, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 12), StageSetting.of_number("h", 110), post } },
                    new StageProp { kind = "snow_drift", x = 1500, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 240), StageSetting.of_number("h", 50) } },
                    new StageProp { kind = "pillar", x = 1900, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 12), StageSetting.of_number("h", 110), post } },
                    // The village below, every window lit by hand.
                    new StageProp { kind = "lamp", x = 2700, y = 560, after = "village_lights", settings = new StageSetting[] { StageSetting.of_number("h", 20), StageSetting.of_number("radius", 90), candle } },
                    new StageProp { kind = "lamp", x = 2820, y = 570, after = "village_lights", settings = new StageSetting[] { StageSetting.of_number("h", 20), StageSetting.of_number("radius", 90), candle } },
                    new StageProp { kind = "lamp", x = 2950, y = 555, after = "village_lights", settings = new StageSetting[] { StageSetting.of_number("h", 20), StageSetting.of_number("radius", 90), candle } },
                    new StageProp { kind = "lamp", x = 3080, y = 575, after = "village_lights", settings = new StageSetting[] { StageSetting.of_number("h", 20), StageSetting.of_number("radius", 90), candle } },
                    // The iced footbridge over the burn.
                    new StageProp { kind = "rail", x = 3220, y = 470, settings = new StageSetting[] { StageSetting.of_number("w", 300), StageSetting.of_number("h", 8), StageSetting.of_text("color", "#cfe0ea") } },
                    new StageProp { kind = "rail", x = 3220, y = 510, settings = new StageSetting[] { StageSetting.of_number("w", 300), StageSetting.of_number("h", 10), StageSetting.of_text("color", "#5a4a3a") } },
                    // The pines, the cabin, its lamp, and Mina's torch on the porch.
                    new StageProp { kind = "tree", x = 3700, y = 520, settings = new StageSetting[] { pine } },
                    new StageProp { kind = "tree", x = 3950, y = 520, settings = new StageSetting[] { pine } },
                    new StageProp { kind = "house", x = 4080, y = 520, settings = new StageSetting[] { StageSetting.of_number("w", 300), StageSetting.of_text("color", "#4a3b2e") } },
                    new StageProp { kind = "window", x = 4260, y = 300, settings = new StageSetting[] { StageSetting.of_number("w", 80), StageSetting.of_number("h", 80) } },
                    new StageProp { kind = "lamp", x = 4300, y = 380, settings = new StageSetting[] { StageSetting.of_number("h", 30), StageSetting.of_number("radius", 380), StageSetting.of_text("color", "rgba(255,206,150,0.26)") } },
                    new StageProp { kind = "lamp", x = 4120, y = 520, after = "porch_seen", settings = new StageSetting[] { StageSetting.of_number("h", 60), StageSetting.of_number("radius", 160), StageSetting.of_text("color", "rgba(255,230,190,0.18)") } },
                },
                things = new StageThingLook[]
                {
                    new StageThingLook { event_name = "old_scent_1", sound = "sniff", pose = "sniff" },
                    new StageThingLook { event_name = "keepsake_drovers_wool", sprite = "keepsake", sound = "sniff" },
                    new StageThingLook { event_name = "old_scent_2", sound = "sniff", pose = "sniff" },
                    new StageThingLook { event_name = "keepsake_far_candle", sprite = "keepsake" },
                    new StageThingLook { event_name = "handrail_cleared", sound = "thud" },
                    new StageThingLook { event_name = "rope_across", pose = "stop" },
                    new StageThingLook { event_name = "keepsake_vicars_note", sprite = "keepsake" },
                },
                // One entry per story rule, in the same order; the comment names the rule.
                rules = new RuleStaging[]
                {
                    new RuleStaging { camera = CameraChange.hold, camera_x = 350, camera_y = 420, camera_zoom = 0.95, caption = "Christmas Eve" },  // 0 level_started
                    new RuleStaging { },  // 1 open_1
                    new RuleStaging { },  // 2 open_2
                    new RuleStaging { camera = CameraChange.release, caption = "" },  // 3 white_out
                    new RuleStaging { darkness = 0.85, shake_seconds = 0.4, mood = "tense" },  // 4 lantern_out: no warm light at all
                    new RuleStaging { },  // 5 he_leads
                    new RuleStaging { },  // 6 old_scent_1
                    new RuleStaging { },  // 7 old_scent_2 (his)
                    new RuleStaging { sound = "whistle" },  // 8 old_scent_2 (hers): her whistle behind him
                    new RuleStaging { },  // 9 his bark back
                    new RuleStaging { sound = "bell" },  // 10 mercy_heard
                    new RuleStaging { mood = "hopeful" },  // 11 mercy_named
                    new RuleStaging { camera = CameraChange.hold, camera_x = 3000, camera_y = 380, camera_zoom = 0.75, darkness = 0.7, wind = 0.5, sound = "bell" },  // 12 village_lights
                    new RuleStaging { camera = CameraChange.release },  // 13 footbridge_reached
                    new RuleStaging { },  // 14 rope_across
                    new RuleStaging { },  // 15 handrail_cleared
                    new RuleStaging { wind = 0.35 },  // 16 over the footbridge
                    new RuleStaging { camera = CameraChange.hold, camera_x = 4150, camera_y = 420, camera_zoom = 0.95, wind = 0.2, mood = "tender" },  // 17 porch_seen
                    new RuleStaging { pose = new[] { new StagePose { who = StageWho.guest, pose = "bark", seconds = 1 } }, sound = "chime" },  // 18 bell_first: his bell, then Biscuit's bark
                    new RuleStaging { },  // 19 mina_held
                    new RuleStaging { darkness = 0.6, sound = "bell" },  // 20 closing: Mercy rings twelve
                    new RuleStaging { },  // 21 chapter_done
                },
                aftermath = new StageAftermath { scene = "cabin_fire", mood = "tender", rain = 0 },
            };
        }
    }
}
