using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B03_MainMenu
{
    public class MainMenuNavigationTests : UITestFixture
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
        public void CancelGoesBackToTheMainPane()
        {
            m_BagelTestAssetList.mainMenuScreenUxml.CloneTree(rootVisualElement);
            var screen = rootVisualElement.Q<MainMenuScreenManager>();
            screen.state.SetMainMenuPane(PlayManagerState.MainMenuPane.Settings);
            simulate.FrameUpdate();
            Assert.IsFalse(screen.mainMenuPane.enabledSelf);

            var backButton = screen.settingsBackButton;
            using (var evt = NavigationCancelEvent.GetPooled())
            {
                evt.target = backButton;
                backButton.SendEvent(evt);
            }
            simulate.FrameUpdate();

            Assert.AreEqual(PlayManagerState.MainMenuPane.Main, screen.state.mainMenuPane);
            Assert.IsTrue(screen.mainMenuPane.enabledSelf);
        }
    }
}
