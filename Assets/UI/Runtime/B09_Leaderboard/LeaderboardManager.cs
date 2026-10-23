using System;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class LeaderboardManager : BagelPane
    {
        MultiColumnListView m_LeaderboardTable;

        public MultiColumnListView leaderboardTable
            => m_LeaderboardTable ??= this.Q<MultiColumnListView>("leaderboard-table");

        public LeaderboardData leaderboardData => state?.leaderboard;

        bool m_Bound;

        protected override void OnAttached()
        {
            if (m_Bound || leaderboardTable == null)
                return;
            m_Bound = true;

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
        }

        protected override void OnStateChanged(PlayManagerState oldState, PlayManagerState newState)
        {
            if (oldState != null)
                oldState.leaderboard.onEntryAdded -= Leaderboard_OnEntryAdded;
            if (newState != null)
                newState.leaderboard.onEntryAdded += Leaderboard_OnEntryAdded;

            if (leaderboardTable != null)
                leaderboardTable.itemsSource = newState?.leaderboard.entries;
        }

        void Leaderboard_OnEntryAdded(int dataIndex)
        {
            if (leaderboardTable == null)
                return;

            leaderboardTable.RefreshItems();

            var viewIndex = leaderboardTable.viewController.GetIndexForId(dataIndex);
            leaderboardTable.SetSelection(viewIndex);
            leaderboardTable.ScrollToItemById(dataIndex);
        }
    }
}
