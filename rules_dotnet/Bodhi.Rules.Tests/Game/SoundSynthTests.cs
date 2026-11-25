// Purpose: proves the made sounds and music (SoundSynth, MusicSynth, WavFile) are safe to play:
// every sound the chapters ask for exists, none is silent, none goes past full volume (which
// crackles), each is the same every time, loops join with no click, and nonsense can't crash it.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bodhi.Game;
using NUnit.Framework;

namespace Bodhi.Rules.Tests.Game
{
    public class SoundSynthTests
    {
        // The sounds ChapterDirector asks for by name, besides those in the chapter data.
        static readonly string[] director_sounds = { "whistle", "bark", "pickup", "sniff", "heartbeat", "eat", "towel", "thunder" };

        // Every sound the 12 chapters and the scene can ask for has a recipe.
        [Test]
        public void every_sound_the_game_asks_for_is_made()
        {
            var wanted = new HashSet<string>(director_sounds);
            foreach (var stage in ChapterStages.all())
            {
                wanted.UnionWith(stage.things.Select(thing => thing.sound));
                wanted.UnionWith(stage.rules.Select(rule => rule.sound));
            }
            wanted.Remove("");
            Assert.That(wanted, Is.SubsetOf(SoundSynth.effect_names));
        }

        // Each effect is a short, real sound: not silent, never past full volume, no broken numbers.
        [Test]
        public void every_effect_is_audible_and_never_clips()
        {
            foreach (string name in SoundSynth.effect_names)
            {
                var samples = SoundSynth.effect(name);
                Assert.That(samples, Is.Not.Null, name);
                Assert.That(samples.Length, Is.InRange(SoundSynth.sample_rate / 20, SoundSynth.sample_rate * 6), name);
                Assert.That(samples.All(sample => !float.IsNaN(sample) && Math.Abs(sample) <= 1f), name);
                // Peak, not average: a footstep is meant to be soft and brief, but never empty.
                Assert.That(samples.Max(sample => Math.Abs(sample)), Is.GreaterThan(0.005), name + " is silent");
            }
        }

        // Close up, sample by sample: every effect fades in and out. A sound that jumps from
        // silence straight to a loud sample, or stops on one, clicks through the speaker.
        [Test]
        public void every_effect_starts_and_ends_without_a_click()
        {
            foreach (string name in SoundSynth.effect_names)
            {
                var samples = SoundSynth.effect(name);
                Assert.That(Math.Abs(samples[0]), Is.LessThan(0.001), name + " starts with a click");
                Assert.That(Math.Abs(samples[samples.Length - 1]), Is.LessThan(0.001), name + " ends with a click");
                Assert.That(Math.Abs(samples[1] - samples[0]), Is.LessThan(0.005), name + " jumps on its first sample");
            }
        }

        // Close up, a buzzy wave drawn sample by sample makes false tones that aren't in tune
        // with it. At 3,000 Hz, a sawtooth's 15th overtone (45,000 Hz) can't be stored and folds
        // down to 900 Hz. The rounded-off jumps must keep that false tone tiny next to the note.
        [Test]
        public void buzzy_waves_have_no_false_tones()
        {
            foreach (var wave in new[] { Wave.sawtooth, Wave.square })
            {
                var builder = new SoundBuilder(0.25, seed: 1);
                builder.tone(wave, 3000, 3000, 0, 0.25, 0.5);
                var samples = builder.finish(1);
                double note = strength_at(samples, 3000), false_tone = strength_at(samples, 900);
                Assert.That(false_tone, Is.LessThan(note * 0.01), wave + " has a false tone at 900 Hz");
            }
        }

        // The same name always makes exactly the same sound (the noise is seeded, not random).
        [Test]
        public void sounds_are_the_same_every_time()
        {
            Assert.That(SoundSynth.effect("bark"), Is.EqualTo(SoundSynth.effect("bark")));
            Assert.That(MusicSynth.music_loop("tender"), Is.EqualTo(MusicSynth.music_loop("tender")));
        }

        // Every mood any chapter plays has music: four chords long, audible, never clipping.
        [Test]
        public void every_mood_the_chapters_use_has_music()
        {
            var moods = new HashSet<string>(ChapterStages.all().SelectMany(stage => stage.all_moods()));
            Assert.That(moods, Is.Not.Empty);
            Assert.That(moods, Has.None.Null.And.None.Empty);
            foreach (string mood in moods)
            {
                var loop = MusicSynth.music_loop(mood);
                Assert.That(loop, Is.Not.Null, mood);
                Assert.That(loop.Length, Is.EqualTo((int)(MusicSynth.chord_seconds(mood) * 4 * SoundSynth.sample_rate)), mood);
                Assert.That(loop.All(sample => !float.IsNaN(sample) && Math.Abs(sample) <= 1f), mood);
                Assert.That(loudness(loop), Is.GreaterThan(0.002), mood + " is silent");
            }
        }

        // A loop must join seamlessly: the step from its last sample back to its first is no
        // bigger than the biggest step inside it. A gap or click at the join would be.
        [Test]
        public void music_and_weather_loops_join_without_a_click()
        {
            var loops = MusicSynth.moods.Select(MusicSynth.music_loop).Concat(new[] { MusicSynth.weather_loop("rain"), MusicSynth.weather_loop("wind") });
            foreach (var loop in loops)
            {
                double biggest_step = 0;
                for (int index = 1; index < loop.Length; index++)
                {
                    biggest_step = Math.Max(biggest_step, Math.Abs(loop[index] - loop[index - 1]));
                }
                Assert.That(Math.Abs(loop[0] - loop[loop.Length - 1]), Is.LessThanOrEqualTo(biggest_step));
            }
        }

        // The filter really filters: a low pass keeps a low tone and cuts a high one, and a band
        // pass keeps its own pitch and cuts one far below it.
        [Test]
        public void filters_keep_and_cut_the_right_pitches()
        {
            Assert.That(filtered_loudness(FilterKind.lowpass, 400, 100), Is.GreaterThan(5 * filtered_loudness(FilterKind.lowpass, 400, 6000)));
            Assert.That(filtered_loudness(FilterKind.highpass, 2000, 8000), Is.GreaterThan(5 * filtered_loudness(FilterKind.highpass, 2000, 100)));
            Assert.That(filtered_loudness(FilterKind.bandpass, 1000, 1000), Is.GreaterThan(5 * filtered_loudness(FilterKind.bandpass, 1000, 60)));
        }

        // A chapter's moods list every mood its music, story moments and aftermath name, once each.
        [Test]
        public void a_chapter_lists_every_mood_it_plays()
        {
            foreach (var stage in ChapterStages.all())
            {
                var moods = stage.all_moods();
                Assert.That(moods, Is.Unique);
                Assert.That(moods, Has.None.Empty, stage.id);
                if (!string.IsNullOrEmpty(stage.mood))
                {
                    Assert.That(moods, Does.Contain(stage.mood), stage.id);
                }
                foreach (var rule in stage.rules.Where(rule => !string.IsNullOrEmpty(rule.mood)))
                {
                    Assert.That(moods, Does.Contain(rule.mood), stage.id);
                }
                if (!string.IsNullOrEmpty(stage.aftermath.mood))
                {
                    Assert.That(moods, Does.Contain(stage.aftermath.mood), stage.id);
                }
            }
        }

        // The storm chapter rains and blows; a chapter with no weather is quiet; and a broken
        // number in the scenery can't turn the rain up to a roar.
        [Test]
        public void weather_sound_follows_the_scenery()
        {
            var storm = ChapterStages.for_chapter("the_storm").weather_sound();
            Assert.That(storm.rain, Is.EqualTo(0.9));   // Its "rain_sound", not its heavier "rain".
            Assert.That(storm.wind, Is.EqualTo(0.8));
            var calm = new ChapterStage { scenery = new StageSetting[0] }.weather_sound();
            Assert.That(calm, Is.EqualTo((0.0, 0.0)));
            var only_rain = new ChapterStage { scenery = new[] { StageSetting.of_number("rain", 0.4) } }.weather_sound();
            Assert.That(only_rain.rain, Is.EqualTo(0.4));
            var broken = new ChapterStage { scenery = new[] { StageSetting.of_number("rain", double.NaN), StageSetting.of_number("wind", -3) } }.weather_sound();
            Assert.That(broken, Is.EqualTo((0.0, 0.0)));
        }

        // A fade glides the same whatever the frame rate: two half-steps land where one whole
        // step does, it gets closer each frame without overshooting, and nonsense can't stick.
        [Test]
        public void fades_glide_the_same_at_any_frame_rate()
        {
            float one_step = MusicSynth.ease_toward(0, 1, 0.1f);
            float two_half_steps = MusicSynth.ease_toward(MusicSynth.ease_toward(0, 1, 0.05f), 1, 0.05f);
            Assert.That(two_half_steps, Is.EqualTo(one_step).Within(1e-5));
            Assert.That(one_step, Is.InRange(0.1f, 0.2f));
            Assert.That(MusicSynth.ease_toward(0, 1, 100), Is.InRange(0.99f, 1f));
            Assert.That(MusicSynth.ease_toward(0.5f, 1, 0), Is.EqualTo(0.5f));
            Assert.That(MusicSynth.ease_toward(0.5f, 1, float.NaN), Is.EqualTo(0.5f));
            Assert.That(MusicSynth.ease_toward(float.NaN, 0.6f, 0.016f), Is.EqualTo(0.6f));
        }

        // A .wav file has the right label, and its numbers say what follows.
        [Test]
        public void wav_files_are_labelled_correctly()
        {
            var bytes = WavFile.to_bytes(new float[] { 0, 0.5f, -0.5f }, SoundSynth.sample_rate);
            Assert.That(Encoding.ASCII.GetString(bytes, 0, 4), Is.EqualTo("RIFF"));
            Assert.That(Encoding.ASCII.GetString(bytes, 8, 8), Is.EqualTo("WAVEfmt "));
            Assert.That(BitConverter.ToInt32(bytes, 4), Is.EqualTo(bytes.Length - 8));
            Assert.That(BitConverter.ToInt32(bytes, 24), Is.EqualTo(SoundSynth.sample_rate));
            Assert.That(BitConverter.ToInt32(bytes, 40), Is.EqualTo(6));  // Three samples, two bytes each.
            Assert.That(BitConverter.ToInt16(bytes, 46), Is.EqualTo(16384));
        }

        // Break it: unknown names, nonsense numbers and out-of-range samples must not crash or
        // make broken sound.
        [Test]
        public void nonsense_cannot_break_the_sound_maker()
        {
            Assert.That(SoundSynth.effect("no_such_sound"), Is.Null);
            Assert.That(SoundSynth.effect(null), Is.Null);
            Assert.That(MusicSynth.music_loop("no_such_mood"), Is.Null);
            Assert.That(MusicSynth.music_loop(null), Is.Null);
            Assert.That(MusicSynth.weather_loop("snow"), Is.Null);

            var builder = new SoundBuilder(double.NaN, seed: 1);
            builder.tone(Wave.sine, 0, 440, 0, 1, 1);
            builder.tone(Wave.sine, double.NaN, double.PositiveInfinity, -5, double.NaN, 1);
            builder.noise(FilterKind.bandpass, double.NaN, double.NaN, 1e12, double.PositiveInfinity);
            builder.swell(FilterKind.lowpass, -50, 0, 1, 0.5);
            builder.add(new double[] { 1, 2, 3 }, -10, wrap: true);  // An empty sound: nowhere to wrap to.
            builder.add(null, 0, wrap: true);
            var made = builder.finish(1);
            Assert.That(made.All(sample => !float.IsNaN(sample) && Math.Abs(sample) <= 1f));

            var short_loop = new SoundBuilder(0.001, seed: 1);  // 44 samples long.
            short_loop.add(new double[] { 0.5 }, -1, wrap: true);
            Assert.That(short_loop.finish(1).Last(), Is.EqualTo(0.5f), "starting one sample early wraps to the end");

            var wav = WavFile.to_bytes(new[] { float.NaN, 3f, -3f }, SoundSynth.sample_rate);
            Assert.That(BitConverter.ToInt16(wav, 44), Is.EqualTo(0));
            Assert.That(BitConverter.ToInt16(wav, 46), Is.EqualTo(short.MaxValue));
            Assert.That(BitConverter.ToInt16(wav, 48), Is.EqualTo(-short.MaxValue));
            Assert.That(WavFile.to_bytes(null, SoundSynth.sample_rate).Length, Is.EqualTo(44));
        }

        // How loud a sound is on average (root mean square).
        static double loudness(float[] samples)
        {
            return Math.Sqrt(samples.Sum(sample => (double)sample * sample) / samples.Length);
        }

        // How strong one pitch is in a sound (the Goertzel method: one bar of a spectrum).
        static double strength_at(float[] samples, double pitch)
        {
            double coefficient = 2 * Math.Cos(2 * Math.PI * pitch / SoundSynth.sample_rate);
            double previous = 0, before_that = 0;
            foreach (float sample in samples)
            {
                double now = sample + coefficient * previous - before_that;
                before_that = previous;
                previous = now;
            }
            return Math.Sqrt(previous * previous + before_that * before_that - coefficient * previous * before_that);
        }

        // How loud a steady tone at `pitch` is after this filter.
        static double filtered_loudness(FilterKind kind, double cutoff, double pitch)
        {
            var filter = new Biquad(kind, cutoff);
            double total = 0;
            int count = SoundSynth.sample_rate / 2;
            for (int index = 0; index < count; index++)
            {
                double output = filter.next(Math.Sin(2 * Math.PI * pitch * index / SoundSynth.sample_rate));
                if (index > count / 2)
                {
                    total += output * output;  // Only the second half, once the filter has settled.
                }
            }
            return Math.Sqrt(total / (count / 2));
        }
    }
}
