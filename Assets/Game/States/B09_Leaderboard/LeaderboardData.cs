using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // Every level's leaderboard, in one list: each entry says which level it was played on (a time on the tutorial's
    // short counter is nothing like one on the longest). One level's board is shown at a time: the level just
    // played, at game over, or the one chosen, from the main menu.
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
            // The level's id; empty for runs from before there were levels, on the tutorial's counter.
            public string level;
        }

        // The path code listens for, when another level's board is shown.
        public static readonly BindingId shownLevelPath = nameof(PlayManagerState.leaderboard) + "." + nameof(shownLevel);

        [SerializeField] List<LeaderboardEntry> m_Entries = new List<LeaderboardEntry>();

        [NonSerialized] PlayManagerState m_Owner;
        [NonSerialized] int m_LastAddedIndex = -1;
        [NonSerialized] string m_DefaultLevel;
        [NonSerialized] string m_ShownLevel;

        [CreateProperty] public List<LeaderboardEntry> entries => m_Entries;
        [CreateProperty] public int count => m_Entries.Count;
        [CreateProperty] public int lastAddedIndex => m_LastAddedIndex;

        // The level whose board is shown (its id).
        [CreateProperty]
        public string shownLevel
        {
            get => string.IsNullOrEmpty(m_ShownLevel) ? m_DefaultLevel : m_ShownLevel;
            set
            {
                if (m_ShownLevel == value)
                    return;
                m_ShownLevel = value;
                m_Owner?.Notify(shownLevelPath);
            }
        }

        public event Action<int> onEntryAdded;

        internal void Bind(PlayManagerState owner) => m_Owner = owner;

        // The first level's id: what an entry from before levels was played on.
        internal string defaultLevel
        {
            get => m_DefaultLevel;
            set => m_DefaultLevel = value;
        }

        public string LevelOf(LeaderboardEntry entry) => string.IsNullOrEmpty(entry.level) ? m_DefaultLevel : entry.level;

        // The entries played on a level, by their place in the whole list.
        public List<int> IndicesFor(string level)
        {
            var indices = new List<int>();
            for (int i = 0; i < m_Entries.Count; i++)
            {
                if (LevelOf(m_Entries[i]) == level)
                    indices.Add(i);
            }
            return indices;
        }

        public int Add(string playerName, int toppings, float time = 0f, string level = null)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                playerName = "Bagel";

            m_Entries.Add(new LeaderboardEntry { playerName = playerName.Trim(), toppings = toppings, time = time, level = string.IsNullOrEmpty(level) ? m_DefaultLevel : level });
            m_LastAddedIndex = m_Entries.Count - 1;
            m_Owner?.Notify(nameof(PlayManagerState.leaderboard));
            onEntryAdded?.Invoke(m_LastAddedIndex);
            return m_LastAddedIndex;
        }

        // A level of the player's that's gone: its board goes with it.
        public void RemoveLevel(string level)
        {
            if (string.IsNullOrEmpty(level) || m_Entries.RemoveAll(entry => LevelOf(entry) == level) == 0)
                return;
            m_LastAddedIndex = -1;
            m_Owner?.Notify(nameof(PlayManagerState.leaderboard));
        }

        // Settings › Game: a clean slate, every level's.
        public void Clear() => Load(null);

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
