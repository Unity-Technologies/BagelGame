using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace Bagel
{
    // Remembers the settings and the leaderboard on the device, as JSON in PlayerPrefs. In the
    // Editor every Play session starts from the start state, so what you test is reproducible.
    [NoAutoStaticsCleanup] // ResetStatics starts every Play session clean
    public static class BagelSave
    {
        public const string key = "BagelGame_Save";

        // Off in tests that play the real scene, so they never read or overwrite the player's save.
        public static bool enabled { get; set; } = true;

        static string s_LastJson;

        // Enter Play Mode Options skip the domain reload in this project: start every session clean.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_LastJson = null;
            enabled = true;
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void StartFromScratch() => Delete();
#endif

        public static bool Load(PlayManagerState state)
        {
            if (!enabled || state == null || !PlayerPrefs.HasKey(key))
                return false;

            var json = PlayerPrefs.GetString(key);
            try
            {
                state.Load(JsonUtility.FromJson<PlayManagerState.Saved>(json));
                s_LastJson = json;
                return true;
            }
            catch (ArgumentException e)
            {
                Debug.LogWarning($"Bagel Game: the save couldn't be read and was ignored ({e.Message})");
                return false;
            }
        }

        public static void Save(PlayManagerState state)
        {
            if (!enabled || state == null)
                return;

            var json = JsonUtility.ToJson(state.ToSaved());
            if (json == s_LastJson)
                return;

            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
            s_LastJson = json;
        }

        public static void Delete()
        {
            PlayerPrefs.DeleteKey(key);
            s_LastJson = null;
        }
    }
}
