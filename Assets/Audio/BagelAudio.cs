using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace Bagel
{
    // Where every sound goes: the game and the UI ask for patches by name, and whoever owns the
    // AudioSource playing BagelSynth plugs it in as source. Without one (UI Builder, editor windows,
    // tests) sounds are only raised as played.
    [NoAutoStaticsCleanup] // ResetStatics starts every Play session with sound on and nothing plugged in
    public static class BagelAudio
    {
        // The drones the game can hold.
        public const int rollDrone = 0;

        static readonly List<SynthNote> s_Notes = new();
        static readonly SynthDrone[] s_Drones = new SynthDrone[SynthEngine.droneCount];

        // Settings: Sound.
        public static bool enabled { get; set; } = true;

        // The instrument of the melodic sounds; the screens' theme sets it.
        public static SoundTone tone { get; set; }

        public static AudioSource source { get; set; }

        public static event Action<string> played;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            enabled = true;
            tone = SoundTone.Wood;
            source = null;
            played = null;
            for (int i = 0; i < s_Drones.Length; i++)
                s_Drones[i] = SynthDrone.silent;
        }

        // Plays patch (from BagelSoundLibrary); false if it wasn't heard.
        public static bool Play(string patch, float semitones = 0f, float pan = 0f, float gain = 1f)
        {
            if (!BagelSoundLibrary.Contains(patch))
            {
                if (patch != null && patch != BagelSoundLibrary.none)
                    Debug.LogWarning($"[BagelAudio] No sound named \"{patch}\".");
                return false;
            }

            played?.Invoke(patch);
            if (!enabled || source == null)
                return false;

            s_Notes.Clear();
            BagelSoundLibrary.Build(patch, tone, semitones, s_Notes, pan, gain);
            return BagelSynth.Play(source, s_Notes);
        }

        // Holds a drone (like BagelAudio.rollDrone) where drone says, until it's set again.
        public static void SetDrone(int index, in SynthDrone drone)
        {
            s_Drones[index] = enabled ? drone : SynthDrone.silent;
            if (source != null)
                BagelSynth.SetDrone(source, index, s_Drones[index]);
        }

        public static SynthDrone GetDrone(int index) => s_Drones[index];
    }
}
