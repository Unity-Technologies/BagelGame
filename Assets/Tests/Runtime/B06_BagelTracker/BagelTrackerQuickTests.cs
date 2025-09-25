using NUnit.Framework;
using UnityEngine.UIElements.TestFramework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel.B06_BagelTracker
{
    public class BagelTrackerQuickTests : UITestFixture
    {
        BagelTestAssetList m_BagelTestAssetList;
        BagelTrackerData m_BagelTrackerData;
        CleanupUtil m_CleanupUtil;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_BagelTestAssetList =
                Resources.Load<BagelTestAssetList>("BagelTestAssetList");
            m_BagelTrackerData = ScriptableObject.CreateInstance<BagelTrackerData>();
            m_BagelTrackerData.toppingsCount = 42;

            m_CleanupUtil = AddTestComponent<CleanupUtil>();
            m_CleanupUtil.AddDestructible(m_BagelTrackerData);
        }

        [Test]
        public void ToppingsCountBinding()
        {
            var root = rootVisualElement;
            m_BagelTestAssetList.bagelTrackerRightDisplayUxml.CloneTree(root);
            var display = root.Q("pane");
            display.dataSource = m_BagelTrackerData;
            simulate.FrameUpdate();

            var toppingsCountBar = display.Q<ProgressBar>("toppings-count-bar");
            Assert.IsNotNull(toppingsCountBar);
            Assert.AreEqual(m_BagelTrackerData.toppingsCount, toppingsCountBar.value);

            m_BagelTrackerData.toppingsCount = 5;
            simulate.FrameUpdate();
            Assert.AreEqual(5f, toppingsCountBar.value);
        }
    }
}
