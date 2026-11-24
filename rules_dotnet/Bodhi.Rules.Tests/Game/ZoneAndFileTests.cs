// Purpose: proves two small helpers the Unity scene leans on. ZoneOccupancy keeps track of who
// stands in a zone even though each character has several colliders and Unity can report a
// "left" without an "entered". SaveFiles keeps the saves on the phone safely: a write that is
// cut short (the phone dies mid-save) never destroys the last good save.
using System;
using System.IO;
using Bodhi.Game;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class ZoneOccupancyTests
    {
        // Someone is inside from their first collider in until their last collider out.
        [Test]
        public void inside_until_every_collider_leaves()
        {
            var zone = new ZoneOccupancy();
            Assert.That(zone.human_inside, Is.False);
            zone.entered(Character.human);
            zone.entered(Character.human);  // Her body and her feet.
            zone.left(Character.human);
            Assert.That(zone.human_inside, Is.True);
            Assert.That(zone.dog_inside, Is.False);
            zone.left(Character.human);
            Assert.That(zone.human_inside, Is.False);
            zone.entered(Character.dog);
            Assert.That(zone.dog_inside, Is.True);
        }

        // Break it: extra "left" reports (a collider switched off inside the zone) never make a
        // count go below zero, so the next "entered" still counts.
        [Test]
        public void extra_leaving_never_goes_below_zero()
        {
            var zone = new ZoneOccupancy();
            zone.left(Character.dog);
            zone.left(Character.dog);
            zone.left(Character.human);
            zone.entered(Character.dog);
            zone.entered(Character.human);
            Assert.That(zone.dog_inside, Is.True);
            Assert.That(zone.human_inside, Is.True);
            zone.clear();
            Assert.That(zone.dog_inside, Is.False);
            Assert.That(zone.human_inside, Is.False);
        }
    }

    public class SaveFilesTests
    {
        string folder;

        // Each test gets its own empty folder.
        [SetUp]
        public void make_folder()
        {
            folder = Path.Combine(Path.GetTempPath(), "bodhi_save_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
        }

        // And it is removed afterwards.
        [TearDown]
        public void remove_folder()
        {
            Directory.Delete(folder, true);
        }

        // What is written reads back the same, and a missing save reads as null.
        [Test]
        public void a_save_reads_back()
        {
            var files = new SaveFiles(folder);
            Assert.That(files.read("campaign"), Is.Null);
            Assert.That(files.write("campaign", "bodhi_campaign 1\nchapter 2\n"), Is.True);
            Assert.That(files.read("campaign"), Is.EqualTo("bodhi_campaign 1\nchapter 2\n"));
            Assert.That(files.write("chapter_in_progress", "x\n"), Is.True, "underscores are fine");
        }

        // Writing again keeps the save before as a backup, for when the newest one won't load.
        [Test]
        public void the_save_before_is_kept_as_a_backup()
        {
            var files = new SaveFiles(folder);
            Assert.That(files.read_backup("chapter"), Is.Null);
            files.write("chapter", "first\n");
            files.write("chapter", "second\n");
            Assert.That(files.read("chapter"), Is.EqualTo("second\n"));
            Assert.That(files.read_backup("chapter"), Is.EqualTo("first\n"));
        }

        // Erasing removes the save and its backup.
        [Test]
        public void erasing_removes_both()
        {
            var files = new SaveFiles(folder);
            files.write("chapter", "first\n");
            files.write("chapter", "second\n");
            files.erase("chapter");
            Assert.That(files.read("chapter"), Is.Null);
            Assert.That(files.read_backup("chapter"), Is.Null);
        }

        // Break it: a write cut short leaves a half-written temporary file. The real save is
        // untouched, and the next write cleans up and succeeds.
        [Test]
        public void a_cut_short_write_leaves_the_save_alone()
        {
            var files = new SaveFiles(folder);
            files.write("campaign", "good\n");
            File.WriteAllText(Path.Combine(folder, "campaign.save.writing"), "goo");
            Assert.That(files.read("campaign"), Is.EqualTo("good\n"));
            Assert.That(files.write("campaign", "better\n"), Is.True);
            Assert.That(files.read("campaign"), Is.EqualTo("better\n"));
        }

        // Break it: bad slot names (which could reach other folders), oversized text and
        // oversized files are refused.
        [Test]
        public void bad_names_and_huge_files_are_refused()
        {
            var files = new SaveFiles(folder);
            foreach (var name in new[] { null, "", "../escape", "Campaign", "a/b", "slot.save", new string('a', 41) })
            {
                Assert.That(files.write(name, "x\n"), Is.False, name ?? "null");
                Assert.That(files.read(name), Is.Null, name ?? "null");
            }
            Assert.That(files.write("campaign", null), Is.False);
            Assert.That(files.write("campaign", new string('x', SaveFiles.largest_save_characters + 1)), Is.False);
            File.WriteAllText(Path.Combine(folder, "campaign.save"), new string('x', SaveFiles.largest_save_characters + 1));
            Assert.That(files.read("campaign"), Is.Null);
            Assert.That(Directory.GetFiles(Path.GetDirectoryName(folder), "escape*"), Is.Empty);
        }

        // Break it: a name can't reach a file outside the save folder, to read it or erase it.
        [Test]
        public void a_name_never_reaches_outside_the_folder()
        {
            string inner = Path.Combine(folder, "inner");
            Directory.CreateDirectory(inner);
            string outside = Path.Combine(folder, "escape.save");
            File.WriteAllText(outside, "not yours\n");
            var files = new SaveFiles(inner);
            Assert.That(files.read("../escape"), Is.Null);
            files.erase("../escape");
            Assert.That(File.Exists(outside), Is.True);
        }

        // Break it: a gigantic save file (here 3 GB, though it takes no real space) is refused
        // before reading, so it can't use up the phone's memory.
        [Test]
        public void a_gigantic_file_is_refused_unread()
        {
            using (var stream = new FileStream(Path.Combine(folder, "campaign.save"), FileMode.Create))
            {
                stream.SetLength(3L * 1024 * 1024 * 1024);
            }
            Assert.That(new SaveFiles(folder).read("campaign"), Is.Null);
        }

        // Break it: a folder that can't be written (gone, or a file in its place) gives false
        // and null, never a crash.
        [Test]
        public void a_missing_folder_never_crashes()
        {
            var files = new SaveFiles(Path.Combine(folder, "not_there"));
            Assert.That(files.write("campaign", "x\n"), Is.False);
            Assert.That(files.read("campaign"), Is.Null);
            files.erase("campaign");
            Assert.Throws<ArgumentNullException>(() => new SaveFiles(null));
        }
    }
}
