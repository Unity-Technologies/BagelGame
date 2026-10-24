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
