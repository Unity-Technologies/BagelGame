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
            var entries = leaderboard.leaderboardData.entries;
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
            Assert.AreEqual(index, table.viewController.GetIdForIndex(table.selectedIndex));
            Assert.AreEqual("Newcomer", leaderboard.leaderboardData.entries[index].playerName);
        }
    }
}
