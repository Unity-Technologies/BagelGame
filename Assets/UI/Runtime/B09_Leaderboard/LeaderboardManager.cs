using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement]
    public partial class LeaderboardManager : VisualElement
    {
        LeaderboardData m_LeaderboardData;

        public LeaderboardData LeaderboardData => m_LeaderboardData;

        public struct Elements
        {
            public MultiColumnListView leaderboard;
        }

        public struct Callbacks
        {
            
        }

        Elements m_Elements;

        public static Elements BindUI(VisualElement root, Callbacks callbacks)
        {
            var elements = new Elements
            {
                leaderboard = root.Q<MultiColumnListView>("leaderboard-view")
            };

            return elements;
        }

        public LeaderboardManager()
        {
            RegisterCallbackOnce<GeometryChangedEvent>(FirstInit);
        }

        void FirstInit(GeometryChangedEvent evt)
        {
            m_Elements = BindUI(this, new Callbacks
            {
            });

            m_LeaderboardData = dataSource as LeaderboardData;
            if (m_LeaderboardData == null)
                return;

            m_Elements.leaderboard.itemsSource = m_LeaderboardData.Entries;
        }
    }
}
