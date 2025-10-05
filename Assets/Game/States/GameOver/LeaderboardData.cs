using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bagel
{
    [CreateAssetMenu(fileName = "Leaderboard", menuName = "Bagel/Leaderboard Data")]
    public class LeaderboardData : ScriptableObject
    {
        [Serializable]
        public struct LeaderboardEntry
        {
            public string playerName;
            public int toppings;
        }

        public List<LeaderboardEntry> Entries = new List<LeaderboardEntry>();
    }
}
