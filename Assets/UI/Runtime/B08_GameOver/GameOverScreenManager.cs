using UnityEngine.UIElements;

namespace Bagel
{
    // The television on the bakery's far wall: the last run's receipt and the leaderboard, all the time. At
    // game over the receipt has its buttons and the leaderboard's form; opened from the main menu, the
    // leaderboard has its Back button. The rest of the time it only shows.
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class GameOverScreenManager : BagelPane
    {
        public static readonly UniqueStyleString gameOverUssClassName = new("b-tv--game-over");
        public static readonly UniqueStyleString leaderboardUssClassName = new("b-tv--leaderboard");

        GameOverPaneManager m_GameOverPaneManager;
        LeaderboardManager m_LeaderboardManager;
        Button m_BackButton;

        public GameOverPaneManager gameOverPaneManager => m_GameOverPaneManager ??= this.Q<GameOverPaneManager>();
        public LeaderboardManager leaderboardManager => m_LeaderboardManager ??= this.Q<LeaderboardManager>();
        public Button backButton => m_BackButton ??= leaderboardManager?.Q<Button>("back-button");

        // The main menu's Leaderboard button brings the camera here.
        public bool isLeaderboardOpen => state != null && state.isMainMenu && state.mainMenuPane == PlayManagerState.MainMenuPane.Leaderboard;

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

            // Back, Escape or the gamepad's back button: the camera goes back to the blackboard.
            if (backButton != null)
                backButton.clicked += GoBackToTheMainMenu;
            RegisterCallback<NavigationCancelEvent>(OnNavigationCancelEvent);
            UpdateMode();
        }

        void OnNavigationCancelEvent(NavigationCancelEvent evt)
        {
            if (!isLeaderboardOpen)
                return;

            GoBackToTheMainMenu();
            evt.StopPropagation();
        }

        void GoBackToTheMainMenu() => state?.SetMainMenuPane(PlayManagerState.MainMenuPane.Main);

        protected override void OnStateChanged(PlayManagerState oldState, PlayManagerState newState) => UpdateMode();

        protected override void OnStatePropertyChanged(in BindingId property)
        {
            if (property == PlayManagerState.currentStatePath || property == PlayManagerState.mainMenuPanePath)
                UpdateMode();
        }

        void UpdateMode()
        {
            EnableInClassList(gameOverUssClassName, state != null && state.isGameOver);
            EnableInClassList(leaderboardUssClassName, isLeaderboardOpen);
        }
    }
}
