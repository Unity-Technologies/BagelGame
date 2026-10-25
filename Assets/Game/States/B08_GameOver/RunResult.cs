using System;
using Unity.Properties;
using UnityEngine;

namespace Bagel
{
    // How the last run went: what the television shows, and what the leaderboard takes. It's kept in the save.
    [Serializable]
    public class RunResult
    {
        [SerializeField] bool m_HasRun;
        [SerializeField] string m_BagelName;
        [SerializeField] string m_BagelNameKey;
        [SerializeField] int m_Toppings;
        [SerializeField] int m_ToppingsMax;
        [SerializeField] bool m_Submitted;
        [SerializeField] float m_Time;
        [SerializeField] float m_Freshness;
        [SerializeField] bool m_Stale;

        [NonSerialized] PlayManagerState m_Owner;

        // False until the first run ends: the television says there's been none.
        [CreateProperty] public bool hasRun => m_HasRun;
        [CreateProperty] public string bagelName => m_BagelName;
        [CreateProperty] public string bagelNameKey => m_BagelNameKey;
        [CreateProperty] public int toppings => m_Toppings;
        [CreateProperty] public int toppingsMax => m_ToppingsMax;
        [CreateProperty] public float time => m_Time;
        [CreateProperty] public float freshness => m_Freshness;
        [CreateProperty] public bool stale => m_Stale;
        [CreateProperty] public bool won => m_Toppings > 0 && !m_Stale;
        [CreateProperty] public string titleKey => m_Stale ? "gameover.stale" : won ? "gameover.win" : "gameover.lose";
        [CreateProperty] public bool canSubmit => won && !m_Submitted;

        [CreateProperty]
        public bool submitted
        {
            get => m_Submitted;
            set
            {
                if (m_Submitted == value)
                    return;
                m_Submitted = value;
                m_Owner?.Notify(nameof(PlayManagerState.lastRun));
            }
        }

        internal void Bind(PlayManagerState owner) => m_Owner = owner;

        internal void Set(string bagelName, string bagelNameKey, int toppings, int toppingsMax, float time = 0f, float freshness = 1f)
        {
            m_HasRun = true;
            m_BagelName = bagelName;
            m_BagelNameKey = bagelNameKey;
            m_Toppings = Mathf.Max(0, toppings);
            m_ToppingsMax = toppingsMax;
            m_Time = time;
            m_Freshness = Mathf.Clamp01(freshness);
            m_Stale = m_Freshness <= 0f;
            m_Submitted = false;
            m_Owner?.Notify(nameof(PlayManagerState.lastRun));
        }

        // The run a save kept: shown again, but it can't go on the leaderboard twice.
        internal void Load(RunResult saved)
        {
            if (saved == null || !saved.m_HasRun)
                return;
            m_HasRun = true;
            m_BagelName = saved.m_BagelName;
            m_BagelNameKey = saved.m_BagelNameKey;
            m_Toppings = saved.m_Toppings;
            m_ToppingsMax = saved.m_ToppingsMax;
            m_Time = saved.m_Time;
            m_Freshness = saved.m_Freshness;
            m_Stale = saved.m_Stale;
            m_Submitted = true;
            m_Owner?.Notify(nameof(PlayManagerState.lastRun));
        }
    }
}
