using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class MainMenuScreenManager : BagelPane
    {
        public static readonly UniqueStyleString backgroundPaneClassName = new UniqueStyleString("b-background-pane");

        VisualElement m_MainMenuPane;
        MainMenuPaneManager m_MainMenuPaneManager;
        VisualElement m_SecondaryPanes;
        VisualElement m_SettingsPane;
        VisualElement m_LeaderboardPane;

        Button m_SettingsBackButton;
        Button m_LeaderboardBackButton;

        public VisualElement mainMenuPane => m_MainMenuPane ??= this.Q<VisualElement>("main-menu-pane");
        public MainMenuPaneManager mainMenuPaneManager => m_MainMenuPaneManager ??= this.Q<MainMenuPaneManager>("main-menu-pane-manager");
        public VisualElement secondaryPanes => m_SecondaryPanes ??= this.Q<VisualElement>("secondary-panes");
        public VisualElement settingsPane => m_SettingsPane ??= this.Q<VisualElement>("settings-pane");
        public VisualElement leaderboardPane => m_LeaderboardPane ??= this.Q<VisualElement>("leaderboard-pane");

        public Button settingsBackButton => m_SettingsBackButton ??= settingsPane.Q<Button>("back-button");
        public Button leaderboardBackButton => m_LeaderboardBackButton ??= leaderboardPane.Q<Button>("back-button");

        bool m_Bound;

        protected override void OnAttached()
        {
            if (m_Bound)
                return;
            m_Bound = true;

            settingsBackButton.clicked += GoToMainMenuPane;
            leaderboardBackButton.clicked += GoToMainMenuPane;
        }

        protected override void OnStateChanged(PlayManagerState oldState, PlayManagerState newState)
        {
            ShowPane(newState != null ? newState.mainMenuPane : PlayManagerState.MainMenuPane.Main);
        }

        protected override void OnStatePropertyChanged(in BindingId property)
        {
            if (property == PlayManagerState.mainMenuPanePath)
                ShowPane(state.mainMenuPane);
        }

        public void GoToMainMenuPane() => state?.SetMainMenuPane(PlayManagerState.MainMenuPane.Main);

        public void GoToSettingsPane() => state?.SetMainMenuPane(PlayManagerState.MainMenuPane.Settings);

        public void GoToLeaderboardPane() => state?.SetMainMenuPane(PlayManagerState.MainMenuPane.Leaderboard);

        void ShowPane(PlayManagerState.MainMenuPane pane)
        {
            mainMenuPane.SetEnabled(pane == PlayManagerState.MainMenuPane.Main);
            settingsPane.SetEnabled(pane == PlayManagerState.MainMenuPane.Settings);
            leaderboardPane.SetEnabled(pane == PlayManagerState.MainMenuPane.Leaderboard);

            // The last secondary pane shown stays in front, behind the main menu.
            if (pane == PlayManagerState.MainMenuPane.Settings)
            {
                settingsPane.RemoveFromClassList(backgroundPaneClassName);
                leaderboardPane.AddToClassList(backgroundPaneClassName);
                settingsPane.BringToFront();
            }
            else if (pane == PlayManagerState.MainMenuPane.Leaderboard)
            {
                settingsPane.AddToClassList(backgroundPaneClassName);
                leaderboardPane.RemoveFromClassList(backgroundPaneClassName);
                leaderboardPane.BringToFront();
            }
        }
    }
}
