using System;
using Unity.Localization;
using Unity.SmartStrings.PersistentVariables;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class GameOverPaneManager : BagelPane
    {
        Label m_Title;
        Label m_Summary;
        IntegerField m_ToppingsNumberField;
        VisualElement m_LeaderboardForm;
        TextField m_AddToLeaderboardNameField;
        Button m_AddToLeaderboardButton;
        Button m_RestartButton;
        Button m_MainMenuButton;

        public Label title => m_Title ??= this.Q<Label>("page-title");
        public Label summary => m_Summary ??= this.Q<Label>("summary-label");
        public IntegerField toppingsNumberField => m_ToppingsNumberField ??= this.Q<IntegerField>("toppings-number-field");
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
        readonly LocalizedString m_BagelName = BagelLocalization.String("bagel.sesame");
        readonly IntVariable m_Toppings = new IntVariable();

        bool m_Bound;

        protected override void OnAttached()
        {
            if (m_Bound)
                return;
            m_Bound = true;

            m_SummaryString.LocalVariables.Add(m_BagelName, "bagel");
            m_SummaryString.LocalVariables.Add(m_Toppings, "toppings");

            // The page title comes from the Page template, so its binding is added here.
            title?.SetBinding("text", m_TitleString);
            summary?.SetBinding("text", m_SummaryString);

            BindUI(new Callbacks
            {
                onAddToLeaderboard = () => state?.SubmitToLeaderboard(addToLeaderboardNameField.value),
                onRestart = () => state?.GoToPlay(),
                onMainMenu = () => state?.GoToMainMenu()
            });
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
            m_TitleString.SetReference(BagelLocalization.table, lastRun.titleKey);
            if (!string.IsNullOrEmpty(lastRun.bagelNameKey))
                m_BagelName.SetReference(BagelLocalization.table, lastRun.bagelNameKey);
            m_Toppings.Value = lastRun.toppings;

            // Changing a variable doesn't re-render the strings that use it.
            m_SummaryString.RefreshString();
        }

        void UpdateLeaderboardForm()
        {
            leaderboardForm?.SetEnabled(state != null && state.lastRun.canSubmit);
        }
    }
}
