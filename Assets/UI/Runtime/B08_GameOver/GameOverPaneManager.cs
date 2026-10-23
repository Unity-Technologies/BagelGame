using System;
using Unity.Properties;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class GameOverPaneManager : BagelPane
    {
        Label m_Title;
        IntegerField m_ToppingsNumberField;
        VisualElement m_LeaderboardForm;
        TextField m_AddToLeaderboardNameField;
        Button m_AddToLeaderboardButton;
        Button m_RestartButton;
        Button m_MainMenuButton;

        public Label title => m_Title ??= this.Q<Label>("page-title");
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

        bool m_Bound;

        protected override void OnAttached()
        {
            if (m_Bound)
                return;
            m_Bound = true;

            // The page title comes from the Page template, so its binding is added here.
            title?.SetBinding("text", new DataBinding
            {
                dataSourcePath = new PropertyPath("lastRun.title"),
                bindingMode = BindingMode.ToTarget
            });

            BindUI(new Callbacks
            {
                onAddToLeaderboard = () => state?.SubmitToLeaderboard(addToLeaderboardNameField.value),
                onRestart = () => state?.GoToPlay(),
                onMainMenu = () => state?.GoToMainMenu()
            });
        }

        protected override void OnStateChanged(PlayManagerState oldState, PlayManagerState newState) => UpdateLeaderboardForm();

        protected override void OnStatePropertyChanged(in BindingId property)
        {
            if (property == PlayManagerState.lastRunPath)
                UpdateLeaderboardForm();
        }

        void UpdateLeaderboardForm()
        {
            leaderboardForm?.SetEnabled(state != null && state.lastRun.canSubmit);
        }
    }
}
