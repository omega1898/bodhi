// Purpose: makes the game's music and its rain and wind, from scratch, as loops that repeat with
// no gap. The music is the browser game's (prototype/web/js/audio.js): slow, soft chords that
// change every seven seconds, with a few piano notes placed loosely, like someone thinking. Each
// mood has its own four chords: low and dark for danger, open and high for tenderness.
//
// The browser plays it live, note by note. Unity plays a recording, so this writes the four
// chords out once as one loop. A chord's last note rings on past the end of the loop, so that
// tail is laid back over the loop's start; when the loop repeats you hear it carry on, not stop.
using System;
using System.Collections.Generic;

namespace Bodhi.Game
{
    public static class MusicSynth
    {
        public const float music_volume = 0.288f;    // The browser's music (0.32) and master (0.9) volumes.
        public const float weather_volume = 0.495f;  // Its ambience (0.55) and master volumes.
        public const double weather_seconds = 4;     // Rain and wind loop every four seconds.

        // Chords for each mood, as note numbers (60 is middle C, 61 the next key up, and so on).
        static readonly Dictionary<string, int[][]> mood_chords = new Dictionary<string, int[][]>
        {
            ["calm"] = new[] { new[] { 48, 55, 64, 71 }, new[] { 45, 52, 60, 67 }, new[] { 41, 48, 57, 64 }, new[] { 43, 50, 59, 62 } },
            ["tense"] = new[] { new[] { 38, 45, 50, 53 }, new[] { 34, 41, 46, 50 }, new[] { 43, 50, 55, 58 }, new[] { 33, 45, 49, 52 } },
            ["tender"] = new[] { new[] { 41, 53, 60, 64, 69 }, new[] { 45, 52, 60, 64, 71 }, new[] { 38, 50, 57, 62, 65 }, new[] { 43, 55, 62, 67, 71 } },
            ["quiet"] = new[] { new[] { 45, 52, 57, 60 }, new[] { 41, 48, 57, 60 }, new[] { 43, 50, 55, 59 }, new[] { 40, 47, 55, 59 } },
            ["hopeful"] = new[] { new[] { 43, 50, 59, 62, 66 }, new[] { 40, 47, 55, 59, 64 }, new[] { 36, 43, 52, 55, 62 }, new[] { 38, 45, 54, 57, 62 } },
        };

        // Every mood there is music for.
        public static IEnumerable<string> moods => mood_chords.Keys;

        public const double fade_seconds = 0.8;  // How gently levels change (the browser's 0.8 s).

        // Moves a volume part of the way toward `target`: the same smooth glide each frame
        // whatever the frame rate, so a fade sounds the same on a slow phone as a fast one.
        public static float ease_toward(float current, float target, float delta_seconds)
        {
            if (float.IsNaN(current) || float.IsInfinity(current))
            {
                return target;
            }
            if (!(delta_seconds > 0) || float.IsInfinity(delta_seconds))
            {
                return current;
            }
            return current + (target - current) * (float)(1 - Math.Exp(-delta_seconds / fade_seconds));
        }

        // Seconds each chord lasts: tense music moves a little faster.
        public static double chord_seconds(string mood) => mood == "tense" ? 5.5 : 7;

        // A note number as a frequency in hertz (vibrations a second). 69 is A, 440 Hz; each
        // step of 12 is an octave, which doubles the frequency.
        public static double note_to_frequency(int note_number) => 440 * Math.Pow(2, (note_number - 69) / 12.0);

        // The loop for a mood, or null for a mood it doesn't know.
        public static float[] music_loop(string mood)
        {
            if (mood == null || !mood_chords.TryGetValue(mood, out var chords))
            {
                return null;
            }
            double seconds = chord_seconds(mood);
            var builder = new SoundBuilder(seconds * chords.Length, seed: 3);
            int piano_count = mood == "tender" ? 4 : mood == "tense" ? 1 : 2;
            for (int chord_index = 0; chord_index < chords.Length; chord_index++)
            {
                var chord = chords[chord_index];
                int chord_start = (int)(chord_index * seconds * SoundSynth.sample_rate);
                foreach (int note in chord)
                {
                    double detune_cents = (builder.chance() - 0.5) * 8;  // A hair out of tune, so it sounds alive.
                    builder.add(pad_note(note_to_frequency(note) * Math.Pow(2, detune_cents / 1200), seconds + 1.5, 0.05), chord_start, wrap: true);
                }
                for (int index = 0; index < piano_count; index++)
                {
                    int note = chord[1 + (int)(builder.chance() * (chord.Length - 1))] + 12;
                    double offset = 0.4 + builder.chance() * (seconds - 1);
                    builder.add(piano_note(note_to_frequency(note), 0.09), chord_start + (int)(offset * SoundSynth.sample_rate), wrap: true);
                }
            }
            return builder.finish(music_volume);
        }

        // Rain ("rain") or wind ("wind") as a loop, or null for anything else. Both are noise:
        // rain keeps more of the hiss, wind only the low rumble.
        public static float[] weather_loop(string kind)
        {
            double cutoff = kind == "rain" ? 2600 : kind == "wind" ? 380 : 0;
            if (cutoff == 0)
            {
                return null;
            }
            var random = new Random(kind == "rain" ? 11 : 13);
            var noise = new double[(int)(weather_seconds * SoundSynth.sample_rate)];
            for (int index = 0; index < noise.Length; index++)
            {
                noise[index] = random.NextDouble() * 2 - 1;
            }
            // Filters the noise twice round and keeps the second time: the filter then starts the
            // loop remembering its own end, so the join has no click.
            var filter = new Biquad(FilterKind.lowpass, cutoff);
            var filtered = new double[noise.Length];
            for (int round = 0; round < 2; round++)
            {
                for (int index = 0; index < noise.Length; index++)
                {
                    filtered[index] = filter.next(noise[index]);
                }
            }
            var builder = new SoundBuilder(weather_seconds, seed: 1);
            builder.add(filtered, 0, wrap: false);
            return builder.finish(weather_volume);
        }

        // One long, soft note that swells in and fades out: the background of the music.
        static double[] pad_note(double frequency, double seconds, double volume)
        {
            var filter = new Biquad(FilterKind.lowpass, 900);
            var samples = new double[(int)(seconds * SoundSynth.sample_rate)];
            double phase = 0;
            for (int index = 0; index < samples.Length; index++)
            {
                double progress = (double)index / samples.Length;
                double envelope = progress < 0.35 ? progress / 0.35 : (1 - progress) / 0.65;
                phase += frequency / SoundSynth.sample_rate;
                samples[index] = filter.next(SoundSynth.wave_at(Wave.triangle, phase)) * volume * envelope;
            }
            return samples;
        }

        // A short note that starts bright and dies away, a little like a piano key: the note
        // plus two quieter ones above it, each dying faster than the one below.
        static double[] piano_note(double frequency, double volume)
        {
            var samples = new double[3 * SoundSynth.sample_rate];
            var harmonics = new[] { (1, 1.0), (2, 0.35), (3, 0.12) };
            foreach (var (harmonic, loudness) in harmonics)
            {
                double fade_seconds = 2.8 / harmonic;
                for (int index = 0; index < samples.Length; index++)
                {
                    double time = (double)index / SoundSynth.sample_rate;
                    double envelope = time < 0.01 ? time / 0.01 : time > fade_seconds ? 0 : Math.Pow(0.0001, (time - 0.01) / (fade_seconds - 0.01));
                    samples[index] += Math.Sin(2 * Math.PI * frequency * harmonic * time) * volume * loudness * envelope;
                }
            }
            return samples;
        }
    }
}
