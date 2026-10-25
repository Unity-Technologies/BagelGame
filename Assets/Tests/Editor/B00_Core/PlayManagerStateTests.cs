using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel.B00_Core
{
    // The game's rules, without any UI.
    public class PlayManagerStateTests
    {
        BagelTestAssetList m_BagelTestAssetList;
        PlayManagerState m_State;
        List<string> m_Notified;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_BagelTestAssetList = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
        }

        [SetUp]
        public void SetUp()
        {
            m_State = PlayManagerState.CreateRuntimeCopy(m_BagelTestAssetList.startState);
            m_Notified = new List<string>();
            m_State.propertyChanged += (_, args) => m_Notified.Add(args.propertyName.ToString());
        }

        [TearDown]
        public void TearDown()
        {
            PlayManagerState.DestroyRuntimeCopy(m_State);
        }

        [Test]
        public void RuntimeCopyLeavesTheStartStateAlone()
        {
            var template = m_BagelTestAssetList.startState;
            var entries = template.leaderboard.count;

            m_State.settings.fieldOfView = 72f;
            m_State.leaderboard.Add("Tester", 12);

            Assert.AreEqual(60f, template.settings.fieldOfView);
            Assert.AreEqual(entries, template.leaderboard.count);
            Assert.AreEqual(entries + 1, m_State.leaderboard.count);
        }

        [Test]
        public void GoToPlayResetsTheTrackerToTheSelectedBagel()
        {
            m_State.SelectBagel(1);
            m_State.tracker.toppingsCount = 3;

            m_State.GoToPlay();

            Assert.IsTrue(m_State.isPlaying);
            Assert.AreEqual(m_State.selectedBagelType.maxToppingCount, m_State.tracker.toppingsCount);
            Assert.AreEqual(m_State.selectedBagelType.maxToppingCount, m_State.tracker.toppingsMaxCount);
        }

        [Test]
        public void PauseOnlyWhilePlaying()
        {
            m_State.GoToMainMenu();
            m_State.Pause();
            Assert.IsFalse(m_State.isPaused);

            m_State.GoToPlay();
            m_State.Pause();
            Assert.IsTrue(m_State.isPaused);

            m_State.TogglePause();
            Assert.IsFalse(m_State.isPaused);
        }

        [Test]
        public void LeavingPlayResumesBeforeTheStateChanges()
        {
            var events = new List<string>();
            m_State.onPauseStateChanged += (_, paused) => events.Add(paused ? "paused" : "resumed");
            m_State.onStateChange += (_, state) => events.Add(state.ToString());

            m_State.GoToPlay();
            m_State.Pause();
            m_State.pausePane = PlayManagerState.PausePane.Settings;
            m_State.GoToMainMenu();

            CollectionAssert.AreEqual(new[] { "Playing", "paused", "resumed", "MainMenu" }, events);
            Assert.IsFalse(m_State.isPaused);
        }

        [Test]
        public void PausingAlwaysOpensThePausePane()
        {
            m_State.GoToPlay();
            m_State.Pause();
            m_State.pausePane = PlayManagerState.PausePane.Settings;
            m_State.Resume();

            m_State.Pause();

            Assert.AreEqual(PlayManagerState.PausePane.Pause, m_State.pausePane);
        }

        [Test]
        public void MainMenuPanesOnlyChangeInTheMainMenu()
        {
            var modes = new List<PlayManagerState.MainMenuPaneMode>();
            m_State.onMainMenuPaneModeChange += (_, mode) => modes.Add(mode);

            m_State.GoToBagelSelection();
            m_State.SetMainMenuPane(PlayManagerState.MainMenuPane.Settings);
            Assert.AreEqual(PlayManagerState.MainMenuPane.Main, m_State.mainMenuPane);

            m_State.GoToMainMenu();
            m_State.SetMainMenuPane(PlayManagerState.MainMenuPane.Settings);
            m_State.SetMainMenuPane(PlayManagerState.MainMenuPane.Leaderboard);
            m_State.SetMainMenuPane(PlayManagerState.MainMenuPane.Main);

            // The camera only moves when going between the main pane and a secondary one.
            CollectionAssert.AreEqual(new[]
            {
                PlayManagerState.MainMenuPaneMode.Secondary,
                PlayManagerState.MainMenuPaneMode.Primary
            }, modes);
        }

        // The bagels go round, both ways; picking one by number stays in range.
        [Test]
        public void BagelSelectionGoesRound()
        {
            var count = m_State.bagelTypeCount;
            Assert.Greater(count, 1);
            Assert.AreEqual(0, m_State.selectedBagelIndex);
            Assert.IsTrue(m_State.canSelectPreviousBagel);
            Assert.IsTrue(m_State.canSelectNextBagel);

            m_State.SelectPreviousBagel();
            Assert.AreEqual(count - 1, m_State.selectedBagelIndex, "before the first is the last");
            Assert.AreEqual(m_State.bagelTypes.collection[count - 1], m_State.selectedBagelType);

            for (int i = 0; i < count; ++i)
                m_State.SelectNextBagel();
            Assert.AreEqual(count - 1, m_State.selectedBagelIndex, "all the way round");
            m_State.SelectNextBagel();
            Assert.AreEqual(0, m_State.selectedBagelIndex, "after the last is the first");

            m_State.SelectBagel(count + 5);
            Assert.AreEqual(count - 1, m_State.selectedBagelIndex);
            m_State.SelectBagel(-3);
            Assert.AreEqual(0, m_State.selectedBagelIndex);
        }

        [Test]
        public void ARunTakesItsTimeAndTheLeaderboardKeepsIt()
        {
            m_State.GoToPlay();
            var tracker = m_State.tracker;

            // However long it takes, a run with toppings left is won: it's time against the toppings lost.
            tracker.elapsed = 600f;
            m_State.FinishRun();
            Assert.IsTrue(m_State.lastRun.won);
            Assert.AreEqual("gameover.win", m_State.lastRun.titleKey);
            Assert.AreEqual(600f, m_State.lastRun.time, 1e-3f);

            m_State.SubmitToLeaderboard("Slowpoke");
            var entry = m_State.leaderboard.entries[m_State.leaderboard.lastAddedIndex];
            Assert.AreEqual(m_State.lastRun.toppings, entry.toppings);
            Assert.AreEqual(600f, entry.time, 1e-3f);

            // A new run's clock starts again.
            m_State.GoToPlay();
            Assert.AreEqual(0f, m_State.tracker.elapsed);
        }

        [Test]
        public void FinishRunKeepsASnapshot()
        {
            m_State.GoToPlay();
            m_State.tracker.toppingsCount = 321;

            m_State.FinishRun();
            m_State.tracker.toppingsCount = 1;

            Assert.IsTrue(m_State.isGameOver);
            Assert.AreEqual(321, m_State.lastRun.toppings);
            Assert.IsTrue(m_State.lastRun.won);
            Assert.AreEqual("gameover.win", m_State.lastRun.titleKey);
            Assert.AreEqual(m_State.selectedBagelType.nameKey, m_State.lastRun.bagelNameKey);
            Assert.AreEqual(m_State.selectedBagelType.displayName, m_State.lastRun.bagelName);
        }

        [Test]
        public void ARunWithoutToppingsIsLost()
        {
            m_State.GoToPlay();
            m_State.tracker.toppingsCount = 0;

            m_State.FinishRun();

            Assert.IsFalse(m_State.lastRun.won);
            Assert.AreEqual("gameover.lose", m_State.lastRun.titleKey);
            Assert.IsFalse(m_State.lastRun.canSubmit);
        }

        [Test]
        public void ARunGoesOnTheLeaderboardOnce()
        {
            var count = m_State.leaderboard.count;
            m_State.GoToPlay();
            m_State.tracker.toppingsCount = 500;
            m_State.FinishRun();

            m_State.SubmitToLeaderboard("  Tester  ");
            m_State.SubmitToLeaderboard("Tester again");

            Assert.AreEqual(count + 1, m_State.leaderboard.count);
            var entry = m_State.leaderboard.entries[m_State.leaderboard.lastAddedIndex];
            Assert.AreEqual("Tester", entry.playerName);
            Assert.AreEqual(500, entry.toppings);
            Assert.IsTrue(m_State.lastRun.submitted);

            // The next run can be submitted again.
            m_State.GoToPlay();
            m_State.FinishRun();
            Assert.IsTrue(m_State.lastRun.canSubmit);
        }

        [Test]
        public void FieldOfViewStaysInItsRange()
        {
            m_State.settings.fieldOfView = 500f;
            Assert.AreEqual(m_State.settings.fieldOfViewMax, m_State.settings.fieldOfView);

            m_State.settings.fieldOfView = -5f;
            Assert.AreEqual(m_State.settings.fieldOfViewMin, m_State.settings.fieldOfView);
        }

        [Test]
        public void ChangesNotifyTheirPathAndNewVersions()
        {
            var version = m_State.GetViewHashCode();

            m_State.tracker.spin = 4f;
            m_State.settings.themeIndex = 1;

            CollectionAssert.AreEqual(new[] { "tracker.spin", "settings.themeIndex" }, m_Notified);
            Assert.AreNotEqual(version, m_State.GetViewHashCode());

            // Setting the same value again changes nothing.
            version = m_State.GetViewHashCode();
            m_State.tracker.spin = 4f;
            Assert.AreEqual(version, m_State.GetViewHashCode());
            Assert.AreEqual(2, m_Notified.Count);
        }

        [Test]
        public void SaveRoundTrip()
        {
            m_State.settings.fieldOfView = 70f;
            m_State.settings.themeIndex = 1;
            m_State.settings.sound = false;
            m_State.settings.soundVolume = 0.25f;
            m_State.leaderboard.Add("Saved", 77);
            var json = JsonUtility.ToJson(m_State.ToSaved());

            var loaded = PlayManagerState.CreateRuntimeCopy(m_BagelTestAssetList.startState);
            try
            {
                loaded.Load(JsonUtility.FromJson<PlayManagerState.Saved>(json));

                Assert.AreEqual(70f, loaded.settings.fieldOfView);
                Assert.AreEqual(1, loaded.settings.themeIndex);
                Assert.IsFalse(loaded.settings.sound);
                Assert.AreEqual(0.25f, loaded.settings.soundVolume);
                Assert.AreEqual(m_State.leaderboard.count, loaded.leaderboard.count);
                Assert.AreEqual("Saved", loaded.leaderboard.entries[loaded.leaderboard.count - 1].playerName);
            }
            finally
            {
                PlayManagerState.DestroyRuntimeCopy(loaded);
            }
        }

        // A save from before the sound settings existed keeps the sound on.
        [Test]
        public void AnOlderSaveKeepsTheSoundOn()
        {
            var saved = JsonUtility.FromJson<PlayManagerState.Saved>("{\"version\":1,\"fieldOfView\":65.0,\"themeIndex\":2}");
            m_State.Load(saved);

            Assert.IsTrue(m_State.settings.sound);
            Assert.AreEqual(0.8f, m_State.settings.soundVolume);
            Assert.AreEqual(2, m_State.settings.themeIndex);
        }
    }
}
