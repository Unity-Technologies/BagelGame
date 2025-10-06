using System;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement]
    public partial class MainMenuPaneManager : VisualElement
    {
        public Button m_PlayButton;
        public Button m_SettingsButton;
        public Button m_LeaderboardButton;
        public Button m_ExitButton;

        public Button playButton => m_PlayButton ??= this.Q<Button>("play-button");
        public Button settingsButton => m_SettingsButton ??= this.Q<Button>("settings-button");
        public Button leaderboardButton => m_LeaderboardButton ??= this.Q<Button>("leaderboard-button");
        public Button exitButton => m_ExitButton ??= this.Q<Button>("exit-button");

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

        public void BindUI(Callbacks callbacks)
        {
            if (playButton != null && callbacks.onPlay != null)
                playButton.clicked += callbacks.onPlay;
            if (settingsButton != null && callbacks.onSettings != null )
                settingsButton.clicked += callbacks.onSettings;
            if (leaderboardButton != null && callbacks.onLeaderboard != null )
                leaderboardButton.clicked += callbacks.onLeaderboard;
            if (exitButton != null && callbacks.onExit != null )
                exitButton.clicked += callbacks.onExit;
        }
    }
}
