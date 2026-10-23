using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B04_Settings
{
    public class DisplaySettingsTests : UITestFixture
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
        public void TogglesSwitchMotionAndEffectsOffOnTheScreen()
        {
            m_BagelTestAssetList.settingsPaneForUIUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();
            var manager = rootVisualElement.Q<SettingsPaneManagerForUI>();
            var reduceMotion = manager.Q<Toggle>("reduce-motion-toggle");
            var visualEffects = manager.Q<Toggle>("visual-effects-toggle");

            Assert.IsFalse(reduceMotion.value);
            Assert.IsTrue(visualEffects.value);
            Assert.IsFalse(manager.ClassListContains(BagelPane.reduceMotionUssClassName));
            Assert.IsFalse(manager.ClassListContains(BagelPane.noVisualEffectsUssClassName));

            simulate.Click(reduceMotion);
            simulate.Click(visualEffects);
            simulate.FrameUpdate();

            Assert.IsTrue(manager.state.settings.reduceMotion);
            Assert.IsFalse(manager.state.settings.visualEffects);
            Assert.IsTrue(manager.ClassListContains(BagelPane.reduceMotionUssClassName));
            Assert.IsTrue(manager.ClassListContains(BagelPane.noVisualEffectsUssClassName));
        }

        [Test]
        public void NoVisualEffectsRemovesTheHoverGlow()
        {
            var state = PlayManagerState.CreateRuntimeCopy(m_BagelTestAssetList.startState);
            try
            {
                var screen = m_BagelTestAssetList.pausePaneUxml.Instantiate();
                screen.styleSheets.Add(m_BagelTestAssetList.themes.collection[0].theme);
                rootVisualElement.Add(screen);
                var pane = screen.Q<PausePaneManager>();
                pane.SetState(state);
                state.settings.visualEffects = false;
                simulate.FrameUpdate();

                // Hover the resume button: with effects off, no drop shadow filter.
                simulate.MouseMove(pane.resumeButton.worldBound.center);
                simulate.FrameUpdate();
                CollectionAssert.IsEmpty(pane.resumeButton.resolvedStyle.filter);
            }
            finally
            {
                PlayManagerState.DestroyRuntimeCopy(state);
            }
        }
    }
}
