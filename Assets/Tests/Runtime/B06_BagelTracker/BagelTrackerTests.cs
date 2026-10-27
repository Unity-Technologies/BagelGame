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
            m_BagelTrackerConstants.speedMax = 6;

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
            var toppingsGauge = m_RightDisplay.toppingsGauge;
            Assert.IsNotNull(toppingsGauge);
            Assert.AreEqual(m_State.tracker.toppingsCount, toppingsGauge.value);
            Assert.AreEqual(1000f, toppingsGauge.maxValue);
            Assert.AreEqual(0.042f, toppingsGauge.fraction, 1e-5f);
            Assert.AreEqual("42", m_RightDisplay.toppingsCount.text);

            m_State.tracker.toppingsCount = 5;
            simulate.FrameUpdate();
            Assert.AreEqual(5f, toppingsGauge.value);
            Assert.AreEqual("5", m_RightDisplay.toppingsCount.text);
        }

        [Test]
        public void SpinAndPainBindings()
        {
            // The knocks are under the speed, on the left wing; the right wing is the toppings' alone.
            Assert.IsNull(m_RightDisplay.spinGauge);
            Assert.IsNull(m_RightDisplay.painGauge);
            Assert.AreEqual(BagelGauge.Kind.Spin, m_LeftDisplay.spinGauge.kind);
            Assert.AreEqual(BagelGauge.Kind.Pain, m_LeftDisplay.painGauge.kind);
            Assert.AreEqual(15f, m_LeftDisplay.spinGauge.maxValue);
            Assert.AreEqual(20f, m_LeftDisplay.painGauge.maxValue);

            m_State.tracker.spin = 7;
            m_State.tracker.force = 5;
            simulate.FrameUpdate();
            Assert.AreEqual(7f, m_LeftDisplay.spinGauge.value);
            Assert.AreEqual(0.25f, m_LeftDisplay.painGauge.fraction, 1e-5f);
        }

        // On the left wing, the speed, then the spin under it, then the pain under that, on the wing's inner edge.
        [Test]
        public void ThePainIsUnderTheSpinUnderTheSpeed()
        {
            var speed = m_LeftDisplay.speedGauge.worldBound;
            var spin = m_LeftDisplay.spinGauge.worldBound;
            var pain = m_LeftDisplay.painGauge.worldBound;

            Assert.LessOrEqual(speed.yMax, spin.yMin, "the spin's under the speed");
            Assert.LessOrEqual(spin.yMax, pain.yMin, "the pain's under the spin");
            Assert.AreEqual(spin.xMax, pain.xMax, 1e-3f, "in one column");
            Assert.AreEqual(speed.xMax, spin.xMax, 1e-3f, "on the inner edge");
        }

        // No words on the gauges: their faces say what they show.
        [Test]
        public void TheGaugesHaveNoCaptions()
        {
            Assert.IsEmpty(m_LeftDisplay.Query<Label>().ToList());
            CollectionAssert.AreEqual(new[] { "toppings-count" }, m_RightDisplay.Query<Label>().ToList().ConvertAll(label => label.name));
        }

        [Test]
        public void TheGaugesShowSpeed()
        {
            m_State.tracker.speed = -3f;
            simulate.FrameUpdate();

            // Backwards is as fast as forwards: half of speedMax (6).
            Assert.AreEqual(0.5f, m_LeftDisplay.speedGauge.fraction, 1e-5f);
            var needle = m_LeftDisplay.speedGauge.Q(className: BagelGauge.needleUssClassName);
            Assert.AreEqual(0f, needle.resolvedStyle.rotate.angle.ToDegrees(), 1e-3f, "half way up: straight up");
        }
    }
}
