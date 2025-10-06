
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement]
    public partial class GameOverScreenManager : VisualElement
    {
        GameOverPaneManager m_GameOverPaneManager;
        LeaderboardManager m_LeaderboardManager;

        public GameOverPaneManager gameOverPaneManager => m_GameOverPaneManager ??= this.Q<GameOverPaneManager>();
        public LeaderboardManager leaderboardManager => m_LeaderboardManager ??= this.Q<LeaderboardManager>();

        public GameOverScreenManager()
        {
            RegisterCallbackOnce<GeometryChangedEvent>(FirstInit);
        }

        void FirstInit(GeometryChangedEvent evt)
        {
            gameOverPaneManager.BindUI(new GameOverPaneManager.Callbacks
            {
                onAddToLeaderboard = AddNameToLeaderboard
            });

            gameOverPaneManager.leaderboardForm.SetEnabled(true);
        }

        void AddNameToLeaderboard()
        {
            var leaderboardData = leaderboardManager.leaderboardData;
            if (leaderboardData == null)
                return;

            leaderboardManager.AddScore(
                gameOverPaneManager.addToLeaderboardNameField.value,
                gameOverPaneManager.toppingsNumberField.value);

            gameOverPaneManager.leaderboardForm.SetEnabled(false);
        }
    }
}
