// Purpose: decides which story line is on screen and for how long, ported from update_subtitles
// in prototype/web/js/game.js. Each line stays long enough to read, there's a short gap between
// lines, and lines that waited too long are dropped so the words always match the moment. The
// Unity scene only draws `text` in the style of `voice`.
using System;
using System.Collections.Generic;
using Bodhi.Rules;

namespace Bodhi.Game
{
    public class SubtitlePacer
    {
        public const double base_seconds = 2.2;               // Every line shows at least this long...
        public const double seconds_per_character = 0.055;    // ...plus this much for each letter.
        public const double gap_seconds = 0.55;               // Blank between two lines.
        public const double stale_after_seconds = 9;          // A line older than this is dropped...
                                                               // ...unless it's the last one waiting.

        // A line waiting its turn, and when it joined the queue.
        sealed class WaitingLine
        {
            public string text;
            public StoryVoice voice;
            public double queued_at;
        }

        readonly string dog_name;
        readonly Queue<WaitingLine> waiting = new Queue<WaitingLine>();
        double seconds_elapsed = 0;
        double seconds_left = 0;  // On the line showing, or in the gap after it.

        // True while a line is on screen.
        public bool showing { get; private set; } = false;

        // The line on screen, with his name filled in, or "" in a gap.
        public string text { get; private set; } = "";

        // How the line on screen is spoken: her thoughts, his senses, or something written.
        public StoryVoice voice { get; private set; } = StoryVoice.her;

        // Makes a pacer that fills {dog} with this name (Bodhi if none is given).
        public SubtitlePacer(string dog_name)
        {
            this.dog_name = string.IsNullOrEmpty(dog_name) ? "Bodhi" : dog_name;
        }

        // Queues the lines the rules handed over (ChapterRun.take_lines), oldest first.
        public void add(List<StoryLine> lines)
        {
            foreach (var line in lines ?? new List<StoryLine>())
            {
                waiting.Enqueue(new WaitingLine { text = StoryText.personalize(line.text, dog_name), voice = line.voice, queued_at = seconds_elapsed });
            }
        }

        // The player tapped the line: move on now (the usual gap still follows).
        public void skip_line()
        {
            if (showing)
            {
                seconds_left = 0;
            }
        }

        // A film scene was skipped: drop every line, including the one on screen.
        public void clear()
        {
            waiting.Clear();
            showing = false;
            text = "";
            seconds_left = 0;
        }

        // Moves time on: ends the line on screen, then the gap, then shows the next. Bad times
        // are ignored.
        public void pass_time(double delta_seconds)
        {
            if (!(delta_seconds > 0) || double.IsInfinity(delta_seconds))
            {
                return;
            }
            seconds_elapsed += delta_seconds;
            seconds_left -= delta_seconds;
            if (seconds_left > 0)
            {
                return;
            }
            if (showing)
            {
                showing = false;
                text = "";
                seconds_left = gap_seconds;
                return;
            }
            while (waiting.Count > 1 && seconds_elapsed - waiting.Peek().queued_at > stale_after_seconds)
            {
                waiting.Dequeue();
            }
            if (waiting.Count == 0)
            {
                return;
            }
            var next = waiting.Dequeue();
            showing = true;
            text = next.text;
            voice = next.voice;
            seconds_left = base_seconds + next.text.Length * seconds_per_character;
        }
    }
}
