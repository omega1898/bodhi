// Purpose: proves the game session, the one object the Unity scene talks to: a new game,
// Continue from the title screen, autosaving at the browser game's moments, moving on to the
// next chapter, and falling back to the backup when the newest save is damaged.
using System;
using System.IO;
using Bodhi.Game;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class GameSessionTests
    {
        static readonly SavedPlaces here = new SavedPlaces(300, 400, 340, 400);
        string folder;

        // Each test gets its own empty save folder.
        [SetUp]
        public void make_folder()
        {
            folder = Path.Combine(Path.GetTempPath(), "bodhi_session_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
        }

        // And it is removed afterwards.
        [TearDown]
        public void remove_folder()
        {
            Directory.Delete(folder, true);
        }

        GameSession new_session()
        {
            return new GameSession(new SaveFiles(folder), "Bodhi");
        }

        // Plays the session's chapter with the robot until `stop_when` says so, then lets one
        // frame pass so the session notices (and autosaves if it should).
        static void play_until(GameSession session, Func<ChapterRun, bool> stop_when)
        {
            ChapterRobot.finish(session.run, skip_films: true, stop_when: stop_when);
            session.tick(0.25, here, safe_spot: true);
        }

        // A new game starts Coming Home, with its opening already under way, and is saved.
        [Test]
        public void a_new_game_starts_the_first_chapter()
        {
            var session = new_session();
            Assert.That(session.has_save, Is.False);
            session.new_game();
            Assert.That(session.run.chapter.id, Is.EqualTo("coming_home"));
            Assert.That(session.run.has_happened("level_started"), Is.True);
            Assert.That(session.has_save, Is.True);
        }

        // Continue with nothing saved is just a new game.
        [Test]
        public void continue_with_no_save_starts_fresh()
        {
            var session = new_session();
            session.continue_game();
            Assert.That(session.run.chapter.id, Is.EqualTo("coming_home"));
            Assert.That(session.restored_places, Is.Null);
        }

        // Story lines reach the subtitles on the next frame, with his name filled in.
        [Test]
        public void story_lines_reach_the_subtitles()
        {
            var session = new_session();
            session.new_game();
            for (int frame = 0; frame < 200 && !session.subtitles.showing; frame++)  // Up to 10 s: the first line comes 4.5 s in.
            {
                session.tick(0.05, here, safe_spot: true);
            }
            Assert.That(session.subtitles.showing, Is.True);
            Assert.That(session.subtitles.text, Does.Not.Contain("{dog}"));
        }

        // A story moment (new beat, new character or new goal) saves at the next safe spot;
        // never while someone is in mid-air.
        [Test]
        public void a_story_moment_saves_at_the_next_safe_spot()
        {
            var session = new_session();
            session.new_game();
            session.tick(0.25, here, safe_spot: true);
            int saves_before = session.chapter_saves_written;
            ChapterRobot.finish(session.run, skip_films: true, stop_when: run => run.objective != "" && run.can_save);
            session.tick(0.25, here, safe_spot: false);
            Assert.That(session.chapter_saves_written, Is.EqualTo(saves_before), "not in mid-air");
            session.tick(0.25, here, safe_spot: true);
            Assert.That(session.chapter_saves_written, Is.EqualTo(saves_before + 1));
        }

        // Without any story moment, it still saves every 30 seconds, and not more often.
        [Test]
        public void it_saves_every_half_minute()
        {
            var session = new_session();
            session.new_game();
            ChapterRobot.finish(session.run, skip_films: true, stop_when: run => run.can_save);
            session.tick(0.25, here, safe_spot: true);
            int saves_before = session.chapter_saves_written;
            int story_moments = 0;
            string objective = session.run.objective;
            for (double seconds = 0; seconds < 2 * GameSession.autosave_seconds + 1; seconds += 0.5)
            {
                session.tick(0.5, here, safe_spot: true);
                if (session.run.objective != objective) story_moments += 1;
                objective = session.run.objective;
            }
            int saves = session.chapter_saves_written - saves_before;
            Assert.That(saves, Is.GreaterThanOrEqualTo(2));
            Assert.That(saves, Is.LessThanOrEqualTo(2 + story_moments));
        }

        // Every story moment in the whole year (a new beat, a new character to play, a new goal)
        // saves at once when it can, or waits for the next safe moment.
        [Test]
        public void every_story_moment_in_the_year_asks_for_a_save()
        {
            var session = new_session();
            session.new_game();
            while (!session.campaign.is_finished)
            {
                var before = (session.run.current_beat, session.run.controlled_character, session.run.objective);
                ChapterRobot.finish(session.run, skip_films: false, after_each_step: run =>
                {
                    var now = (run.current_beat, run.controlled_character, run.objective);
                    int saves_before = session.chapter_saves_written;
                    session.tick(0, here, safe_spot: true);
                    if (!now.Equals(before) && !run.is_complete)
                    {
                        Assert.That(session.chapter_saves_written > saves_before || (session.save_waiting && !run.can_save), Is.True,
                            run.chapter.id + " after " + run.event_order[run.event_order.Count - 1]);
                    }
                    before = now;
                });
                session.tick(0, here, safe_spot: true);
                session.next_chapter();
            }
        }

        // The phone hides the game: save now, if it's a moment that can be saved.
        [Test]
        public void saving_now_only_at_a_safe_moment()
        {
            var session = new_session();
            session.new_game();
            Assert.That(session.run.can_save, Is.False, "the opening film");
            Assert.That(session.save_now(here, safe_spot: true), Is.False);
            ChapterRobot.finish(session.run, skip_films: true, stop_when: run => run.can_save);
            Assert.That(session.save_now(here, safe_spot: false), Is.False);
            Assert.That(session.save_now(here, safe_spot: true), Is.True);
        }

        // Continue puts you back mid-chapter, in the same story state and the same places.
        [Test]
        public void continue_resumes_mid_chapter()
        {
            var first = new_session();
            first.new_game();
            play_until(first, run => run.event_order.Count >= 6 && run.can_save);
            Assert.That(first.save_now(here, safe_spot: true), Is.True);

            var later = new_session();
            later.continue_game();
            Assert.That(later.run.event_order, Is.EqualTo(first.run.event_order));
            Assert.That(later.restored_places.Value.dog_x, Is.EqualTo(here.dog_x));
            Assert.That(later.run.take_lines(), Is.Empty);
            later.tick(0.25, here, safe_spot: true);
            Assert.That(later.chapter_saves_written, Is.EqualTo(0), "continuing isn't itself a story moment");
            play_until(later, run => run.is_complete);
            later.next_chapter();
            Assert.That(later.restored_places, Is.Null, "the next chapter starts from its beginning");
        }

        // New Game forgets the chapter in progress, so Continue can't bring it back.
        [Test]
        public void a_new_game_forgets_the_old_chapter()
        {
            var first = new_session();
            first.new_game();
            play_until(first, run => run.event_order.Count >= 6 && run.can_save);
            first.save_now(here, safe_spot: true);
            first.new_game();
            var later = new_session();
            later.continue_game();
            Assert.That(later.restored_places, Is.Null);
            Assert.That(later.run.event_order, Is.EqualTo(new[] { "level_started" }));
        }

        // Finishing a chapter records it, clears the chapter save and waits for the next one.
        [Test]
        public void finishing_a_chapter_moves_the_year_on()
        {
            var session = new_session();
            session.new_game();
            play_until(session, run => run.event_order.Count >= 6 && run.can_save);
            Assert.That(session.save_now(here, safe_spot: true), Is.True);
            play_until(session, run => run.is_complete);
            Assert.That(session.chapter_finished, Is.True);
            Assert.That(session.campaign.current_chapter.id, Is.EqualTo("the_storm"));
            var reloaded = new_session();
            reloaded.continue_game();
            Assert.That(reloaded.run.chapter.id, Is.EqualTo("the_storm"));
            Assert.That(reloaded.restored_places, Is.Null, "the old chapter's save is gone");
            Assert.That(reloaded.refused_saves, Is.EqualTo(0));
            Assert.That(session.next_chapter(), Is.True);
            Assert.That(session.run.chapter.id, Is.EqualTo("the_storm"));
            session.tick(0, here, safe_spot: false);
            Assert.That(session.save_waiting, Is.False, "a chapter's start isn't itself a story moment");
            Assert.That(session.chapter_finished, Is.False);
            Assert.That(session.next_chapter(), Is.False, "only once a chapter is finished");
        }

        // Skipping a film clears its lines from the screen too.
        [Test]
        public void skipping_a_film_clears_the_subtitles()
        {
            var session = new_session();
            session.new_game();
            for (int frame = 0; frame < 200 && !session.subtitles.showing; frame++)
            {
                session.tick(0.05, here, safe_spot: true);
            }
            Assert.That(session.subtitles.showing, Is.True, "a line of the opening film");
            Assert.That(session.skip_film(), Is.True);
            session.tick(0.05, here, safe_spot: true);
            Assert.That(session.run.cutscene_playing, Is.False);
            Assert.That(session.subtitles.showing, Is.False);
            for (int frame = 0; frame < 400 && !session.subtitles.showing; frame++)
            {
                session.tick(0.05, here, safe_spot: true);
            }
            Assert.That(session.subtitles.showing, Is.True, "a line after the film");
            Assert.That(session.skip_film(), Is.False, "no film playing now");
            Assert.That(session.subtitles.showing, Is.True, "and the line stays");
        }

        // After the last chapter there's nothing to play, before or after reloading.
        [Test]
        public void a_finished_year_has_nothing_to_play()
        {
            var session = new_session();
            session.new_game();
            for (int chapter = 0; chapter < 12; chapter++)
            {
                play_until(session, run => run.is_complete);
                session.next_chapter();
            }
            Assert.That(session.campaign.is_finished, Is.True);
            Assert.That(session.run, Is.Null);
            session.tick(0.25, here, safe_spot: true);  // Nothing to do, and no crash.
            var reloaded = new_session();
            reloaded.continue_game();
            Assert.That(reloaded.campaign.is_finished, Is.True);
            Assert.That(reloaded.run, Is.Null);
        }

        // Break it: a damaged chapter save falls back to the one before it.
        [Test]
        public void a_damaged_chapter_save_uses_the_backup()
        {
            var first = new_session();
            first.new_game();
            play_until(first, run => run.event_order.Count >= 6 && run.can_save);
            first.save_now(here, safe_spot: true);
            var events_then = first.run.event_order.Count;
            play_until(first, run => run.event_order.Count >= events_then + 2 && run.can_save);
            first.save_now(here, safe_spot: true);
            File.WriteAllText(Path.Combine(folder, "chapter.save"), "garbage\n");

            var later = new_session();
            later.continue_game();
            Assert.That(later.restored_places, Is.Not.Null);
            Assert.That(later.run.event_order.Count, Is.LessThan(first.run.event_order.Count));
            Assert.That(later.refused_saves, Is.EqualTo(1));
        }

        // Break it: when both chapter saves are damaged, the chapter starts again from its start.
        [Test]
        public void two_damaged_chapter_saves_restart_the_chapter()
        {
            var first = new_session();
            first.new_game();
            play_until(first, run => run.event_order.Count >= 6 && run.can_save);
            first.save_now(here, safe_spot: true);
            File.WriteAllText(Path.Combine(folder, "chapter.save"), "garbage\n");
            File.WriteAllText(Path.Combine(folder, "chapter.save.backup"), "more garbage\n");

            var later = new_session();
            later.continue_game();
            Assert.That(later.run.chapter.id, Is.EqualTo("coming_home"));
            Assert.That(later.restored_places, Is.Null);
            Assert.That(later.refused_saves, Is.EqualTo(2));
        }

        // Break it: a damaged year save falls back to its backup; with both damaged, a new game.
        [Test]
        public void a_damaged_year_save_uses_the_backup()
        {
            var first = new_session();
            first.new_game();
            play_until(first, run => run.is_complete);
            first.next_chapter();
            play_until(first, run => run.is_complete);
            for (int frame = 0; frame < 5; frame++)
            {
                first.tick(0.25, here, safe_spot: true);  // The aftermath screen: nothing is saved again.
            }
            File.WriteAllText(Path.Combine(folder, "campaign.save"), "garbage\n");

            var later = new_session();
            later.continue_game();
            Assert.That(later.campaign.current_chapter.id, Is.EqualTo("the_storm"), "the backup, one chapter earlier");
            File.WriteAllText(Path.Combine(folder, "campaign.save.backup"), "garbage\n");
            later.continue_game();  // The same session again: its year must not carry over.
            Assert.That(later.campaign.current_chapter.id, Is.EqualTo("coming_home"));
            Assert.That(new_session().refused_saves, Is.EqualTo(0));
            var last = new_session();
            last.continue_game();
            Assert.That(last.refused_saves, Is.EqualTo(2));
        }

        // Break it: calling things before a game has started does nothing and doesn't crash.
        [Test]
        public void nothing_happens_before_a_game_starts()
        {
            var session = new_session();
            session.tick(0.25, here, safe_spot: true);
            Assert.That(session.save_now(here, safe_spot: true), Is.False);
            Assert.That(session.skip_film(), Is.False);
            Assert.That(session.next_chapter(), Is.False);
            Assert.That(session.run, Is.Null);
            Assert.Throws<ArgumentNullException>(() => new GameSession(null, "Bodhi"));
        }

        // Break it: one session used again (New Game or Continue after playing) starts clean:
        // nothing on screen, no save waiting, the half-minute count from zero.
        [Test]
        public void a_session_used_again_starts_clean()
        {
            foreach (bool use_continue in new[] { false, true })
            {
                string label = use_continue ? "continue" : "new game";
                var session = new_session();
                session.new_game();
                ChapterRobot.finish(session.run, skip_films: true, stop_when: run => run.can_save && run.objective != "");
                Assert.That(session.save_now(here, safe_spot: true), Is.True);  // The save Continue goes back to.
                string objective_then = session.run.objective;
                ChapterRobot.finish(session.run, skip_films: true, stop_when: run => run.objective != objective_then);
                session.tick(20, here, safe_spot: false);
                for (int frame = 0; frame < 400 && !session.subtitles.showing; frame++)
                {
                    session.tick(0.05, here, safe_spot: false);
                }
                Assert.That(session.subtitles.showing && session.save_waiting, Is.True, label + ": set up");
                if (use_continue)
                {
                    session.continue_game();
                    Assert.That(session.restored_places, Is.Not.Null, label);
                }
                else
                {
                    session.new_game();
                }
                Assert.That(session.subtitles.showing, Is.False, label);
                Assert.That(session.save_waiting, Is.False, label);
                session.tick(0.25, here, safe_spot: true);
                int saves = session.chapter_saves_written;
                session.tick(15, here, safe_spot: true);
                Assert.That(session.chapter_saves_written, Is.EqualTo(saves), label + ": no half-minute save yet");
            }
        }

        // Break it: a frame time that isn't a number doesn't stop the half-minute saves.
        [Test]
        public void a_bad_frame_time_doesnt_stop_autosave()
        {
            var session = new_session();
            session.new_game();
            ChapterRobot.finish(session.run, skip_films: true, stop_when: run => run.can_save);
            session.tick(0.25, here, safe_spot: true);
            session.tick(double.NaN, here, safe_spot: true);
            int saves_before = session.chapter_saves_written;
            for (double seconds = 0; seconds < GameSession.autosave_seconds + 1; seconds += 0.5)
            {
                session.tick(0.5, here, safe_spot: true);
            }
            Assert.That(session.chapter_saves_written, Is.GreaterThan(saves_before));
        }

        // The title's memory box reads the saved year without starting or changing anything.
        [Test]
        public void the_saved_year_is_read_without_starting_a_chapter()
        {
            var session = new_session();
            session.new_game();
            play_until(session, run => run.is_complete);
            var title_screen = new_session();
            var year = title_screen.saved_year();
            Assert.That(year.current_chapter.id, Is.EqualTo("the_storm"));
            Assert.That(year.keepsakes_found, Is.EqualTo(session.campaign.keepsakes_found));
            Assert.That(title_screen.run, Is.Null, "reading the year starts no chapter");
        }

        // Break it: with no save, or a damaged one, it's a new year, and nothing is counted or written.
        [Test]
        public void break_it_a_missing_or_damaged_year_reads_as_a_new_one()
        {
            var session = new_session();
            Assert.That(session.saved_year().chapter_number, Is.EqualTo(1));
            new SaveFiles(folder).write("campaign", "nonsense\nchapter 999");
            Assert.That(session.saved_year().chapter_number, Is.EqualTo(1));
            Assert.That(session.refused_saves, Is.EqualTo(0), "looking isn't loading");
            Assert.That(new SaveFiles(folder).read("campaign"), Is.EqualTo("nonsense\nchapter 999"), "nothing is written");
        }
    }
}
