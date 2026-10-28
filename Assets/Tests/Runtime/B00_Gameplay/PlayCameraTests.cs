using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bagel.B00_Gameplay
{
    // The camera behind the bagel starts every game the same way, looking straight at it: the first game, a restart
    // after the bagel's reached the toaster, and a restart from the pause menu halfway down the table. It keeps
    // updating while another camera's live, and the bagel jumps back to the start for a new game: it mustn't carry
    // over where it was looking.
    public class PlayCameraTests
    {
        PlayManager m_PlayManager;
        BagelController m_Bagel;
        Transform m_Tracker;

        PlayManagerState state => m_PlayManager.state;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.captureDeltaTime = Time.fixedDeltaTime;
            var assets = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
            yield return SceneManager.LoadSceneAsync(assets.mainSceneName, LoadSceneMode.Single);
            yield return null;

            m_PlayManager = Object.FindAnyObjectByType<PlayManager>();
            m_Bagel = Object.FindAnyObjectByType<BagelController>(FindObjectsInactive.Include);
            m_Tracker = Object.FindAnyObjectByType<BagelTracker>().transform;
        }

        [TearDown]
        public void TearDown()
        {
            m_PlayManager.playInputBindings.touchMove = Vector2.zero;
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
        }

        // Long enough for the camera to blend from the last one and settle.
        static IEnumerator Seconds(float seconds)
        {
            var end = Time.time + seconds;
            while (Time.time < end)
                yield return null;
        }

        // How high up the screen the bagel is, from the bottom (0) to the top (1).
        float bagelOnScreen => Camera.main.WorldToViewportPoint(m_Tracker.position).y;

        void AssertLookingAtTheBagel(string when)
        {
            Assert.AreEqual(0.5f, bagelOnScreen, 0.05f, $"{when}: the bagel's in the middle of the screen");
        }

        [UnityTest]
        public IEnumerator EveryGameStartsLookingAtTheBagel()
        {
            state.GoToBagelSelection();
            state.GoToPlay();
            yield return Seconds(3f);
            AssertLookingAtTheBagel("the first game");

            // Rolled into the toaster: the game's over, and the television's camera takes over.
            var body = m_Bagel.GetComponent<Rigidbody>();
            var nearTheToaster = m_Bagel.transform.position + new Vector3(0f, 0f, 22f);
            body.position = nearTheToaster;
            m_Bagel.transform.position = nearTheToaster;
            m_PlayManager.playInputBindings.touchMove = Vector2.up;
            for (int i = 0; i < 500 && !state.isGameOver; i++)
                yield return null;
            m_PlayManager.playInputBindings.touchMove = Vector2.zero;
            Assert.IsTrue(state.isGameOver, "reached the toaster");
            yield return Seconds(3f);

            // Restart, from the television.
            state.GoToPlay();
            yield return Seconds(3f);
            AssertLookingAtTheBagel("restarted after the toaster");

            // Halfway down the table, restart from the pause menu.
            m_PlayManager.playInputBindings.touchMove = new Vector2(0.6f, 0.8f);
            yield return Seconds(2f);
            m_PlayManager.playInputBindings.touchMove = Vector2.zero;
            state.Pause();
            yield return null;
            state.Resume();
            state.GoToPlay();
            yield return Seconds(3f);
            AssertLookingAtTheBagel("restarted from the pause menu");
        }
    }
}
