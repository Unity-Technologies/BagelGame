using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B08_GameOver
{
    public class GameOverTests : UITestFixture
    {
        BagelTestAssetList m_BagelTestAssetList;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_BagelTestAssetList = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
        }

        [TearDown]
        public void TearDown()
        {
            rootVisualElement.Clear();
        }

        // Opened on its own, the pane plays with a copy of the start state: finish a run on it.
        GameOverPaneManager ShowGameOver(int toppingsLeft)
        {
            m_BagelTestAssetList.gameOverPaneUxml.CloneTree(rootVisualElement);
            var gameOverPaneManager = rootVisualElement.Q<GameOverPaneManager>();
            var state = gameOverPaneManager.state;
            state.tracker.Reset(1000);
            state.tracker.toppingsCount = toppingsLeft;
            state.FinishRun();
            simulate.FrameUpdate();
            return gameOverPaneManager;
        }

        [Test]
        public void AddNameToLeaderboardField()
        {
            // Load the UXML.
            var gameOverPaneManager = ShowGameOver(42);

            // Check initial state.
            Assert.AreEqual("", gameOverPaneManager.addToLeaderboardNameField.value);

            // Simulate typing a name.
            gameOverPaneManager.addToLeaderboardNameField.Focus();
            simulate.FrameUpdate();
            simulate.TypingText("Player1");
            simulate.FrameUpdate();

            // Verify the name was entered.
            Assert.AreEqual("Player1", gameOverPaneManager.addToLeaderboardNameField.value);
        }

        // The receipt says which level it was, and the television shows that level's board.
        [Test]
        public void TheReceiptSaysWhichLevel()
        {
            m_BagelTestAssetList.gameOverPaneUxml.CloneTree(rootVisualElement);
            var gameOverPaneManager = rootVisualElement.Q<GameOverPaneManager>();
            var state = gameOverPaneManager.state;
            state.GoToLevelSelection();
            state.SelectLevel(2);
            state.GoToPlay();
            state.tracker.toppingsCount = 10;
            state.FinishRun();
            simulate.FrameUpdate();

            Assert.AreEqual("Banquet", gameOverPaneManager.levelLabel.text);
            Assert.AreEqual("banquet", state.lastRun.levelId);
            Assert.AreEqual("banquet", state.leaderboard.shownLevel);
        }

        [Test]
        public void WinShowsToppingsLeft()
        {
            var gameOverPaneManager = ShowGameOver(42);

            Assert.AreEqual("You Win!", gameOverPaneManager.title.text);
            Assert.AreEqual("42", gameOverPaneManager.toppingsKeptLabel.text);
            Assert.IsTrue(gameOverPaneManager.leaderboardForm.enabledInHierarchy);
        }

        [Test]
        public void LoseDisablesLeaderboardForm()
        {
            var gameOverPaneManager = ShowGameOver(0);

            Assert.AreEqual("You Lose!", gameOverPaneManager.title.text);
            Assert.AreEqual("0", gameOverPaneManager.toppingsKeptLabel.text);
            Assert.IsFalse(gameOverPaneManager.leaderboardForm.enabledInHierarchy);
        }

        [Test]
        public void BeforeTheFirstRunTheReceiptSaysThereIsNone()
        {
            m_BagelTestAssetList.gameOverPaneUxml.CloneTree(rootVisualElement);
            var gameOverPaneManager = rootVisualElement.Q<GameOverPaneManager>();
            simulate.FrameUpdate();

            Assert.IsFalse(gameOverPaneManager.state.lastRun.hasRun);
            Assert.AreEqual("Last Run", gameOverPaneManager.title.text);
            Assert.AreEqual("No bagel toasted yet. Play a game!", gameOverPaneManager.summary.text);
            Assert.AreEqual(DisplayStyle.None, gameOverPaneManager.stamp.resolvedStyle.display);

            gameOverPaneManager.state.tracker.Reset(1000);
            gameOverPaneManager.state.tracker.toppingsCount = 42;
            gameOverPaneManager.state.FinishRun();
            simulate.FrameUpdate();
            Assert.AreEqual("You Win!", gameOverPaneManager.title.text);
            Assert.AreEqual(DisplayStyle.Flex, gameOverPaneManager.stamp.resolvedStyle.display);
        }

        // The television always shows the last run and the leaderboard; its buttons are only there at game over,
        // and the leaderboard's Back only when the main menu opened it.
        [Test]
        public void TheTelevisionsButtonsShowOnlyWhenItsInUse()
        {
            m_BagelTestAssetList.gameOverScreenUxml.CloneTree(rootVisualElement);
            var screen = rootVisualElement.Q<GameOverScreenManager>();
            var state = screen.state;
            var buttons = screen.Q("bottom-container");
            state.GoToMainMenu();
            simulate.FrameUpdate();
            Assert.AreEqual(DisplayStyle.None, buttons.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, screen.backButton.resolvedStyle.display);

            state.SetMainMenuPane(PlayManagerState.MainMenuPane.Leaderboard);
            simulate.FrameUpdate();
            Assert.IsTrue(screen.isLeaderboardOpen);
            Assert.AreEqual(DisplayStyle.None, buttons.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.Flex, screen.backButton.resolvedStyle.display);

            state.GoToBagelSelection();
            state.GoToPlay();
            state.FinishRun();
            simulate.FrameUpdate();
            Assert.AreEqual(DisplayStyle.Flex, buttons.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, screen.backButton.resolvedStyle.display);
        }

        [Test]
        public void BackOrCancelLeavesTheLeaderboardForTheMainMenu()
        {
            panelSize = new Vector2(1200f, 800f);
            m_BagelTestAssetList.gameOverScreenUxml.CloneTree(rootVisualElement);
            var screen = rootVisualElement.Q<GameOverScreenManager>();
            var state = screen.state;
            state.GoToMainMenu();
            state.SetMainMenuPane(PlayManagerState.MainMenuPane.Leaderboard);
            simulate.FrameUpdate();

            simulate.Click(screen.backButton);
            simulate.FrameUpdate();
            Assert.AreEqual(PlayManagerState.MainMenuPane.Main, state.mainMenuPane);

            state.SetMainMenuPane(PlayManagerState.MainMenuPane.Leaderboard);
            simulate.FrameUpdate();
            using (var evt = NavigationCancelEvent.GetPooled())
            {
                evt.target = screen.backButton;
                screen.backButton.SendEvent(evt);
            }
            simulate.FrameUpdate();
            Assert.AreEqual(PlayManagerState.MainMenuPane.Main, state.mainMenuPane);
        }

        [Test]
        public void AddToLeaderboardAddsOneEntry()
        {
            var gameOverPaneManager = ShowGameOver(42);
            var leaderboard = gameOverPaneManager.state.leaderboard;
            var count = leaderboard.count;

            gameOverPaneManager.addToLeaderboardNameField.value = "Player1";
            simulate.Click(gameOverPaneManager.addToLeaderboardButton);
            simulate.FrameUpdate();

            Assert.AreEqual(count + 1, leaderboard.count);
            Assert.AreEqual("Player1", leaderboard.entries[leaderboard.lastAddedIndex].playerName);
            Assert.AreEqual(42, leaderboard.entries[leaderboard.lastAddedIndex].toppings);
            Assert.IsFalse(gameOverPaneManager.leaderboardForm.enabledInHierarchy);

            // A second click can't add the same run twice.
            simulate.Click(gameOverPaneManager.addToLeaderboardButton);
            simulate.FrameUpdate();
            Assert.AreEqual(count + 1, leaderboard.count);
        }
    }
}
