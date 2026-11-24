// Purpose: turns the station chapter's progress into a small text file and back again.
// Ported from core/src/railway_save.cpp. The loader treats the file as hostile: a player can
// edit it, a crash can cut it short, and an older or newer game can leave a file this code
// doesn't understand. Nothing in the chapter changes unless every check passes (spec R7.3).
//
// Example of a version 1 save file (one "name value" pair per line):
//   bodhi_railway_save 1
//   beat final_obstacle
//   controlled_character dog
//   latest_time_ms 381500
//   ...
using System;
using System.Globalization;
using System.Text;

namespace Bodhi.Rules
{
    // Why a save file was refused. `none` means it loaded.
    public enum SaveLoadError
    {
        none,
        file_too_large,
        bad_character,
        bad_line,
        missing_header,
        unknown_version,
        unknown_field,
        duplicate_field,
        missing_field,
        unknown_beat,
        bad_value,
        impossible_combination,
    }

    // "partial" means this class is written across two files; the rest is in RailwayChapter.cs.
    public partial class RailwayChapter
    {
        const string save_header = "bodhi_railway_save";
        const long supported_save_version = 1;
        const int maximum_save_characters = 4096;
        const long maximum_time_milliseconds = (long)TelemetryLog.maximum_time_seconds * 1000;
        const long feeling_scale = 1000000;  // Feelings are saved in millionths.

        // Every field after the header line, in the order save_progress writes them.
        // The eight "finished_ms_" names follow the order of ChapterBeat.
        static readonly string[] save_field_names =
        {
            "beat", "controlled_character", "latest_time_ms", "beat_started_at_ms",
            "finished_ms_together", "finished_ms_separation", "finished_ms_human_section",
            "finished_ms_dog_section", "finished_ms_near_contact", "finished_ms_final_obstacle",
            "finished_ms_reunion", "finished_ms_aftermath",
            "human_final_action_done", "dog_final_action_done",
            "closeness_need_millionths", "fear_millionths", "trust_millionths",
        };

        // Positions in save_field_names, so code reads "beat_field" instead of a bare number.
        const int beat_field = 0;
        const int controlled_field = 1;
        const int latest_time_field = 2;
        const int beat_started_field = 3;
        const int first_finished_field = 4;
        const int human_action_field = 12;
        const int dog_action_field = 13;
        const int closeness_field = 14;
        const int fear_field = 15;
        const int trust_field = 16;

        // The loaded values, held here until every check passes.
        class SavedProgress
        {
            public ChapterBeat beat;
            public Character controlled;
            public long latest_time_ms;
            public long beat_started_at_ms;
            public readonly long[] finished_ms_by_beat = new long[ChapterBeats.beat_count];
            public bool human_final_action_done;
            public bool dog_final_action_done;
            public long closeness_need_millionths;
            public long fear_millionths;
            public long trust_millionths;
        }

        // A readable name for each error, for the playtest log.
        public static string save_load_error_name(SaveLoadError error)
        {
            return Enum.IsDefined(typeof(SaveLoadError), error) ? error.ToString() : "unknown";
        }

        // The chapter's progress as save-file text.
        public string save_progress()
        {
            long latest_ms = to_milliseconds(latest_time_seconds);
            var text = new StringBuilder();
            text.Append(save_header).Append(' ').Append(supported_save_version).Append('\n');
            append_field(text, beat_field, ChapterBeats.name_of(current_beat));
            append_field(text, controlled_field, controlled_character.ToString());
            append_field(text, latest_time_field, number_text(latest_ms));
            // Math.Min keeps rounding from ever writing a time later than latest_ms,
            // which the loader would refuse as impossible.
            append_field(text, beat_started_field, number_text(Math.Min(to_milliseconds(beat_started_at_seconds), latest_ms)));
            for (int index = 0; index < ChapterBeats.beat_count; index += 1)
            {
                append_field(text, first_finished_field + index, number_text(Math.Min(to_milliseconds(finished_seconds_by_beat[index]), latest_ms)));
            }
            append_field(text, human_action_field, human_final_action_done ? "1" : "0");
            append_field(text, dog_action_field, dog_final_action_done ? "1" : "0");
            append_field(text, closeness_field, number_text(to_millionths(bond.closeness_need)));
            append_field(text, fear_field, number_text(to_millionths(bond.fear)));
            append_field(text, trust_field, number_text(to_millionths(bond.trust)));
            return text.ToString();
        }

        // Loads progress from save-file text. Returns why it was refused, or `none`.
        // A refused file leaves the chapter exactly as it was.
        public SaveLoadError load_progress(string save_text)
        {
            var values = new string[save_field_names.Length];
            SaveLoadError error = read_save_fields(save_text, values);
            if (error != SaveLoadError.none)
            {
                return error;
            }
            var progress = new SavedProgress();
            error = convert_save_fields(values, progress);
            if (error != SaveLoadError.none)
            {
                return error;
            }
            if (!is_reachable(progress))
            {
                return SaveLoadError.impossible_combination;
            }

            // Every check passed: only now does anything in the chapter change.
            current_beat = progress.beat;
            controlled_character = progress.controlled;
            latest_time_seconds = progress.latest_time_ms / 1000.0;
            beat_started_at_seconds = progress.beat_started_at_ms / 1000.0;
            for (int index = 0; index < ChapterBeats.beat_count; index += 1)
            {
                finished_seconds_by_beat[index] = progress.finished_ms_by_beat[index] / 1000.0;
            }
            human_final_action_done = progress.human_final_action_done;
            dog_final_action_done = progress.dog_final_action_done;
            bond.closeness_need = (double)progress.closeness_need_millionths / feeling_scale;
            bond.fear = (double)progress.fear_millionths / feeling_scale;
            bond.trust = (double)progress.trust_millionths / feeling_scale;
            telemetry.record(latest_time_seconds, "progress_loaded", ChapterBeats.name_of(current_beat));
            return SaveLoadError.none;
        }

        // Step 1: checks the file's shape and collects each field's text, without judging values.
        static SaveLoadError read_save_fields(string save_text, string[] values)
        {
            if (save_text == null || save_text.Length == 0)
            {
                return SaveLoadError.bad_line;
            }
            if (save_text.Length > maximum_save_characters)
            {
                return SaveLoadError.file_too_large;
            }
            // Only these characters may appear. Refusing everything else up front means later
            // steps never meet a carriage return, a zero, a minus sign or a decimal point.
            foreach (char letter in save_text)
            {
                bool allowed = (letter >= 'a' && letter <= 'z') || (letter >= '0' && letter <= '9') || letter == '_' || letter == ' ' || letter == '\n';
                if (!allowed)
                {
                    return SaveLoadError.bad_character;
                }
            }
            if (save_text[save_text.Length - 1] != '\n')
            {
                return SaveLoadError.bad_line;  // Cut short mid-line.
            }
            var seen = new bool[save_field_names.Length];
            bool is_header_line = true;
            int line_start = 0;
            while (line_start < save_text.Length)
            {
                // Always found, because the text was checked to end with a newline.
                int line_end = save_text.IndexOf('\n', line_start);
                string line = save_text.Substring(line_start, line_end - line_start);
                line_start = line_end + 1;

                if (!split_line(line, out string name, out string value))
                {
                    return SaveLoadError.bad_line;
                }
                if (is_header_line)
                {
                    is_header_line = false;
                    if (name != save_header)
                    {
                        return SaveLoadError.missing_header;
                    }
                    if (!parse_whole_number(value, 1000000, out long version) || version != supported_save_version)
                    {
                        return SaveLoadError.unknown_version;
                    }
                    continue;
                }
                int index = Array.IndexOf(save_field_names, name);
                if (index < 0)
                {
                    return SaveLoadError.unknown_field;
                }
                if (seen[index])
                {
                    return SaveLoadError.duplicate_field;
                }
                seen[index] = true;
                values[index] = value;
            }
            foreach (bool was_seen in seen)
            {
                if (!was_seen)
                {
                    return SaveLoadError.missing_field;  // Also catches a header-only file.
                }
            }
            return SaveLoadError.none;
        }

        // Step 2: turns each field's text into a value, checking each one's own range.
        static SaveLoadError convert_save_fields(string[] values, SavedProgress progress)
        {
            bool beat_found = false;
            for (int index = 0; index < ChapterBeats.beat_count; index += 1)
            {
                if (values[beat_field] == ChapterBeats.name_of((ChapterBeat)index))
                {
                    progress.beat = (ChapterBeat)index;
                    beat_found = true;
                }
            }
            if (!beat_found)
            {
                return SaveLoadError.unknown_beat;
            }
            if (values[controlled_field] == "human")
            {
                progress.controlled = Character.human;
            }
            else if (values[controlled_field] == "dog")
            {
                progress.controlled = Character.dog;
            }
            else
            {
                return SaveLoadError.bad_value;
            }
            bool all_numbers_fit =
                parse_whole_number(values[latest_time_field], maximum_time_milliseconds, out progress.latest_time_ms)
                & parse_whole_number(values[beat_started_field], maximum_time_milliseconds, out progress.beat_started_at_ms)
                & parse_whole_number(values[human_action_field], 1, out long human_done)
                & parse_whole_number(values[dog_action_field], 1, out long dog_done)
                & parse_whole_number(values[closeness_field], feeling_scale, out progress.closeness_need_millionths)
                & parse_whole_number(values[fear_field], feeling_scale, out progress.fear_millionths)
                & parse_whole_number(values[trust_field], feeling_scale, out progress.trust_millionths);
            for (int index = 0; index < ChapterBeats.beat_count; index += 1)
            {
                all_numbers_fit &= parse_whole_number(values[first_finished_field + index], maximum_time_milliseconds, out progress.finished_ms_by_beat[index]);
            }
            if (!all_numbers_fit)
            {
                return SaveLoadError.bad_value;
            }
            progress.human_final_action_done = human_done == 1;
            progress.dog_final_action_done = dog_done == 1;
            return SaveLoadError.none;
        }

        // Step 3: refuses combinations real play can never produce, because the story rules
        // couldn't continue from them (for example, Bodhi in control during the reunion).
        static bool is_reachable(SavedProgress progress)
        {
            if (progress.beat_started_at_ms > progress.latest_time_ms)
            {
                return false;
            }
            foreach (long finished_ms in progress.finished_ms_by_beat)
            {
                if (finished_ms > progress.latest_time_ms)
                {
                    return false;
                }
            }
            // Who is in control must match the beat, as enter_beat decides.
            bool either_allowed = progress.beat == ChapterBeat.near_contact || progress.beat == ChapterBeat.final_obstacle;
            Character expected = progress.beat == ChapterBeat.dog_section ? Character.dog : Character.human;
            if (!either_allowed && progress.controlled != expected)
            {
                return false;
            }
            // The final actions only count during the final obstacle, both are needed to leave
            // it, and leaving it is the only way to reach the reunion.
            bool human_done = progress.human_final_action_done;
            bool dog_done = progress.dog_final_action_done;
            if (progress.beat == ChapterBeat.final_obstacle)
            {
                return !(human_done && dog_done);
            }
            if (progress.beat == ChapterBeat.reunion || progress.beat == ChapterBeat.aftermath)
            {
                return human_done && dog_done;
            }
            return !human_done && !dog_done;
        }

        // Splits "name value" at its single space. Refuses empty parts and extra spaces.
        static bool split_line(string line, out string name, out string value)
        {
            name = "";
            value = "";
            int space = line.IndexOf(' ');
            if (space <= 0 || space + 1 == line.Length || line.IndexOf(' ', space + 1) >= 0)
            {
                return false;
            }
            name = line.Substring(0, space);
            value = line.Substring(space + 1);
            return true;
        }

        // Reads digits only (no sign, no decimal point) as a number from 0 to `maximum`.
        // At most 12 digits, so the number can't overflow before the range check.
        static bool parse_whole_number(string text, long maximum, out long number)
        {
            number = 0;
            if (string.IsNullOrEmpty(text) || text.Length > 12)
            {
                return false;
            }
            long result = 0;
            foreach (char digit in text)
            {
                if (digit < '0' || digit > '9')
                {
                    return false;
                }
                result = result * 10 + (digit - '0');
            }
            if (result > maximum)
            {
                return false;
            }
            number = result;
            return true;
        }

        // Seconds to whole milliseconds, rounded down so a saved time is never later than the
        // real one (a later time would make the next real event look like it went backwards).
        static long to_milliseconds(double seconds)
        {
            double safe_seconds = double.IsNaN(seconds) ? 0.0 : Math.Clamp(seconds, 0.0, TelemetryLog.maximum_time_seconds);
            return (long)Math.Floor(safe_seconds * 1000.0);
        }

        // A feeling (0 to 1) to the nearest millionth.
        static long to_millionths(double feeling)
        {
            double safe_feeling = double.IsNaN(feeling) ? 0.0 : Math.Clamp(feeling, 0.0, 1.0);
            return (long)Math.Round(safe_feeling * feeling_scale, MidpointRounding.AwayFromZero);
        }

        // A whole number as plain digits, the same on every phone.
        static string number_text(long number)
        {
            return number.ToString(CultureInfo.InvariantCulture);
        }

        // Adds one "name value" line to a save file being written.
        static void append_field(StringBuilder text, int field, string value)
        {
            text.Append(save_field_names[field]).Append(' ').Append(value).Append('\n');
        }
    }
}
