// Purpose: writes each sound effect, music mood and weather loop to its own .wav file in the
// folder named on the command line (or "audio_preview"), to listen to outside Unity.
using System;
using System.IO;
using Bodhi.Game;

namespace Bodhi.Audio.Preview
{
    public static class Program
    {
        // Makes the folder, then one file per sound: effect_bark.wav, music_calm.wav, weather_rain.wav.
        public static void Main(string[] arguments)
        {
            string folder = arguments.Length > 0 ? arguments[0] : "audio_preview";
            Directory.CreateDirectory(folder);
            foreach (string name in SoundSynth.effect_names)
            {
                write(folder, "effect_" + name, SoundSynth.effect(name));
            }
            foreach (string mood in MusicSynth.moods)
            {
                write(folder, "music_" + mood, MusicSynth.music_loop(mood));
            }
            write(folder, "weather_rain", MusicSynth.weather_loop("rain"));
            write(folder, "weather_wind", MusicSynth.weather_loop("wind"));
        }

        // Saves one sound and says where it went.
        static void write(string folder, string name, float[] samples)
        {
            string path = Path.Combine(folder, name + ".wav");
            File.WriteAllBytes(path, WavFile.to_bytes(samples, SoundSynth.sample_rate));
            Console.WriteLine(path);
        }
    }
}
