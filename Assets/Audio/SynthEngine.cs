using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Audio;

namespace Bagel
{
    // The synth's voices: renders SynthNotes and SynthDrones, mixed and softly limited. Plain unmanaged
    // data and math, so it runs Burst-compiled on the audio thread inside BagelSynth, and the same code
    // renders offline in tests.
    // Every note is a voice: a sine (optionally frequency-modulated, for bells), a band-limited triangle,
    // square or saw, or noise; then a pitch glide and vibrato, random grains (crackle), a resonant
    // state-variable filter whose cutoff sweeps over the note, and an attack plus exponential decay. When
    // all voices are busy, the one closest to its end makes room. Drones hold until they're faded out.
    public struct SynthEngine
    {
        public const int voiceCount = 24;
        public const int droneCount = 2;

        const float k_TwoPi = 2f * math.PI;
        const float k_Minus60dB = -6.9077554f; // ln(0.001)
        const float k_DroneSmoothing = 0.03f;  // seconds a drone takes to get most of the way to a change

        struct Voice
        {
            public SynthNote note;
            public int wait;          // samples until the note starts
            public float time;        // seconds since it started
            public float phase;       // oscillator phase, 0..1
            public float modPhase;    // FM modulator phase, 0..1
            public float vibratoPhase;
            public float ic1, ic2;    // filter state
            public float glideLog;    // ln(frequencyEnd / frequency)
            public float cutoffLog;   // ln(cutoffEnd / cutoff)
            public float panLeft, panRight;
            public float grainGain;
            public int grainLeft;     // samples left in the current crackle grain
            public uint random;
            public bool active;
        }

        struct Drone
        {
            public SynthDrone target;
            public float gain, frequency, cutoff, pan; // where it is now, sliding toward the target
            public float phase;
            public float ic1, ic2;
            public float grainGain;
            public int grainLeft;
            public uint random;
        }

        FixedList4096Bytes<Voice> m_Voices;
        FixedList512Bytes<Drone> m_Drones;
        float m_SampleRate;
        float m_Dt;
        float m_Smoothing;
        uint m_Seed;
        int m_ActiveCount;

        // Gain applied to the mix before the soft limiter.
        public float volume;

        public readonly int activeVoices => m_ActiveCount;
        public readonly float sampleRate => m_SampleRate;

        public static SynthEngine Create(int sampleRate, float volume, uint seed = 0x9E3779B9u)
        {
            var engine = new SynthEngine { volume = volume, m_Seed = seed == 0 ? 1u : seed };
            for (int i = 0; i < voiceCount; i++)
                engine.m_Voices.Add(default);
            for (int i = 0; i < droneCount; i++)
                engine.m_Drones.Add(new Drone { target = SynthDrone.silent, grainGain = 1f, random = (uint)(i + 1) * 0x9E3779B9u });
            engine.Configure(sampleRate);
            return engine;
        }

        // The output format changed (e.g. headphones plugged in): keep playing at the new rate.
        public void Configure(int sampleRate)
        {
            m_SampleRate = math.max(8000, sampleRate);
            m_Dt = 1f / m_SampleRate;
            m_Smoothing = 1f - math.exp(-m_Dt / k_DroneSmoothing);
        }

        public readonly bool IsDroneAudible(int index) => index >= 0 && index < m_Drones.Length && IsAudible(m_Drones[index]);

        static bool IsAudible(in Drone drone) => drone.gain > 1e-5f || drone.target.gain > 0f;

        readonly bool anyDroneAudible
        {
            get
            {
                for (int i = 0; i < m_Drones.Length; i++)
                {
                    if (IsAudible(m_Drones[i]))
                        return true;
                }
                return false;
            }
        }

        public void StopAll()
        {
            for (int i = 0; i < m_Voices.Length; i++)
                m_Voices.ElementAt(i).active = false;
            m_ActiveCount = 0;
            for (int i = 0; i < m_Drones.Length; i++)
            {
                ref var drone = ref m_Drones.ElementAt(i);
                drone.target.gain = 0f;
                drone.gain = 0f;
            }
        }

        public void SetDrone(int index, in SynthDrone target)
        {
            if (index < 0 || index >= m_Drones.Length)
                return;

            ref var drone = ref m_Drones.ElementAt(index);

            // A drone that was silent starts where it's asked to be, instead of sliding up from nothing.
            if (drone.gain <= 1e-5f)
            {
                drone.frequency = target.frequency;
                drone.cutoff = target.cutoff;
                drone.pan = target.pan;
            }
            drone.target = target;
        }

        public void Start(in SynthNote note)
        {
            if (note.gain <= 0f || note.decay <= 0f || m_Voices.Length == 0)
                return;

            // A free voice, or the one with the least left to play.
            var slot = -1;
            var leastLeft = float.MaxValue;
            for (int i = 0; i < m_Voices.Length; i++)
            {
                ref var v = ref m_Voices.ElementAt(i);
                if (!v.active)
                {
                    slot = i;
                    break;
                }
                var left = v.wait * m_Dt + v.note.attack + v.note.decay - v.time;
                if (left < leastLeft)
                {
                    leastLeft = left;
                    slot = i;
                }
            }

            ref var voice = ref m_Voices.ElementAt(slot);
            if (!voice.active)
                m_ActiveCount++;

            m_Seed = m_Seed * 1664525u + 1013904223u;
            var pan = math.clamp(note.pan, -1f, 1f);
            voice = new Voice
            {
                note = note,
                wait = (int)math.max(0f, note.delay * m_SampleRate),
                glideLog = note.frequencyEnd > 0f && note.frequency > 0f && note.glide > 0f ? math.log(note.frequencyEnd / note.frequency) : 0f,
                cutoffLog = note.cutoffEnd > 0f && note.cutoff > 0f ? math.log(note.cutoffEnd / note.cutoff) : 0f,
                panLeft = math.cos((pan + 1f) * math.PI * 0.25f),
                panRight = math.sin((pan + 1f) * math.PI * 0.25f),
                grainGain = 1f,
                random = m_Seed | 1u,
                active = true,
            };
        }

        // Renders what's playing into buffer (overwriting it).
        public void Render(ChannelBuffer buffer)
        {
            var frames = buffer.frameCount;
            var channels = buffer.channelCount;
            if (m_ActiveCount == 0 && !anyDroneAudible)
            {
                buffer.Clear();
                return;
            }

            for (int frame = 0; frame < frames; frame++)
            {
                float left = 0f, right = 0f;
                for (int i = 0; i < m_Voices.Length; i++)
                {
                    ref var v = ref m_Voices.ElementAt(i);
                    if (!v.active)
                        continue;
                    if (v.wait > 0)
                    {
                        v.wait--;
                        continue;
                    }
                    var sample = Next(ref v);
                    left += sample * v.panLeft;
                    right += sample * v.panRight;
                    if (!v.active)
                        m_ActiveCount--;
                }

                for (int i = 0; i < m_Drones.Length; i++)
                {
                    ref var d = ref m_Drones.ElementAt(i);
                    if (!IsAudible(d))
                        continue;
                    var sample = Next(ref d);
                    left += sample * math.cos((d.pan + 1f) * math.PI * 0.25f);
                    right += sample * math.sin((d.pan + 1f) * math.PI * 0.25f);
                }

                left = math.tanh(left * volume);
                right = math.tanh(right * volume);
                if (channels == 1)
                {
                    buffer[0, frame] = (left + right) * 0.7071f;
                }
                else
                {
                    buffer[0, frame] = left;
                    buffer[1, frame] = right;
                    for (int c = 2; c < channels; c++)
                        buffer[c, frame] = 0f;
                }
            }
        }

        float Next(ref Voice v)
        {
            ref readonly var n = ref v.note;
            var t = v.time;
            var length = n.attack + n.decay;

            // Pitch: exponential glide, then vibrato.
            var frequency = n.frequency;
            if (v.glideLog != 0f)
                frequency *= math.exp(v.glideLog * math.min(t / n.glide, 1f));
            if (n.vibratoDepth != 0f)
            {
                frequency *= math.exp2(n.vibratoDepth / 12f * math.sin(k_TwoPi * v.vibratoPhase));
                v.vibratoPhase = math.frac(v.vibratoPhase + n.vibratoRate * m_Dt);
            }

            float s;
            if (n.wave == SynthWave.Sine && n.fmRatio > 0f && n.fmIndex > 0f)
            {
                var index = n.fmDecay > 0f ? n.fmIndex * math.exp(-t / n.fmDecay) : n.fmIndex;
                s = math.sin(k_TwoPi * v.phase + index * math.sin(k_TwoPi * v.modPhase));
                v.modPhase = math.frac(v.modPhase + frequency * n.fmRatio * m_Dt);
                v.phase = math.frac(v.phase + math.min(frequency * m_Dt, 0.5f));
            }
            else
            {
                s = Oscillator(n.wave, ref v.phase, frequency, ref v.random);
            }

            if (n.crackle > 0f)
                s *= Crackle(n.crackle, ref v.grainLeft, ref v.grainGain, ref v.random);

            // The cutoff sweeps exponentially over the note.
            if (n.filter != SynthFilter.None && n.cutoff > 0f)
            {
                var cutoff = n.cutoff;
                if (v.cutoffLog != 0f)
                    cutoff *= math.exp(v.cutoffLog * math.min(t / length, 1f));
                s = Filter(n.filter, s, cutoff, n.resonance, ref v.ic1, ref v.ic2);
            }

            // Envelope: linear attack, then -60 dB over the decay.
            var amplitude = t < n.attack ? t / n.attack : math.exp(k_Minus60dB * (t - n.attack) / n.decay);
            v.time = t + m_Dt;
            if (v.time >= length)
                v.active = false;
            return s * amplitude * n.gain;
        }

        float Next(ref Drone d)
        {
            ref readonly var target = ref d.target;
            d.gain += (target.gain - d.gain) * m_Smoothing;
            d.frequency += (target.frequency - d.frequency) * m_Smoothing;
            d.cutoff += (target.cutoff - d.cutoff) * m_Smoothing;
            d.pan += (target.pan - d.pan) * m_Smoothing;

            var s = Oscillator(target.wave, ref d.phase, d.frequency, ref d.random);
            if (target.crackle > 0f)
                s *= Crackle(target.crackle, ref d.grainLeft, ref d.grainGain, ref d.random);
            if (target.filter != SynthFilter.None && d.cutoff > 0f)
                s = Filter(target.filter, s, d.cutoff, target.resonance, ref d.ic1, ref d.ic2);
            return s * d.gain;
        }

        float Oscillator(SynthWave wave, ref float phase, float frequency, ref uint random)
        {
            var step = math.min(frequency * m_Dt, 0.5f);
            float s;
            switch (wave)
            {
                case SynthWave.Triangle:
                    s = 4f * math.abs(phase - 0.5f) - 1f;
                    break;
                case SynthWave.Square:
                    s = (phase < 0.5f ? 1f : -1f) + PolyBlep(phase, step) - PolyBlep(math.frac(phase + 0.5f), step);
                    break;
                case SynthWave.Saw:
                    s = 2f * phase - 1f - PolyBlep(phase, step);
                    break;
                case SynthWave.Noise:
                    s = NextRandom(ref random) * 2f - 1f;
                    break;
                default:
                    s = math.sin(k_TwoPi * phase);
                    break;
            }
            phase = math.frac(phase + step);
            return s;
        }

        // Random grains of 2-9 ms, mostly quiet with loud spikes.
        float Crackle(float amount, ref int grainLeft, ref float grainGain, ref uint random)
        {
            if (--grainLeft <= 0)
            {
                grainLeft = (int)((0.002f + 0.007f * NextRandom(ref random)) * m_SampleRate);
                grainGain = math.pow(NextRandom(ref random), 1f + 6f * amount);
            }
            return grainGain;
        }

        // A TPT state-variable filter.
        float Filter(SynthFilter type, float s, float cutoff, float resonance, ref float ic1, ref float ic2)
        {
            cutoff = math.clamp(cutoff, 20f, 0.45f * m_SampleRate);
            var g = math.tan(math.PI * cutoff * m_Dt);
            var k = 1f / math.max(0.1f, resonance);
            var a1 = 1f / (1f + g * (g + k));
            var a2 = g * a1;
            var a3 = g * a2;
            var v3 = s - ic2;
            var v1 = a1 * ic1 + a2 * v3;
            var v2 = ic2 + a2 * ic1 + a3 * v3;
            ic1 = 2f * v1 - ic1;
            ic2 = 2f * v2 - ic2;
            return type switch
            {
                SynthFilter.LowPass => v2,
                SynthFilter.BandPass => v1 * k,
                _ => s - k * v1 - v2,
            };
        }

        // Band-limits the steps of the square and saw, so high notes don't alias into a buzz.
        static float PolyBlep(float phase, float step)
        {
            if (phase < step)
            {
                var x = phase / step;
                return x + x - x * x - 1f;
            }
            if (phase > 1f - step)
            {
                var x = (phase - 1f) / step;
                return x * x + x + x + 1f;
            }
            return 0f;
        }

        static float NextRandom(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (state >> 8) * (1f / 16777216f);
        }
    }
}
