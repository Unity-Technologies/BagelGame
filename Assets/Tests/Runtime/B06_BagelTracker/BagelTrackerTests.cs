using NUnit.Framework;
using UnityEngine.UIElements.TestFramework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel.B06_BagelTracker
{
    public class BagelTrackerTests : UITestFixture
    {
        BagelTestAssetList m_BagelTestAssetList;
        BagelTrackerConstants m_BagelTrackerConstants;
        PlayManagerState m_State;

        BagelTrackerPaneManager m_LeftDisplay;
        BagelTrackerPaneManager m_RightDisplay;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_BagelTestAssetList = Resources.Load<BagelTestAssetList>("BagelTestAssetList");

            m_BagelTrackerConstants = ScriptableObject.CreateInstance<BagelTrackerConstants>();
            m_BagelTrackerConstants.hideFlags = HideFlags.HideAndDontSave;
            m_BagelTrackerConstants.spinMax = 15;
            m_BagelTrackerConstants.forceMax = 20;

        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Object.DestroyImmediate(m_BagelTrackerConstants);
        }

        [SetUp]
        public void SetUp()
        {
            m_State = PlayManagerState.CreateRuntimeCopy(null);
            m_State.tracker.constants = m_BagelTrackerConstants;
            m_State.tracker.Reset(1000);
            m_State.tracker.toppingsCount = 42;

            // The fixture clears the root after each test, so the displays are added for each one.
            var leftDisplay = m_BagelTestAssetList.bagelTrackerLeftDisplayUxml.Instantiate();
            var rightDisplay = m_BagelTestAssetList.bagelTrackerRightDisplayUxml.Instantiate();
            rootVisualElement.Add(leftDisplay);
            rootVisualElement.Add(rightDisplay);
            m_LeftDisplay = leftDisplay.Q<BagelTrackerPaneManager>();
            m_RightDisplay = rightDisplay.Q<BagelTrackerPaneManager>();

            m_LeftDisplay.SetState(m_State);
            m_RightDisplay.SetState(m_State);
            simulate.FrameUpdate();
        }

        [TearDown]
        public void TearDown()
        {
            rootVisualElement.Clear();
            PlayManagerState.DestroyRuntimeCopy(m_State);
        }

        [Test]
        public void ToppingsCountBinding()
        {
            var toppingsCountBar = m_RightDisplay.toppingsCountBar;
            Assert.IsNotNull(toppingsCountBar);
            Assert.AreEqual(m_State.tracker.toppingsCount, toppingsCountBar.value);
            Assert.AreEqual(1000f, toppingsCountBar.highValue);
            Assert.AreEqual("42", toppingsCountBar.title);

            m_State.tracker.toppingsCount = 5;
            simulate.FrameUpdate();
            Assert.AreEqual(5f, toppingsCountBar.value);
            Assert.AreEqual("5", toppingsCountBar.title);
        }

        [Test]
        public void SpinAndPainBindings()
        {
            Assert.AreEqual(15f, m_LeftDisplay.spinBar.highValue);
            Assert.AreEqual(20f, m_LeftDisplay.painBar.highValue);

            m_State.tracker.spin = 7;
            m_State.tracker.force = 3;
            simulate.FrameUpdate();
            Assert.AreEqual(7f, m_LeftDisplay.spinBar.value);
            Assert.AreEqual(3f, m_LeftDisplay.painBar.value);
        }
    }
}
