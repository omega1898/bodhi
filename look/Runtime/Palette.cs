// Purpose: the game's twelve colours, the single source every material, light, particle and
// screen reads from (docs/unity_art_bible.md, section 2), plus the contrast measure the art
// bible's readability rules are checked with. Plain C#: no Unity code, so it is tested anywhere.
using System;
using System.Globalization;

namespace Bodhi.Look
{
    // One named colour in the palette, kept as the hex text an artist would type.
    public readonly struct PaletteEntry
    {
        public readonly string name;
        public readonly string hex;

        // Makes a named palette colour.
        public PaletteEntry(string name, string hex)
        {
            this.name = name;
            this.hex = hex;
        }
    }

    // A colour as three numbers from 0 to 1 (red, green, blue).
    public readonly struct PaletteColour
    {
        public readonly double red;
        public readonly double green;
        public readonly double blue;

        // Makes a colour from three channels between 0 and 1.
        public PaletteColour(double red, double green, double blue)
        {
            this.red = red;
            this.green = green;
            this.blue = blue;
        }

        // Reads "#RRGGBB". Returns false, and black, for anything else, so a typing mistake
        // is caught by the palette tests instead of quietly showing up as black in the game.
        public static bool try_parse(string text, out PaletteColour colour)
        {
            colour = new PaletteColour(0, 0, 0);
            if (text == null || text.Length != 7 || text[0] != '#')
            {
                return false;
            }
            if (!int.TryParse(text.Substring(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int packed))
            {
                return false;
            }
            colour = new PaletteColour(((packed >> 16) & 0xFF) / 255.0, ((packed >> 8) & 0xFF) / 255.0, (packed & 0xFF) / 255.0);
            return true;
        }
    }

    public static class Palette
    {
        public static readonly PaletteEntry night = new PaletteEntry("night", "#1B2433");             // The plane right behind the characters.
        public static readonly PaletteEntry rain = new PaletteEntry("rain", "#34465C");               // Middle distance.
        public static readonly PaletteEntry far_mist = new PaletteEntry("far_mist", "#5C7186");       // The farthest layer.
        public static readonly PaletteEntry wet_ground = new PaletteEntry("wet_ground", "#2A2F36");   // The ground they stand on.
        public static readonly PaletteEntry shadow = new PaletteEntry("shadow", "#121722");           // Shadows: blue, never black.
        public static readonly PaletteEntry her_coat = new PaletteEntry("her_coat", "#2C6563");       // Her teal raincoat.
        public static readonly PaletteEntry her_scarf = new PaletteEntry("her_scarf", "#B9532A");     // Her rust scarf.
        public static readonly PaletteEntry her_hair = new PaletteEntry("her_hair", "#DCB866");       // Her blonde hair.
        public static readonly PaletteEntry bodhi_coat = new PaletteEntry("bodhi_coat", "#E2B47B");   // Bodhi's apricot curls.
        public static readonly PaletteEntry bodhi_shade = new PaletteEntry("bodhi_shade", "#B8875A"); // The shaded side of his curls.
        public static readonly PaletteEntry moon_rim = new PaletteEntry("moon_rim", "#9FC3D9");       // The cold rim light on both of them.
        public static readonly PaletteEntry warm_amber = new PaletteEntry("warm_amber", "#F0B65E");   // Lamplight, firelight, reunions only.

        // Every colour, for tests and for the Unity side to build its colour list from.
        public static readonly PaletteEntry[] all =
        {
            night, rain, far_mist, wet_ground, shadow, her_coat, her_scarf, her_hair, bodhi_coat, bodhi_shade, moon_rim, warm_amber,
        };

        // How far apart two colours stand in brightness, from 1 (the same) to 21 (black on
        // white). This is the WCAG contrast ratio, the common measure of whether something can
        // be seen against its background. An unreadable colour counts as black.
        public static double contrast(PaletteEntry first, PaletteEntry second)
        {
            PaletteColour.try_parse(first.hex, out var first_colour);
            PaletteColour.try_parse(second.hex, out var second_colour);
            double brighter = Math.Max(luminance(first_colour), luminance(second_colour));
            double darker = Math.Min(luminance(first_colour), luminance(second_colour));
            return (brighter + 0.05) / (darker + 0.05);
        }

        // How bright a colour looks to the eye (green counts most, blue least), after undoing
        // the "gamma" curve screens store colours with.
        static double luminance(PaletteColour colour)
        {
            return 0.2126 * linear(colour.red) + 0.7152 * linear(colour.green) + 0.0722 * linear(colour.blue);
        }

        // Turns one stored channel into how much light the screen actually gives out.
        static double linear(double channel)
        {
            return channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }
    }
}
