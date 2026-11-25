// Purpose: proves the iPhone app's animation pictures (game/Resources/Frames) match the frame list
// (FrameSheets.Data.cs): every set has every animation, every picture exists at the size the list
// says, every drawing's box is inside its picture, and its feet point is at the bottom of it.
using System.IO;
using System.Linq;
using Bodhi.Game;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class FrameSheetsTests
    {
        // The repository's root folder, found by walking up from where the tests run.
        static string repository()
        {
            var folder = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (folder != null && !Directory.Exists(Path.Combine(folder.FullName, "game", "Resources", "Frames")))
            {
                folder = folder.Parent;
            }
            Assert.That(folder, Is.Not.Null, "couldn't find game/Resources/Frames above the test folder");
            return folder.FullName;
        }

        // A PNG's width and height, read from its header (bytes 16 to 23, high byte first).
        static (int width, int height) png_size(string path)
        {
            var bytes = File.ReadAllBytes(path);
            // Pictures are kept in Git LFS. Without `git lfs pull` the file is a small text
            // pointer, not the picture, so say that rather than report a nonsense size.
            if (System.Text.Encoding.ASCII.GetString(bytes, 0, System.Math.Min(bytes.Length, 7)) == "version")
            {
                Assert.Fail(path + " is a Git LFS pointer, not the picture: run git lfs pull.");
            }
            int read(int at) => (bytes[at] << 24) | (bytes[at + 1] << 16) | (bytes[at + 2] << 8) | bytes[at + 3];
            return (read(16), read(20));
        }

        [Test]
        public void every_set_has_every_animation_with_its_drawings()
        {
            string[] her = { "idle", "walk", "run", "jump", "land", "turn", "stop", "skid", "kneel", "embrace", "sit" };
            string[] dog = { "idle", "walk", "trot", "gallop", "jump", "land", "turn", "skid", "stop", "sit", "sniff", "bark", "sleep", "lean", "embrace" };
            Assert.That(FrameSheets.all.Select(set => set.name), Is.EquivalentTo(new[] { "her", "bodhi", "bodhi_puppy", "biscuit" }));
            foreach (var set in FrameSheets.all)
            {
                var expected = set.name == "her" ? her : dog;
                Assert.That(set.animations.Select(animation => animation.name), Is.EquivalentTo(expected), set.name);
                foreach (var animation in set.animations)
                {
                    Assert.That(animation.count, Is.GreaterThanOrEqualTo(9), set.name + " " + animation.name + " has too few drawings to look smooth");
                    foreach (var drawing in animation.frames)
                    {
                        Assert.That(drawing.foot_count, Is.EqualTo(set.name == "her" ? 2 : 4), set.name + " " + animation.name + " feet");
                    }
                }
                foreach (var cycle in set.name == "her" ? new[] { "walk", "run" } : new[] { "walk", "trot", "gallop" })
                {
                    Assert.That(set.animation(cycle).cycle_length, Is.GreaterThan(0), set.name + " " + cycle + " needs its cycle length");
                }
            }
        }

        [Test]
        public void every_picture_exists_and_every_box_is_inside_it()
        {
            string folder = repository();
            foreach (var set in FrameSheets.all)
            {
                foreach (var animation in set.animations)
                {
                    string label = set.name + " " + animation.name;
                    string path = Path.Combine(folder, "game", "Resources", animation.picture + ".png");
                    Assert.That(File.Exists(path), label + ": " + path + " is missing");
                    Assert.That(png_size(path), Is.EqualTo((animation.picture_width, animation.picture_height)), label + " picture size");
                    Assert.That(animation.picture_width, Is.LessThanOrEqualTo(2048), label + " is too wide for older iPhones");
                    Assert.That(animation.picture_height, Is.LessThanOrEqualTo(2048), label + " is too tall for older iPhones");
                    foreach (var drawing in animation.frames)
                    {
                        Assert.That(drawing.x >= 0 && drawing.y >= 0 && drawing.width > 0 && drawing.height > 0, label + " box");
                        Assert.That(drawing.x + drawing.width, Is.LessThanOrEqualTo(animation.picture_width), label + " box runs off the right");
                        Assert.That(drawing.y + drawing.height, Is.LessThanOrEqualTo(animation.picture_height), label + " box runs off the top");
                        // The feet are within the drawing's width, and at its bottom: just above
                        // it (a shadow or a paw reaches a little below the feet) or, mid-leap,
                        // under it, but never up in the body.
                        Assert.That(drawing.pivot_x, Is.InRange(0, drawing.width), label + " feet across");
                        Assert.That(drawing.pivot_y, Is.InRange(-drawing.height * 0.5, drawing.height * 0.2), label + " feet height");
                    }
                }
            }
        }

        [Test]
        public void bodhi_is_drawn_as_a_puppy_only_while_young()
        {
            Assert.That(FrameSheets.dog_set_for_age("eight_weeks"), Is.EqualTo("bodhi_puppy"));
            Assert.That(FrameSheets.dog_set_for_age("five_months"), Is.EqualTo("bodhi_puppy"));
            Assert.That(FrameSheets.dog_set_for_age("six_months"), Is.EqualTo("bodhi"));
            Assert.That(FrameSheets.dog_set_for_age(""), Is.EqualTo("bodhi"));
            Assert.That(FrameSheets.dog_set_for_age(null), Is.EqualTo("bodhi"));
            Assert.That(FrameSheets.set("nobody"), Is.Null);
        }
    }
}
