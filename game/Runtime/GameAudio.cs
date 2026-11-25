// Purpose: plays every sound in the game. Sound effects, music and rain and wind are made in
// code (SoundSynth, MusicSynth in game/Logic), so the game is never silent, even with no audio
// files at all. If an artist later adds a file to Resources/Sounds or Resources/Music with the
// same name, that file is played instead, with no code change.
//
// Music for a chapter is made while the chapter loads (prepare), because making a 28-second loop
// takes a moment, and doing that mid-chapter would make the game stutter.
using System.Collections.Generic;
using UnityEngine;

namespace Bodhi.Game
{
    public class GameAudio : MonoBehaviour
    {
        public const float music_level = 0.6f;     // How loud the music sits under the effects.
        public const float rain_level = 0.5f;      // The browser game's rain and wind levels.
        public const float wind_level = 0.7f;

        readonly Dictionary<string, AudioClip> effects = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, AudioClip> music = new Dictionary<string, AudioClip>();
        AudioSource effect_player;
        AudioSource music_now, music_before;       // Two, so one mood fades into the next.
        AudioSource rain_player, wind_player;
        string playing_mood = null;
        float rain_target = 0, wind_target = 0;

        // Makes the players. Unity calls this once, when the component is added.
        void Awake()
        {
            effect_player = gameObject.AddComponent<AudioSource>();
            music_now = looping_player();
            music_before = looping_player();
            rain_player = looping_player();
            wind_player = looping_player();
            rain_player.clip = made_clip("weather_rain", MusicSynth.weather_loop("rain"));
            wind_player.clip = made_clip("weather_wind", MusicSynth.weather_loop("wind"));
            rain_player.Play();
            wind_player.Play();
        }

        // Each frame: fades the music and the weather gently toward where they should be.
        void Update()
        {
            float seconds = Time.deltaTime;
            music_now.volume = MusicSynth.ease_toward(music_now.volume, music_level, seconds);
            music_before.volume = MusicSynth.ease_toward(music_before.volume, 0, seconds);
            rain_player.volume = MusicSynth.ease_toward(rain_player.volume, rain_target, seconds);
            wind_player.volume = MusicSynth.ease_toward(wind_player.volume, wind_target, seconds);
        }

        // Makes the music for every mood this chapter can play, and lets go of the rest.
        public void prepare(IEnumerable<string> moods)
        {
            var wanted = new HashSet<string>(moods);
            wanted.Remove(null);
            wanted.Remove("");
            foreach (string mood in new List<string>(music.Keys))
            {
                if (!wanted.Contains(mood) && mood != playing_mood)
                {
                    Object.Destroy(music[mood]);
                    music.Remove(mood);
                }
            }
            foreach (string mood in wanted)
            {
                music_clip(mood);
            }
        }

        // Plays a sound effect once: an artist's file if there is one, otherwise the made one.
        public void play_sound(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }
            if (!effects.TryGetValue(name, out var clip))
            {
                clip = Resources.Load<AudioClip>("Sounds/" + name);
                if (clip == null)  // Unity objects are checked with == null, never ??.
                {
                    clip = made_clip(name, SoundSynth.effect(name));
                }
                effects[name] = clip;  // Remembered, so each sound is only made once.
            }
            if (clip != null)
            {
                effect_player.PlayOneShot(clip);
            }
        }

        // Fades into the music for a mood, if it has changed (the old mood fades out underneath).
        public void play_music(string mood)
        {
            if (string.IsNullOrEmpty(mood) || mood == playing_mood)
            {
                return;
            }
            var clip = music_clip(mood);
            if (clip == null)
            {
                return;  // An unknown mood: keep what's playing rather than stop dead.
            }
            playing_mood = mood;
            var swap = music_before;
            music_before = music_now;
            music_now = swap;
            music_now.clip = clip;
            music_now.volume = 0;
            music_now.Play();
        }

        // How hard it rains and blows (0 for none, about 1 for a storm); they fade to it.
        public void set_weather(float rain, float wind)
        {
            rain_target = Mathf.Clamp(rain, 0, 2) * rain_level;
            wind_target = Mathf.Clamp(wind, 0, 2) * wind_level;
        }

        // A mood's music: an artist's file if there is one, otherwise made (and kept).
        AudioClip music_clip(string mood)
        {
            if (!music.TryGetValue(mood, out var clip))
            {
                clip = Resources.Load<AudioClip>("Music/" + mood);
                if (clip == null)
                {
                    clip = made_clip("music_" + mood, MusicSynth.music_loop(mood));
                }
                if (clip != null)
                {
                    music[mood] = clip;
                }
            }
            return clip;
        }

        // A silent player that loops.
        AudioSource looping_player()
        {
            var player = gameObject.AddComponent<AudioSource>();
            player.loop = true;
            player.volume = 0;
            return player;
        }

        // Turns made samples into a clip Unity can play (one channel, 44,100 samples a second).
        static AudioClip made_clip(string name, float[] samples)
        {
            if (samples == null || samples.Length == 0)
            {
                return null;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, SoundSynth.sample_rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
