using NUnit.Framework;
using UnityEngine.UIElements.TestFramework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel.T6_BagelTracker
{
    public class BagelTrackerTests : UITestFixture
    {
        BagelTestAssetList m_BagelTestAssetList;
        BagelTrackerConstants m_BagelTrackerConstants;
        BagelTrackerData m_BagelTrackerData;

        VisualElement m_LeftDisplay;
        VisualElement m_RightDisplay;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_BagelTestAssetList = Resources.Load<BagelTestAssetList>("BagelTestAssetList");

            m_BagelTrackerConstants = ScriptableObject.CreateInstance<BagelTrackerConstants>();
            m_BagelTrackerConstants.hideFlags = HideFlags.HideAndDontSave;

            m_BagelTrackerData = ScriptableObject.CreateInstance<BagelTrackerData>();
            m_BagelTrackerData.hideFlags = HideFlags.HideAndDontSave;
            m_BagelTrackerData.constants = m_BagelTrackerConstants;
            m_BagelTrackerData.toppingsCount = 42;

            m_LeftDisplay = m_BagelTestAssetList.bagelTrackerLeftDisplayUxml.Instantiate();
            m_RightDisplay = m_BagelTestAssetList.bagelTrackerRightDisplayUxml.Instantiate();
            rootVisualElement.Add(m_LeftDisplay);
            rootVisualElement.Add(m_RightDisplay);
            m_LeftDisplay = m_LeftDisplay.Q("pane");
            m_RightDisplay = m_RightDisplay.Q("pane");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ScriptableObject.DestroyImmediate(m_BagelTrackerData);
        }

        [SetUp]
        public void SetUp()
        {
            m_LeftDisplay.dataSource = m_BagelTrackerData;
            m_RightDisplay.dataSource = m_BagelTrackerData;
            simulate.FrameUpdate();
        }

        [Test]
        public void ToppingsCountBinding()
        {
            var toppingsCountBar = m_RightDisplay.Q<ProgressBar>("toppings-count-bar");
            Assert.IsNotNull(toppingsCountBar);
            Assert.AreEqual(m_BagelTrackerData.toppingsCount, toppingsCountBar.value);

            m_BagelTrackerData.toppingsCount = 5;
            simulate.FrameUpdate();
            Assert.AreEqual(5f, toppingsCountBar.value);
        }
    }
}
