using System;
using UnityEngine;

namespace Bagel
{
    public enum SynthWave : byte
    {
        Sine,
        Triangle,
        Square,
        Saw,
        Noise
    }

    public enum SynthFilter : byte
    {
        None,
        LowPass,
        BandPass,
        HighPass
    }

    // One voice of a sound: an oscillator (or noise), an optional FM modulator, pitch glide and vibrato,
    // an optional resonant filter with a cutoff sweep, and an envelope (an attack, then an exponential
    // decay that reaches -60 dB after decay seconds). A sound is a few notes, each with its own delay.
    // Plain unmanaged data, so a note travels as a message from the main thread to the audio thread.
    // Build notes with Tone or Noise and the chained setters:
    // SynthNote.Tone(SynthWave.Sine, 400f, 0.08f, 0.5f).GlideTo(900f, 0.035f)
    [Serializable]
    public struct SynthNote
    {
        // Seconds after the start of the sound.
        public float delay;
        public SynthWave wave;
        // Hz (noise ignores it).
        public float frequency;
        // Hz the pitch glides to, exponentially, over glide seconds (0: no glide).
        public float frequencyEnd;
        public float glide;
        // Vibrato speed (Hz) and depth (semitones).
        public float vibratoRate;
        public float vibratoDepth;
        // FM: the modulator runs at fmRatio times the pitch (0: no FM). Its index starts at fmIndex and
        // falls by e every fmDecay seconds, so bells start bright.
        public float fmRatio;
        public float fmIndex;
        public float fmDecay;
        public float attack;
        // Seconds from the end of the attack to -60 dB; the voice ends there.
        public float decay;
        public float gain;
        public SynthFilter filter;
        // Filter cutoff (Hz) at the start, and where it sweeps to over the note (0: fixed).
        public float cutoff;
        public float cutoffEnd;
        // Filter Q (0.707: no resonance).
        public float resonance;
        // 0 to 1: chops the note into random grains, like crumbs or a sprinkle of seeds.
        public float crackle;
        // -1 (left) to 1 (right).
        public float pan;

        // Seconds from the start of the sound to the end of this note.
        public readonly float end => delay + attack + decay;

        public static SynthNote Tone(SynthWave wave, float frequency, float decay, float gain) => new()
        {
            wave = wave,
            frequency = frequency,
            attack = 0.002f,
            decay = decay,
            gain = gain,
            resonance = 0.707f,
        };

        public static SynthNote Noise(float decay, float gain) => Tone(SynthWave.Noise, 0f, decay, gain);

        public SynthNote At(float seconds) { delay = seconds; return this; }
        public SynthNote Attack(float seconds) { attack = seconds; return this; }
        public SynthNote GlideTo(float hz, float seconds) { frequencyEnd = hz; glide = seconds; return this; }
        public SynthNote Vibrato(float rate, float semitones) { vibratoRate = rate; vibratoDepth = semitones; return this; }
        public SynthNote Fm(float ratio, float index, float decaySeconds) { fmRatio = ratio; fmIndex = index; fmDecay = decaySeconds; return this; }
        public SynthNote Crackle(float amount) { crackle = amount; return this; }
        public SynthNote Pan(float value) { pan = value; return this; }

        public SynthNote Filter(SynthFilter type, float cutoffHz, float cutoffEndHz = 0f, float q = 0.707f)
        {
            filter = type;
            cutoff = cutoffHz;
            cutoffEnd = cutoffEndHz;
            resonance = q;
            return this;
        }

        // Shifts the pitch (not the filter) by semitones.
        public SynthNote Transpose(float semitones)
        {
            if (semitones == 0f)
                return this;
            var ratio = Mathf.Pow(2f, semitones / 12f);
            frequency *= ratio;
            frequencyEnd *= ratio;
            return this;
        }
    }

    // A sound that holds for as long as the game wants it, like the bagel rolling: the main thread keeps
    // sending where it should be, and the synth slides there smoothly. A gain of 0 fades it out.
    [Serializable]
    public struct SynthDrone
    {
        public SynthWave wave;
        public float frequency;
        public float gain;
        public SynthFilter filter;
        public float cutoff;
        public float resonance;
        public float crackle;
        public float pan;

        public static readonly SynthDrone silent = new() { resonance = 0.707f };
    }
}
