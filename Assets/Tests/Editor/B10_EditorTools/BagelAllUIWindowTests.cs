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

            var sources = new[] { "Assets/Scenes/Main.unity", "Assets/Game/Bagels/Tracker/BagelTracker.prefab", "Assets/Game/States/B05_BagelSelection/BagelSelectionPodium.prefab", "Assets/Game/Levels/Breakfast/Breakfast.prefab" };
            foreach (var source in sources)
            {
                foreach (Match match in Regex.Matches(File.ReadAllText(source), @"sourceAsset: \{fileID: \d+, guid: (\w+)"))
                {
                    var guid = match.Groups[1].Value;
                    Assert.IsTrue(shown.Contains(guid), $"{AssetDatabase.GUIDToAssetPath(guid)} ({source}) has no card");
                }
            }
        }

        // Every screen shows at once, none dimmed: a setup stages a moment of the game instead.
        [Test]
        public void SetupsStageTheGameAndEveryCardShows()
        {
            Assert.IsTrue(window.state.isMainMenu);
            Assert.IsNull(rootVisualElement.Q("state-strip"));

            window.ApplySetup("Game Over/Win");
            simulate.FrameUpdate();
            Assert.IsTrue(window.state.isGameOver);
            foreach (var card in window.cards)
                Assert.AreEqual(1f, card.root.resolvedStyle.opacity, card.spec.title);

            window.ApplySetup("Pause/Settings");
            Assert.IsTrue(window.state.isPaused);
            Assert.AreEqual(PlayManagerState.PausePane.Settings, window.state.pausePane);
        }

        [Test]
        public void TheThemeAndLanguageStripsRestyleEveryCard()
        {
            var themes = AssetDatabase.LoadAssetAtPath<BagelThemeCollection>("Assets/UI/Runtime/B00_Themes/T0_Common/BagelThemeCollection.asset");
            Assert.AreEqual(themes.collection.Count, window.themeStrip.childCount);

            simulate.Click(rootVisualElement.Q("theme-daylight"));
            simulate.FrameUpdate();
            Assert.AreEqual(2, window.state.settings.themeIndex);
            Assert.AreEqual(themes.collection[2].theme, window.currentTheme);
            Assert.AreEqual(2, window.themeStrip.selectedIndex);

            try
            {
                simulate.Click(rootVisualElement.Q("language-fr"));
                simulate.FrameUpdate();
                Assert.AreEqual("fr", BagelLocalization.localeCode);
                Assert.AreEqual(window.languageStrip.IndexOf(rootVisualElement.Q("language-fr")), window.languageStrip.selectedIndex);
                Assert.AreEqual("Jouer", Card("Main Menu").root.Q<Button>("play-button").text);
            }
            finally
            {
                BagelToolbar.SelectLanguage("en");
            }
            Assert.AreEqual(window.languageStrip.IndexOf(rootVisualElement.Q("language-en")), window.languageStrip.selectedIndex);
        }

        [Test]
        public void ResetStartsAgainFromTheStartState()
        {
            window.ApplySetup("Game Over/Lose");
            window.state.settings.themeIndex = 3;
            simulate.Click(rootVisualElement.Q("roll-toggle"));
            simulate.FrameUpdate();
            Assert.IsTrue(BagelOptionStrip.IsOn(rootVisualElement.Q("roll-toggle")));

            simulate.Click(rootVisualElement.Q("reset-button"));
            simulate.FrameUpdate();

            Assert.IsTrue(window.state.isMainMenu);
            Assert.AreEqual(0, window.state.settings.themeIndex);
            Assert.AreEqual(0, window.themeStrip.selectedIndex);
            Assert.IsFalse(BagelOptionStrip.IsOn(rootVisualElement.Q("roll-toggle")));
        }

        [Test]
        public void TheDemoResetPutsTheWindowBack()
        {
            window.SetZoom(0.7f);
            simulate.Click(rootVisualElement.Q("inspector-toggle"));
            window.ApplySetup("Tracker/Wrecked");
            BagelToolbar.SelectTheme(2);
            simulate.FrameUpdate();
            Assert.IsTrue(rootVisualElement.Q("inspector-dock").ClassListContains("b-inspector-dock--shown"));

            DemoReset.ResetProjectStates();
            simulate.FrameUpdate();

            Assert.AreEqual(1f, window.zoom);
            Assert.IsTrue(window.state.isMainMenu);
            Assert.IsFalse(rootVisualElement.Q("inspector-dock").ClassListContains("b-inspector-dock--shown"));
            Assert.IsFalse(BagelOptionStrip.IsOn(rootVisualElement.Q("inspector-toggle")));
            Assert.IsNull(BagelThemes.previewTheme);
            Assert.AreEqual("en", BagelLocalization.localeCode);
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
