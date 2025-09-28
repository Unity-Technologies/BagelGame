using NUnit.Framework;
using UnityEditor.UIElements.TestFramework;
using UnityEngine.UIElements;

namespace Bagel.B10_EditorTools
{
    public class BagelAllUIWindowTests : EditorWindowUITestFixture<BagelAllUIWindow>
    {
        public BagelAllUIWindowTests()
        {
            debugMode = true;
        }

        [Test]
        public void MainMenuLeaderboardButtonTest()
        {
            simulate.FrameUpdate();

            var mainMenuDriver = rootVisualElement.Q<MainMenuScreenManager>();
            var mainMenuPane = mainMenuDriver.Q<VisualElement>("main-menu-pane");
            var leaderboardButton = mainMenuDriver.Q<Button>("leaderboard-button");
            var leaderboardPane = mainMenuDriver.Q<VisualElement>("leaderboard-pane");

            simulate.Click(leaderboardButton);
            simulate.FrameUpdate();
            Assert.IsTrue(leaderboardPane.enabledSelf);
            Assert.IsFalse(mainMenuPane.enabledSelf);
        }
    }
}
