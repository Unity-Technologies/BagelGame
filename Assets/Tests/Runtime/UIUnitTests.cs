using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel
{
    public class UIUnitTests : UITestFixture
    {
        BagelTestAssetList m_BagelTestAssetList;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                m_BagelTestAssetList = ScriptableObject.CreateInstance<BagelTestAssetList>();
                return;
            }
#endif
            m_BagelTestAssetList = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
        }

        [TearDown]
        public void TearDown()
        {
            rootVisualElement.Clear();
        }

        void SetUp(VisualTreeAsset uxml)
        {
            Assert.IsNotNull(uxml);
            uxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();
        }

        [Test]
        public void MainMenu()
        {
            SetUp(m_BagelTestAssetList.mainMenuUxml);

            bool playClicked = false;
            bool exitClicked = false;

            // Get the elements.
            var elements = MainMenuScreenDriver.BindUI(rootVisualElement, new MainMenuScreenDriver.Callbacks
            {
                onPlay = () => playClicked = true,
                onExit = () => exitClicked = true
            });

            // Play Button
            simulate.Click(elements.playButton);
            simulate.FrameUpdate();
            Assert.IsTrue(playClicked);

            // Exit Button
            simulate.Click(elements.exitButton);
            simulate.FrameUpdate();
            Assert.IsFalse(exitClicked); // Should not trigger yet.
            simulate.MouseDown(elements.exitButton);
            simulate.FrameUpdate(elements.exitButton.holdTime);
            Assert.IsTrue(exitClicked);
        }

        [Test]
        public void PauseScreen()
        {
            SetUp(m_BagelTestAssetList.pauseScreenUxml);

            bool resumeClicked = false;
            bool restartClicked = false;
            bool mainMenuClicked = false;

            // Get the elements.
            var elements = PauseScreenDriver.BindUI(rootVisualElement, new PauseScreenDriver.Callbacks
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
