// Purpose: proves the story memory (spec 3.6): facts like "swept_away_by_river" travel between
// chapters, survive a save, and hostile text can't add facts or crash the loader.
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class StoryMemoryTests
    {
        // A fact is remembered once and can be checked.
        [Test]
        public void facts_are_remembered()
        {
            var memory = new StoryMemory();
            Assert.That(memory.remember("found_mina_glove"), Is.True);
            Assert.That(memory.remember("found_mina_glove"), Is.False);  // Already known.
            Assert.That(memory.knows("found_mina_glove"), Is.True);
            Assert.That(memory.knows("swept_away_by_river"), Is.False);
            Assert.That(memory.count, Is.EqualTo(1));
        }

        // Saved as text in alphabetical order, and loaded back the same.
        [Test]
        public void memory_survives_a_save()
        {
            var memory = new StoryMemory();
            memory.remember("swept_away_by_river");
            memory.remember("found_mina_glove");
            string text = memory.to_text();
            Assert.That(text, Is.EqualTo("found_mina_glove\nswept_away_by_river\n"));
            var loaded = new StoryMemory();
            Assert.That(loaded.load_text(text), Is.True);
            Assert.That(loaded.to_text(), Is.EqualTo(text));
        }

        // Loading replaces what was remembered; it doesn't add to it.
        [Test]
        public void loading_replaces_the_memory()
        {
            var memory = new StoryMemory();
            memory.remember("found_mina_glove");
            Assert.That(memory.load_text("swept_away_by_river\n"), Is.True);
            Assert.That(memory.knows("found_mina_glove"), Is.False);
            Assert.That(memory.to_text(), Is.EqualTo("swept_away_by_river\n"));
        }

        // Break it: bad fact names are refused and counted.
        [Test]
        public void break_it_bad_fact_names_are_refused()
        {
            var memory = new StoryMemory();
            Assert.That(memory.remember(null), Is.False);
            Assert.That(memory.remember(""), Is.False);
            Assert.That(memory.remember("Swept_Away"), Is.False);
            Assert.That(memory.remember(new string('a', 65)), Is.False);
            Assert.That(memory.refused_fact_count, Is.EqualTo(4));
            Assert.That(memory.count, Is.EqualTo(0));
        }

        // Break it: the number of facts is capped, so a runaway loop can't grow it forever.
        [Test]
        public void break_it_the_number_of_facts_is_capped()
        {
            var memory = new StoryMemory();
            for (int index = 0; index < 1000; index += 1) { memory.remember("fact_" + index); }
            Assert.That(memory.count, Is.EqualTo(StoryMemory.maximum_fact_count));
        }

        // Break it: a damaged memory file is refused whole, and the memory is left as it was.
        [Test]
        public void break_it_a_damaged_file_changes_nothing()
        {
            var memory = new StoryMemory();
            memory.remember("found_mina_glove");
            Assert.That(memory.load_text("swept_away\nBAD LINE\n"), Is.False);
            Assert.That(memory.load_text("swept_away"), Is.False);         // Cut short.
            Assert.That(memory.load_text(null), Is.False);
            Assert.That(memory.load_text("a\na\n"), Is.False);             // Twice.
            Assert.That(memory.load_text(new string('a', 20000) + "\n"), Is.False);
            Assert.That(memory.to_text(), Is.EqualTo("found_mina_glove\n"));
            Assert.That(new StoryMemory().load_text(""), Is.True);         // Nothing remembered yet is fine.
        }
    }
}
