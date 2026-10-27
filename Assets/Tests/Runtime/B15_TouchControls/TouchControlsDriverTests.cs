using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Bagel.B15_TouchControls
{
    // The touch controls in the game, with a touchscreen (as the Device Simulator adds one): there while the bagel
    // rolls, gone when it's paused or the game's over (and the stick lets go), and not there at all without a
    // touchscreen. The stick rolls the bagel, and the pause button pauses. Every frame is one physics step.
    public class TouchControlsDriverTests
    {
        Touchscreen m_Touchscreen;
        PlayManager m_PlayManager;
        BagelController m_Bagel;
        VisualElement m_Root;
        TouchControlsManager m_Manager;

        PlayManagerState state => m_PlayManager.state;
        bool shown => m_Root.resolvedStyle.display == DisplayStyle.Flex;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Before the scene loads, like a phone's.
            m_Touchscreen = InputSystem.AddDevice<Touchscreen>("Bagel Test Touchscreen");
            Time.captureDeltaTime = Time.fixedDeltaTime;

            var assets = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
            yield return SceneManager.LoadSceneAsync(assets.mainSceneName, LoadSceneMode.Single);
            yield return null;

            m_PlayManager = Object.FindAnyObjectByType<PlayManager>();
            m_Bagel = Object.FindAnyObjectByType<BagelController>(FindObjectsInactive.Include);
            var panelRenderer = Object.FindAnyObjectByType<TouchControlsDriver>().GetComponent<PanelRenderer>();
            PanelRenderer.VersionedUIReloadCallback found = (_, root, _) => m_Root = root;
            panelRenderer.RegisterUIReloadCallback(found);
            panelRenderer.UnregisterUIReloadCallback(found);
            m_Manager = m_Root.Q<TouchControlsManager>();

            state.GoToBagelSelection();
            state.GoToPlay();
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
            if (m_Touchscreen != null && m_Touchscreen.added)
                InputSystem.RemoveDevice(m_Touchscreen);
        }

        static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; ++i)
                yield return null;
        }

        // A finger on the screen, at a place on the controls' panel.
        void Touch(int finger, Vector2 position, UnityEngine.TouchPhase phase)
        {
            var touch = new UnityEngine.Touch { fingerId = finger, position = position, phase = phase, tapCount = 1 };
            EventBase evt = phase switch
            {
                UnityEngine.TouchPhase.Began => PointerDownEvent.GetPooled(touch),
                UnityEngine.TouchPhase.Moved => PointerMoveEvent.GetPooled(touch),
                _ => PointerUpEvent.GetPooled(touch)
            };
            using (evt)
                m_Root.SendEvent(evt);
        }

        Vector2 stickMiddle => m_Manager.moveStick.worldBound.center;

        [UnityTest]
        public IEnumerator PushedUpTheStickRollsTheBagelForward()
        {
            Assert.IsTrue(shown, "there while the bagel rolls");
            var start = m_Bagel.transform.position;
            var forward = m_Bagel.GetAbsoluteForward();

            Touch(0, stickMiddle, UnityEngine.TouchPhase.Began);
            Touch(0, stickMiddle + new Vector2(0f, -m_Manager.moveStick.layout.width), UnityEngine.TouchPhase.Moved);
            yield return Frames(50);

            Assert.Less(Vector2.Distance(Vector2.up, m_PlayManager.playInputBindings.touchMove), 1e-3f, "up");
            Assert.Greater(Vector3.Dot(m_Bagel.transform.position - start, forward), 0.5f, "rolled forward");
        }

        [UnityTest]
        public IEnumerator PausedTheyreGoneAndTheStickLetsGo()
        {
            Touch(0, stickMiddle, UnityEngine.TouchPhase.Began);
            Touch(0, stickMiddle + new Vector2(30f, 0f), UnityEngine.TouchPhase.Moved);
            yield return null;
            Assert.IsTrue(m_Manager.moveStick.isHeld);

            var pause = m_Manager.pauseButton.worldBound.center;
            Touch(1, pause, UnityEngine.TouchPhase.Began);
            Touch(1, pause, UnityEngine.TouchPhase.Ended);
            yield return null;

            Assert.IsTrue(state.isPaused, "the pause button paused");
            Assert.IsFalse(shown, "gone behind the pause screen");
            Assert.IsFalse(m_Manager.moveStick.isHeld);
            Assert.AreEqual(Vector2.zero, m_PlayManager.playInputBindings.touchMove);

            state.Resume();
            yield return null;
            Assert.IsTrue(shown, "back");
        }

        [UnityTest]
        public IEnumerator WithTheGameOverTheyreGone()
        {
            state.FinishRun();
            yield return null;

            Assert.IsFalse(shown);
        }

        [UnityTest]
        public IEnumerator WithoutATouchscreenTheyreNotThere()
        {
            InputSystem.RemoveDevice(m_Touchscreen);
            yield return null;
            Assume.That(!TouchControlsDriver.hasTouchScreen, "this computer has a touchscreen of its own");

            Assert.IsFalse(shown);

            InputSystem.AddDevice(m_Touchscreen);
            yield return null;
            Assert.IsTrue(shown, "one's plugged in");
        }
    }
}
