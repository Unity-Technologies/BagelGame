using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.T7_Pause
{
    public class PauseScreenTests : UITestFixture
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
        public void PauseScreen()
        {
            m_BagelTestAssetList.pauseScreenUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();

            bool resumeClicked = false;
            bool restartClicked = false;
            bool mainMenuClicked = false;

            // Get the elements.
            var elements = PauseScreenManager.BindUI(rootVisualElement, new PauseScreenManager.Callbacks
            {
                onResume = () => resumeClicked = true,
                onRestart = () => restartClicked = true,
                onMainMenu = () => mainMenuClicked = true
            } );

            // Resume Button
            simulate.Click(elements.resumeButton);
            simulate.FrameUpdate();
            Assert.IsTrue(resumeClicked);

            // Restart Button
            simulate.Click(elements.restartButton);
            simulate.FrameUpdate();
            Assert.IsFalse(restartClicked); // Should not trigger yet.
            simulate.MouseDown(elements.restartButton);
            simulate.FrameUpdate(elements.restartButton.holdTime);
            Assert.IsTrue(restartClicked);

            // Main Menu Button
            simulate.Click(elements.mainMenuButton);
            simulate.FrameUpdate();
            Assert.IsFalse(mainMenuClicked); // Should not trigger yet.
            simulate.MouseDown(elements.mainMenuButton);
            simulate.FrameUpdate(elements.mainMenuButton.holdTime);
            Assert.IsTrue(mainMenuClicked);
        }
    }
}
