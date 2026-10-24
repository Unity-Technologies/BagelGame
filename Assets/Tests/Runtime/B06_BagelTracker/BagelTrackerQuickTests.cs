using NUnit.Framework;
using UnityEngine.UIElements.TestFramework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel.B06_BagelTracker
{
    public class BagelTrackerQuickTests : UITestFixture
    {
        BagelTestAssetList m_BagelTestAssetList;
        PlayManagerState m_State;
        CleanupUtil m_CleanupUtil;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_BagelTestAssetList =
                Resources.Load<BagelTestAssetList>("BagelTestAssetList");
            m_State = PlayManagerState.CreateRuntimeCopy(null);
            m_State.tracker.toppingsCount = 42;

            m_CleanupUtil = AddTestComponent<CleanupUtil>();
            m_CleanupUtil.AddDestructible(m_State);
        }

        [Test]
        public void ToppingsCountBinding()
        {
            var root = rootVisualElement;
            m_BagelTestAssetList.bagelTrackerRightDisplayUxml.CloneTree(root);
            var display = root.Q<BagelTrackerPaneManager>();
            display.SetState(m_State);
            simulate.FrameUpdate();

            var toppingsGauge = display.toppingsGauge;
            Assert.IsNotNull(toppingsGauge);
            Assert.AreEqual(m_State.tracker.toppingsCount, toppingsGauge.value);

            m_State.tracker.toppingsCount = 5;
            simulate.FrameUpdate();
            Assert.AreEqual(5f, toppingsGauge.value);
        }
    }
}
