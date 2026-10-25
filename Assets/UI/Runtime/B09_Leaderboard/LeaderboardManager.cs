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
        bool m_ColumnsReady;

        protected override void OnAttached()
        {
            if (m_Bound || leaderboardTable == null)
                return;
            m_Bound = true;

            // The page title comes from the Page template, so its binding is added here.
            BagelLocalization.Bind(this.Q<Label>("page-title"), "text", "leaderboard.title");
            UpdateColumnTitles();
            SetUpColumns();
        }

        // Before the list gets its entries: it sorts them with the columns' comparisons.
        void SetUpColumns()
        {
            if (m_ColumnsReady || leaderboardTable == null)
                return;
            m_ColumnsReady = true;

            leaderboardTable.columns["name"].makeCell = () => new Label();
            leaderboardTable.columns["toppings"].makeCell = () => new Label();
            leaderboardTable.columns["time"].makeCell = () => new Label();

            leaderboardTable.columns["name"].bindCell = (VisualElement element, int index) =>
                (element as Label).text = leaderboardData.entries[index].playerName;
            leaderboardTable.columns["toppings"].bindCell = (VisualElement element, int index) =>
                (element as Label).text = BagelConverters.FormatNumber(leaderboardData.entries[index].toppings);
            leaderboardTable.columns["time"].bindCell = (VisualElement element, int index) =>
                (element as Label).text = BagelConverters.FormatTime(leaderboardData.entries[index].time);

            leaderboardTable.columns["name"].comparison = (a, b) =>
                string.Compare(leaderboardData.entries[a].playerName, leaderboardData.entries[b].playerName, StringComparison.Ordinal);
            // The most toppings first and, between runs that kept as many, the faster one. Sorted by time, the
            // fastest first and, between runs as fast, the one that kept more.
            leaderboardTable.columns["toppings"].comparison = (a, b) =>
            {
                var order = leaderboardData.entries[a].toppings.CompareTo(leaderboardData.entries[b].toppings);
                return order != 0 ? order : leaderboardData.entries[b].time.CompareTo(leaderboardData.entries[a].time);
            };
            leaderboardTable.columns["time"].comparison = (a, b) =>
            {
                var order = leaderboardData.entries[a].time.CompareTo(leaderboardData.entries[b].time);
                return order != 0 ? order : leaderboardData.entries[b].toppings.CompareTo(leaderboardData.entries[a].toppings);
            };
        }

        // Columns aren't elements, so their titles can't be bound: set them for each language. The counts are
        // written in the language's digits: the rows are bound again.
        protected override void OnLocaleChanged()
        {
            UpdateColumnTitles();
            leaderboardTable?.RefreshItems();
        }

        void UpdateColumnTitles()
        {
            if (leaderboardTable == null)
                return;

            leaderboardTable.columns["name"].title = BagelLocalization.Get("leaderboard.name", "Name");
            leaderboardTable.columns["toppings"].title = BagelLocalization.Get("leaderboard.toppings", "Toppings Left");
            leaderboardTable.columns["time"].title = BagelLocalization.Get("receipt.time", "Time");
        }

        protected override void OnStateChanged(PlayManagerState oldState, PlayManagerState newState)
        {
            if (oldState != null)
                oldState.leaderboard.onEntryAdded -= Leaderboard_OnEntryAdded;
            if (newState != null)
                newState.leaderboard.onEntryAdded += Leaderboard_OnEntryAdded;

            if (leaderboardTable == null)
                return;

            SetUpColumns();
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
