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

        // Tab goes down the pause buttons and round again, never into the Settings hidden behind them; and back into
        // the pause buttons once Settings close.
        [Test]
        public void TabGoesRoundThePauseButtons()
        {
            m_BagelTestAssetList.pauseScreenUxml.CloneTree(rootVisualElement);
            var screen = rootVisualElement.Q<PauseScreenManager>();
            var state = screen.state;
            state.GoToPlay();
            state.Pause();
            simulate.FrameUpdate();
            var pause = screen.pausePaneManager;
            var buttons = new VisualElement[] { pause.resumeButton, pause.settingsButton, pause.restartButton, pause.mainMenuButton };

            void Tab(VisualElement from, bool forward)
            {
                using var evt = NavigationMoveEvent.GetPooled(forward ? NavigationMoveEvent.Direction.Next : NavigationMoveEvent.Direction.Previous);
                evt.target = from;
                from.SendEvent(evt);
                simulate.FrameUpdate();
            }

            VisualElement focused() => rootVisualElement.focusController.focusedElement as VisualElement;

            pause.resumeButton.Focus();
            simulate.FrameUpdate();
            for (int i = 1; i <= buttons.Length; i++)
            {
                Tab(focused(), true);
                Assert.AreEqual(buttons[i % buttons.Length], focused(), $"Tab {i}");
            }
            Tab(focused(), false);
            Assert.AreEqual(pause.mainMenuButton, focused(), "Shift+Tab from the first is the last");

            simulate.Click(pause.settingsButton);
            simulate.FrameUpdate();
            SendCancel(screen.settingsPaneManager.tabs[0].tabHeader);
            simulate.FrameUpdate();
            Tab(focused(), true);
            Assert.IsTrue(screen.pausePane.Contains(focused()), "Settings closed, Tab's in the pause buttons");
        }
    }
}
