// Purpose: checks the art bible's colour rules (docs/unity_art_bible.md, section 2) by numbers,
// so a palette change that would make the characters hard to see fails before anyone ships it.
using Bodhi.Look;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class PaletteTests
    {
        // Every colour in the palette is a valid six-digit hex colour.
        [Test]
        public void every_palette_colour_is_valid()
        {
            foreach (var entry in Palette.all)
            {
                Assert.That(PaletteColour.try_parse(entry.hex, out _), Is.True, entry.name);
            }
        }

        // Shadows are blue, never pure black or grey: blue is the strongest channel.
        [Test]
        public void shadows_are_blue_never_black()
        {
            PaletteColour.try_parse(Palette.shadow.hex, out var shadow);
            Assert.That(shadow.blue, Is.GreaterThan(shadow.red));
            Assert.That(shadow.blue, Is.GreaterThan(shadow.green));
            Assert.That(shadow.blue, Is.GreaterThan(0.0));
        }

        // The rim light makes both silhouettes read against the plane behind them.
        [Test]
        public void the_rim_light_stands_out_against_the_night()
        {
            Assert.That(Palette.contrast(Palette.moon_rim, Palette.night), Is.GreaterThanOrEqualTo(4.5));
        }

        // Bodhi's coat and her hair read at a glance, even small, against the night.
        [Test]
        public void bodhi_and_her_hair_read_against_the_night()
        {
            Assert.That(Palette.contrast(Palette.bodhi_coat, Palette.night), Is.GreaterThanOrEqualTo(4.5));
            Assert.That(Palette.contrast(Palette.her_hair, Palette.night), Is.GreaterThanOrEqualTo(4.5));
        }

        // Her teal coat stands out more against the night than against the middle distance,
        // which is why the plane right behind the characters must always be the night.
        [Test]
        public void her_coat_needs_the_night_behind_her()
        {
            Assert.That(Palette.contrast(Palette.her_coat, Palette.night), Is.GreaterThan(Palette.contrast(Palette.her_coat, Palette.rain)));
            Assert.That(Palette.contrast(Palette.her_coat, Palette.night), Is.GreaterThanOrEqualTo(2.0));
        }

        // The warm accent is the brightest warm colour, so the eye goes to warmth first.
        [Test]
        public void the_warm_accent_outshines_the_cold_ambient()
        {
            Assert.That(Palette.contrast(Palette.warm_amber, Palette.night), Is.GreaterThan(Palette.contrast(Palette.rain, Palette.night)));
        }

        // Contrast is the same whichever colour comes first, and a colour against itself is 1.
        [Test]
        public void contrast_is_symmetric_and_one_for_the_same_colour()
        {
            Assert.That(Palette.contrast(Palette.night, Palette.warm_amber), Is.EqualTo(Palette.contrast(Palette.warm_amber, Palette.night)));
            Assert.That(Palette.contrast(Palette.night, Palette.night), Is.EqualTo(1.0).Within(1e-9));
        }

        // Break it: bad hex text is refused instead of crashing or turning into black.
        [TestCase(null)]
        [TestCase("")]
        [TestCase("#12345")]
        [TestCase("#1234567")]
        [TestCase("#GGGGGG")]
        [TestCase("123456")]
        public void break_it_bad_hex_is_refused(string text)
        {
            Assert.That(PaletteColour.try_parse(text, out _), Is.False);
        }
    }
}
