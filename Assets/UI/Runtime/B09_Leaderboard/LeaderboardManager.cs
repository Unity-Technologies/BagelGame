using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Bagel
{
    // One level's leaderboard: its name, < and > for the boards of the levels before and after it, and its runs, the
    // most toppings first. It's the level just played at game over, and the chosen one from the main menu.
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class LeaderboardManager : BagelPane
    {
        public static readonly string emptyUssClassName = "b-leaderboard__empty";

        MultiColumnListView m_LeaderboardTable;
        Label m_LevelName;
        Button m_PreviousLevelButton;
        Button m_NextLevelButton;

        // The shown level's entries, and where each is in the whole leaderboard.
        readonly List<LeaderboardData.LeaderboardEntry> m_Shown = new();
        readonly List<int> m_ShownIndices = new();

        public MultiColumnListView leaderboardTable
            => m_LeaderboardTable ??= this.Q<MultiColumnListView>("leaderboard-table");
        public Label levelName => m_LevelName ??= this.Q<Label>("level-name");
        public Button previousLevelButton => m_PreviousLevelButton ??= this.Q<Button>("previous-level-button");
        public Button nextLevelButton => m_NextLevelButton ??= this.Q<Button>("next-level-button");

        public LeaderboardData leaderboardData => state?.leaderboard;
        public IReadOnlyList<LeaderboardData.LeaderboardEntry> shownEntries => m_Shown;

        // Where an entry of the whole leaderboard is in the shown board; -1 if it's another level's.
        public int ShownIndexOf(int dataIndex) => m_ShownIndices.IndexOf(dataIndex);

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
            if (previousLevelButton != null)
                previousLevelButton.clicked += () => state?.ShowLeaderboardLevel(-1);
            if (nextLevelButton != null)
                nextLevelButton.clicked += () => state?.ShowLeaderboardLevel(1);
            UpdateLevelName();
        }

        // Before the list gets its entries: it sorts them with the columns' comparisons.
        void SetUpColumns()
        {
            if (m_ColumnsReady || leaderboardTable == null)
                return;
            m_ColumnsReady = true;

            // A level nobody's played yet says so, in the player's language.
            leaderboardTable.makeNoneElement = () =>
            {
                var none = new Label(BagelLocalization.Get("leaderboard.empty", "Nobody's played this table yet")) { name = "empty-label" };
                none.AddToClassList(emptyUssClassName);
                return none;
            };
            leaderboardTable.columns["name"].makeCell = () => new Label();
            leaderboardTable.columns["toppings"].makeCell = () => new Label();
            leaderboardTable.columns["time"].makeCell = () => new Label();

            leaderboardTable.columns["name"].bindCell = (VisualElement element, int index) =>
                (element as Label).text = m_Shown[index].playerName;
            leaderboardTable.columns["toppings"].bindCell = (VisualElement element, int index) =>
                (element as Label).text = BagelConverters.FormatNumber(m_Shown[index].toppings);
            leaderboardTable.columns["time"].bindCell = (VisualElement element, int index) =>
                (element as Label).text = BagelConverters.FormatTime(m_Shown[index].time);

            leaderboardTable.columns["name"].comparison = (a, b) =>
                string.Compare(m_Shown[a].playerName, m_Shown[b].playerName, StringComparison.Ordinal);
            // The most toppings first and, between runs that kept as many, the faster one. Sorted by time, the
            // fastest first and, between runs as fast, the one that kept more.
            leaderboardTable.columns["toppings"].comparison = (a, b) =>
            {
                var order = m_Shown[a].toppings.CompareTo(m_Shown[b].toppings);
                return order != 0 ? order : m_Shown[b].time.CompareTo(m_Shown[a].time);
            };
            leaderboardTable.columns["time"].comparison = (a, b) =>
            {
                var order = m_Shown[a].time.CompareTo(m_Shown[b].time);
                return order != 0 ? order : m_Shown[b].toppings.CompareTo(m_Shown[a].toppings);
            };
        }

        // Columns aren't elements, so their titles can't be bound: set them for each language. The counts are
        // written in the language's digits: the rows are bound again.
        protected override void OnLocaleChanged()
        {
            UpdateColumnTitles();
            UpdateLevelName();
            if (leaderboardTable?.Q<Label>("empty-label") is { } empty)
                empty.text = BagelLocalization.Get("leaderboard.empty", "Nobody's played this table yet");
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
            // The list makes its rows as it gets its entries.
            UpdateBoard(false);
            leaderboardTable.itemsSource = m_Shown;
        }

        protected override void OnStatePropertyChanged(in BindingId property)
        {
            // A run added or cleared, another level's board, the levels themselves.
            if (property == PlayManagerState.leaderboardPath || property == LeaderboardData.shownLevelPath || property == PlayManagerState.levelsPath)
                UpdateBoard();
        }

        // The shown level's runs, and its name. The rows are made again only once the list has made them: asked to
        // more than once before the panel lays it out, the list leaves a row's text behind.
        void UpdateBoard(bool refresh = true)
        {
            m_Shown.Clear();
            m_ShownIndices.Clear();
            var data = leaderboardData;
            if (data != null)
            {
                foreach (var index in data.IndicesFor(data.shownLevel))
                {
                    m_Shown.Add(data.entries[index]);
                    m_ShownIndices.Add(index);
                }
            }
            if (refresh && leaderboardTable != null && leaderboardTable.itemsSource == m_Shown && leaderboardTable.panel != null)
                leaderboardTable.RefreshItems();
            UpdateLevelName();
        }

        void UpdateLevelName()
        {
            var level = state?.leaderboardLevel;
            if (levelName != null)
                levelName.text = LevelSelectionScreenManager.LevelName(level);
            previousLevelButton?.SetEnabled(state != null && state.canShowPreviousLeaderboardLevel);
            nextLevelButton?.SetEnabled(state != null && state.canShowNextLeaderboardLevel);
        }


        void Leaderboard_OnEntryAdded(int dataIndex)
        {
            if (leaderboardTable == null)
                return;

            UpdateBoard();
            var id = ShownIndexOf(dataIndex);
            if (id < 0)
                return;

            var viewIndex = leaderboardTable.viewController.GetIndexForId(id);
            leaderboardTable.SetSelection(viewIndex);
            leaderboardTable.ScrollToItemById(id);
        }
    }
}
