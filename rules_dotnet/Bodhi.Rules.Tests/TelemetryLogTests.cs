// Purpose: proves the playtest log refuses broken times, caps its size, and can't be tricked
// into writing fake fields. Ported from core/tests/test_telemetry_log.cpp.
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class TelemetryLogTests
    {
        // Normal entries are kept in order.
        [Test]
        public void normal_entries_are_kept()
        {
            var log = new TelemetryLog();
            Assert.That(log.record(0.0, "beat_entered", "together"), Is.True);
            Assert.That(log.record(2.5, "world_event", "walkway_collapsed"), Is.True);
            Assert.That(log.entries.Count, Is.EqualTo(2));
        }

        // Broken times are refused: backwards, negative, not a number, infinite, absurdly large.
        [Test]
        public void broken_times_are_refused()
        {
            var log = new TelemetryLog();
            log.record(10.0, "first", "");
            Assert.That(log.record(9.0, "backwards", ""), Is.False);
            Assert.That(log.record(-1.0, "negative", ""), Is.False);
            Assert.That(log.record(double.NaN, "nan", ""), Is.False);
            Assert.That(log.record(double.PositiveInfinity, "infinite", ""), Is.False);
            Assert.That(log.record(1.0e300, "huge", ""), Is.False);
            Assert.That(log.rejected_entry_count, Is.EqualTo(5));
            Assert.That(log.entries.Count, Is.EqualTo(1));
        }

        // The size cap holds, so a runaway loop can't fill the phone's memory.
        [Test]
        public void the_size_cap_holds()
        {
            var log = new TelemetryLog(3);
            for (int index = 0; index < 10; index += 1)
            {
                log.record(index, "tick", "");
            }
            Assert.That(log.entries.Count, Is.EqualTo(3));
            Assert.That(log.dropped_entry_count, Is.EqualTo(7));
        }

        // Quotes, backslashes, newlines and control characters become safe text.
        [Test]
        public void text_is_escaped_for_json()
        {
            Assert.That(TelemetryLog.escape_json_string("a\"b"), Is.EqualTo("a\\\"b"));
            Assert.That(TelemetryLog.escape_json_string("a\\b"), Is.EqualTo("a\\\\b"));
            Assert.That(TelemetryLog.escape_json_string("line\nbreak"), Is.EqualTo("line\\nbreak"));
            Assert.That(TelemetryLog.escape_json_string("bell\u0007"), Is.EqualTo("bell\\u0007"));
        }

        // Break it: a detail that tries to close the string and add a fake "kind" field stays
        // inside the detail, so each line still has exactly three fields.
        [Test]
        public void break_it_a_detail_cannot_inject_a_field()
        {
            var log = new TelemetryLog();
            log.record(1.0, "world_event", "x\",\"kind\":\"admin");
            Assert.That(log.to_json_lines(),
                Is.EqualTo("{\"time_seconds\":1.000,\"kind\":\"world_event\",\"detail\":\"x\\\",\\\"kind\\\":\\\"admin\"}\n"));
        }

        // Break it: missing text is stored as empty text, never crashes the log or its output.
        [Test]
        public void break_it_missing_text_is_stored_as_empty()
        {
            var log = new TelemetryLog();
            Assert.That(log.record(1.0, null, null), Is.True);
            Assert.That(log.to_json_lines(), Is.EqualTo("{\"time_seconds\":1.000,\"kind\":\"\",\"detail\":\"\"}\n"));
        }
    }
}
