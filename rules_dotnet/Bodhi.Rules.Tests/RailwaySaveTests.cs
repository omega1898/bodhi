// Purpose: proves the station chapter's progress survives a save and load, and that every kind
// of broken or hostile save file is refused without changing the chapter (spec R7.3).
// Ported from core/tests/test_railway_save.cpp, including its 10,000-file fuzz test.
using System;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class RailwaySaveTests
    {
        // Plays into the final obstacle and pulls the lever, so the save holds a half-finished
        // two-route puzzle with Bodhi in control: the hardest state to restore correctly.
        static void play_into_final_obstacle(RailwayChapter chapter)
        {
            chapter.report_world_event("walkway_collapsed", 60.0);
            chapter.report_world_event("separation_scene_finished", 75.0);
            chapter.report_world_event("maintenance_power_restored", 140.0);
            chapter.report_world_event("dog_barked_from_window", 230.0);
            chapter.report_world_event("pair_met_at_fence", 300.0);
            chapter.report_world_event("fence_touch_finished", 345.0);
            chapter.report_world_event("jammed_gate_lever_pulled", 380.0);
            chapter.request_character_switch(381.5);
        }

        // A known-good save file to break in different ways.
        static string valid_save()
        {
            var chapter = new RailwayChapter();
            play_into_final_obstacle(chapter);
            return chapter.save_progress();
        }

        // Replaces the whole line that starts with "name " by `new_line` (empty removes it).
        static string replace_line(string save_text, string name, string new_line)
        {
            int start = save_text.IndexOf("\n" + name + " ", StringComparison.Ordinal) + 1;
            int end = save_text.IndexOf('\n', start) + 1;
            return save_text.Substring(0, start) + new_line + save_text.Substring(end);
        }

        // Everything after the header line.
        static string body() => valid_save().Substring(valid_save().IndexOf('\n') + 1);

        // Loads a file into a chapter with progress, checks the refusal, and checks the chapter
        // is exactly as it was: same progress, no new log entries.
        static void check_refused(string save_text, SaveLoadError expected_error)
        {
            var chapter = new RailwayChapter();
            chapter.report_world_event("walkway_collapsed", 60.0);
            string progress_before = chapter.save_progress();
            int log_size_before = chapter.telemetry.entries.Count;
            Assert.That(chapter.load_progress(save_text), Is.EqualTo(expected_error));
            Assert.That(chapter.save_progress(), Is.EqualTo(progress_before));
            Assert.That(chapter.telemetry.entries.Count, Is.EqualTo(log_size_before));
        }

        // Round trip: a loaded chapter matches the saved one and can be finished from there.
        [Test]
        public void a_saved_chapter_loads_and_can_be_finished()
        {
            var original = new RailwayChapter();
            play_into_final_obstacle(original);
            var restored = new RailwayChapter();
            Assert.That(restored.load_progress(valid_save()), Is.EqualTo(SaveLoadError.none));
            Assert.That(restored.save_progress(), Is.EqualTo(valid_save()));
            Assert.That(restored.current_beat, Is.EqualTo(ChapterBeat.final_obstacle));
            Assert.That(restored.controlled_character, Is.EqualTo(Character.dog));
            Assert.That(restored.bond.fear, Is.EqualTo(original.bond.fear).Within(0.000001));
            Assert.That(restored.seconds_spent_in(ChapterBeat.near_contact, 400.0), Is.EqualTo(45.0));
            // The lever pull was saved, so Bodhi's action alone now finishes the puzzle.
            restored.report_world_event("debris_cleared", 420.0);
            Assert.That(restored.current_beat, Is.EqualTo(ChapterBeat.reunion));
            Assert.That(restored.refused_input_count, Is.EqualTo(0));
            // Time can't go backwards past the loaded time.
            Assert.That(restored.report_world_event("glove_found", 100.0), Is.False);
        }

        // Each refusal the loader promises, one at a time.
        [Test]
        public void each_kind_of_bad_file_is_refused()
        {
            string save = valid_save();
            check_refused(body(), SaveLoadError.missing_header);
            check_refused("bodhi_railway_save 2\n" + body(), SaveLoadError.unknown_version);
            check_refused("bodhi_railway_save 0\n" + body(), SaveLoadError.unknown_version);
            check_refused("bodhi_railway_save 99999999999999999999\n", SaveLoadError.unknown_version);
            check_refused(replace_line(save, "beat", "beat secret_ending\n"), SaveLoadError.unknown_beat);
            check_refused(replace_line(save, "beat", "beat \n"), SaveLoadError.bad_line);
            check_refused(save + new string('a', 4096), SaveLoadError.file_too_large);
            check_refused(save + "fear_millionths 0\n", SaveLoadError.duplicate_field);
            check_refused(save + "beat together\n", SaveLoadError.duplicate_field);
            check_refused(replace_line(save, "trust_millionths", ""), SaveLoadError.missing_field);
            check_refused("bodhi_railway_save 1\n", SaveLoadError.missing_field);
            check_refused(save + "skip_to_reunion 1\n", SaveLoadError.unknown_field);
        }

        // Break it: a file of exactly 4096 characters isn't refused for its size, only its content.
        [Test]
        public void break_it_the_size_limit_is_over_four_kilobytes()
        {
            var chapter = new RailwayChapter();
            Assert.That(chapter.load_progress(new string('a', 4095) + "\n"), Is.Not.EqualTo(SaveLoadError.file_too_large));
        }

        // Break it: characters and shapes the format never writes.
        [Test]
        public void break_it_characters_and_shapes_never_written_are_refused()
        {
            string save = valid_save();
            check_refused("", SaveLoadError.bad_line);
            check_refused(null, SaveLoadError.bad_line);
            check_refused(save.Substring(0, save.Length - 1), SaveLoadError.bad_line);
            check_refused(replace_line(save, "fear_millionths", "fear_millionths -5\n"), SaveLoadError.bad_character);
            check_refused(replace_line(save, "fear_millionths", "fear_millionths 0.5\n"), SaveLoadError.bad_character);
            check_refused(replace_line(save, "beat", "beat Final_Obstacle\n"), SaveLoadError.bad_character);
            check_refused(save + "\r\n", SaveLoadError.bad_character);
            check_refused("bodhi_railway_save 1\0\n", SaveLoadError.bad_character);
            check_refused(replace_line(save, "beat", "beat final_obstaclé\n"), SaveLoadError.bad_character);
            check_refused(replace_line(save, "beat", "beat  final_obstacle\n"), SaveLoadError.bad_line);
            check_refused(save + "\n", SaveLoadError.bad_line);
        }

        // Break it: values out of range, including numbers too long to fit in memory.
        [Test]
        public void break_it_values_out_of_range_are_refused()
        {
            string save = valid_save();
            check_refused(replace_line(save, "fear_millionths", "fear_millionths 1000001\n"), SaveLoadError.bad_value);
            check_refused(replace_line(save, "latest_time_ms", "latest_time_ms 99999999999999\n"), SaveLoadError.bad_value);
            check_refused(replace_line(save, "human_final_action_done", "human_final_action_done 2\n"), SaveLoadError.bad_value);
            check_refused(replace_line(save, "controlled_character", "controlled_character cat\n"), SaveLoadError.bad_value);
            // More than 12 digits is refused even when the number is small, so no number can
            // grow big enough to wrap around before its range is checked.
            check_refused(replace_line(save, "dog_final_action_done", "dog_final_action_done 0000000000000\n"), SaveLoadError.bad_value);
        }

        // Break it: values fine alone but impossible together.
        [Test]
        public void break_it_impossible_combinations_are_refused()
        {
            string save = valid_save();
            check_refused(replace_line(save, "beat_started_at_ms", "beat_started_at_ms 999999\n"), SaveLoadError.impossible_combination);
            check_refused(replace_line(save, "dog_final_action_done", "dog_final_action_done 1\n"), SaveLoadError.impossible_combination);
            check_refused(replace_line(save, "beat", "beat reunion\n"), SaveLoadError.impossible_combination);
            // Bodhi in control during her section.
            string human_section = replace_line(
                replace_line(replace_line(save, "beat", "beat human_section\n"), "human_final_action_done", "human_final_action_done 0\n"),
                "controlled_character", "controlled_character dog\n");
            check_refused(human_section, SaveLoadError.impossible_combination);
        }

        // Break it: 10,000 randomly damaged save files. Each must either load (and then save
        // cleanly again) or be refused with the chapter unchanged. Never a crash. The fixed seed
        // makes every run test the same files, so a failure can be replayed.
        [Test]
        public void break_it_ten_thousand_damaged_files_never_crash()
        {
            string save = valid_save();
            var random_numbers = new Random(20261104);
            int loaded_count = 0;
            int refused_count = 0;
            for (int attempt = 0; attempt < 10000; attempt += 1)
            {
                var damaged = new System.Text.StringBuilder(save);
                int damage_count = 1 + random_numbers.Next(4);
                for (int damage = 0; damage < damage_count; damage += 1)
                {
                    int position = damaged.Length == 0 ? 0 : random_numbers.Next(damaged.Length);
                    char random_byte = (char)random_numbers.Next(256);
                    switch (random_numbers.Next(6))
                    {
                        case 0: if (damaged.Length > 0) { damaged[position] = random_byte; } break;          // Overwrite.
                        case 1: damaged.Insert(position, random_byte); break;                                 // Insert.
                        case 2: if (damaged.Length > 0) { damaged.Remove(position, 1); } break;               // Delete.
                        case 3: if (damaged.Length > 0) { damaged[position] = (char)('0' + random_numbers.Next(10)); } break;  // A digit.
                        case 4:                                                                                // Copy a chunk.
                            if (damaged.Length > 0)
                            {
                                string text = damaged.ToString();
                                int from = random_numbers.Next(text.Length);
                                damaged.Insert(position, text.Substring(from, Math.Min(40, text.Length - from)));
                            }
                            break;
                        default: damaged.Length = position; break;                                            // Cut short.
                    }
                }
                var chapter = new RailwayChapter();
                chapter.report_world_event("walkway_collapsed", 60.0);
                string progress_before = chapter.save_progress();
                if (chapter.load_progress(damaged.ToString()) == SaveLoadError.none)
                {
                    loaded_count += 1;
                    Assert.That(new RailwayChapter().load_progress(chapter.save_progress()), Is.EqualTo(SaveLoadError.none));
                }
                else
                {
                    refused_count += 1;
                    Assert.That(chapter.save_progress(), Is.EqualTo(progress_before));
                }
            }
            Assert.That(refused_count, Is.GreaterThan(9000));
            Assert.That(loaded_count + refused_count, Is.EqualTo(10000));
        }

        // Every error has a readable name for the playtest log.
        [Test]
        public void every_error_has_a_name()
        {
            foreach (SaveLoadError error in Enum.GetValues(typeof(SaveLoadError)))
            {
                Assert.That(RailwayChapter.save_load_error_name(error), Is.EqualTo(error.ToString()));
            }
            Assert.That(RailwayChapter.save_load_error_name((SaveLoadError)99), Is.EqualTo("unknown"));
        }
    }
}
