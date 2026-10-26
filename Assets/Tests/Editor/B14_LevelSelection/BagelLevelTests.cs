using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Bagel.B14_LevelSelection
{
    // The levels as data: the game's own, the library they're set with, where a piece can stand, and the player's own
    // levels, chosen, added, changed, saved and gone, without any UI.
    public class BagelLevelTests
    {
        BagelTestAssetList m_BagelTestAssetList;
        PlayManagerState m_State;
        List<string> m_Notified;

        BagelLevelLibrary library => m_State.levelLibrary;

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
        public void TheTutorialComesFirstThenLongerLevels()
        {
            var levels = m_State.levels;
            Assert.AreEqual(3, levels.Count);
            Assert.AreEqual("breakfast", levels[0].id, "the tutorial first, chosen");
            Assert.AreEqual(levels[0], m_State.selectedLevel);
            Assert.AreEqual(27.5f, levels[0].length, "as long as the counter always was");
            for (int i = 1; i < levels.Count; i++)
            {
                Assert.Greater(levels[i].length, levels[i - 1].length, $"{levels[i].id} is longer");
                Assert.Greater(levels[i].pieceCount, levels[i - 1].pieceCount, $"{levels[i].id} has more on it");
                Assert.IsFalse(levels[i].custom, "the game's own levels aren't the player's to change");
            }
        }

        [Test]
        public void EveryPieceOfTheGamesLevelsIsInTheLibraryAndOnTheCloth()
        {
            var ids = new HashSet<string>();
            foreach (var level in m_State.levels)
            {
                Assert.IsTrue(ids.Add(level.id), $"{level.id} once");
                Assert.IsFalse(string.IsNullOrEmpty(level.nameKey), $"{level.id} has a name in the string table");
                foreach (var piece in level.pieces)
                {
                    var entry = library.Find(piece.kind);
                    Assert.IsNotNull(entry, $"{level.id}: {piece.kind} is in the library");
                    Assert.IsTrue(level.CanPlace(piece.position, entry.HalfExtents(piece.angle)), $"{level.id}: {piece} stands on the cloth, clear of the start and the toaster");
                }
            }
        }

        [Test]
        public void EveryPieceInTheLibraryIsAPrefabStandingOnItsFootprint()
        {
            var ids = new HashSet<string>();
            foreach (var entry in library.entries)
            {
                Assert.IsTrue(ids.Add(entry.id), $"{entry.id} once");
                Assert.IsNotNull(entry.prefab, entry.id);
                Assert.AreEqual(entry.id, entry.prefab.name, "the prefab is named for it");
                Assert.IsFalse(string.IsNullOrEmpty(entry.nameKey), $"{entry.id} has a name in the string table");
                Assert.Greater(entry.size.x, 0f, entry.id);
                Assert.Greater(entry.size.y, 0f, entry.id);

                var root = entry.prefab.transform;
                Assert.AreEqual(Vector3.zero, root.localPosition, $"{entry.id}'s root is the middle of its footprint");
                Assert.AreEqual(Quaternion.identity, root.localRotation, $"{entry.id} is unturned");
                Assert.AreEqual(entry.course, entry.prefab.GetComponentInChildren<TableItem>() == null, $"{entry.id}: the course has no table things, breakfast has");

                var contents = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(entry.prefab));
                try
                {
                    var renderers = contents.GetComponentsInChildren<MeshRenderer>();
                    Assert.Greater(renderers.Length, 0, entry.id);
                    foreach (var renderer in renderers)
                    {
                        var center = renderer.bounds.center;
                        Assert.LessOrEqual(Mathf.Abs(center.x), entry.size.x * 0.5f + 0.5f, $"{entry.id}: {renderer.name} is over its footprint");
                        Assert.LessOrEqual(Mathf.Abs(center.z), entry.size.y * 0.5f + 0.5f, $"{entry.id}: {renderer.name} is over its footprint");
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
        }

        [Test]
        public void PiecesStandOnTheClothClearOfTheStartAndTheToaster()
        {
            var level = m_State.selectedLevel;
            var half = new Vector2(0.5f, 0.5f);
            Assert.IsTrue(level.CanPlace(new Vector2(3f, 5f), half));
            Assert.IsFalse(level.CanPlace(Vector2.zero, half), "the bagel starts there");
            Assert.IsFalse(level.CanPlace(new Vector2(0.9f, 0.9f), half), "too near the start");
            Assert.IsFalse(level.CanPlace(new Vector2(5.4f, 5f), half), "off the cloth's side");
            Assert.IsFalse(level.CanPlace(new Vector2(0f, BagelLevel.clothStart), half), "off the cloth's start");
            Assert.IsFalse(level.CanPlace(new Vector2(0f, level.length - 1f), half), "on the toaster");
            Assert.IsTrue(level.CanPlace(new Vector2(4f, level.length), half), "beside the toaster");
            Assert.IsFalse(level.CanPlace(new Vector2(4f, level.length + BagelLevel.clothBeyond), half), "off the cloth's end");

            Assert.AreEqual(new Vector2(1.5f, -2f), BagelLevel.Snap(new Vector2(1.6f, -2.2f)), "on the gingham's half-meter grid");
        }

        [Test]
        public void TurnedPiecesTakeTheirFootprintRound()
        {
            var platform = library.Find("Platform");
            Assert.AreEqual(new Vector2(0.5f, 2.5f), platform.HalfExtents(0f));
            var across = platform.HalfExtents(90f);
            Assert.AreEqual(2.5f, across.x, 1e-4f, "turned a quarter, it lies across the counter");
            Assert.AreEqual(0.5f, across.y, 1e-4f);

            var piece = new BagelLevelPiece("Platform", 0f, 10f, 90f);
            Assert.IsTrue(platform.Contains(piece, new Vector2(2f, 10f)));
            Assert.IsFalse(platform.Contains(piece, new Vector2(0f, 12f)));

            var jam = library.Find("JamJar");
            Assert.IsTrue(jam.round);
            var jar = new BagelLevelPiece("JamJar", 1f, 1f, 30f);
            Assert.IsTrue(jam.Contains(jar, new Vector2(1.4f, 1f)));
            Assert.IsFalse(jam.Contains(jar, new Vector2(1.45f, 1.45f)), "round: not in its box's corner");
        }

        [Test]
        public void LevelsGoUpAndDownTheListWithoutGoingRound()
        {
            Assert.IsFalse(m_State.canSelectPreviousLevel);
            m_State.SelectPreviousLevel();
            Assert.AreEqual(0, m_State.selectedLevelIndex, "the first stays");

            BagelLevel chosen = null;
            m_State.onSetLevel += (_, level) => chosen = level;
            m_State.SelectNextLevel();
            Assert.AreEqual(1, m_State.selectedLevelIndex);
            Assert.AreEqual(m_State.levels[1], chosen);
            CollectionAssert.Contains(m_Notified, nameof(PlayManagerState.selectedLevel));

            m_State.SelectLevel(m_State.levelCount - 1);
            Assert.IsFalse(m_State.canSelectNextLevel);
            m_State.SelectNextLevel();
            Assert.AreEqual(m_State.levelCount - 1, m_State.selectedLevelIndex, "the last stays");
        }

        [Test]
        public void OnlyThePlayersOwnLevelsAreEditedAndOnlyWhileChoosing()
        {
            m_State.GoToBagelSelection();
            m_State.GoToLevelSelection();
            Assert.IsTrue(m_State.isLevelSelection);
            m_State.EditLevel(true);
            Assert.IsFalse(m_State.isEditingLevel, "the game's own levels are edited in the Editor");

            var level = m_State.AddLevel("Mine");
            Assert.IsTrue(level.custom);
            Assert.AreEqual(level, m_State.selectedLevel, "a new level is chosen");
            Assert.AreEqual(BagelLevel.newLength, level.length);
            Assert.AreEqual(0, level.pieceCount, "empty but for the toaster");
            m_State.EditLevel(true);
            Assert.IsTrue(m_State.isEditingLevel);

            m_State.SelectLevel(0);
            Assert.IsFalse(m_State.isEditingLevel, "choosing a level of the game's stops editing");

            m_State.SelectLevel(m_State.levelCount - 1);
            m_State.EditLevel(true);
            m_State.GoToPlay();
            Assert.IsFalse(m_State.isEditingLevel, "playing stops editing");
        }

        [Test]
        public void AddedLevelsAreNumberedAndDeletedOnesGo()
        {
            var first = m_State.AddLevel("One");
            var second = m_State.AddLevel("Two");
            Assert.AreEqual(PlayManagerState.customLevelPrefix + "1", first.id);
            Assert.AreEqual(PlayManagerState.customLevelPrefix + "2", second.id);
            Assert.AreEqual(5, m_State.levelCount);
            Assert.AreEqual(4, m_State.selectedLevelIndex);

            m_State.DeleteLevel(m_State.levels[0]);
            Assert.AreEqual(5, m_State.levelCount, "the game's own levels stay");

            m_State.DeleteLevel(second);
            Assert.AreEqual(4, m_State.levelCount);
            Assert.AreEqual(first, m_State.selectedLevel, "the one before is chosen");
            Assert.IsTrue(second == null, "gone for good");
            Assert.AreEqual(PlayManagerState.customLevelPrefix + "2", m_State.AddLevel("Again").id, "numbered after the highest left");
        }

        [Test]
        public void ThePlayersLevelsAndTheChosenOneAreSaved()
        {
            var level = m_State.AddLevel("Long Table");
            level.length = 50f;
            level.Add(new BagelLevelPiece("Ramp", 1f, 10f, 90f));
            level.Add(new BagelLevelPiece("SugarCubes", -2f, 20f));
            m_Notified.Clear();
            level.Add(new BagelLevelPiece("Orange", 3f, 30f));
            CollectionAssert.Contains(m_Notified, nameof(PlayManagerState.levels), "a change is saved");

            var json = JsonUtility.ToJson(m_State.ToSaved());
            var loaded = PlayManagerState.CreateRuntimeCopy(m_BagelTestAssetList.startState);
            try
            {
                loaded.Load(JsonUtility.FromJson<PlayManagerState.Saved>(json));
                Assert.AreEqual(4, loaded.levelCount);
                var copy = loaded.selectedLevel;
                Assert.AreEqual(level.id, copy.id, "the chosen level is chosen again");
                Assert.IsTrue(copy.custom);
                Assert.AreEqual("Long Table", copy.displayName);
                Assert.AreEqual(50f, copy.length);
                CollectionAssert.AreEqual(level.pieces, copy.pieces);
            }
            finally
            {
                PlayManagerState.DestroyRuntimeCopy(loaded);
            }
        }

        [Test]
        public void ARunGoesOnItsLevelsLeaderboard()
        {
            var tutorialRuns = m_State.leaderboard.IndicesFor("breakfast").Count;
            Assert.AreEqual(m_State.leaderboard.count, tutorialRuns, "runs from before levels were on the tutorial's counter");

            m_State.GoToBagelSelection();
            m_State.GoToLevelSelection();
            m_State.SelectLevel(1);
            m_State.GoToPlay();
            m_State.tracker.toppingsCount = 77;
            m_State.FinishRun();
            Assert.AreEqual("brunch", m_State.lastRun.levelId);
            Assert.AreEqual("level.brunch", m_State.lastRun.levelNameKey);
            Assert.AreEqual("brunch", m_State.leaderboard.shownLevel, "the television shows the level just played");

            m_State.SubmitToLeaderboard("Bruncher");
            var entry = m_State.leaderboard.entries[m_State.leaderboard.lastAddedIndex];
            Assert.AreEqual("brunch", entry.level);
            CollectionAssert.AreEqual(new[] { m_State.leaderboard.lastAddedIndex }, m_State.leaderboard.IndicesFor("brunch"));
            Assert.AreEqual(tutorialRuns, m_State.leaderboard.IndicesFor("breakfast").Count, "not on the tutorial's board");

            // From the main menu, the leaderboard opens on the chosen level's board, and goes through them.
            m_State.SelectLevel(2);
            m_State.GoToMainMenu();
            m_State.SetMainMenuPane(PlayManagerState.MainMenuPane.Leaderboard);
            Assert.AreEqual("banquet", m_State.leaderboard.shownLevel);
            Assert.IsFalse(m_State.canShowNextLeaderboardLevel);
            m_State.ShowLeaderboardLevel(-1);
            Assert.AreEqual(m_State.levels[1], m_State.leaderboardLevel);
            m_State.ShowLeaderboardLevel(-5);
            Assert.AreEqual(m_State.levels[0], m_State.leaderboardLevel, "no further than the first");
        }

        [Test]
        public void APlayersLevelsBoardGoesWithIt()
        {
            var level = m_State.AddLevel("Mine");
            m_State.leaderboard.Add("Me", 100, 30f, level.id);
            m_State.leaderboard.Add("Me again", 120, 28f, level.id);
            var count = m_State.leaderboard.count;
            m_State.leaderboard.shownLevel = level.id;

            m_State.DeleteLevel(level);
            Assert.AreEqual(count - 2, m_State.leaderboard.count);
            Assert.AreEqual("breakfast", m_State.leaderboard.shownLevel, "back to the first level's board");
        }

        [Test]
        public void EveryRunsLevelIsSaved()
        {
            var level = m_State.AddLevel("Mine");
            m_State.GoToBagelSelection();
            m_State.GoToLevelSelection();
            m_State.GoToPlay();
            m_State.tracker.toppingsCount = 5;
            m_State.FinishRun();
            m_State.SubmitToLeaderboard("Saver");

            var json = JsonUtility.ToJson(m_State.ToSaved());
            var loaded = PlayManagerState.CreateRuntimeCopy(m_BagelTestAssetList.startState);
            try
            {
                loaded.Load(JsonUtility.FromJson<PlayManagerState.Saved>(json));
                Assert.AreEqual(level.id, loaded.lastRun.levelId);
                Assert.AreEqual("Mine", loaded.lastRun.levelName);
                Assert.AreEqual(1, loaded.leaderboard.IndicesFor(level.id).Count);
            }
            finally
            {
                PlayManagerState.DestroyRuntimeCopy(loaded);
            }
        }

        [Test]
        public void ALevelMadeShorterLosesWhatsOffTheClothOrOnTheToaster()
        {
            var level = m_State.AddLevel("Short");
            level.length = 60f;
            level.Add(new BagelLevelPiece("Orange", 0f, 50f));
            level.Add(new BagelLevelPiece("Orange", 0f, 20f));
            level.Add(new BagelLevelPiece("Orange", 0f, 40.5f));
            level.Add(new BagelLevelPiece("Orange", 4f, 41f));
            level.length = 40f;
            Assert.AreEqual(2, level.pieceCount);
            Assert.AreEqual(20f, level.pieces[0].z);
            Assert.AreEqual(4f, level.pieces[1].x, "beside the toaster stays");

            level.length = 1000f;
            Assert.AreEqual(BagelLevel.maxLength, level.length);
            level.length = 1f;
            Assert.AreEqual(BagelLevel.minLength, level.length, "never shorter than the tutorial");
        }
    }
}
