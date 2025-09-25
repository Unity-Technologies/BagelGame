using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B08_GameOver
{
    public class GameOverQuickTests : UITestFixture
    {
        BagelTestAssetList m_BagelTestAssetList;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_BagelTestAssetList =
                Resources.Load<BagelTestAssetList>(
                    "BagelTestAssetList");
        }

        [Test]
        public void AddNameToLeaderboardField()
        {
            var root = rootVisualElement;
            m_BagelTestAssetList.gameOverScreenUxml.CloneTree(root);
            simulate.FrameUpdate();

            var field = root.Q<TextField>("leaderboard-name-field");

            // Simulate typing a name.
            field.Focus();
            simulate.FrameUpdate();
            simulate.TypingText("Player1");
            simulate.FrameUpdate();

            // Verify the name was entered.
            Assert.AreEqual("Player1", field.value);
        }
    }
}
