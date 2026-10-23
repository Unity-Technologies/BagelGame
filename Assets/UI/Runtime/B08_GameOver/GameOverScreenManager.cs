using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class GameOverScreenManager : BagelPane
    {
        GameOverPaneManager m_GameOverPaneManager;
        LeaderboardManager m_LeaderboardManager;

        public GameOverPaneManager gameOverPaneManager => m_GameOverPaneManager ??= this.Q<GameOverPaneManager>();
        public LeaderboardManager leaderboardManager => m_LeaderboardManager ??= this.Q<LeaderboardManager>();

        bool m_Bound;

        protected override void OnAttached()
        {
            if (m_Bound)
                return;
            m_Bound = true;

            // After adding a score, show it: the leaderboard selects and scrolls to the new row.
            gameOverPaneManager.BindUI(new GameOverPaneManager.Callbacks
            {
                onAddToLeaderboard = () => leaderboardManager?.leaderboardTable?.Focus()
            });
        }
    }
}
