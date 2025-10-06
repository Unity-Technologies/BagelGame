using System;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement]
    public partial class LeaderboardManager : VisualElement
    {
        [UxmlAttribute]
        public LeaderboardData leaderboardData;

        MultiColumnListView m_LeaderboardTable;

        public MultiColumnListView leaderboardTable
            => m_LeaderboardTable ??= this.Q<MultiColumnListView>("leaderboard-table");

        public LeaderboardManager()
        {
            RegisterCallbackOnce<GeometryChangedEvent>(FirstInit);
        }

        public void AddScore(string playerName, int toppings)
        {
            if (leaderboardData == null)
                return;
            if (leaderboardTable == null)
                return;

            var newEntry = new LeaderboardData.LeaderboardEntry
            {
                playerName = playerName,
                toppings = toppings
            };

            var dataIndex = leaderboardData.entries.Count;
            leaderboardData.entries.Add(newEntry);

            leaderboardTable.RefreshItems();

            var viewIndex = leaderboardTable.viewController.GetIndexForId(dataIndex);
            leaderboardTable.SetSelection(viewIndex);
            leaderboardTable.ScrollToItemById(dataIndex);
            leaderboardTable.Focus();
        }

        void FirstInit(GeometryChangedEvent evt)
        {
            if (leaderboardTable == null)
                return;

            leaderboardTable.columns["name"].makeCell = () => new Label();
            leaderboardTable.columns["toppings"].makeCell = () => new Label();

            leaderboardTable.columns["name"].bindCell = (VisualElement element, int index) =>
                (element as Label).text = leaderboardData.entries[index].playerName;
            leaderboardTable.columns["toppings"].bindCell = (VisualElement element, int index) =>
                (element as Label).text = leaderboardData.entries[index].toppings.ToString();

            leaderboardTable.columns["name"].comparison = (a, b) =>
                string.Compare(leaderboardData.entries[a].playerName, leaderboardData.entries[b].playerName, StringComparison.Ordinal);
            leaderboardTable.columns["toppings"].comparison = (a, b) =>
                (leaderboardData.entries[a].toppings).CompareTo(leaderboardData.entries[b].toppings);

            leaderboardTable.itemsSource = leaderboardData.entries;
        }
    }
}
