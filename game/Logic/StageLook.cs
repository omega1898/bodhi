// Purpose: small decisions about how a chapter looks, kept out of Unity so they can be tested:
// reading the chapters' colours ("#c8a27a", "rgba(255,214,170,0.14)"), whether a floor or wall
// is there right now, whether a prop shows, and the rough box each prop fills. The Unity scene
// draws plain coloured boxes from these until a human artist paints the real pictures.
using System;
using System.Globalization;
using Bodhi.Rules;

namespace Bodhi.Game
{
    // A colour as four numbers from 0 to 1: red, green, blue and how solid it is (alpha).
    public struct StageColor
    {
        public double red, green, blue, alpha;

        // Makes a colour.
        public StageColor(double red, double green, double blue, double alpha = 1)
        {
            this.red = red;
            this.green = green;
            this.blue = blue;
            this.alpha = alpha;
        }
    }

    // Where a prop's rough box goes, in layout units: its top-left corner and its size.
    public struct PropBox
    {
        public double left, top, width, height;
    }

    public static class StageLook
    {
        // Reads a colour written the browser's way: "#abc", "#aabbcc", "rgb(1,2,3)" or
        // "rgba(1,2,3,0.5)". False (and `color` left see-through black) if it can't be read.
        public static bool parse_color(string text, out StageColor color)
        {
            color = new StageColor(0, 0, 0, 0);
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }
            string trimmed = text.Trim().ToLowerInvariant();
            if (trimmed.StartsWith("#"))
            {
                return parse_hex(trimmed.Substring(1), out color);
            }
            bool with_alpha = trimmed.StartsWith("rgba(");
            if (!with_alpha && !trimmed.StartsWith("rgb("))
            {
                return false;
            }
            if (!trimmed.EndsWith(")"))
            {
                return false;
            }
            int open = trimmed.IndexOf('(');
            string[] parts = trimmed.Substring(open + 1, trimmed.Length - open - 2).Split(',');
            if (parts.Length != (with_alpha ? 4 : 3))
            {
                return false;
            }
            var numbers = new double[4] { 0, 0, 0, 255 };
            for (int index = 0; index < parts.Length; index++)
            {
                if (!double.TryParse(parts[index].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || double.IsNaN(value) || double.IsInfinity(value))
                {
                    return false;
                }
                numbers[index] = index == 3 ? value * 255 : value;  // Alpha is written 0 to 1.
            }
            color = new StageColor(clamp_channel(numbers[0]), clamp_channel(numbers[1]), clamp_channel(numbers[2]), clamp_channel(numbers[3]));
            return true;
        }

        // True if this floor or wall is there now: not taken away by the story, and (for one that
        // starts hidden) put in place by it. With no run yet, only what starts visible is there.
        public static bool solid_is_present(StageSolid solid, ChapterRun run)
        {
            if (solid == null)
            {
                return false;
            }
            bool has_id = !string.IsNullOrEmpty(solid.id);
            if (has_id && run != null && run.solid_is_removed(solid.id))
            {
                return false;
            }
            if (solid.hidden)
            {
                return run != null && run.solid_is_added(solid.id);
            }
            return true;
        }

        // True if this one stops her (or him). A gap only he fits through stops only her.
        public static bool solid_blocks(StageSolid solid, Character who)
        {
            if (solid == null)
            {
                return false;
            }
            switch (solid.blocks)
            {
                case SolidBlocks.human:
                    return who == Character.human;
                case SolidBlocks.dog:
                    return who == Character.dog;
                default:
                    return true;
            }
        }

        // True if this prop shows now: none of its "until" events has happened, and its "after"
        // event (if it has one) has. The browser game swaps the carrier, closed then open, this way.
        public static bool prop_is_showing(StageProp prop, ChapterRun run)
        {
            if (prop == null)
            {
                return false;
            }
            foreach (var event_name in prop.until ?? Array.Empty<string>())
            {
                if (run != null && run.has_happened(event_name))
                {
                    return false;
                }
            }
            return string.IsNullOrEmpty(prop.after) || (run != null && run.has_happened(prop.after));
        }

        // The colour to paint a floor or wall, or "" for one that isn't drawn (an invisible wall
        // that only keeps them in). The chapter's own colour wins, then the kind's usual one.
        public static string solid_color(StageSolid solid, ChapterStage stage)
        {
            if (solid == null || solid.kind == "invisible")
            {
                return "";
            }
            if (!string.IsNullOrEmpty(solid.color))
            {
                return solid.color;
            }
            switch (solid.kind)
            {
                case "floor":
                    return scenery_text(stage, "ground_top", "#5a5a48");
                case "wall":
                case "room_wall":
                case "pillar":
                    return scenery_text(stage, solid.kind == "room_wall" ? "room_wall" : "wall", "#6a5e50");
                case "glass":
                    return "rgba(170,200,220,0.35)";
                case "gate":
                    return "#4a3b2a";
                default:
                    return "#6b5233";  // Planks, crates, shelves: wood.
            }
        }

        // The rough box a prop fills, in layout units. The chapters place most props by their
        // bottom-left corner on the floor; signs, windows and walls by their top-left corner; a
        // tree, a standing lamp or a person by the middle of their base; a hanging lamp from the
        // ceiling. "w" and "h" settings, when a chapter gives them, set the size.
        public static PropBox prop_box(StageProp prop)
        {
            if (prop == null)
            {
                return new PropBox();
            }
            double width, height;
            string anchor = "bottom_left";
            switch (prop.kind)
            {
                case "room_wall": width = 300; height = 320; anchor = "top_left"; break;
                case "window": width = 60; height = 80; anchor = "top_left"; break;
                case "poster": width = 60; height = 80; anchor = "top_left"; break;
                case "photo": width = 40; height = 30; anchor = "top_left"; break;
                case "drawing": width = 50; height = 40; anchor = "top_left"; break;
                case "scrawl": width = 120; height = 40; anchor = "top_left"; break;
                case "chalk_heart": width = 40; height = 40; anchor = "top_left"; break;
                case "ribbon": width = 30; height = 40; anchor = "top_left"; break;
                case "sign":
                    var words = sign_text(prop);
                    width = words.Length * 7 + 22;
                    height = 26;
                    anchor = "top_left";
                    break;
                case "lamp":
                    bool hanging = has_flag(prop, "hanging");
                    width = 24;
                    height = hanging ? 48 : 130;
                    anchor = hanging ? "top_middle" : "bottom_middle";
                    break;
                case "tree": width = 80; height = 220; anchor = "bottom_middle"; break;
                case "people": width = 40; height = 170; anchor = "bottom_middle"; break;
                case "road_sign": width = 30; height = 150; anchor = "bottom_middle"; break;
                case "fire": width = 60; height = 80; anchor = "bottom_middle"; break;
                case "lightning": width = 0; height = 0; break;  // A flash, drawn by the scene's effects.
                case "car": width = 200; height = 80; break;
                case "carrier": width = 60; height = 45; break;
                case "house": width = 300; height = 260; break;
                case "garden_fence": width = 140; height = 40; break;
                case "doorframe": width = 70; height = 200; break;
                case "kitchen": width = 200; height = 90; break;
                case "bathtub": width = 140; height = 60; break;
                case "sofa": width = 160; height = 70; break;
                case "dog_basket": width = 70; height = 30; break;
                case "double_bed": width = 180; height = 60; break;
                case "bed": width = 120; height = 55; break;
                case "cot": width = 80; height = 80; break;
                case "flowers": width = 60; height = 25; break;
                case "debris": width = 80; height = 40; break;
                case "bars": width = 80; height = 120; break;
                case "wall": width = 40; height = 200; break;
                case "suitcase": width = 60; height = 40; break;
                case "snow_drift": width = 160; height = 40; break;
                case "pillar": width = 40; height = 300; break;
                case "tent": width = 180; height = 120; break;
                case "rail": width = 200; height = 20; break;
                case "hay":
                case "bale": width = 80; height = 50; break;
                case "glass": width = 20; height = 150; break;
                case "crash_barrier": width = 200; height = 30; break;
                case "bench": width = 120; height = 45; break;
                case "tucked_blanket": width = 100; height = 20; break;
                case "train": width = 600; height = 200; break;
                case "stove": width = 80; height = 90; break;
                case "stairs": width = 200; height = 200; break;
                case "locker": width = 60; height = 180; break;
                default: width = 60; height = 60; break;
            }
            width = setting_size(prop, "w", width);
            height = setting_size(prop, "h", height);
            var box = new PropBox { width = width, height = height };
            switch (anchor)
            {
                case "top_left": box.left = prop.x; box.top = prop.y; break;
                case "top_middle": box.left = prop.x - width / 2; box.top = prop.y; break;
                case "bottom_middle": box.left = prop.x - width / 2; box.top = prop.y - height; break;
                default: box.left = prop.x; box.top = prop.y - height; break;
            }
            return box;
        }

        // A size setting if the chapter gives a usable one, otherwise the kind's usual size.
        static double setting_size(StageProp prop, string name, double usual)
        {
            var setting = ChapterStage.find_setting(prop.settings, name);
            bool usable = setting.kind == SettingKind.number && setting.number > 0 && !double.IsInfinity(setting.number);
            return usable ? setting.number : usual;
        }

        // A sign's words, or "" if it has none.
        static string sign_text(StageProp prop)
        {
            var setting = ChapterStage.find_setting(prop.settings, "text");
            return setting.kind == SettingKind.text ? setting.text ?? "" : "";
        }

        // True if the prop has this flag set.
        static bool has_flag(StageProp prop, string name)
        {
            var setting = ChapterStage.find_setting(prop.settings, name);
            return setting.kind == SettingKind.flag && setting.number != 0;
        }

        // A scenery colour, or the fallback if the chapter has none.
        static string scenery_text(ChapterStage stage, string name, string fallback)
        {
            var setting = stage != null ? stage.scenery_setting(name) : StageSetting.of_nothing(name);
            return setting.kind == SettingKind.text && !string.IsNullOrEmpty(setting.text) ? setting.text : fallback;
        }

        // Reads "abc" or "aabbcc" after the #.
        static bool parse_hex(string digits, out StageColor color)
        {
            color = new StageColor(0, 0, 0, 0);
            if (digits.Length == 3)
            {
                digits = new string(new[] { digits[0], digits[0], digits[1], digits[1], digits[2], digits[2] });
            }
            if (digits.Length != 6 || !int.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int value))
            {
                return false;
            }
            color = new StageColor(((value >> 16) & 255) / 255.0, ((value >> 8) & 255) / 255.0, (value & 255) / 255.0, 1);
            return true;
        }

        // A 0-to-255 channel as 0 to 1, kept in range.
        static double clamp_channel(double value)
        {
            return Math.Max(0, Math.Min(255, value)) / 255.0;
        }
    }
}
