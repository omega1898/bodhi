// Purpose: makes every sound effect in the game from scratch, as a list of numbers (samples),
// so the game needs no recorded sound files. It is the browser game's recipes
// (prototype/web/js/audio.js) worked out ahead of time instead of live: the same whistle, bark,
// gate and thunder, plus a sniff, eating and the towel, which the browser game never had.
//
// How digital sound works, in one paragraph: a speaker cone moves in and out. A sound file is a
// list of where the cone should be, 44,100 times a second, each a number from -1 (all the way in)
// to 1 (all the way out). A steady tone is a wave that repeats; noise is random numbers; a filter
// keeps the low or high or middle part of a sound; an envelope fades it in and out.
//
// Everything here is plain C#, so the tests can check it without Unity. MusicSynth.cs builds the
// music and the rain and wind from the same parts. If an artist adds a recorded file to
// Resources/Sounds with the same name, the scene plays that instead.
using System;

namespace Bodhi.Game
{
    public enum Wave { sine, triangle, sawtooth, square }
    public enum FilterKind { lowpass, highpass, bandpass }

    public static class SoundSynth
    {
        public const int sample_rate = 44100;   // Samples a second, CD quality.
        public const float effects_volume = 0.72f;  // The browser game's effects (0.8) and master (0.9) volumes together.

        // Every effect this file can make. The scene asks for these by name.
        public static readonly string[] effect_names =
        {
            "whistle", "bark", "whine", "step", "splash", "collapse", "gate", "lever", "thud",
            "chime", "bell", "thunder", "pickup", "heartbeat", "sniff", "eat", "towel",
        };

        // Makes the named effect, or returns null for a name it doesn't know (the scene stays silent).
        public static float[] effect(string name)
        {
            switch (name)
            {
                case "whistle":
                    return mix(0.7, b => { b.tone(Wave.sine, 1500, 2100, 0, 0.22, 0.16); b.tone(Wave.sine, 2100, 1650, 0.28, 0.32, 0.16); });
                case "bark":
                    return mix(0.5, b =>
                    {
                        foreach (double delay in new[] { 0, 0.24 })
                        {
                            b.voice(520, 230, delay, 0.14, 0.18);                      // The throat.
                            b.noise(FilterKind.bandpass, 900, delay, 0.12, 0.25);     // The breath.
                        }
                    });
                case "whine": return mix(0.7, b => b.tone(Wave.sine, 980, 760, 0, 0.6, 0.05));
                case "step": return mix(0.1, b => b.noise(FilterKind.bandpass, 1400, 0, 0.05, 0.05));
                case "splash": return mix(0.3, b => b.noise(FilterKind.highpass, 1800, 0, 0.25, 0.18));
                case "collapse":
                    return mix(2.3, b =>
                    {
                        b.noise(FilterKind.lowpass, 220, 0, 2.2, 0.9);
                        b.noise(FilterKind.bandpass, 1200, 0.15, 0.6, 0.35);
                        b.noise(FilterKind.bandpass, 700, 0.55, 0.5, 0.3);
                    });
                case "gate": return mix(1.5, b => { b.noise(FilterKind.bandpass, 420, 0, 1.4, 0.35); b.tone(Wave.square, 90, 60, 0, 1.2, 0.05); });
                case "lever": return mix(0.6, b => { b.noise(FilterKind.bandpass, 2400, 0, 0.08, 0.4); b.noise(FilterKind.lowpass, 300, 0.1, 0.4, 0.5); });
                case "thud": return mix(0.9, b => b.noise(FilterKind.lowpass, 160, 0, 0.8, 0.8));
                case "chime": return mix(2.5, b => bell_strike(b, 880, 0, 2.4, 0.12, new[] { 1, 2.76, 5.4 }, new[] { 1, 0.42, 0.25 }));
                case "bell":
                    return mix(4.9, b =>
                    {
                        foreach (double delay in new[] { 0, 1.1, 2.2 })
                        {
                            bell_strike(b, 523, delay, 2.6, 0.18, new[] { 1, 2.4, 4.1 }, new[] { 1, 0.44, 0.22 });
                        }
                    });
                case "thunder": return mix(4.3, b => { b.noise(FilterKind.lowpass, 120, 0.2, 4, 0.7); b.noise(FilterKind.lowpass, 300, 0.1, 1.2, 0.4); });
                case "pickup": return mix(0.25, b => b.tone(Wave.sine, 660, 990, 0, 0.18, 0.08));
                case "heartbeat": return mix(0.5, b => { b.tone(Wave.sine, 70, 50, 0, 0.18, 0.4); b.tone(Wave.sine, 70, 50, 0.28, 0.18, 0.3); });
                // New for Unity: three quick sniffs, crunching, and a rub of the towel.
                case "sniff":
                    return mix(0.4, b =>
                    {
                        foreach (double delay in new[] { 0, 0.11, 0.22 })
                        {
                            b.noise(FilterKind.bandpass, 3200, delay, 0.07, 0.14);
                        }
                    });
                case "eat":
                    return mix(0.8, b =>
                    {
                        foreach (double delay in new[] { 0, 0.17, 0.35, 0.55 })
                        {
                            b.noise(FilterKind.bandpass, 2300, delay, 0.05, 0.3);
                            b.noise(FilterKind.lowpass, 450, delay, 0.09, 0.3);
                        }
                    });
                case "towel":
                    return mix(1.0, b =>
                    {
                        foreach (double delay in new[] { 0, 0.3, 0.6 })
                        {
                            b.swell(FilterKind.bandpass, 1100, delay, 0.28, 0.12);
                        }
                    });
                default:
                    return null;
            }
        }

        // A struck bell or chime: a few pure tones at set ratios above the first, ringing down.
        static void bell_strike(SoundBuilder builder, double frequency, double delay, double seconds, double volume, double[] ratios, double[] loudness)
        {
            for (int index = 0; index < ratios.Length; index++)
            {
                builder.tone(Wave.sine, frequency * ratios[index], frequency * ratios[index] * 0.999, delay, seconds, volume * loudness[index]);
            }
        }

        // Makes a sound `seconds` long from the parts the recipe adds, at the effects volume.
        static float[] mix(double seconds, Action<SoundBuilder> recipe)
        {
            var builder = new SoundBuilder(seconds, seed: 7);
            recipe(builder);
            return builder.finish(effects_volume);
        }

        // One sample of a wave at `phase` (0 to 1 through one cycle). `step` is how far the
        // phase moves each sample (pitch / sample rate). A sawtooth or square jumps in one
        // instant; drawn sample by sample, that jump makes false, out-of-tune whistles
        // ("aliasing"). With a step, each jump is rounded off over the two samples around it
        // (the "PolyBLEP" method), which removes most of those false tones.
        public static double wave_at(Wave wave, double phase, double step = 0)
        {
            phase -= Math.Floor(phase);
            step = step > 0 && step < 0.5 ? step : 0;
            switch (wave)
            {
                case Wave.triangle: return phase < 0.5 ? 4 * phase - 1 : 3 - 4 * phase;
                case Wave.sawtooth: return 2 * phase - 1 - rounded_jump(phase, step);
                case Wave.square:
                    double half_way = phase + 0.5 - Math.Floor(phase + 0.5);
                    return (phase < 0.5 ? 1 : -1) + rounded_jump(phase, step) - rounded_jump(half_way, step);
                default: return Math.Sin(2 * Math.PI * phase);
            }
        }

        // The PolyBLEP correction: how much to soften a jump that happens at phase 0, for a
        // sample this close to it. Zero everywhere except the sample just before and just after.
        static double rounded_jump(double phase, double step)
        {
            if (step <= 0)
            {
                return 0;
            }
            if (phase < step)
            {
                double t = phase / step;            // Just after the jump.
                return t + t - t * t - 1;
            }
            if (phase > 1 - step)
            {
                double t = (phase - 1) / step;      // Just before the jump.
                return t * t + t + t + 1;
            }
            return 0;
        }
    }

    // A filter that keeps the low, high or middle part of a sound (the standard "biquad" design
    // from Robert Bristow-Johnson's Audio EQ Cookbook, the same one browsers use).
    public sealed class Biquad
    {
        readonly double b0, b1, b2, a1, a2;
        double x1, x2, y1, y2;  // The last two inputs and outputs: a filter remembers.

        public Biquad(FilterKind kind, double frequency, double q = 1)
        {
            double usable = double.IsNaN(frequency) ? 1000 : frequency;
            double nyquist_safe = Math.Min(Math.Max(usable, 10), SoundSynth.sample_rate * 0.45);  // Keeps the maths stable.
            double w = 2 * Math.PI * nyquist_safe / SoundSynth.sample_rate;
            double alpha = Math.Sin(w) / (2 * q);
            double cos = Math.Cos(w);
            double a0;
            switch (kind)
            {
                case FilterKind.highpass:
                    b0 = (1 + cos) / 2; b1 = -(1 + cos); b2 = (1 + cos) / 2; break;
                case FilterKind.bandpass:
                    b0 = alpha; b1 = 0; b2 = -alpha; break;
                default:
                    b0 = (1 - cos) / 2; b1 = 1 - cos; b2 = (1 - cos) / 2; break;
            }
            a0 = 1 + alpha;
            a1 = -2 * cos / a0; a2 = (1 - alpha) / a0;
            b0 /= a0; b1 /= a0; b2 /= a0;
        }

        // Filters one sample.
        public double next(double input)
        {
            double output = b0 * input + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
            x2 = x1; x1 = input; y2 = y1; y1 = output;
            return output;
        }
    }

    // Adds parts (tones, noise bursts) into one buffer of samples, then finishes it.
    public sealed class SoundBuilder
    {
        readonly double[] samples;
        readonly Random random;

        public int length => samples.Length;

        // `seed` makes the noise the same every time, so a sound never changes between runs.
        public SoundBuilder(double seconds, int seed)
        {
            samples = new double[Math.Max(1, (int)(Math.Max(0, seconds) * SoundSynth.sample_rate))];
            random = new Random(seed);
        }

        // A random number from 0 to 1 (for loosely placed piano notes).
        public double chance() => random.NextDouble();

        // A note whose pitch slides from one frequency to another (the browser's play_sliding_tone):
        // fades in over 30 ms, holds, and fades out over its last 30%.
        public void tone(Wave wave, double from_frequency, double to_frequency, double delay, double seconds, double volume)
        {
            if (!(from_frequency > 0) || !(to_frequency > 0) || double.IsInfinity(from_frequency) || double.IsInfinity(to_frequency))
            {
                return;  // No pitch (or nonsense): nothing to play, rather than NaN noise.
            }
            int start = to_index(delay), count = to_index(seconds);
            double phase = 0;
            for (int index = 0; index < count && start + index < samples.Length; index++)
            {
                double time = (double)index / SoundSynth.sample_rate;
                double progress = time / seconds;
                double frequency = from_frequency * Math.Pow(to_frequency / from_frequency, progress);  // Exponential slide.
                phase += frequency / SoundSynth.sample_rate;
                double envelope = time < 0.03 ? time / 0.03 : progress < 0.7 ? 1 : (1 - progress) / 0.3;
                samples[start + index] += SoundSynth.wave_at(wave, phase, frequency / SoundSynth.sample_rate) * volume * envelope;
            }
        }

        // A voice: a buzzing throat (sawtooth) shaped by the mouth, which lets two bands of
        // pitches through (about 1,000 and 2,600 Hz for a dog) and softens the rest, with a
        // slight wobble in pitch so it doesn't sound like a machine. Used for the bark.
        public void voice(double from_frequency, double to_frequency, double delay, double seconds, double volume)
        {
            if (!(from_frequency > 0) || !(to_frequency > 0) || double.IsInfinity(from_frequency) || double.IsInfinity(to_frequency) || !(volume > 0) || double.IsInfinity(volume))
            {
                return;
            }
            var mouth = new Biquad(FilterKind.bandpass, 1000, 1.4);
            var nose = new Biquad(FilterKind.bandpass, 2600, 2.5);
            var softener = new Biquad(FilterKind.lowpass, 3500);
            int start = to_index(delay), count = to_index(seconds);
            double phase = 0;
            for (int index = 0; index < count && start + index < samples.Length; index++)
            {
                double time = (double)index / SoundSynth.sample_rate;
                double progress = (double)index / count;
                double wobble = 1 + 0.015 * Math.Sin(2 * Math.PI * 28 * time);
                double frequency = from_frequency * Math.Pow(to_frequency / from_frequency, progress) * wobble;
                phase += frequency / SoundSynth.sample_rate;
                double throat = SoundSynth.wave_at(Wave.sawtooth, phase, frequency / SoundSynth.sample_rate);
                double shaped = mouth.next(throat) + 0.5 * nose.next(throat) + 0.25 * softener.next(throat);
                double envelope = time < 0.008 ? time / 0.008 : Math.Pow(1 - progress, 1.5);  // Quick bite, quick fall.
                samples[start + index] += shaped * volume * envelope;
            }
        }

        // A burst of filtered noise that dies away (the browser's play_noise_burst): splashes,
        // footsteps, crashes. It rises to `volume` in 2 ms (starting at full volume in one
        // sample is a click) and falls to almost nothing over `seconds`.
        public void noise(FilterKind kind, double frequency, double delay, double seconds, double volume)
        {
            if (!(volume > 0) || double.IsInfinity(volume))
            {
                return;
            }
            var filter = new Biquad(kind, frequency);
            int start = to_index(delay), count = to_index(seconds);
            for (int index = 0; index < count && start + index < samples.Length; index++)
            {
                double progress = (double)index / count;
                double attack = Math.Min(1, index / (0.002 * SoundSynth.sample_rate));
                double envelope = attack * volume * Math.Pow(0.0001 / Math.Max(volume, 0.0001), progress);
                samples[start + index] += filter.next(random.NextDouble() * 2 - 1) * envelope;
            }
        }

        // Filtered noise that swells in and out, like a rub or a breath.
        public void swell(FilterKind kind, double frequency, double delay, double seconds, double volume)
        {
            var filter = new Biquad(kind, frequency);
            int start = to_index(delay), count = to_index(seconds);
            for (int index = 0; index < count && start + index < samples.Length; index++)
            {
                double envelope = volume * Math.Sin(Math.PI * index / count);
                samples[start + index] += filter.next(random.NextDouble() * 2 - 1) * envelope;
            }
        }

        // Adds a ready-made sound starting at `start` samples, wrapping round to the beginning
        // if `wrap` (so a loop's tail fades out under its own start, with no gap or click).
        public void add(double[] sound, int start, bool wrap)
        {
            if (sound == null || samples.Length == 0)
            {
                return;  // Nothing to add, or nowhere to put it.
            }
            for (int index = 0; index < sound.Length; index++)
            {
                int at = start + index;
                if (wrap)
                {
                    at = ((at % samples.Length) + samples.Length) % samples.Length;  // Wraps negatives too.
                }
                if (at >= 0 && at < samples.Length)
                {
                    samples[at] += sound[index];
                }
            }
        }

        // Scales everything by `volume`, quietens the whole sound if it would go past full
        // (rather than chopping its peaks, which crackles), and returns it ready to play.
        public float[] finish(float volume)
        {
            double peak = 0;
            foreach (double sample in samples)
            {
                peak = Math.Max(peak, Math.Abs(sample * volume));
            }
            double scale = peak > 0.98 ? volume * 0.98 / peak : volume;
            var result = new float[samples.Length];
            for (int index = 0; index < samples.Length; index++)
            {
                result[index] = (float)(samples[index] * scale);
            }
            return result;
        }

        static int to_index(double seconds) => double.IsNaN(seconds) || seconds <= 0 ? 0 : (int)Math.Min(seconds * SoundSynth.sample_rate, int.MaxValue / 2);
    }
}
