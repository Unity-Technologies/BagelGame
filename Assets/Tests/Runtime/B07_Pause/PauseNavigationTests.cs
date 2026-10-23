using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B07_Pause
{
    // The gamepad's back button: out of Settings first, then back to the game.
    public class PauseNavigationTests : UITestFixture
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

        static void SendCancel(VisualElement target)
        {
            using var evt = NavigationCancelEvent.GetPooled();
            evt.target = target;
            target.SendEvent(evt);
        }

        [Test]
        public void CancelLeavesSettingsThenResumes()
        {
            m_BagelTestAssetList.pauseScreenUxml.CloneTree(rootVisualElement);
            var screen = rootVisualElement.Q<PauseScreenManager>();
            var state = screen.state;
            state.GoToPlay();
            state.Pause();
            simulate.FrameUpdate();

            simulate.Click(screen.pausePaneManager.settingsButton);
            simulate.FrameUpdate();
            Assert.AreEqual(PlayManagerState.PausePane.Settings, state.pausePane);
            Assert.IsTrue(screen.pausePane.ClassListContains(PauseScreenManager.inactivePaneClassName));

            SendCancel(screen.settingsPaneManager.backButton);
            simulate.FrameUpdate();
            Assert.AreEqual(PlayManagerState.PausePane.Pause, state.pausePane);
            Assert.IsTrue(state.isPaused);

            SendCancel(screen.pausePaneManager.resumeButton);
            simulate.FrameUpdate();
            Assert.IsFalse(state.isPaused);
        }
    }
}
