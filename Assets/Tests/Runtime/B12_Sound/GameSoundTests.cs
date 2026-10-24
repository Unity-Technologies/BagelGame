using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bagel.B12_Sound
{
    // What the game sounds like in the real level: the scene's AudioSource plays the synth, and the
    // game's events play their patches through BagelAudio.
    public class GameSoundTests
    {
        readonly List<string> m_Played = new();
        Gamepad m_Gamepad;
        PlayManager m_PlayManager;
        BagelController m_Bagel;
        Rigidbody m_Body;
        Vector3 m_Start;

        PlayManagerState state => m_PlayManager.state;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            m_Gamepad = InputSystem.AddDevice<Gamepad>("Bagel Test Gamepad");
            Time.captureDeltaTime = Time.fixedDeltaTime;

            var assets = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
            yield return SceneManager.LoadSceneAsync(assets.mainSceneName, LoadSceneMode.Single);
            yield return null;

            m_PlayManager = Object.FindAnyObjectByType<PlayManager>();
            m_Bagel = Object.FindAnyObjectByType<BagelController>(FindObjectsInactive.Include);
            m_Body = m_Bagel.GetComponent<Rigidbody>();
            BagelAudio.played += m_Played.Add;
        }

        [TearDown]
        public void TearDown()
        {
            BagelAudio.played -= m_Played.Add;
            m_Played.Clear();
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
            InputSystem.RemoveDevice(m_Gamepad);
        }

        static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; ++i)
                yield return null;
        }

        IEnumerator Play()
        {
            state.GoToBagelSelection();
            state.GoToPlay();
            yield return null;
            m_Start = m_Bagel.transform.position;
        }

        void Stick(Vector2 value) => InputSystem.QueueStateEvent(m_Gamepad, new GamepadState { leftStick = value });

        void Place(Vector3 position)
        {
            m_Body.position = position;
            m_Body.rotation = Quaternion.identity;
            m_Body.linearVelocity = Vector3.zero;
            m_Body.angularVelocity = Vector3.zero;
            m_Bagel.transform.SetPositionAndRotation(position, Quaternion.identity);
        }

        [UnityTest]
        public IEnumerator TheScenePlaysTheSynth()
        {
            yield return null;

            var source = BagelAudio.source;
            Assert.IsNotNull(source);
            Assert.IsInstanceOf<BagelSynth>(source.generator);
            if (!source.isPlaying)
                Assert.Inconclusive("Audio is off on this machine.");
            Assert.IsTrue(BagelAudio.Play("pop"));
        }

        [UnityTest]
        public IEnumerator GoingToTheBagelsSwishesAndARunStartsWithGo()
        {
            yield return Play();

            Assert.AreEqual(new[] { "swish", "go" }, m_Played);
        }

        [UnityTest]
        public IEnumerator RollingHoldsTheRollUntilPaused()
        {
            yield return Play();
            Assert.AreEqual(0f, BagelAudio.GetDrone(BagelAudio.rollDrone).gain, "standing still");

            // Along the start, before the first ramp throws it in the air.
            Stick(Vector2.up);
            yield return Frames(30);
            Assert.IsTrue(m_Bagel.isGrounded);
            Assert.Greater(BagelAudio.GetDrone(BagelAudio.rollDrone).gain, 0f, "rolling");

            state.Pause();
            yield return null;
            Assert.AreEqual(0f, BagelAudio.GetDrone(BagelAudio.rollDrone).gain, "paused");
            CollectionAssert.Contains(m_Played, "open");

            state.Resume();
            yield return Frames(5);
            CollectionAssert.Contains(m_Played, "close");
        }

        [UnityTest]
        public IEnumerator LandingHardThuds()
        {
            yield return Play();
            m_Played.Clear();

            Place(m_Start + Vector3.up * 3f);
            yield return Frames(80);

            CollectionAssert.Contains(m_Played, "impact");
        }

        [UnityTest]
        public IEnumerator TheToasterDingsAndTheRunIsWon()
        {
            yield return Play();
            m_Played.Clear();

            Place(new Vector3(0f, m_Start.y, 24f));
            Stick(Vector2.up);
            for (int i = 0; i < 500 && !state.isGameOver; ++i)
                yield return null;

            Assert.IsTrue(state.lastRun.won);
            Assert.AreEqual(new[] { "toaster", "win" }, m_Played.FindAll(patch => patch is "toaster" or "win" or "lose"));
        }

        [UnityTest]
        public IEnumerator FallingOffWhistlesAndTheRunIsLost()
        {
            yield return Play();
            m_Played.Clear();

            Place(new Vector3(20f, m_Start.y, 10f));
            for (int i = 0; i < 300 && !state.isGameOver; ++i)
                yield return null;

            Assert.AreEqual(new[] { "fall", "lose" }, m_Played.FindAll(patch => patch is "fall" or "win" or "lose"));
        }

        [UnityTest]
        public IEnumerator TheSoundSettingsMuteAndSetTheVolume()
        {
            yield return Play();

            state.settings.soundVolume = 0.3f;
            Assert.AreEqual(0.3f, BagelAudio.source.volume, 1e-5f);

            state.settings.sound = false;
            Assert.IsFalse(BagelAudio.enabled);
            Assert.IsFalse(BagelAudio.Play("pop"), "muted");

            Stick(Vector2.up);
            yield return Frames(60);
            Assert.AreEqual(0f, BagelAudio.GetDrone(BagelAudio.rollDrone).gain, "muted while rolling");

            state.settings.sound = true;
            Assert.IsTrue(BagelAudio.enabled);
        }
    }
}
