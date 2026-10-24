using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace Bagel
{
    // The instrument the melodic sounds play on. Each theme picks one (--b-sound-tone in its styles).
    public enum SoundTone
    {
        // Classic: a marimba with a woodblock knock.
        Wood,
        // Edgy: a plucked string.
        Pluck,
        // High Contrast: a glass chime.
        Glass,
        // Daylight: a celesta with a little jingle.
        Bell
    }

    // Every sound effect in the game, as code: each patch is a handful of SynthNotes. Kitchen and
    // cartoon sounds (a doughy thud, a sprinkle of seeds, the toaster's spring and bell) and UI ones
    // (pops, clicks, little arpeggios), all in C major pentatonic so overlapping sounds agree.
    // The UI picks patches by name from USS (--b-sound: pop;), so any press or event can be retuned
    // without code; the game plays its own by name. Melodic patches play on the theme's SoundTone.
    [NoAutoStaticsCleanup] // a read-only patch table; s_Jitter only lives inside one Build call
    public static class BagelSoundLibrary
    {
        public const string none = "none";

        // C major pentatonic.
        const float G4 = 392f;
        const float C5 = 523.25f, E5 = 659.26f, G5 = 783.99f;
        const float C6 = 1046.5f, E6 = 1318.51f, G6 = 1567.98f, A6 = 1760f;
        const float C7 = 2093f, D7 = 2349.32f, E7 = 2637.02f, G7 = 3135.96f, A7 = 3520f;
        static readonly float[] s_Sparkle = { C7, E7, G7, D7, A7, E7, G7, C7 };
        static readonly float[] s_Seeds = { G6, A6, C7, D7, E7 };

        static readonly Dictionary<string, Action<Builder>> s_Patches = new(StringComparer.Ordinal)
        {
            // Presses
            ["pop"] = b => { b.Jitter(0.6f); b.Add(Sine(420f, 0.085f, 0.55f).GlideTo(980f, 0.035f)); b.Add(Click(4000f, 0.12f)); },
            ["tab"] = b => { b.Tone(C5, 0f, 0.5f); b.Add(Sine(300f, 0.04f, 0.15f).GlideTo(600f, 0.02f)); },
            ["click"] = b =>
            {
                b.Add(SynthNote.Tone(SynthWave.Square, 1900f, 0.03f, 0.18f).GlideTo(1300f, 0.02f).Filter(SynthFilter.LowPass, 5000f));
                b.Add(Click(4000f, 0.25f, SynthFilter.BandPass));
            },
            ["back"] = b => b.Add(Sine(880f, 0.13f, 0.45f).GlideTo(420f, 0.08f)),
            ["nope"] = b =>
            {
                b.Add(SynthNote.Tone(SynthWave.Triangle, 392f, 0.1f, 0.45f).Filter(SynthFilter.LowPass, 2000f));
                b.Add(SynthNote.Tone(SynthWave.Triangle, 311.13f, 0.16f, 0.45f).Filter(SynthFilter.LowPass, 1800f).At(0.1f));
            },
            // A gamepad or keyboard moving the focus: barely there.
            ["tick"] = b => { b.Add(Sine(1800f, 0.03f, 0.16f)); b.Add(Click(6000f, 0.06f)); },
            // A long press that went all the way.
            ["confirm"] = b => { b.Tone(G5, 0f, 0.4f); b.Tone(C6, 0.07f, 0.45f, 1.3f); b.Add(Click(3000f, 0.2f)); },

            // Screens
            ["open"] = b => { b.Add(Sine(330f, 0.16f, 0.4f).GlideTo(880f, 0.09f).Attack(0.005f)); b.Tone(E6, 0.06f, 0.22f); },
            ["close"] = b => b.Add(Sine(780f, 0.14f, 0.35f).GlideTo(300f, 0.1f)),
            ["swish"] = b => b.Add(SynthNote.Noise(0.22f, 1.2f).Attack(0.03f).Filter(SynthFilter.BandPass, 500f, 3800f, 1.3f)),
            ["chime"] = b => { b.Tone(E6, 0f, 0.32f); b.Tone(G6, 0.08f, 0.32f); },
            // A name on the leaderboard: the register's "ka-ching".
            ["submit"] = b =>
            {
                b.Add(SynthNote.Tone(SynthWave.Square, 700f, 0.025f, 0.14f).GlideTo(500f, 0.02f).Filter(SynthFilter.LowPass, 3000f));
                b.Add(SynthNote.Noise(0.05f, 0.5f).Filter(SynthFilter.BandPass, 4500f, 0f, 1.2f).Crackle(0.4f));
                b.Add(Bell(E6, 0.9f, 0.28f).At(0.07f).Pan(-0.2f));
                b.Add(Bell(A6, 0.8f, 0.24f).At(0.075f).Pan(0.2f));
            },

            // The run
            ["go"] = b =>
            {
                b.Tone(G5, 0f, 0.4f);
                b.Tone(G5, 0.16f, 0.4f);
                b.Tone(C6, 0.32f, 0.5f, 1.5f);
                b.Add(SynthNote.Noise(0.25f, 0.5f).Attack(0.03f).Filter(SynthFilter.BandPass, 400f, 2500f, 1.2f).At(0.3f));
            },
            // The bagel hitting something: a doughy thud. The game scales it by how hard.
            ["impact"] = b =>
            {
                b.Jitter(1.5f);
                b.Add(Sine(150f, 0.14f, 0.7f).GlideTo(70f, 0.09f));
                b.Add(SynthNote.Noise(0.07f, 0.5f).Filter(SynthFilter.LowPass, 900f, 200f, 0.9f));
            },
            // Toppings flying off: a sprinkle of tiny ticks.
            ["sprinkle"] = b =>
            {
                b.Add(SynthNote.Noise(0.25f, 0.35f).Filter(SynthFilter.HighPass, 5000f).Crackle(0.85f));
                for (int i = 0; i < 4; i++)
                    b.Add(Sine(s_Seeds[(int)b.RandomRange(0f, s_Seeds.Length - 0.01f)], 0.04f, 0.1f).At(i * 0.045f + b.RandomRange(0f, 0.02f)).Pan(b.RandomRange(-0.5f, 0.5f)));
            },
            // Off the edge.
            ["fall"] = b =>
            {
                b.Add(Sine(1400f, 1f, 0.3f).GlideTo(180f, 0.9f).Vibrato(6f, 0.3f));
                b.Add(SynthNote.Noise(0.6f, 0.15f).Attack(0.2f).Filter(SynthFilter.BandPass, 2000f, 300f, 1.5f));
            },
            // Into the toaster: its spring goes boing, its bell goes ding.
            ["toaster"] = b =>
            {
                b.Add(Sine(260f, 0.45f, 0.45f).GlideTo(520f, 0.04f).Vibrato(16f, 1.2f));
                b.Add(SynthNote.Tone(SynthWave.Square, 900f, 0.02f, 0.12f).Filter(SynthFilter.LowPass, 3000f));
                b.Add(Bell(E7, 1.2f, 0.3f).At(0.12f));
            },
            ["win"] = b =>
            {
                b.Tone(C5, 0f, 0.5f);
                b.Tone(E5, 0.09f, 0.5f);
                b.Tone(G5, 0.18f, 0.5f);
                b.Tone(C6, 0.27f, 0.5f);
                b.Add(Sine(130f, 0.3f, 0.3f).GlideTo(90f, 0.2f).At(0.4f));
                b.Tone(E6, 0.4f, 0.3f, 2f);
                b.Tone(G6, 0.4f, 0.28f, 2f);
                b.Tone(C7, 0.4f, 0.26f, 2f);
                Sparkles(b, 0.5f, s_Sparkle.Length, 0.07f);
            },
            // Womp womp.
            ["lose"] = b =>
            {
                b.Add(SynthNote.Tone(SynthWave.Triangle, G4, 0.3f, 0.5f).Filter(SynthFilter.LowPass, 1500f));
                b.Add(SynthNote.Tone(SynthWave.Triangle, 370f, 0.3f, 0.5f).Filter(SynthFilter.LowPass, 1400f).At(0.3f));
                b.Add(SynthNote.Tone(SynthWave.Triangle, 349.23f, 0.9f, 0.55f).GlideTo(311.13f, 0.6f).Vibrato(5f, 0.4f).Filter(SynthFilter.LowPass, 1300f, 500f).At(0.6f));
            },
        };

        // Every patch name, for tools and tests.
        public static IEnumerable<string> patchNames => s_Patches.Keys;

        public static bool Contains(string patch) => patch != null && s_Patches.ContainsKey(patch);

        // Appends the notes of patch to notes, played on tone, transposed by semitones and scaled by gain.
        // False for an unknown patch (or none).
        public static bool Build(string patch, SoundTone tone, float semitones, List<SynthNote> notes, float pan = 0f, float gain = 1f)
        {
            if (patch == null || !s_Patches.TryGetValue(patch, out var build))
                return false;
            var start = notes.Count;
            build(new Builder(tone, notes));

            // Transpose and pan after building, so a patch's own jitter and the caller's pitch add up.
            for (int i = start; i < notes.Count; i++)
            {
                var note = notes[i].Transpose(semitones + s_Jitter);
                if (pan != 0f)
                    note.pan = Mathf.Clamp(note.pan + pan, -1f, 1f);
                note.gain *= gain;
                notes[i] = note;
            }
            s_Jitter = 0f;
            return true;
        }

        public static bool TryParseTone(string value, out SoundTone tone) =>
            Enum.TryParse(value?.Trim().Trim('"', '\''), ignoreCase: true, out tone);

        static float s_Jitter;

        readonly struct Builder
        {
            readonly SoundTone m_Tone;
            readonly List<SynthNote> m_Notes;

            public Builder(SoundTone tone, List<SynthNote> notes)
            {
                m_Tone = tone;
                m_Notes = notes;
            }

            public void Add(SynthNote note) => m_Notes.Add(note);

            // Detunes the whole sound by up to ±semitones, so repeats don't sound identical.
            public void Jitter(float semitones) => s_Jitter = UnityEngine.Random.Range(-semitones, semitones);

            public float RandomRange(float min, float max) => UnityEngine.Random.Range(min, max);

            // One note on the theme's instrument.
            public void Tone(float frequency, float delay, float gain, float length = 1f)
            {
                switch (m_Tone)
                {
                    case SoundTone.Pluck:
                        // A plucked string: a bright saw whose filter closes, bent slightly into pitch.
                        Add(SynthNote.Tone(SynthWave.Saw, frequency * 1.012f, 0.9f * length, 0.5f * gain).Attack(0.001f)
                            .GlideTo(frequency, 0.05f).Filter(SynthFilter.LowPass, frequency * 7f, frequency * 1.3f, 1.1f).At(delay));
                        Add(SynthNote.Noise(0.01f, 0.15f * gain).Filter(SynthFilter.HighPass, 2500f).At(delay));
                        break;
                    case SoundTone.Glass:
                        // Glass: inharmonic FM (an octave up for low notes), a long ring and a tiny "tink".
                        Add(Sine(frequency * Octave(frequency), 1.4f * length, 0.42f * gain).Fm(2.76f, 1.6f, 0.25f).At(delay));
                        Add(Sine(frequency * 5.4f, 0.05f, 0.12f * gain).At(delay));
                        break;
                    case SoundTone.Bell:
                        // A celesta (an octave up for low notes) with a short jingle.
                        Add(Bell(frequency * Octave(frequency), 0.7f * length, 0.42f * gain).At(delay));
                        Add(SynthNote.Noise(0.14f, 0.08f * gain).Filter(SynthFilter.HighPass, 6500f, 0f, 0.9f).Crackle(0.75f).At(delay));
                        break;
                    default:
                        // Wood: a marimba bar (bright FM attack, pure ring) and a woodblock knock.
                        Add(Sine(frequency, 0.3f * length, gain).Fm(4f, 1.4f, 0.03f).At(delay));
                        Add(SynthNote.Noise(0.012f, 0.18f * gain).Filter(SynthFilter.BandPass, 3000f, 0f, 1.5f).At(delay));
                        break;
                }
            }
        }

        // Glass and celesta sound an octave up, except notes that are already high.
        static float Octave(float frequency) => frequency < 1000f ? 2f : 1f;

        static SynthNote Sine(float frequency, float decay, float gain) => SynthNote.Tone(SynthWave.Sine, frequency, decay, gain);

        static SynthNote Bell(float frequency, float decay, float gain) => Sine(frequency, decay, gain).Fm(3.5f, 2.5f, 0.1f);

        static SynthNote Click(float cutoff, float gain, SynthFilter filter = SynthFilter.LowPass) =>
            SynthNote.Noise(0.008f, gain).Filter(filter, cutoff);

        static void Sparkles(Builder b, float start, int count, float gain)
        {
            for (int i = 0; i < count; i++)
                b.Add(Sine(s_Sparkle[i % s_Sparkle.Length], 0.12f, gain).At(start + i * 0.07f).Pan(i % 2 == 0 ? -0.5f : 0.5f));
        }

        // The sound of the bagel rolling: rumbling noise that rises with speed. Rough ground crackles;
        // ice just hisses.
        public static SynthDrone Roll(float speed01, float roughness)
        {
            speed01 = Mathf.Clamp01(speed01);
            roughness = Mathf.Clamp01(roughness);
            return new SynthDrone
            {
                wave = SynthWave.Noise,
                gain = 0.9f * speed01,
                filter = SynthFilter.LowPass,
                cutoff = Mathf.Lerp(900f, 180f, roughness) + speed01 * Mathf.Lerp(2400f, 600f, roughness),
                resonance = 0.9f,
                crackle = roughness * 0.45f,
            };
        }
    }
}
