using System;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement]
    public partial class MainMenuScreenManager : VisualElement
    {
        public static readonly string backgroundPaneClassName = "b-background-pane";

        public struct Elements
        {
            public MainMenuScreenManager manager;

            public VisualElement mainMenuPane;
            public VisualElement secondaryPanes;
            public VisualElement settingsPane;
            public VisualElement leaderboardPane;

            public Button playButton;
            public Button settingsButton;
            public Button leaderboardButton;
            public Button exitButton;

            public Button settingsBackButton;
            public Button leaderboardBackButton;
        }

        public struct Callbacks
        {
            public PlayManagerState playManagerState;

            public Action onPlay;
            public Action onSettings;
            public Action onLeaderboard;
            public Action onExit;

            public Action onSettingsBack;
            public Action onLeaderboardBack;
        }

        public static Elements BindUI(VisualElement root, Callbacks callbacks)
        {
            var elements = new Elements
            {
                manager = root.Q<MainMenuScreenManager>(),
                
                mainMenuPane = root.Q<VisualElement>("main-menu-pane"),
                secondaryPanes = root.Q<VisualElement>("secondary-panes"),
                settingsPane = root.Q<VisualElement>("settings-pane"),
                leaderboardPane = root.Q<VisualElement>("leaderboard-pane"),

                playButton = root.Q<Button>("play-button"),
                settingsButton = root.Q<Button>("settings-button"),
                leaderboardButton = root.Q<Button>("leaderboard-button"),
                exitButton = root.Q<Button>("exit-button"),

                settingsBackButton = null,
                leaderboardBackButton = null
            };

            if (elements.mainMenuPane != null)
                elements.settingsBackButton = elements.settingsPane.Q<Button>("back-button");
            if (elements.leaderboardPane != null )
                elements.leaderboardBackButton = elements.leaderboardPane.Q<Button>("back-button");

            if (elements.manager != null && callbacks.playManagerState != null)
                elements.manager.SetPlayManagerState(callbacks.playManagerState);

            if (elements.playButton != null && callbacks.onPlay != null)
                elements.playButton.clicked += callbacks.onPlay;
            if (elements.settingsButton != null && callbacks.onSettings != null )
                elements.settingsButton.clicked += callbacks.onSettings;
            if (elements.leaderboardButton != null && callbacks.onLeaderboard != null )
                elements.leaderboardButton.clicked += callbacks.onLeaderboard;
            if (elements.exitButton != null && callbacks.onExit != null )
                elements.exitButton.clicked += callbacks.onExit;

            if (elements.settingsBackButton != null && callbacks.onSettingsBack != null )
                elements.settingsBackButton.clicked += callbacks.onSettingsBack;
            if (elements.leaderboardBackButton != null && callbacks.onLeaderboardBack != null )
                elements.leaderboardBackButton.clicked += callbacks.onLeaderboardBack;

            return elements;
        }

        Elements m_Elements;
        PlayManagerState m_PlayManagerState;

        public Elements UI => m_Elements;

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
            m_Elements = BindUI(this, new Callbacks
            {
                onSettings = GoToSettingsPane,
                onLeaderboard = GoToLeaderboardPane,
                onSettingsBack = GoToMainMenuPane,
                onLeaderboardBack = GoToMainMenuPane
            } );
        }

        public void GoToMainMenuPane()
        {
            m_Elements.mainMenuPane.SetEnabled(true);
            m_Elements.settingsPane.SetEnabled(false);
            m_Elements.leaderboardPane.SetEnabled(false);

            m_PlayManagerState?.SetMainMenuPaneMode(PlayManagerState.MainMenuPaneMode.Primary);
        }

        public void GoToSettingsPane()
        {
            m_Elements.mainMenuPane.SetEnabled(false);
            m_Elements.settingsPane.SetEnabled(true);
            m_Elements.leaderboardPane.SetEnabled(false);

            m_Elements.settingsPane.RemoveFromClassList(backgroundPaneClassName);
            m_Elements.leaderboardPane.AddToClassList(backgroundPaneClassName);

            m_Elements.settingsPane.BringToFront();

            m_PlayManagerState?.SetMainMenuPaneMode(PlayManagerState.MainMenuPaneMode.Secondary);
        }

        public void GoToLeaderboardPane()
        {
            m_Elements.mainMenuPane.SetEnabled(false);
            m_Elements.settingsPane.SetEnabled(false);
            m_Elements.leaderboardPane.SetEnabled(true);

            m_Elements.settingsPane.AddToClassList(backgroundPaneClassName);
            m_Elements.leaderboardPane.RemoveFromClassList(backgroundPaneClassName);

            m_Elements.leaderboardPane.BringToFront();

            m_PlayManagerState?.SetMainMenuPaneMode(PlayManagerState.MainMenuPaneMode.Secondary);
        }
    }
}
