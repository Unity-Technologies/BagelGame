using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B07_Pause
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
            m_BagelTestAssetList.pausePaneUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();

            bool resumeClicked = false;
            bool restartClicked = false;
            bool mainMenuClicked = false;

            // Get the elements.
            var pausePaneManager = rootVisualElement.Q<PausePaneManager>();
            pausePaneManager.BindUI(new PausePaneManager.Callbacks
            {
                onResume = () => resumeClicked = true,
                onRestart = () => restartClicked = true,
                onMainMenu = () => mainMenuClicked = true
            } );

            // Resume Button
            simulate.Click(pausePaneManager.resumeButton);
            simulate.FrameUpdate();
            Assert.IsTrue(resumeClicked);

            // Restart Button
            simulate.MouseDown(pausePaneManager.restartButton);
            simulate.FrameUpdate(pausePaneManager.restartButton.holdTime);
            Assert.IsTrue(restartClicked);

            // Main Menu Button
            simulate.MouseDown(pausePaneManager.mainMenuButton);
            simulate.FrameUpdate(pausePaneManager.mainMenuButton.holdTime);
            Assert.IsTrue(mainMenuClicked);
        }
    }
}
