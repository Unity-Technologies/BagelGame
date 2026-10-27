using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B15_TouchControls
{
    // The touch controls by themselves: the thumb stick follows the finger that pressed it out to its rim and goes
    // back to the middle when it lifts, keeps that finger while another presses pause, never takes the focus, and is
    // what the bagel's input reads while it's held.
    public class TouchControlsTests : UITestFixture
    {
        BagelTestAssetList m_BagelTestAssetList;
        TouchControlsManager m_Manager;
        BagelTouchStick m_Stick;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_BagelTestAssetList = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
        }

        // The fixture clears the root after each test, so the controls are added for each one.
        [SetUp]
        public void SetUp()
        {
            m_BagelTestAssetList.touchControlsUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();
            m_Manager = rootVisualElement.Q<TouchControlsManager>();
            m_Stick = m_Manager.moveStick;
        }

        [TearDown]
        public void TearDown()
        {
            rootVisualElement.Clear();
        }

        Vector2 middle => m_Stick.worldBound.center;

        // How far the finger goes for the stick to be all the way over.
        float reach => m_Stick.layout.width * 0.5f - m_Stick.knob.layout.width * 0.25f;

        void AssertValue(Vector2 expected, string what)
        {
            Assert.Less(Vector2.Distance(expected, m_Stick.value), 1e-3f, $"{what}: {m_Stick.value:F3}, not {expected:F3}");
        }

        // A finger on the screen: each one is a pointer of its own.
        void Touch(int finger, Vector2 position, TouchPhase phase)
        {
            var touch = new Touch { fingerId = finger, position = position, phase = phase, tapCount = 1 };
            EventBase evt = phase switch
            {
                TouchPhase.Began => PointerDownEvent.GetPooled(touch),
                TouchPhase.Moved => PointerMoveEvent.GetPooled(touch),
                _ => PointerUpEvent.GetPooled(touch)
            };
            using (evt)
                rootVisualElement.SendEvent(evt);
            simulate.FrameUpdate();
        }

        [Test]
        public void TheKnobFollowsTheFingerOutToTheRim()
        {
            simulate.MouseDown(middle);
            simulate.MouseMove(middle + new Vector2(reach * 0.5f, 0f));
            simulate.FrameUpdate();
            Assert.IsTrue(m_Stick.isHeld);
            Assert.IsTrue(m_Stick.ClassListContains(BagelTouchStick.heldUssClassName));
            AssertValue(new Vector2(0.5f, 0f), "half way to the right");
            Assert.AreEqual(reach * 0.5f, m_Stick.knob.style.translate.value.x.value, 1e-3f, "the knob's under the finger");

            // Up the screen is up the stick, and past the rim is as far as it goes.
            simulate.MouseMove(middle + new Vector2(0f, -reach * 3f));
            simulate.FrameUpdate();
            AssertValue(new Vector2(0f, 1f), "up, all the way");

            simulate.MouseUp(middle + new Vector2(0f, -reach * 3f));
            simulate.FrameUpdate();
            Assert.IsFalse(m_Stick.isHeld);
            Assert.IsFalse(m_Stick.ClassListContains(BagelTouchStick.heldUssClassName));
            AssertValue(Vector2.zero, "back in the middle");
        }

        // One thumb on the stick, the other on pause: the stick keeps its own finger.
        [Test]
        public void TheStickKeepsItsFingerWhileAnotherPressesPause()
        {
            var paused = false;
            m_Manager.BindUI(new TouchControlsManager.Callbacks { onPause = () => paused = true });

            Touch(0, middle, TouchPhase.Began);
            Touch(0, middle + new Vector2(-reach, 0f), TouchPhase.Moved);
            AssertValue(new Vector2(-1f, 0f), "left");

            var pause = m_Manager.pauseButton.worldBound.center;
            Touch(1, pause, TouchPhase.Began);
            Touch(1, pause, TouchPhase.Ended);
            Assert.IsTrue(paused, "paused");
            Assert.IsTrue(m_Stick.isHeld, "still held");
            AssertValue(new Vector2(-1f, 0f), "still left");

            Touch(0, middle + new Vector2(-reach, 0f), TouchPhase.Ended);
            Assert.IsFalse(m_Stick.isHeld);
            AssertValue(Vector2.zero, "let go");
        }

        // They're for touch: a gamepad's or the keyboard's focus never lands on them.
        [Test]
        public void TheControlsNeverTakeTheFocus()
        {
            simulate.Click(m_Manager.pauseButton);
            simulate.MouseDown(middle);
            simulate.MouseUp(middle);
            simulate.FrameUpdate();

            Assert.IsFalse(m_Manager.pauseButton.focusable);
            Assert.IsFalse(m_Stick.focusable);
            Assert.IsNull(rootVisualElement.focusController.focusedElement);
        }

        [Test]
        public void HeldTheStickIsTheBagelsInput()
        {
            var bindings = new GameObject("Bagel Test Input").AddComponent<PlayInputBindings>();
            try
            {
                m_Manager.playInputBindings = bindings;

                simulate.MouseDown(middle);
                simulate.MouseMove(middle + new Vector2(reach * 0.5f, -reach * 0.5f));
                simulate.FrameUpdate();
                Assert.AreEqual(m_Stick.value, bindings.touchMove);
                Assert.Less(Vector2.Distance(new Vector2(1f, 1f).normalized, bindings.GetMovementVectorNormalized()), 1e-3f, "which way, as the keys and the gamepad say it");

                // Barely off the middle, it's still.
                simulate.MouseMove(middle + new Vector2(reach * 0.1f, 0f));
                simulate.FrameUpdate();
                Assert.AreEqual(Vector2.zero, bindings.GetMovementVectorNormalized());

                simulate.MouseUp(middle);
                simulate.FrameUpdate();
                Assert.AreEqual(Vector2.zero, bindings.touchMove);
            }
            finally
            {
                Object.DestroyImmediate(bindings.gameObject);
            }
        }
    }
}
