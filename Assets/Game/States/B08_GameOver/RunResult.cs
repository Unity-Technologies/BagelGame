using System;
using Unity.Properties;
using UnityEngine;

namespace Bagel
{
    [Serializable]
    public class RunResult
    {
        [SerializeField] string m_BagelName;
        [SerializeField] string m_BagelNameKey;
        [SerializeField] int m_Toppings;
        [SerializeField] int m_ToppingsMax;
        [SerializeField] bool m_Submitted;
        [SerializeField] float m_Time;
        [SerializeField] float m_Freshness;
        [SerializeField] bool m_Stale;

        [NonSerialized] PlayManagerState m_Owner;

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
    }
}
