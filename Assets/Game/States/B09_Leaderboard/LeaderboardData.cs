using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;

namespace Bagel
{
    [Serializable]
    public class LeaderboardData
    {
        [Serializable]
        public struct LeaderboardEntry
        {
            public string playerName;
            public int toppings;
            // Seconds the run took: the other half of the score, against the toppings it kept.
            public float time;
        }

        [SerializeField] List<LeaderboardEntry> m_Entries = new List<LeaderboardEntry>();

        [NonSerialized] PlayManagerState m_Owner;
        [NonSerialized] int m_LastAddedIndex = -1;

        [CreateProperty] public List<LeaderboardEntry> entries => m_Entries;
        [CreateProperty] public int count => m_Entries.Count;
        [CreateProperty] public int lastAddedIndex => m_LastAddedIndex;

        public event Action<int> onEntryAdded;

        internal void Bind(PlayManagerState owner) => m_Owner = owner;

        public int Add(string playerName, int toppings, float time = 0f)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                playerName = "Bagel";

            m_Entries.Add(new LeaderboardEntry { playerName = playerName.Trim(), toppings = toppings, time = time });
            m_LastAddedIndex = m_Entries.Count - 1;
            m_Owner?.Notify(nameof(PlayManagerState.leaderboard));
            onEntryAdded?.Invoke(m_LastAddedIndex);
            return m_LastAddedIndex;
        }

        public void Load(List<LeaderboardEntry> entries)
        {
            m_Entries.Clear();
            if (entries != null)
                m_Entries.AddRange(entries);
            m_LastAddedIndex = -1;
            m_Owner?.Notify(nameof(PlayManagerState.leaderboard));
        }
    }
}
