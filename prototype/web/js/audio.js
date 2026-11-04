// Purpose: every sound in the game, made from scratch with the browser's Web Audio system:
// rain, wind, the whistle, the bark, footsteps, and slow music that follows the mood.
// No sound files are needed. Browsers only allow sound after the player taps or presses a key,
// so nothing plays until start_audio() is called from a button.
'use strict';

const audio = {
    context: null,
    master_gain: null,
    music_gain: null,
    ambience_gain: null,
    effects_gain: null,
    rain_gain: null,
    wind_gain: null,
    noise_buffer: null,
    muted: false,
    mood: 'calm',
    next_chord_time: 0,
    chord_index: 0,
};

// Chords for each mood, as note numbers (60 is middle C). Lower, darker chords for danger;
// open, high chords for tenderness. The music walks through each list in a loop.
const mood_chords = {
    calm:    [[48, 55, 64, 71], [45, 52, 60, 67], [41, 48, 57, 64], [43, 50, 59, 62]],
    tense:   [[38, 45, 50, 53], [34, 41, 46, 50], [43, 50, 55, 58], [33, 45, 49, 52]],
    tender:  [[41, 53, 60, 64, 69], [45, 52, 60, 64, 71], [38, 50, 57, 62, 65], [43, 55, 62, 67, 71]],
    quiet:   [[45, 52, 57, 60], [41, 48, 57, 60], [43, 50, 55, 59], [40, 47, 55, 59]],
    hopeful: [[43, 50, 59, 62, 66], [40, 47, 55, 59, 64], [36, 43, 52, 55, 62], [38, 45, 54, 57, 62]],
};

// Turns a note number into a frequency in hertz (vibrations per second).
function note_to_frequency(note_number) {
    return 440 * Math.pow(2, (note_number - 69) / 12);
}

// Creates the audio system. Must run inside a tap or key press, or the browser refuses.
function start_audio() {
    if (audio.context) {
        if (audio.context.state === 'suspended') {
            audio.context.resume();
        }
        return;
    }
    const AudioContextType = window.AudioContext || window.webkitAudioContext;
    if (!AudioContextType) {
        return;  // Very old browser: the game still works, silently.
    }
    audio.context = new AudioContextType();
    const context = audio.context;

    audio.master_gain = context.createGain();
    audio.master_gain.gain.value = audio.muted ? 0 : 0.9;
    audio.master_gain.connect(context.destination);

    audio.music_gain = context.createGain();
    audio.music_gain.gain.value = 0.32;
    audio.music_gain.connect(audio.master_gain);
    audio.ambience_gain = context.createGain();
    audio.ambience_gain.gain.value = 0.55;
    audio.ambience_gain.connect(audio.master_gain);
    audio.effects_gain = context.createGain();
    audio.effects_gain.gain.value = 0.8;
    audio.effects_gain.connect(audio.master_gain);

    // Two seconds of random values: the raw material for rain, wind, splashes and barks.
    const sample_count = context.sampleRate * 2;
    audio.noise_buffer = context.createBuffer(1, sample_count, context.sampleRate);
    const samples = audio.noise_buffer.getChannelData(0);
    for (let index = 0; index < sample_count; index++) {
        samples[index] = Math.random() * 2 - 1;
    }

    audio.rain_gain = start_noise_loop('lowpass', 2600, 0);
    audio.wind_gain = start_noise_loop('lowpass', 380, 0);
    audio.next_chord_time = context.currentTime + 0.5;
}

// Starts a never-ending filtered noise (rain or wind) and returns its volume control.
function start_noise_loop(filter_type, filter_frequency, starting_volume) {
    const context = audio.context;
    const source = context.createBufferSource();
    source.buffer = audio.noise_buffer;
    source.loop = true;
    const filter = context.createBiquadFilter();
    filter.type = filter_type;
    filter.frequency.value = filter_frequency;
    const gain = context.createGain();
    gain.gain.value = starting_volume;
    source.connect(filter);
    filter.connect(gain);
    gain.connect(audio.ambience_gain);
    source.start();
    return gain;
}

// Turns all sound on or off, and returns the new state.
function toggle_mute() {
    audio.muted = !audio.muted;
    if (audio.master_gain) {
        audio.master_gain.gain.setTargetAtTime(audio.muted ? 0 : 0.9, audio.context.currentTime, 0.1);
    }
    return audio.muted;
}

// Sets how loud the rain and wind are (0 = silent, 1 = storm), fading over a second.
function set_weather_sound(rain_amount, wind_amount) {
    if (!audio.context) {
        return;
    }
    const now = audio.context.currentTime;
    audio.rain_gain.gain.setTargetAtTime(rain_amount * 0.5, now, 0.8);
    audio.wind_gain.gain.setTargetAtTime(wind_amount * 0.7, now, 0.8);
}

// Changes the music's mood. The next chord uses the new set.
function set_music_mood(mood) {
    if (mood_chords[mood] && audio.mood !== mood) {
        audio.mood = mood;
        audio.chord_index = 0;
        if (audio.context) {
            audio.next_chord_time = Math.min(audio.next_chord_time, audio.context.currentTime + 0.3);
        }
    }
}

// Called every frame: when it's time, plays the next slow chord and a few soft piano notes.
function update_music() {
    if (!audio.context || audio.context.currentTime < audio.next_chord_time) {
        return;
    }
    const chords = mood_chords[audio.mood];
    const chord = chords[audio.chord_index % chords.length];
    audio.chord_index += 1;
    const start_time = audio.next_chord_time;
    const chord_seconds = audio.mood === 'tense' ? 5.5 : 7;
    for (const note_number of chord) {
        play_pad_note(note_to_frequency(note_number), start_time, chord_seconds + 1.5, 0.05);
    }
    // A few piano notes from the chord's upper half, placed loosely in time like someone thinking.
    const piano_count = audio.mood === 'tender' ? 4 : audio.mood === 'tense' ? 1 : 2;
    for (let index = 0; index < piano_count; index++) {
        const note_number = chord[1 + Math.floor(Math.random() * (chord.length - 1))] + 12;
        const offset_seconds = 0.4 + Math.random() * (chord_seconds - 1);
        play_piano_note(note_to_frequency(note_number), start_time + offset_seconds, 0.09);
    }
    audio.next_chord_time = start_time + chord_seconds;
}

// One long, soft note that swells in and fades out: the background of the music.
function play_pad_note(frequency, start_time, duration_seconds, volume) {
    const context = audio.context;
    const oscillator = context.createOscillator();
    oscillator.type = 'triangle';
    oscillator.frequency.value = frequency;
    oscillator.detune.value = (Math.random() - 0.5) * 8;
    const filter = context.createBiquadFilter();
    filter.type = 'lowpass';
    filter.frequency.value = 900;
    const gain = context.createGain();
    gain.gain.setValueAtTime(0, start_time);
    gain.gain.linearRampToValueAtTime(volume, start_time + duration_seconds * 0.35);
    gain.gain.linearRampToValueAtTime(0, start_time + duration_seconds);
    oscillator.connect(filter);
    filter.connect(gain);
    gain.connect(audio.music_gain);
    oscillator.start(start_time);
    oscillator.stop(start_time + duration_seconds + 0.1);
}

// A short note that starts bright and dies away, a little like a piano key.
function play_piano_note(frequency, start_time, volume) {
    const context = audio.context;
    for (const [harmonic, harmonic_volume] of [[1, 1], [2, 0.35], [3, 0.12]]) {
        const oscillator = context.createOscillator();
        oscillator.type = 'sine';
        oscillator.frequency.value = frequency * harmonic;
        const gain = context.createGain();
        gain.gain.setValueAtTime(0, start_time);
        gain.gain.linearRampToValueAtTime(volume * harmonic_volume, start_time + 0.01);
        gain.gain.exponentialRampToValueAtTime(0.0001, start_time + 2.8 / harmonic);
        oscillator.connect(gain);
        gain.connect(audio.music_gain);
        oscillator.start(start_time);
        oscillator.stop(start_time + 3);
    }
}

// A burst of filtered noise: the building block of splashes, crashes and footsteps.
function play_noise_burst(filter_type, frequency, duration_seconds, volume, start_delay, pan) {
    const context = audio.context;
    const start_time = context.currentTime + (start_delay || 0);
    const source = context.createBufferSource();
    source.buffer = audio.noise_buffer;
    const filter = context.createBiquadFilter();
    filter.type = filter_type;
    filter.frequency.value = frequency;
    const gain = context.createGain();
    gain.gain.setValueAtTime(volume, start_time);
    gain.gain.exponentialRampToValueAtTime(0.0001, start_time + duration_seconds);
    let last_node = gain;
    if (context.createStereoPanner) {
        const panner = context.createStereoPanner();
        panner.pan.value = Math.max(-1, Math.min(1, pan || 0));
        gain.connect(panner);
        last_node = panner;
    }
    source.connect(filter);
    filter.connect(gain);
    last_node.connect(audio.effects_gain);
    source.start(start_time, Math.random() * 1.5);
    source.stop(start_time + duration_seconds + 0.05);
}

// A note whose pitch slides from one frequency to another, used for whistles and whines.
function play_sliding_tone(type, from_frequency, to_frequency, start_delay, duration_seconds, volume, pan) {
    const context = audio.context;
    const start_time = context.currentTime + start_delay;
    const oscillator = context.createOscillator();
    oscillator.type = type;
    oscillator.frequency.setValueAtTime(from_frequency, start_time);
    oscillator.frequency.exponentialRampToValueAtTime(to_frequency, start_time + duration_seconds);
    const gain = context.createGain();
    gain.gain.setValueAtTime(0, start_time);
    gain.gain.linearRampToValueAtTime(volume, start_time + 0.03);
    gain.gain.setValueAtTime(volume, start_time + duration_seconds * 0.7);
    gain.gain.linearRampToValueAtTime(0, start_time + duration_seconds);
    let last_node = gain;
    if (context.createStereoPanner) {
        const panner = context.createStereoPanner();
        panner.pan.value = Math.max(-1, Math.min(1, pan || 0));
        gain.connect(panner);
        last_node = panner;
    }
    oscillator.connect(gain);
    last_node.connect(audio.effects_gain);
    oscillator.start(start_time);
    oscillator.stop(start_time + duration_seconds + 0.05);
}

// Plays a named sound effect. `distance` (0 near, 1 far) makes it quieter; `pan` is -1 left to 1 right.
function play_sound(sound_name, distance, pan) {
    if (!audio.context || audio.muted) {
        return;
    }
    const nearness = 1 - Math.min(0.85, distance || 0);
    switch (sound_name) {
        case 'whistle':
            play_sliding_tone('sine', 1500, 2100, 0, 0.22, 0.16 * nearness, pan);
            play_sliding_tone('sine', 2100, 1650, 0.28, 0.32, 0.16 * nearness, pan);
            break;
        case 'bark':
            for (const delay of [0, 0.24]) {
                play_sliding_tone('sawtooth', 520, 230, delay, 0.13, 0.12 * nearness, pan);
                play_noise_burst('bandpass', 900, 0.12, 0.25 * nearness, delay, pan);
            }
            break;
        case 'whine':
            play_sliding_tone('sine', 980, 760, 0, 0.6, 0.05 * nearness, pan);
            break;
        case 'step':
            play_noise_burst('bandpass', 1400, 0.05, 0.05 * nearness, 0, pan);
            break;
        case 'splash':
            play_noise_burst('highpass', 1800, 0.25, 0.18 * nearness, 0, pan);
            break;
        case 'collapse':
            play_noise_burst('lowpass', 220, 2.2, 0.9, 0, pan);
            play_noise_burst('bandpass', 1200, 0.6, 0.35, 0.15, pan);
            play_noise_burst('bandpass', 700, 0.5, 0.3, 0.55, pan);
            break;
        case 'gate':
            play_noise_burst('bandpass', 420, 1.4, 0.35, 0, pan);
            play_sliding_tone('square', 90, 60, 0, 1.2, 0.05, pan);
            break;
        case 'lever':
            play_noise_burst('bandpass', 2400, 0.08, 0.4, 0, pan);
            play_noise_burst('lowpass', 300, 0.4, 0.5, 0.1, pan);
            break;
        case 'thud':
            play_noise_burst('lowpass', 160, 0.8, 0.8, 0, pan);
            break;
        case 'chime':
            for (const [ratio, volume] of [[1, 0.12], [2.76, 0.05], [5.4, 0.03]]) {
                play_sliding_tone('sine', 880 * ratio, 879 * ratio, 0, 2.4, volume, pan);
            }
            break;
        case 'bell':
            for (const delay of [0, 1.1, 2.2]) {
                for (const [ratio, volume] of [[1, 0.18], [2.4, 0.08], [4.1, 0.04]]) {
                    play_sliding_tone('sine', 523 * ratio, 522 * ratio, delay, 2.6, volume, pan);
                }
            }
            break;
        case 'thunder':
            play_noise_burst('lowpass', 120, 4, 0.7, 0.2, pan);
            play_noise_burst('lowpass', 300, 1.2, 0.4, 0.1, pan);
            break;
        case 'pickup':
            play_sliding_tone('sine', 660, 990, 0, 0.18, 0.08, pan);
            break;
        case 'heartbeat':
            play_sliding_tone('sine', 70, 50, 0, 0.18, 0.4, 0);
            play_sliding_tone('sine', 70, 50, 0.28, 0.18, 0.3, 0);
            break;
        default:
            break;
    }
}
