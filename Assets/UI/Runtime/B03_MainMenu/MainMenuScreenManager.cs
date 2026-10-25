using UnityEngine.UIElements;

namespace Bagel
{
    // The blackboard: the main menu, and Settings beside it. The leaderboard is on the television (the game
    // over screen): its button sends the camera there, and this menu smudges until it comes back.
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class MainMenuScreenManager : BagelPane
    {
        VisualElement m_MainMenuPane;
        MainMenuPaneManager m_MainMenuPaneManager;
        VisualElement m_SecondaryPanes;
        VisualElement m_SettingsPane;

        Button m_SettingsBackButton;

        public VisualElement mainMenuPane => m_MainMenuPane ??= this.Q<VisualElement>("main-menu-pane");
        public MainMenuPaneManager mainMenuPaneManager => m_MainMenuPaneManager ??= this.Q<MainMenuPaneManager>("main-menu-pane-manager");
        public VisualElement secondaryPanes => m_SecondaryPanes ??= this.Q<VisualElement>("secondary-panes");
        public VisualElement settingsPane => m_SettingsPane ??= this.Q<VisualElement>("settings-pane");

        public Button settingsBackButton => m_SettingsBackButton ??= settingsPane.Q<Button>("back-button");

        bool m_Bound;

        protected override void OnAttached()
        {
            if (m_Bound)
                return;
            m_Bound = true;

            settingsBackButton.clicked += GoToMainMenuPane;

            // Escape or the gamepad's back button leaves Settings (the television handles its own).
            RegisterCallback<NavigationCancelEvent>(OnNavigationCancelEvent);
        }

        void OnNavigationCancelEvent(NavigationCancelEvent evt)
        {
            if (state == null || state.mainMenuPane == PlayManagerState.MainMenuPane.Main)
                return;

            GoToMainMenuPane();
            evt.StopPropagation();
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
        }
    }
}
