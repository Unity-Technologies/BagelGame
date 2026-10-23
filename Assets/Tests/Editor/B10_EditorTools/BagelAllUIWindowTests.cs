using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEditor.UIElements.TestFramework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel.B10_EditorTools
{
    public class BagelAllUIWindowTests
        : EditorWindowUITestFixture<BagelAllUIWindow>
    {
        // The window stays open between tests: each one starts from a fresh state.
        [SetUp]
        public void SetUp()
        {
            panelSize = new Vector2(1600f, 1000f);
            window.ResetState();
            simulate.FrameUpdate();
        }

        BagelAllUIWindow.Card Card(string title) => System.Linq.Enumerable.First(window.cards, card => card.spec.title == title);

        [Test]
        public void MainMenuLeaderboardButtonTest()
        {
            simulate.FrameUpdate();

            var driver = rootVisualElement.Q<MainMenuScreenManager>();
            var mainMenuPane = driver.Q<VisualElement>("main-menu-pane");
            var leaderboardButton = driver.Q<Button>("leaderboard-button");
            var leaderboardPane = driver.Q<VisualElement>("leaderboard-pane");

            simulate.Click(leaderboardButton);
            simulate.FrameUpdate();
            Assert.IsTrue(leaderboardPane.enabledSelf);
            Assert.IsFalse(mainMenuPane.enabledSelf);
        }

        [Test]
        public void EveryScreenInTheGameHasACard()
        {
            // The UXML of every PanelRenderer in the scene and the prefabs it uses.
            var shown = new HashSet<string>();
            foreach (var spec in BagelAllUIWindow.cardSpecs)
                foreach (var path in spec.uxmlPaths)
                    shown.Add(AssetDatabase.AssetPathToGUID("Assets/UI/Runtime/" + path));

            var sources = new[] { "Assets/Scenes/Main.unity", "Assets/Game/Bagels/Tracker/BagelTracker.prefab", "Assets/Game/States/B05_BagelSelection/BagelSelectionPodium.prefab" };
            foreach (var source in sources)
            {
                foreach (Match match in Regex.Matches(File.ReadAllText(source), @"sourceAsset: \{fileID: \d+, guid: (\w+)"))
                {
                    var guid = match.Groups[1].Value;
                    Assert.IsTrue(shown.Contains(guid), $"{AssetDatabase.GUIDToAssetPath(guid)} ({source}) has no card");
                }
            }
        }

        [Test]
        public void TheStateStripSwitchesTheGameState()
        {
            Assert.IsTrue(window.state.isMainMenu);
            Assert.IsFalse(Card("Main Menu").root.ClassListContains("b-card--inactive"));

            var gameOverToggle = rootVisualElement.Q<ToolbarToggle>("state-game-over");
            Assert.IsFalse(gameOverToggle.value);
            gameOverToggle.value = true;
            simulate.FrameUpdate();

            Assert.IsTrue(window.state.isGameOver);
            Assert.IsTrue(Card("Main Menu").root.ClassListContains("b-card--inactive"));
            Assert.IsFalse(Card("Game Over").root.ClassListContains("b-card--inactive"));
            Assert.IsTrue(rootVisualElement.Q<ToolbarToggle>("state-game-over").value);
            Assert.IsFalse(rootVisualElement.Q<ToolbarToggle>("state-main-menu").value);
        }

        [Test]
        public void EveryCardPlaysOnTheSameState()
        {
            simulate.FrameUpdate();
            window.ApplySetup("Game Over/Win");
            simulate.FrameUpdate();

            var gameOver = Card("Game Over").root.Q<GameOverPaneManager>();
            var leaderboard = Card("Leaderboard").root.Q<LeaderboardManager>();
            Assert.AreEqual(window.state, gameOver.state);
            Assert.AreEqual(window.state, leaderboard.state);

            var count = leaderboard.leaderboardTable.itemsSource.Count;
            gameOver.addToLeaderboardNameField.value = "Window";
            rootVisualElement.Q<ScrollView>("stage").ScrollTo(gameOver.addToLeaderboardButton);
            simulate.FrameUpdate();
            simulate.Click(gameOver.addToLeaderboardButton);
            simulate.FrameUpdate();

            Assert.AreEqual(count + 1, leaderboard.leaderboardTable.itemsSource.Count);
        }

        [Test]
        public void ThemesRestyleEveryCard()
        {
            simulate.FrameUpdate();
            var themes = AssetDatabase.LoadAssetAtPath<BagelThemeCollection>("Assets/UI/Runtime/B00_Themes/T0_Common/BagelThemeCollection.asset");

            window.state.settings.themeIndex = 3;
            simulate.FrameUpdate();

            Assert.AreEqual(themes.collection[3].theme, window.currentTheme);
            foreach (var card in window.cards)
                foreach (var screen in card.screens)
                    Assert.IsTrue(screen.styleSheets.Contains(themes.collection[3].theme), card.spec.title);
        }

        [Test]
        public void NothingInTheProjectChanges()
        {
            var assets = new List<Object>
            {
                AssetDatabase.LoadAssetAtPath<PlayManagerState>("Assets/Game/Core/PlayManagerState.asset"),
                AssetDatabase.LoadAssetAtPath<BagelThemeCollection>("Assets/UI/Runtime/B00_Themes/T0_Common/BagelThemeCollection.asset"),
                AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/Runtime/B01_PanelSettings/ScreenSpacePanelSettings.asset"),
                AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/Runtime/B01_PanelSettings/WorldSpacePanelSettings.asset"),
            };
            var before = assets.ConvertAll(asset => EditorJsonUtility.ToJson(asset));
            var sceneWasDirty = EditorSceneManager.GetActiveScene().isDirty;

            simulate.FrameUpdate();
            foreach (var setup in BagelAllUIWindow.setupNames)
            {
                window.ApplySetup(setup);
                simulate.FrameUpdate();
            }
            window.state.settings.themeIndex = 2;
            window.state.settings.fieldOfView = 75f;
            window.state.SubmitToLeaderboard("Nobody");
            simulate.FrameUpdate();

            for (int i = 0; i < assets.Count; ++i)
                Assert.AreEqual(before[i], EditorJsonUtility.ToJson(assets[i]), assets[i].name);
            Assert.AreEqual(sceneWasDirty, EditorSceneManager.GetActiveScene().isDirty);
        }
    }
}
