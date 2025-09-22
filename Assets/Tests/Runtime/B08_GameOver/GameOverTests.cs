using NUnit.Framework;
using UnityEngine;
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

        [Test]
        public void AddNameToLeaderboardField()
        {
            // Load the UXML.
            m_BagelTestAssetList.gameOverScreenUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();

            // Get the elements.
            var elements = GameOverScreenManager.BindUI(rootVisualElement);

            // Check initial state.
            Assert.AreEqual("", elements.addToLeaderboardNameField.value);

            // Simulate typing a name.
            elements.addToLeaderboardNameField.Focus();
            simulate.FrameUpdate();
            simulate.TypingText("Player1");
            simulate.FrameUpdate();

            // Verify the name was entered.
            Assert.AreEqual("Player1", elements.addToLeaderboardNameField.value);
        }
    }
}
