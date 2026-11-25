// Purpose: a playtest log that records what happened and when, so we can measure play
// (for example, how long players linger at the fence). Ported from core/src/telemetry_log.cpp.
// It refuses broken times, caps its own size, and escapes text so a line can't be faked.
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Bodhi.Rules
{
    // One line of the log: when, what kind of thing, and a short detail.
    public readonly struct TelemetryEntry
    {
        public readonly double time_seconds;
        public readonly string kind;
        public readonly string detail;

        // Makes an entry. Missing text (null) is stored as empty text.
        public TelemetryEntry(double time_seconds, string kind, string detail)
        {
            this.time_seconds = time_seconds;
            this.kind = kind ?? "";
            this.detail = detail ?? "";
        }
    }

    public class TelemetryLog
    {
        // About 115 days in seconds. Anything bigger is a bug, not a play session.
        public const double maximum_time_seconds = 1.0e7;

        readonly List<TelemetryEntry> stored_entries = new List<TelemetryEntry>();
        readonly int maximum_entry_count;
        double latest_time_seconds = 0.0;

        // The kept entries, oldest first. Read-only from outside.
        public IReadOnlyList<TelemetryEntry> entries => stored_entries;

        // How many entries were refused for a broken time.
        public int rejected_entry_count { get; private set; }

        // How many entries were dropped because the log was full.
        public int dropped_entry_count { get; private set; }

        // Makes a log that keeps at most this many entries.
        public TelemetryLog(int maximum_entry_count = 100000)
        {
            this.maximum_entry_count = maximum_entry_count < 0 ? 0 : maximum_entry_count;
        }

        // Adds an entry. Returns false (and counts it) if the time is broken or the log is full.
        public bool record(double time_seconds, string kind, string detail)
        {
            bool time_is_usable = double.IsFinite(time_seconds)
                && time_seconds >= 0.0
                && time_seconds <= maximum_time_seconds
                && time_seconds >= latest_time_seconds;
            if (!time_is_usable)
            {
                rejected_entry_count += 1;
                return false;
            }
            if (stored_entries.Count >= maximum_entry_count)
            {
                dropped_entry_count += 1;
                return false;
            }
            latest_time_seconds = time_seconds;
            stored_entries.Add(new TelemetryEntry(time_seconds, kind, detail));
            return true;
        }

        // The whole log as JSON Lines: one {"time_seconds":..,"kind":..,"detail":..} per line.
        public string to_json_lines()
        {
            var output = new StringBuilder();
            foreach (var entry in stored_entries)
            {
                // "F3" means three decimals; InvariantCulture keeps a dot even on a French phone.
                output.Append("{\"time_seconds\":")
                    .Append(entry.time_seconds.ToString("F3", CultureInfo.InvariantCulture))
                    .Append(",\"kind\":\"").Append(escape_json_string(entry.kind))
                    .Append("\",\"detail\":\"").Append(escape_json_string(entry.detail))
                    .Append("\"}\n");
            }
            return output.ToString();
        }

        // Makes text safe inside a JSON string: quotes, backslashes and control characters
        // are written as escape codes, so they can't end the string early.
        public static string escape_json_string(string text)
        {
            if (text == null)
            {
                return "";
            }
            var output = new StringBuilder(text.Length);
            foreach (char character in text)
            {
                switch (character)
                {
                    case '"': output.Append("\\\""); break;
                    case '\\': output.Append("\\\\"); break;
                    case '\n': output.Append("\\n"); break;
                    case '\r': output.Append("\\r"); break;
                    case '\t': output.Append("\\t"); break;
                    default:
                        if (character < 0x20)
                        {
                            // "x4" writes the number as four lowercase hex digits, e.g. 0007.
                            output.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            output.Append(character);
                        }
                        break;
                }
            }
            return output.ToString();
        }
    }
}
