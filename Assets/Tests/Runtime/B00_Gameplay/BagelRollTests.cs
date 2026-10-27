using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bagel.B00_Gameplay
{
    // The bagel in the real level: rolling, turning, losing toppings and ending runs. Input
    // comes from a virtual gamepad, and every frame is exactly one physics step, so each run
    // plays out the same way every time.
    public class BagelRollTests
    {
        Gamepad m_Gamepad;
        PlayManager m_PlayManager;
        BagelController m_Bagel;
        Rigidbody m_Body;
        Vector3 m_Start;

        PlayManagerState state => m_PlayManager.state;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Before the scene loads: the game's input actions find the virtual gamepad.
            m_Gamepad = InputSystem.AddDevice<Gamepad>("Bagel Test Gamepad");
            Time.captureDeltaTime = Time.fixedDeltaTime;

            var assets = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
            yield return SceneManager.LoadSceneAsync(assets.mainSceneName, LoadSceneMode.Single);
            yield return null;

            m_PlayManager = Object.FindAnyObjectByType<PlayManager>();
            m_Bagel = Object.FindAnyObjectByType<BagelController>(FindObjectsInactive.Include);
            m_Body = m_Bagel.GetComponent<Rigidbody>();

            state.GoToBagelSelection();
            state.GoToPlay();
            yield return null;
            m_Start = m_Bagel.transform.position;
        }

        [TearDown]
        public void TearDown()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
            InputSystem.RemoveDevice(m_Gamepad);
        }

        static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; ++i)
                yield return null;
        }

        // The gamepad's state changes like a real one's: the input update picks it up next frame.
        void Stick(Vector2 value) => InputSystem.QueueStateEvent(m_Gamepad, new GamepadState { leftStick = value });

        // Puts the bagel somewhere else on (or off) the level, upright and still, facing the toaster.
        void Place(Vector3 position)
        {
            m_Body.position = position;
            m_Body.rotation = Quaternion.identity;
            m_Body.linearVelocity = Vector3.zero;
            m_Body.angularVelocity = Vector3.zero;
            m_Bagel.transform.SetPositionAndRotation(position, Quaternion.identity);
        }

        [UnityTest]
        public IEnumerator RollingForwardMovesTheBagelAlong()
        {
            Stick(Vector2.up);
            yield return Frames(50);

            Assert.Greater(m_Bagel.transform.position.z, m_Start.z + 0.5f);
            Assert.Greater(state.tracker.speed, 0f);
            Assert.Greater(state.tracker.spin, 0f);
        }

        [UnityTest]
        public IEnumerator TurningTurnsTheBagel()
        {
            var forward = m_Bagel.GetAbsoluteForward();
            Stick(new Vector2(1f, 0.5f));
            yield return Frames(50);

            Assert.Greater(Vector3.Angle(forward, m_Bagel.GetAbsoluteForward()), 5f);
        }

        // How far it's turned (right, clockwise from above), and how far it's gone forward and right, since the start.
        float Turned(Vector3 forward) => Vector3.SignedAngle(forward, m_Bagel.GetAbsoluteForward(), Vector3.up);
        static float Along(Vector3 moved, Vector3 direction) => Vector3.Dot(moved, direction);

        // Most of the way to its turn speed, over half a second (a little of it's taken getting there, and by the
        // ground's grip).
        float HalfASecondsTurn => m_Bagel.bagelType.turnSpeed * 0.5f * 0.7f;

        [UnityTest]
        public IEnumerator StandingStillItTurnsOnTheSpot()
        {
            var forward = m_Bagel.GetAbsoluteForward();
            Stick(Vector2.right);
            yield return Frames(25);

            Assert.Greater(Turned(forward), HalfASecondsTurn);
            Assert.Less(Vector3.Distance(m_Start, m_Bagel.transform.position), 0.2f, "where it was");
        }

        // Holding forward and right turns it as fast as right alone, though the input's a diagonal.
        [UnityTest]
        public IEnumerator RollingItTurnsJustAsFast()
        {
            var forward = m_Bagel.GetAbsoluteForward();
            Stick(new Vector2(1f, 1f));
            yield return Frames(25);

            Assert.Greater(Turned(forward), HalfASecondsTurn);
        }

        [UnityTest]
        public IEnumerator LettingGoStopsTheTurn()
        {
            var forward = m_Bagel.GetAbsoluteForward();
            Stick(Vector2.right);
            yield return Frames(25);
            Stick(Vector2.zero);
            yield return Frames(10);
            var turned = Turned(forward);
            yield return Frames(25);

            Assert.Less(Mathf.Abs(Turned(forward) - turned), 3f);
        }

        // Back and right goes back and to the right, like a car in reverse: it turns left to get there.
        [UnityTest]
        public IEnumerator BackAndRightGoesBackAndRight()
        {
            var forward = m_Bagel.GetAbsoluteForward();
            var right = Vector3.Cross(Vector3.up, forward);
            Stick(new Vector2(1f, -1f));
            yield return Frames(30);

            var moved = m_Bagel.transform.position - m_Start;
            Assert.Less(Along(moved, forward), -0.3f, "back");
            Assert.Greater(Along(moved, right), 0.3f, "and right");
            Assert.Less(Turned(forward), -20f, "turned left");
        }

        [UnityTest]
        public IEnumerator ForwardAndRightGoesForwardAndRight()
        {
            var forward = m_Bagel.GetAbsoluteForward();
            var right = Vector3.Cross(Vector3.up, forward);
            Stick(new Vector2(1f, 1f));
            yield return Frames(30);

            var moved = m_Bagel.transform.position - m_Start;
            Assert.Greater(Along(moved, forward), 0.3f, "forward");
            Assert.Greater(Along(moved, right), 0.3f, "and right");
            Assert.Greater(Turned(forward), 20f, "turned right");
        }

        [UnityTest]
        public IEnumerator ABagelAtRestKeepsItsToppings()
        {
            yield return Frames(100);

            Assert.AreEqual(state.tracker.toppingsMaxCount, state.tracker.toppingsCount);
            Assert.IsTrue(state.isPlaying);
        }

        [UnityTest]
        public IEnumerator SpinningLosesToppings()
        {
            Stick(Vector2.up);
            yield return Frames(150);

            Assert.Less(state.tracker.toppingsCount, state.tracker.toppingsMaxCount);
        }

        [UnityTest]
        public IEnumerator ReachingTheToasterWins()
        {
            Place(new Vector3(0f, m_Start.y, 24f));
            Stick(Vector2.up);
            for (int i = 0; i < 500 && !state.isGameOver; ++i)
                yield return null;

            Assert.IsTrue(state.isGameOver, $"stopped at {m_Bagel.transform.position}");
            Assert.IsTrue(state.lastRun.won);
            Assert.AreEqual(state.tracker.toppingsCount, state.lastRun.toppings);
        }

        [UnityTest]
        public IEnumerator FallingOffTheLevelLoses()
        {
            Place(new Vector3(20f, m_Start.y, 10f));
            for (int i = 0; i < 300 && !state.isGameOver; ++i)
                yield return null;

            Assert.IsTrue(state.isGameOver);
            Assert.IsFalse(state.lastRun.won);
        }

        [UnityTest]
        public IEnumerator RestartingPutsTheBagelBack()
        {
            Stick(Vector2.up);
            yield return Frames(80);
            Stick(Vector2.zero);

            state.GoToPlay();
            yield return null;

            Assert.Less(Vector3.Distance(m_Start, m_Bagel.transform.position), 0.05f);
            Assert.AreEqual(state.tracker.toppingsMaxCount, state.tracker.toppingsCount);
        }

        // A knock at the end of one game isn't felt at the start of the next.
        [UnityTest]
        public IEnumerator ANewGameForgetsTheLastOnesKnocks()
        {
            Place(m_Start + Vector3.up * 3f);
            m_Body.WakeUp();
            var knock = 0f;
            for (int i = 0; i < 120; i++)
            {
                yield return null;
                knock = Mathf.Max(knock, m_Bagel.currentForce);
            }
            Assert.Greater(knock, 1f, "dropped on the table");
            state.FinishRun();
            yield return null;

            state.GoToPlay();
            Assert.AreEqual(0f, m_Bagel.currentForce, "forgotten");
            yield return Frames(60);
            Assert.AreEqual(state.tracker.toppingsMaxCount, state.tracker.toppingsCount);
        }

        [UnityTest]
        public IEnumerator PausingFreezesTheBagel()
        {
            Stick(Vector2.up);
            yield return Frames(30);

            state.Pause();
            yield return null;
            Assert.AreEqual(0f, Time.timeScale);
            var position = m_Bagel.transform.position;
            yield return Frames(30);
            Assert.AreEqual(position, m_Bagel.transform.position);

            state.Resume();
            yield return Frames(10);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreNotEqual(position, m_Bagel.transform.position);
        }

        [UnityTest]
        public IEnumerator TheStartButtonPauses()
        {
            InputSystem.QueueStateEvent(m_Gamepad, new GamepadState().WithButton(GamepadButton.Start));
            yield return null;
            InputSystem.QueueStateEvent(m_Gamepad, new GamepadState());
            yield return null;

            Assert.IsTrue(state.isPaused);
        }

        // Close to a wall, the wing on that side folds back towards the camera, like a car's mirror: its outer edge
        // comes back, and the other wing stays open.
        [UnityTest]
        public IEnumerator AWingFoldsBackNearAWall()
        {
            var hud = Object.FindAnyObjectByType<BagelHud>();
            var camera = Camera.main.transform;
            var side = Vector3.ProjectOnPlane(hud.transform.right, Vector3.up).normalized;
            float Back(Transform wing) => Vector3.Dot(wing.TransformDirection(Vector3.left), -camera.forward);
            var openBack = Back(hud.leftWing);

            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                wall.transform.SetPositionAndRotation(hud.transform.position - side * 1f, Quaternion.LookRotation(side));
                wall.transform.localScale = new Vector3(3f, 3f, 0.2f);
                yield return Frames(90);

                Assert.Greater(hud.leftFold, 10f, "the left wing folds");
                Assert.Less(hud.rightFold, 1f, "the right wing stays open");
                Assert.Greater(Back(hud.leftWing), openBack + 0.1f, "its outer edge comes back towards the camera");
            }
            finally
            {
                Object.Destroy(wall);
            }
        }
    }
}
