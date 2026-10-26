using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B09_Leaderboard
{
    public class LeaderboardTests : UITestFixture
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

        LeaderboardManager ShowLeaderboard()
        {
            panelSize = new Vector2(500f, 700f);
            m_BagelTestAssetList.leaderboardPaneUxml.CloneTree(rootVisualElement);

            // The list sorts once its columns are laid out.
            simulate.FrameUpdate();
            simulate.FrameUpdate();
            return rootVisualElement.Q<LeaderboardManager>();
        }

        [Test]
        public void TheBestScoresComeFirst()
        {
            var leaderboard = ShowLeaderboard();
            var entries = leaderboard.shownEntries;
            Assert.Greater(entries.Count, 0);
            Assert.AreEqual(entries.Count, leaderboard.leaderboardTable.itemsSource.Count);

            // Rows are recycled, so ask the list which entry it shows first.
            var best = entries.Max(entry => entry.toppings);
            var firstEntry = leaderboard.leaderboardTable.viewController.GetIdForIndex(0);
            Assert.AreEqual(best, entries[firstEntry].toppings);
        }

        [Test]
        public void ANewScoreIsSelected()
        {
            var leaderboard = ShowLeaderboard();
            var index = leaderboard.state.leaderboard.Add("Newcomer", 500);
            simulate.FrameUpdate();

            var table = leaderboard.leaderboardTable;
            Assert.AreEqual(leaderboard.ShownIndexOf(index), table.viewController.GetIdForIndex(table.selectedIndex));
            Assert.AreEqual("Newcomer", leaderboard.shownEntries[table.viewController.GetIdForIndex(table.selectedIndex)].playerName);
        }

        // Each level has its own board: the runs from before there were levels are the tutorial's; < and > go
        // through the levels.
        [Test]
        public void EachLevelHasItsOwnBoard()
        {
            var leaderboard = ShowLeaderboard();
            var state = leaderboard.state;
            var tutorialCount = leaderboard.shownEntries.Count;
            Assert.AreEqual(state.leaderboard.count, tutorialCount, "the runs from before levels are the tutorial's");
            Assert.AreEqual("Breakfast", leaderboard.levelName.text);
            Assert.IsFalse(leaderboard.previousLevelButton.enabledSelf);

            state.leaderboard.Add("Bruncher", 321, 61f, "brunch");
            simulate.FrameUpdate();
            Assert.AreEqual(tutorialCount, leaderboard.shownEntries.Count, "another level's run isn't on this board");

            simulate.Click(leaderboard.nextLevelButton);
            simulate.FrameUpdate();
            Assert.AreEqual("brunch", state.leaderboard.shownLevel);
            Assert.AreEqual("Brunch", leaderboard.levelName.text);
            Assert.AreEqual(1, leaderboard.shownEntries.Count);
            Assert.AreEqual("Bruncher", leaderboard.shownEntries[0].playerName);
            Assert.AreEqual(1, leaderboard.leaderboardTable.itemsSource.Count);
            Assert.IsTrue(leaderboard.previousLevelButton.enabledSelf);

            simulate.Click(leaderboard.nextLevelButton);
            simulate.FrameUpdate();
            Assert.AreEqual(0, leaderboard.shownEntries.Count, "nobody's played the banquet yet");
            Assert.IsFalse(leaderboard.nextLevelButton.enabledSelf, "the last level");
        }
    }
}
