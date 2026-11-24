// Purpose: everything a chapter needs on screen that the story rules don't: how big it is, its
// floors, walls and water, the furniture and scenery, where she and Bodhi start, and what each
// story rule does to the scene (move someone, dim the lights, shake the camera, raise a flood).
// It is the browser game's "drawing-only" chapter data (prototype/web/js/levels.js), exported by
// rules_dotnet/tools/export_chapters.js into game/Logic/Chapters/, so one Unity scene can build
// any of the twelve chapters without anyone placing things by hand.
//
// Positions are layout units with y pointing down, like the browser game (LayoutSpace turns them
// into metres). A box's x and y are its top-left corner; a person's place is their feet.
using System;

namespace Bodhi.Game
{
    // Who a staging change is about.
    public enum StageWho
    {
        human,  // Her.
        dog,    // Bodhi.
        guest,  // A visiting dog (Biscuit next door), in the chapters that have one.
    }

    // Which of them a floor or wall stops.
    public enum SolidBlocks
    {
        everyone,
        human,  // Only her (a gap he can squeeze through).
        dog,    // Only him (a gate she can step over).
    }

    // What kind of value a setting holds.
    public enum SettingKind
    {
        nothing,  // The chapter wrote null: "none of this here".
        text,
        number,
        flag,     // True or false, kept in `number` as 1 or 0.
    }

    // One named value of a prop or of the scenery, e.g. "color" = "#c8a27a" or "radius" = 220.
    // Props are too varied for a field each (a lamp has a radius, a sign has text), so they keep
    // their extras as a list. A nested value is named with dots: "art.far", "marks.0.1".
    public struct StageSetting
    {
        public string name;
        public SettingKind kind;
        public string text;    // For text. Empty otherwise.
        public double number;  // For numbers, and flags as 1 or 0.

        // Makes a text setting.
        public static StageSetting of_text(string name, string text)
        {
            return new StageSetting { name = name, kind = SettingKind.text, text = text ?? "" };
        }

        // Makes a number setting.
        public static StageSetting of_number(string name, double number)
        {
            return new StageSetting { name = name, kind = SettingKind.number, text = "", number = number };
        }

        // Makes a true-or-false setting.
        public static StageSetting of_flag(string name, bool flag)
        {
            return new StageSetting { name = name, kind = SettingKind.flag, text = "", number = flag ? 1 : 0 };
        }

        // Makes a "none here" setting.
        public static StageSetting of_nothing(string name)
        {
            return new StageSetting { name = name, kind = SettingKind.nothing, text = "" };
        }
    }

    // A floor, wall, plank, gate or crate. The story can remove or add one by its id.
    public sealed class StageSolid
    {
        public string id = "";                          // "" if the story never changes it.
        public string kind = "floor";                   // What it looks like: floor, plank, gate, glass...
        public double x, y, width, height;
        public SolidBlocks blocks = SolidBlocks.everyone;
        public bool hidden = false;                     // Starts missing until the story adds it.
        public string color = "";                       // "" means the kind's usual colour.
    }

    // A pool, river or flood. The story can raise or lower it by its id.
    public sealed class StageWater
    {
        public string id = "";
        public double x, y, width, height;              // `y` is the surface at the start.
        public double flow = 0;                         // How fast it pushes, units a second (+ right).
        public bool mud = false;                        // Mud: muddies him instead of washing him.
    }

    // A piece of furniture or scenery: a lamp, a bed, a tree, a train.
    public sealed class StageProp
    {
        public string kind = "";
        public double x, y;
        public string[] until = Array.Empty<string>();  // Gone (or changed) once any of these happen.
        public string after = "";                       // Drawn changed once this has happened.
        public StageSetting[] settings = Array.Empty<StageSetting>();  // Everything else it has.
    }

    // How one thing you can use looks and sounds. Lined up with ChapterScript.things.
    public sealed class StageThingLook
    {
        public string event_name = "";                  // The same as the ChapterThing's.
        public string sprite = "";                      // "" means the usual marker.
        public string sound = "";                       // Played when it's used; "" for none.
        public string pose = "";                        // The pose to strike using it; "" for none.
    }

    // The visiting dog, in the chapters that have one.
    public sealed class StageGuest
    {
        public double x, feet_y;
        public double size = 1;                         // Compared with a grown dog.
        public string mode = "still";                   // still, follow or gone.
        public string pose = "";                        // The pose it holds while still.
    }

    // The quiet scene shown after a chapter ends.
    public sealed class StageAftermath
    {
        public string scene = "";                       // Which picture: first_night, bedtime...
        public string mood = "";
        public double rain = 0;                         // 0 none to 1 heavy.
    }

    // Someone put somewhere at once (a cut in a film scene).
    public struct StagePlacement
    {
        public StageWho who;
        public double x, feet_y;
    }

    // Someone turned to face left (-1) or right (1).
    public struct StageFacing
    {
        public StageWho who;
        public int direction;
    }

    // Someone striking a pose for a while (sit, kneel, sleep...).
    public struct StagePose
    {
        public StageWho who;
        public string pose;
        public double seconds;
    }

    // Someone walking on their own to a spot (she walks; he runs).
    public struct StageWalk
    {
        public StageWho who;
        public double x;
    }

    // Someone shown or hidden (she has got into bed, and the bed shows her asleep).
    public struct StageHiding
    {
        public StageWho who;
        public bool hidden;
    }

    // A pool or flood rising or falling to a new surface.
    public struct StageWaterLevel
    {
        public string water_id;
        public double top;
    }

    // What a rule does to the camera.
    public enum CameraChange
    {
        none,     // Leaves it.
        hold,     // Holds a fixed shot (a film scene).
        release,  // Goes back to following the player.
    }

    // What a rule does to the scent trail his nose follows.
    public enum ScentChange
    {
        none,
        point,  // A trail leads to (scent_x, scent_y).
        clear,  // No trail any more.
    }

    // What a rule does to what Bodhi carries in his mouth.
    public enum CarryChange
    {
        none,
        carry,  // He now carries `carry` (a glove, a collar).
        drop,   // He drops whatever he had.
    }

    // A short visual effect: lightning, falling debris, a photo coming into view.
    public sealed class StageEffect
    {
        public string kind = "";
        public double x, y, width;                      // Lightning has no place: all 0.
        public bool has_place = false;
    }

    // Everything one story rule does to the scene. Lined up with ChapterScript.rules: rule 4's
    // staging is staging[4]. Lasting changes (darkness, water, who's hidden) count even when a
    // save is replayed; the moment-only ones (sound, shake, a caption) only when it plays live.
    public sealed class RuleStaging
    {
        public StagePlacement[] place = Array.Empty<StagePlacement>();
        public StageFacing[] face = Array.Empty<StageFacing>();
        public StagePose[] pose = Array.Empty<StagePose>();
        public StageWalk[] walk = Array.Empty<StageWalk>();
        public StageHiding[] hide = Array.Empty<StageHiding>();
        public StageWaterLevel[] water = Array.Empty<StageWaterLevel>();
        public CameraChange camera = CameraChange.none;
        public double camera_x, camera_y;
        public double camera_zoom = 1;
        public double? darkness = null;                 // 0 bright to 0.95 nearly black.
        public string mood = "";                        // "" leaves the music as it is.
        public ScentChange scent = ScentChange.none;
        public double scent_x, scent_y;
        public CarryChange carry = CarryChange.none;
        public string carry_item = "";
        public double? dog_runs_to = null;              // He runs to this x on his own.
        public string guest_mode = "";                  // "" leaves the visiting dog as it is.
        public string caption = null;                   // A big caption; "" clears it; null leaves it.
        public string sound = "";
        public double shake_seconds = 0;
        public bool fade_in = false;                    // A soft cut from black.
        public StageEffect effect = null;

        // True if this rule changes nothing on screen.
        public bool is_empty =>
            place.Length == 0 && face.Length == 0 && pose.Length == 0 && walk.Length == 0 && hide.Length == 0
            && water.Length == 0 && camera == CameraChange.none && darkness == null && mood.Length == 0
            && scent == ScentChange.none && carry == CarryChange.none && dog_runs_to == null
            && guest_mode.Length == 0 && caption == null && sound.Length == 0 && shake_seconds <= 0
            && !fade_in && effect == null;
    }

    // A whole chapter's stage.
    public sealed class ChapterStage
    {
        public string id = "";                          // The same as the ChapterScript's.
        public double width = 1200;
        public double height = LayoutSpace.usual_layout_height;
        public string mood = "";                        // The music's mood at the start.
        public StageSetting[] scenery = Array.Empty<StageSetting>();  // Sky colours, rain, fog...
        public double human_start_x, human_start_feet_y;
        public double dog_start_x, dog_start_feet_y;
        public double ball_x, ball_y;                   // Where the ball starts (its centre), if the chapter has one.
        public StageGuest guest = null;
        public StageSolid[] solids = Array.Empty<StageSolid>();
        public StageWater[] water = Array.Empty<StageWater>();
        public StageProp[] props = Array.Empty<StageProp>();
        public StageThingLook[] things = Array.Empty<StageThingLook>();
        public RuleStaging[] rules = Array.Empty<RuleStaging>();
        public StageAftermath aftermath = new StageAftermath();

        // The scenery value with this name, or a "nothing" setting if the chapter has none.
        public StageSetting scenery_setting(string name)
        {
            return find_setting(scenery, name);
        }

        // The setting with this name in a list, or a "nothing" setting if it isn't there.
        public static StageSetting find_setting(StageSetting[] settings, string name)
        {
            foreach (var setting in settings ?? Array.Empty<StageSetting>())
            {
                if (setting.name == name)
                {
                    return setting;
                }
            }
            return StageSetting.of_nothing(name);
        }

        // The solid with this id, or null.
        public StageSolid find_solid(string solid_id)
        {
            if (string.IsNullOrEmpty(solid_id))
            {
                return null;
            }
            foreach (var solid in solids ?? Array.Empty<StageSolid>())
            {
                if (solid != null && solid.id == solid_id)
                {
                    return solid;
                }
            }
            return null;
        }

        // The water with this id, or null.
        public StageWater find_water(string water_id)
        {
            if (string.IsNullOrEmpty(water_id))
            {
                return null;
            }
            foreach (var pool in water ?? Array.Empty<StageWater>())
            {
                if (pool != null && pool.id == water_id)
                {
                    return pool;
                }
            }
            return null;
        }
    }
}
