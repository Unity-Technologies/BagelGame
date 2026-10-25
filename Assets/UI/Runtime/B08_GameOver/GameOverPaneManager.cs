using System;
using Unity.Localization;
using Unity.SmartStrings.PersistentVariables;
using UnityEngine.UIElements;

namespace Bagel
{
    // The last run's receipt: the result, what the bagel kept, its time, the leaderboard form, and
    // the result stamped across it (a UIAnimationClip slams the stamp down each time it shows). Before the
    // first run it only says there's been none yet.
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class GameOverPaneManager : BagelPane
    {
        public static readonly UniqueStyleString stampLostUssClassName = new("b-stamp--lost");
        public static readonly UniqueStyleString stampSlamUssClassName = new("b-stamp--slam");
        public static readonly UniqueStyleString emptyUssClassName = new("b-receipt--empty");

        VisualElement m_Receipt;
        Label m_Title;
        Label m_TimeLabel;
        Label m_Stamp;
        Label m_Summary;
        Label m_ToppingsKeptLabel;
        VisualElement m_LeaderboardForm;
        TextField m_AddToLeaderboardNameField;
        Button m_AddToLeaderboardButton;
        Button m_RestartButton;
        Button m_MainMenuButton;

        public VisualElement receipt => m_Receipt ??= this.Q<VisualElement>("receipt");
        public Label title => m_Title ??= this.Q<Label>("page-title");
        public Label summary => m_Summary ??= this.Q<Label>("summary-label");
        public Label timeLabel => m_TimeLabel ??= this.Q<Label>("time-label");
        public Label stamp => m_Stamp ??= this.Q<Label>("stamp");
        public Label toppingsKeptLabel => m_ToppingsKeptLabel ??= this.Q<Label>("toppings-kept-label");
        public VisualElement leaderboardForm => m_LeaderboardForm ??= this.Q<VisualElement>("leaderboard-form");
        public TextField addToLeaderboardNameField => m_AddToLeaderboardNameField ??= this.Q<TextField>("leaderboard-name-field");
        public Button addToLeaderboardButton => m_AddToLeaderboardButton ??= this.Q<Button>("add-to-leaderboard-button");
        public Button restartButton => m_RestartButton ??= this.Q<Button>("restart-button");
        public Button mainMenuButton => m_MainMenuButton ??= this.Q<Button>("main-menu-button");

        public struct Callbacks
        {
            public Action onAddToLeaderboard;
            public Action onRestart;
            public Action onMainMenu;
        }

        public void BindUI(Callbacks callbacks)
        {
            if (addToLeaderboardButton != null && callbacks.onAddToLeaderboard != null)
                addToLeaderboardButton.clicked += callbacks.onAddToLeaderboard;
            if (restartButton != null && callbacks.onRestart != null )
                restartButton.clicked += callbacks.onRestart;
            if (mainMenuButton != null && callbacks.onMainMenu != null )
                mainMenuButton.clicked += callbacks.onMainMenu;
        }

        // "You Win!" or "You Lose!", and a Smart String summary: "Your Sesame bagel kept 123 toppings!"
        readonly LocalizedString m_TitleString = BagelLocalization.String("gameover.win");
        readonly LocalizedString m_SummaryString = BagelLocalization.String("gameover.summary");
        readonly LocalizedString m_NoRunString = BagelLocalization.String("tv.no-run");
        readonly LocalizedString m_BagelName = BagelLocalization.String("bagel.sesame");
        readonly IntVariable m_Toppings = new IntVariable();
        // The count as the language writes it: Arabic picks its plural with {toppings} and shows {count}.
        readonly StringVariable m_Count = new StringVariable();
        readonly LocalizedString m_StampString = BagelLocalization.String("stamp.win");

        bool m_Bound;

        protected override void OnAttached()
        {
            if (m_Bound)
                return;
            m_Bound = true;

            m_SummaryString.LocalVariables.Add(m_BagelName, "bagel");
            m_SummaryString.LocalVariables.Add(m_Toppings, "toppings");
            m_SummaryString.LocalVariables.Add(m_Count, "count");

            // The page title comes from the Page template, so its binding is added here. The summary's comes with
            // the result: it's the run's, or the receipt saying there's been none.
            title?.SetBinding("text", m_TitleString);
            stamp?.SetBinding("text", m_StampString);

            BindUI(new Callbacks
            {
                onAddToLeaderboard = () => state?.SubmitToLeaderboard(addToLeaderboardNameField.value),
                onRestart = () => state?.GoToPlay(),
                onMainMenu = () => state?.GoToMainMenu()
            });
            UpdateResult();
        }

        protected override void OnStateChanged(PlayManagerState oldState, PlayManagerState newState) => UpdateResult();

        protected override void OnStatePropertyChanged(in BindingId property)
        {
            if (property == PlayManagerState.lastRunPath)
                UpdateResult();
        }

        void UpdateResult()
        {
            UpdateLeaderboardForm();

            if (state == null)
                return;

            var lastRun = state.lastRun;
            receipt?.EnableInClassList(emptyUssClassName, !lastRun.hasRun);
            if (m_Bound)
                summary?.SetBinding("text", lastRun.hasRun ? m_SummaryString : m_NoRunString);
            m_TitleString.SetReference(BagelLocalization.table, lastRun.hasRun ? lastRun.titleKey : "tv.last-run");
            if (!string.IsNullOrEmpty(lastRun.bagelNameKey))
                m_BagelName.SetReference(BagelLocalization.table, lastRun.bagelNameKey);
            m_Toppings.Value = lastRun.toppings;

            UpdateReceipt();
            SlamStamp();
        }

        protected override void OnLocaleChanged() => UpdateReceipt();

        void UpdateReceipt()
        {
            if (state == null)
                return;

            var lastRun = state.lastRun;
            toppingsKeptLabel?.SetText(BagelConverters.FormatNumber(lastRun.toppings));
            timeLabel?.SetText(BagelConverters.FormatTime(lastRun.time));

            // Changing a variable doesn't re-render the strings that use it.
            m_Count.Value = BagelConverters.FormatNumber(lastRun.toppings);
            m_SummaryString.RefreshString();

            m_StampString.SetReference(BagelLocalization.table, lastRun.won ? "stamp.win" : "stamp.lose");
            stamp?.EnableInClassList(stampLostUssClassName, !lastRun.won);
        }

        // A one-shot clip plays when its class is added: take it off, and put it back a frame later.
        void SlamStamp()
        {
            if (stamp == null || state == null || !state.isGameOver || !state.lastRun.hasRun)
                return;

            stamp.RemoveFromClassList(stampSlamUssClassName);
            stamp.schedule.Execute(() => stamp.AddToClassList(stampSlamUssClassName)).ExecuteLater(16);
        }

        void UpdateLeaderboardForm()
        {
            leaderboardForm?.SetEnabled(state != null && state.lastRun.canSubmit);
        }
    }
}
