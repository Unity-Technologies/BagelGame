using System;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement]
    public partial class LeaderboardManager : VisualElement
    {
        [UxmlAttribute]
        public LeaderboardData leaderboardData;

        MultiColumnListView m_Leaderboard;

        public MultiColumnListView Leaderboard
            => m_Leaderboard ??= this.Q<MultiColumnListView>("leaderboard-table");

        public LeaderboardManager()
        {
            RegisterCallbackOnce<GeometryChangedEvent>(FirstInit);
        }

        public void AddScore(string playerName, int toppings)
        {
            if (leaderboardData == null)
                return;
            if (Leaderboard == null)
                return;

            var newEntry = new LeaderboardData.LeaderboardEntry
            {
                playerName = playerName,
                toppings = toppings
            };

            var dataIndex = leaderboardData.Entries.Count;
            leaderboardData.Entries.Add(newEntry);

            Leaderboard.RefreshItems();

            var viewIndex = Leaderboard.viewController.GetIndexForId(dataIndex);
            Leaderboard.SetSelection(viewIndex);
            Leaderboard.ScrollToItemById(dataIndex);
            Leaderboard.Focus();
        }

        void FirstInit(GeometryChangedEvent evt)
        {
            if (Leaderboard == null)
                return;

            Leaderboard.columns["name"].makeCell = () => new Label();
            Leaderboard.columns["toppings"].makeCell = () => new Label();

            Leaderboard.columns["name"].bindCell = (VisualElement element, int index) =>
                (element as Label).text = leaderboardData.Entries[index].playerName;
            Leaderboard.columns["toppings"].bindCell = (VisualElement element, int index) =>
                (element as Label).text = leaderboardData.Entries[index].toppings.ToString();

            Leaderboard.columns["name"].comparison = (a, b) =>
                string.Compare(leaderboardData.Entries[a].playerName, leaderboardData.Entries[b].playerName, StringComparison.Ordinal);
            Leaderboard.columns["toppings"].comparison = (a, b) =>
                (leaderboardData.Entries[a].toppings).CompareTo(leaderboardData.Entries[b].toppings);

            Leaderboard.itemsSource = leaderboardData.Entries;
        }
    }
}
