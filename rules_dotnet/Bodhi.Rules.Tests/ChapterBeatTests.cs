// Purpose: proves the chapter's story order: the designed path is allowed, shortcuts are not.
// Ported from core/tests/test_chapter_beat.cpp.
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class ChapterBeatTests
    {
        // The designed path is allowed, including going back and forth between the two routes.
        [Test]
        public void the_designed_path_is_allowed()
        {
            Assert.That(ChapterBeats.is_allowed_transition(ChapterBeat.together, ChapterBeat.separation), Is.True);
            Assert.That(ChapterBeats.is_allowed_transition(ChapterBeat.human_section, ChapterBeat.dog_section), Is.True);
            Assert.That(ChapterBeats.is_allowed_transition(ChapterBeat.dog_section, ChapterBeat.human_section), Is.True);
            Assert.That(ChapterBeats.is_allowed_transition(ChapterBeat.reunion, ChapterBeat.aftermath), Is.True);
        }

        // Shortcuts, backward steps and staying put are refused.
        [Test]
        public void shortcuts_and_backward_steps_are_refused()
        {
            Assert.That(ChapterBeats.is_allowed_transition(ChapterBeat.together, ChapterBeat.reunion), Is.False);
            Assert.That(ChapterBeats.is_allowed_transition(ChapterBeat.aftermath, ChapterBeat.together), Is.False);
            Assert.That(ChapterBeats.is_allowed_transition(ChapterBeat.separation, ChapterBeat.separation), Is.False);
        }

        // Every beat has its name; a forced bad value gets "unknown" instead of garbage.
        [Test]
        public void beats_have_names_and_bad_values_are_unknown()
        {
            Assert.That(ChapterBeats.name_of(ChapterBeat.dog_section), Is.EqualTo("dog_section"));
            Assert.That(ChapterBeats.name_of((ChapterBeat)99), Is.EqualTo("unknown"));
        }
    }
}
