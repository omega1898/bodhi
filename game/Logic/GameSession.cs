// Purpose: the one object the Unity scene talks to while the game runs. It holds the year
// (Campaign) and the chapter being played (ChapterRun), passes story lines to the subtitles, and
// saves at the browser game's moments: at every story moment (a new beat, a new character to
// play or a new goal), every 30 seconds, and when the phone hides the game. It handles the title
// screen's New Game and Continue, and moving on to the next chapter.
//
// The scene still decides everything about space: where the characters stand, and whether this
// is a safe spot to save (both on the ground). It passes those in each frame.
using System;
using Bodhi.Rules;

namespace Bodhi.Game
{
    public class GameSession
    {
        public const double autosave_seconds = 30;
        const string campaign_slot = "campaign";  // The year: chapter, care, keepsakes, memories.
        const string chapter_slot = "chapter";    // The chapter in progress.

        readonly SaveFiles files;
        double seconds_since_save = 0;
        bool save_wanted = false;                 // A story moment happened; save at the next safe spot.
        ChapterBeat last_beat;
        Character last_controlled;
        string last_objective;

        // The year being played.
        public Campaign campaign { get; private set; } = new Campaign();

        // The chapter being played, or null before a game starts and after the last chapter.
        public ChapterRun run { get; private set; }

        // The story lines on screen.
        public SubtitlePacer subtitles { get; }

        // Where the two of them stood in the save Continue loaded, or null when the chapter
        // started from its beginning. The scene puts them there if each spot is free.
        public SavedPlaces? restored_places { get; private set; }

        // True once the chapter has ended and been recorded; the scene shows the aftermath,
        // then calls next_chapter().
        public bool chapter_finished { get; private set; }

        // Saves that were found but refused as damaged, for tests and for a curious player.
        public int refused_saves { get; private set; }

        // Chapter saves written so far (for tests).
        public int chapter_saves_written { get; private set; }

        // True when a story moment is waiting for a safe spot to be saved (for tests).
        public bool save_waiting => save_wanted;

        // True if there's a game to continue.
        public bool has_save => files.read(campaign_slot) != null;

        // Makes a session that keeps its saves in these files, and fills {dog} with this name.
        public GameSession(SaveFiles files, string dog_name)
        {
            this.files = files ?? throw new ArgumentNullException(nameof(files));
            subtitles = new SubtitlePacer(dog_name);
        }

        // New Game: forgets any save and starts the first chapter.
        public void new_game()
        {
            campaign = new Campaign();
            files.write(campaign_slot, campaign.to_text());
            start_chapter();
        }

        // Continue: loads the year, then the chapter in progress, each from its newest save or
        // else its backup. A chapter with no usable save starts from its beginning; with no
        // usable year save at all, it's a new game.
        public void continue_game()
        {
            campaign = new Campaign();  // A failed load leaves it like this: a new year.
            if (!load_campaign(files.read(campaign_slot)))
            {
                load_campaign(files.read_backup(campaign_slot));
            }
            ChapterRun resumed = resume_chapter(files.read(chapter_slot)) ?? resume_chapter(files.read_backup(chapter_slot));
            if (resumed == null)
            {
                start_chapter();
                return;
            }
            run = resumed;
            start_fresh_moment();
        }

        // One frame: time passes in the story and the subtitles, lines reach the screen, a
        // finished chapter is recorded, and the chapter saves if it's time. `places_now` is where
        // the two of them stand; `safe_spot` is true when both are on the ground.
        public void tick(double delta_seconds, SavedPlaces places_now, bool safe_spot)
        {
            if (run == null || chapter_finished)
            {
                return;
            }
            run.pass_time(delta_seconds);
            subtitles.add(run.take_lines());
            subtitles.pass_time(delta_seconds);
            if (run.is_complete)
            {
                finish_chapter();
                return;
            }
            if (run.current_beat != last_beat || run.controlled_character != last_controlled || run.objective != last_objective)
            {
                save_wanted = true;
                remember_story_moment();
            }
            if (delta_seconds > 0 && !double.IsInfinity(delta_seconds))
            {
                seconds_since_save += delta_seconds;
            }
            if (save_wanted || seconds_since_save >= autosave_seconds)
            {
                save_now(places_now, safe_spot);
            }
        }

        // Saves the chapter now if it's a moment that can be saved: for when the phone hides the
        // game. True if it saved.
        public bool save_now(SavedPlaces places_now, bool safe_spot)
        {
            if (run == null || !safe_spot || !run.can_save)
            {
                return false;
            }
            if (!files.write(chapter_slot, run.to_save_text(places_now)))
            {
                return false;
            }
            chapter_saves_written += 1;
            save_wanted = false;
            seconds_since_save = 0;
            return true;
        }

        // Skip on a film scene: clears its lines from the screen, then jumps to its end. Lines the
        // end of the scene says still show, as in the browser game. False if no film is playing.
        public bool skip_film()
        {
            if (run == null || !run.cutscene_playing)
            {
                return false;
            }
            subtitles.clear();
            return run.skip_cutscene();
        }

        // After the aftermath screen: starts the next chapter. False unless a chapter just ended.
        public bool next_chapter()
        {
            if (!chapter_finished)
            {
                return false;
            }
            start_chapter();
            return true;
        }

        // Records the finished chapter in the year and saves the year. The chapter save is no
        // longer needed.
        void finish_chapter()
        {
            campaign.finish_chapter(run);
            files.write(campaign_slot, campaign.to_text());
            files.erase(chapter_slot);
            chapter_finished = true;
        }

        // Starts the year's next chapter from its beginning (or nothing, once the year is over).
        // Any chapter save is removed first, so Continue can't bring back an older chapter.
        void start_chapter()
        {
            restored_places = null;
            files.erase(chapter_slot);
            run = campaign.begin_chapter();
            run?.start();
            start_fresh_moment();
        }

        // A chapter has just started or been continued: not finished, nothing on screen, no save
        // waiting, the half-minute count from zero, and the story's beat, character and goal noted.
        void start_fresh_moment()
        {
            chapter_finished = false;
            subtitles.clear();
            save_wanted = false;
            seconds_since_save = 0;
            if (run != null)
            {
                remember_story_moment();
            }
        }

        // The year as saved, read without starting a chapter, counting anything or writing anything.
        // The memory box on the title screen shows it. A new year if there's no usable save.
        public Campaign saved_year()
        {
            var year = new Campaign();
            string text = files.read(campaign_slot);
            if (text != null && year.load_text(text))
            {
                return year;
            }
            string backup = files.read_backup(campaign_slot);
            if (backup != null && year.load_text(backup))
            {
                return year;
            }
            return new Campaign();
        }

        // Loads the year from a save's text, counting a damaged one. False if there was none.
        bool load_campaign(string text)
        {
            if (text == null)
            {
                return false;
            }
            if (campaign.load_text(text))
            {
                return true;
            }
            refused_saves += 1;
            return false;
        }

        // Carries on the chapter from a save's text, counting a damaged one. Null if none.
        ChapterRun resume_chapter(string text)
        {
            if (text == null)
            {
                return null;
            }
            var resumed = campaign.resume_chapter(text, out SavedPlaces places);
            if (resumed == null)
            {
                refused_saves += 1;
                return null;
            }
            restored_places = places;
            return resumed;
        }

        // Notes the story's beat, character and goal, to notice the next story moment.
        void remember_story_moment()
        {
            last_beat = run.current_beat;
            last_controlled = run.controlled_character;
            last_objective = run.objective;
        }
    }
}
