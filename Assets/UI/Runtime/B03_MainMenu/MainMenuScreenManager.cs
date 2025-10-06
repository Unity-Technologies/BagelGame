using System;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement]
    public partial class MainMenuScreenManager : VisualElement
    {
        public static readonly string backgroundPaneClassName = "b-background-pane";

        public VisualElement m_MainMenuPane;
        public MainMenuPaneManager m_MainMenuPaneManager;
        public VisualElement m_SecondaryPanes;
        public VisualElement m_SettingsPane;
        public VisualElement m_LeaderboardPane;

        public Button m_SettingsBackButton;
        public Button m_LeaderboardBackButton;

        public VisualElement mainMenuPane => m_MainMenuPane ??= this.Q<VisualElement>("main-menu-pane");
        public MainMenuPaneManager mainMenuPaneManager => m_MainMenuPaneManager ??= this.Q<MainMenuPaneManager>("main-menu-pane-manager");
        public VisualElement secondaryPanes => m_SecondaryPanes ??= this.Q<VisualElement>("secondary-panes");
        public VisualElement settingsPane => m_SettingsPane ??= this.Q<VisualElement>("settings-pane");
        public VisualElement leaderboardPane => m_LeaderboardPane ??= this.Q<VisualElement>("leaderboard-pane");

        public Button settingsBackButton => m_SettingsBackButton ??= settingsPane.Q<Button>("back-button");
        public Button leaderboardBackButton => m_LeaderboardBackButton ??= leaderboardPane.Q<Button>("back-button");

        public struct Callbacks
        {
            public PlayManagerState playManagerState;

            public Action onSettingsBack;
            public Action onLeaderboardBack;
        }

        public void BindUI(Callbacks callbacks)
        {
            if (callbacks.playManagerState != null )
                SetPlayManagerState(callbacks.playManagerState);

            if (settingsBackButton != null && callbacks.onSettingsBack != null )
                settingsBackButton.clicked += callbacks.onSettingsBack;
            if (leaderboardBackButton != null && callbacks.onLeaderboardBack != null )
                leaderboardBackButton.clicked += callbacks.onLeaderboardBack;
        }

        PlayManagerState m_PlayManagerState;

        public MainMenuScreenManager()
        {
            RegisterCallbackOnce<GeometryChangedEvent>(FirstInit);
        }

        public void SetPlayManagerState(PlayManagerState playManagerState)
        {
            m_PlayManagerState = playManagerState;
        }

        void FirstInit(GeometryChangedEvent evt)
        {
            BindUI(new Callbacks
            {
                onSettingsBack = GoToMainMenuPane,
                onLeaderboardBack = GoToMainMenuPane
            } );

            mainMenuPaneManager.BindUI(new MainMenuPaneManager.Callbacks
            {
                onSettings = GoToSettingsPane,
                onLeaderboard = GoToLeaderboardPane,
            } );
        }

        public void GoToMainMenuPane()
        {
            mainMenuPane.SetEnabled(true);
            settingsPane.SetEnabled(false);
            leaderboardPane.SetEnabled(false);

            m_PlayManagerState?.SetMainMenuPaneMode(PlayManagerState.MainMenuPaneMode.Primary);
        }

        public void GoToSettingsPane()
        {
            mainMenuPane.SetEnabled(false);
            settingsPane.SetEnabled(true);
            leaderboardPane.SetEnabled(false);

            settingsPane.RemoveFromClassList(backgroundPaneClassName);
            leaderboardPane.AddToClassList(backgroundPaneClassName);

            settingsPane.BringToFront();

            m_PlayManagerState?.SetMainMenuPaneMode(PlayManagerState.MainMenuPaneMode.Secondary);
        }

        public void GoToLeaderboardPane()
        {
            mainMenuPane.SetEnabled(false);
            settingsPane.SetEnabled(false);
            leaderboardPane.SetEnabled(true);

            settingsPane.AddToClassList(backgroundPaneClassName);
            leaderboardPane.RemoveFromClassList(backgroundPaneClassName);

            leaderboardPane.BringToFront();

            m_PlayManagerState?.SetMainMenuPaneMode(PlayManagerState.MainMenuPaneMode.Secondary);
        }
    }
}
