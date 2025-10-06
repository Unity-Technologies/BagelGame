using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B03_MainMenu
{
    public class MainMenuScreenTests : UITestFixture
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
        public void MainMenu()
        {
            m_BagelTestAssetList.mainMenuUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();

            bool playClicked = false;
            bool exitClicked = false;

            // Get the elements.
            var mainMenuPaneManager = rootVisualElement.Q<MainMenuPaneManager>();
            mainMenuPaneManager.BindUI(new MainMenuPaneManager.Callbacks
            {
                onPlay = () => playClicked = true,
                onExit = () => exitClicked = true
            });

            // Play Button
            simulate.Click(mainMenuPaneManager.playButton);
            simulate.FrameUpdate();
            Assert.IsTrue(playClicked);

            // Exit Button
            simulate.Click(mainMenuPaneManager.exitButton);
            simulate.FrameUpdate();
            Assert.IsTrue(exitClicked);
        }
    }
}
