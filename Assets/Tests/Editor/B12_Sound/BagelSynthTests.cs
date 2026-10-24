using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace Bagel.B12_Sound
{
    // Every sound is code (BagelSoundLibrary) rendered by the synth (SynthEngine): these render them
    // offline and check what comes out.
    public class BagelSynthTests
    {
        const int k_SampleRate = 48000;
        const int k_BlockFrames = 512;

        [Test]
        public void EveryPatchPlaysAudiblyAndEndsOnEveryInstrument()
        {
            var notes = new List<SynthNote>();
            var buffer = new float[k_BlockFrames * 2];
            foreach (var patch in BagelSoundLibrary.patchNames)
            {
                foreach (SoundTone tone in Enum.GetValues(typeof(SoundTone)))
                {
                    notes.Clear();
                    Assert.IsTrue(BagelSoundLibrary.Build(patch, tone, 0f, notes), patch);
                    var engine = SynthEngine.Create(k_SampleRate, 1f);
                    foreach (var note in notes)
                        engine.Start(note);

                    var peak = 0f;
                    var finite = true;
                    for (var seconds = 0f; engine.activeVoices > 0 && seconds < 4f; seconds += (float)k_BlockFrames / k_SampleRate)
                    {
                        engine.Render(new ChannelBuffer(buffer, 2));
                        foreach (var sample in buffer)
                        {
                            finite &= float.IsFinite(sample);
                            peak = Mathf.Max(peak, Mathf.Abs(sample));
                        }
                    }

                    Assert.IsTrue(finite, $"{patch} on {tone}: NaN or infinity");
                    Assert.AreEqual(0, engine.activeVoices, $"{patch} on {tone}: still playing after 4 s");
                    Assert.That(peak, Is.InRange(0.1f, 0.99f), $"{patch} on {tone}: peak");
                }
            }
        }

        [Test]
        public void TheRollHoldsUntilItFadesOut()
        {
            var engine = SynthEngine.Create(k_SampleRate, 1f);
            engine.SetDrone(BagelAudio.rollDrone, BagelSoundLibrary.Roll(0.8f, 1f));
            Assert.Greater(Render(ref engine, 2f), 0.05f, "rolling");
            Assert.Greater(Render(ref engine, 0.1f), 0.05f, "still rolling, with no end of its own");

            engine.SetDrone(BagelAudio.rollDrone, BagelSoundLibrary.Roll(0f, 1f));
            Render(ref engine, 1f);
            Assert.Less(Render(ref engine, 0.1f), 0.001f, "stopped");
            Assert.IsFalse(engine.IsDroneAudible(BagelAudio.rollDrone));
        }

        [Test]
        public void IceRollsWithAHigherHissThanGround()
        {
            var ice = BagelSoundLibrary.Roll(0.5f, 0f);
            var ground = BagelSoundLibrary.Roll(0.5f, 1f);
            Assert.Greater(ice.cutoff, ground.cutoff);
            Assert.Less(ice.crackle, ground.crackle);
            Assert.Greater(BagelSoundLibrary.Roll(1f, 1f).gain, BagelSoundLibrary.Roll(0.2f, 1f).gain);
        }

        [Test]
        public void TheSynthIsSilentUntilItGetsNotesThroughTheScriptableAudioPipeline()
        {
            // The same path an AudioSource takes, on a context mixed by hand: notes are messages to the
            // generator's control part, which pipes them to its real-time part.
            var format = new AudioFormat(AudioSpeakerMode.Stereo, k_SampleRate, k_BlockFrames);
            var manual = ControlContext.CreateManualControlContext(format);
            var synth = ScriptableObject.CreateInstance<BagelSynth>();
            try
            {
                var instance = ((IAudioGenerator)synth).CreateInstance(manual.context, format, new GeneratorInstance.CreationParameters());
                ulong tick = 0;
                Assert.AreEqual(0f, Mix(manual, instance, 4, ref tick), "silence before any note");

                var notes = new List<SynthNote>();
                BagelSoundLibrary.Build("pop", SoundTone.Wood, 0f, notes);
                Assert.IsTrue(BagelSynth.Play(manual.context, instance, notes));
                Assert.Greater(Mix(manual, instance, 8, ref tick), 0.1f, "the pop");

                Assert.IsTrue(BagelSynth.SetDrone(manual.context, instance, BagelAudio.rollDrone, BagelSoundLibrary.Roll(1f, 1f)));
                Mix(manual, instance, 40, ref tick);
                Assert.Greater(Mix(manual, instance, 8, ref tick), 0.05f, "the roll, long after the pop");

                manual.context.Destroy(instance);
            }
            finally
            {
                Object.DestroyImmediate(synth);
                manual.Dispose();
            }
        }

        [Test]
        public void MoreNotesThanVoicesReplaceTheOnesClosestToTheirEnd()
        {
            var engine = SynthEngine.Create(k_SampleRate, 1f);
            for (int i = 0; i < SynthEngine.voiceCount + 6; i++)
                engine.Start(SynthNote.Tone(SynthWave.Sine, 440f + i, 0.5f + i * 0.1f, 0.1f));
            Assert.AreEqual(SynthEngine.voiceCount, engine.activeVoices);

            var buffer = new float[k_BlockFrames * 2];
            engine.Render(new ChannelBuffer(buffer, 2));
            Assert.AreEqual(SynthEngine.voiceCount, engine.activeVoices);
        }

        [Test]
        public void GainScalesAWholePatch()
        {
            var full = new List<SynthNote>();
            var half = new List<SynthNote>();
            BagelSoundLibrary.Build("impact", SoundTone.Wood, 0f, full);
            BagelSoundLibrary.Build("impact", SoundTone.Wood, 0f, half, gain: 0.5f);
            Assert.AreEqual(full.Count, half.Count);
            for (int i = 0; i < full.Count; i++)
                Assert.AreEqual(full[i].gain * 0.5f, half[i].gain, 1e-6f);
        }

        [Test]
        public void EverySoundTheStylesNameExists()
        {
            // --b-sound: pop; and --b-sound-<cue>: patch; name patches, --b-sound-tone an instrument.
            var declaration = new Regex(@"(--b-sound(?:-[a-z-]+)?)\s*:\s*([^;]+);");
            var checkedCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:StyleSheet", new[] { "Assets/UI" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (Match match in declaration.Matches(File.ReadAllText(path)))
                {
                    var property = match.Groups[1].Value;
                    var value = match.Groups[2].Value.Trim().Trim('"', '\'');
                    if (value.StartsWith("var("))
                        continue;
                    if (property == BagelSound.pitchProperty)
                        Assert.IsTrue(float.TryParse(value, out _), $"{path}: {property}: {value}");
                    else if (property == BagelSound.toneProperty)
                        Assert.IsTrue(BagelSoundLibrary.TryParseTone(value, out _), $"{path}: {property}: {value}");
                    else
                        Assert.IsTrue(value == BagelSoundLibrary.none || BagelSoundLibrary.Contains(value), $"{path}: {property}: {value} isn't a sound");
                    checkedCount++;
                }
            }
            Assert.Greater(checkedCount, 10);
        }

        static float Render(ref SynthEngine engine, float seconds)
        {
            var buffer = new float[k_BlockFrames * 2];
            var peak = 0f;
            for (var time = 0f; time < seconds; time += (float)k_BlockFrames / k_SampleRate)
            {
                engine.Render(new ChannelBuffer(buffer, 2));
                foreach (var sample in buffer)
                    peak = Mathf.Max(peak, Mathf.Abs(sample));
            }
            return peak;
        }

        static float Mix(ControlContext.Manual manual, GeneratorInstance instance, int blocks, ref ulong tick)
        {
            var data = new float[k_BlockFrames * 2];
            var output = new float[k_BlockFrames * 2];
            var peak = 0f;
            for (int b = 0; b < blocks; b++)
            {
                manual.Update();
                var context = manual.BeginMix(tick);
                if (context == null)
                    Assert.Inconclusive("Audio is bypassed on this machine.");
                Array.Clear(data, 0, data.Length);
                context.Value.Process(instance, new ChannelBuffer(data, 2), default);
                manual.EndMix(new ChannelBuffer(output, 2));
                tick += k_BlockFrames;
                foreach (var sample in data)
                    peak = Mathf.Max(peak, Mathf.Abs(sample));
            }
            return peak;
        }
    }
}
