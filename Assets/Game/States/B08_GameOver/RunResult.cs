using System;
using Unity.Properties;
using UnityEngine;

namespace Bagel
{
    [Serializable]
    public class RunResult
    {
        [SerializeField] string m_BagelName;
        [SerializeField] int m_Toppings;
        [SerializeField] int m_ToppingsMax;
        [SerializeField] bool m_Submitted;

        [NonSerialized] PlayManagerState m_Owner;

        [CreateProperty] public string bagelName => m_BagelName;
        [CreateProperty] public int toppings => m_Toppings;
        [CreateProperty] public int toppingsMax => m_ToppingsMax;
        [CreateProperty] public bool won => m_Toppings > 0;
        [CreateProperty] public string title => won ? "You Win!" : "You Lose!";
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

        internal void Set(string bagelName, int toppings, int toppingsMax)
        {
            m_BagelName = bagelName;
            m_Toppings = Mathf.Max(0, toppings);
            m_ToppingsMax = toppingsMax;
            m_Submitted = false;
            m_Owner?.Notify(nameof(PlayManagerState.lastRun));
        }
    }
}
