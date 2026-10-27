using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.UIElements.TestFramework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel.B10_EditorTools
{
    // The Bagel Features window: every feature says what it is and how the game uses it, every line of the
    // project it points at is still there, and every example is the game's UI, live, with the parts that use
    // the feature outlined. The list searches, the tries change the example, and the toolbar's theme and
    // language reach it.
    public class BagelFeaturesWindowTests : EditorWindowUITestFixture<BagelFeaturesWindow>
    {
        // The window stays open between tests: each one starts from a fresh state on the first feature.
        [SetUp]
        public void SetUp()
        {
            panelSize = new Vector2(1400f, 900f);
            window.ResetWindow();
            simulate.FrameUpdate();
        }

        static IEnumerable<BagelFeatureCatalog.Feature> features => BagelFeatureCatalog.features;

        void Show(string featureId)
        {
            window.Select(featureId);
            simulate.FrameUpdate();
            simulate.FrameUpdate();
        }

        Button TryButton(string label) => window.tries.Query<Button>().Where(button => button.text == label).First();

        [Test]
        public void EveryFeatureSaysWhatItIsAndHowTheGameUsesIt()
        {
            var ids = new HashSet<string>();
            foreach (var feature in features)
            {
                Assert.IsTrue(ids.Add(feature.id), $"{feature.id} once");
                Assert.IsNotEmpty(feature.title, feature.id);
                Assert.IsNotEmpty(feature.about, feature.id);
                Assert.IsNotEmpty(feature.usage, feature.id);
                Assert.Greater(feature.sources.Length, 0, $"{feature.id} is somewhere in the project");

                var example = feature.example;
                if (example == null || example.inspector)
                    continue;
                Assert.IsNotNull(example.cardSpec.uxmlPaths, $"{feature.id}: Bagel All UI has a {example.card} card");
                if (example.setup != null)
                    Assert.Contains(example.setup, BagelAllUIWindow.setupNames, $"{feature.id}: a Bagel All UI setup");
                Assert.Greater(example.parts.Length, 0, $"{feature.id} outlines something");
            }
            Assert.GreaterOrEqual(ids.Count, 30);
        }

        [Test]
        public void EverySourceIsWhereItSays()
        {
            foreach (var feature in features)
            {
                foreach (var source in feature.sources)
                {
                    Assert.IsTrue(File.Exists(source.path), $"{feature.id}: {source.path}");
                    Assert.Greater(source.FindLine(), 0, $"{feature.id}: \"{source.find}\" isn't in {source.path} any more");
                    Assert.IsFalse(Path.IsPathRooted(source.path), $"{feature.id}: {source.path} is in the project");
                }
            }
        }

        [Test]
        public void EveryLinkLeadsSomewhere()
        {
            var scene = File.ReadAllText("Assets/Scenes/Main.unity");
            foreach (var feature in features)
            {
                foreach (var link in feature.links)
                {
                    var what = $"{feature.id}: {link.label}";
                    switch (link.kind)
                    {
                        case BagelFeatureCatalog.Link.Kind.UIBuilder:
                            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(link.target), what);
                            break;
                        case BagelFeatureCatalog.Link.Kind.Asset:
                            Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(link.target), what);
                            break;
                        case BagelFeatureCatalog.Link.Kind.GameObject:
                            StringAssert.Contains("m_Name: " + link.target + "\n", scene.Replace("\r", ""), what);
                            break;
                        case BagelFeatureCatalog.Link.Kind.Menu:
                            Assert.IsTrue(Menu.GetEnabled(link.target), what);
                            break;
                    }
                }
            }
        }

        // Each feature's card, at its moment of the game, with at least one part that uses the feature in sight.
        [Test]
        public void EveryExampleShowsTheGamesUIWithItsPartsOutlined()
        {
            foreach (var feature in features)
            {
                var example = feature.example;
                if (example == null || example.inspector)
                    continue;
                Show(feature.id);
                Assert.AreEqual(feature, window.feature);
                Assert.Greater(window.screens.Count, 0, feature.id);
                Assert.AreSame(window.example, window.screens[0].parent, feature.id);
                Assert.Greater(window.parts.Count, 0, $"{feature.id}: its parts are on its card");
                Assert.Greater(window.outlines.childCount, 0, $"{feature.id}: and outlined");
                var scale = window.example.resolvedStyle.scale.value.x;
                Assert.That(scale, Is.GreaterThan(0f).And.LessThanOrEqualTo(BagelFeaturesWindow.maxScale), feature.id);
            }
        }

        [Test]
        public void TheEditorsFeaturesShowTheInspectorOrNoStage()
        {
            Show("property-drawers");
            Assert.IsNotNull(window.example.Q<InspectorElement>(), "the state's Inspector, with its drawers");
            Assert.IsTrue(window.stage.ClassListContains(BagelFeaturesWindow.inspectorStageUssClassName));
            Assert.AreEqual(0, window.outlines.childCount);

            Show("main-toolbar");
            Assert.AreEqual(DisplayStyle.None, window.stage.resolvedStyle.display, "nothing to show live");
            Assert.AreEqual(0, window.example.childCount);
        }

        [Test]
        public void TheSearchNeedsEveryWord()
        {
            var all = features.Count();
            Assert.AreEqual(all, window.shownFeatures.Count);

            window.Search("gradient");
            simulate.FrameUpdate();
            Assert.Contains("gradients", window.shownFeatures.ToList());
            Assert.AreEqual(DisplayStyle.None, window.rows["tab-view"].resolvedStyle.display);

            window.Search("chalk shader");
            Assert.Contains("filters", window.shownFeatures.ToList());
            Assert.IsFalse(window.shownFeatures.Contains("gradients"));

            window.Search("nothing-is-called-this");
            simulate.FrameUpdate();
            Assert.AreEqual(0, window.shownFeatures.Count);
            Assert.AreEqual(DisplayStyle.Flex, rootVisualElement.Q("nothing").resolvedStyle.display);

            window.Search("");
            Assert.AreEqual(all, window.shownFeatures.Count);
        }

        [Test]
        public void ClicksAndArrowsPickFeatures()
        {
            var row = window.rows["multi-column-list-view"];
            rootVisualElement.Q<ScrollView>("list").ScrollTo(row);
            simulate.FrameUpdate();
            simulate.Click(row);
            simulate.FrameUpdate();
            Assert.AreEqual("multi-column-list-view", window.feature.id);
            Assert.IsTrue(row.ClassListContains(BagelFeaturesWindow.selectedRowUssClassName));
            Assert.IsNotNull(window.example.Q<MultiColumnListView>(), "the leaderboard, live");

            simulate.KeyPress(KeyCode.DownArrow);
            simulate.FrameUpdate();
            Assert.AreEqual("tab-view", window.feature.id, "the next one down");
            simulate.KeyPress(KeyCode.UpArrow);
            Assert.AreEqual("multi-column-list-view", window.feature.id);
        }

        [Test]
        public void TriesChangeTheExample()
        {
            Show("multi-column-list-view");
            var leaderboard = window.state.leaderboard;
            var runs = leaderboard.IndicesFor(leaderboard.shownLevel).Count;
            simulate.Click(TryButton("Add a run"));
            simulate.FrameUpdate();
            Assert.AreEqual(runs + 1, leaderboard.IndicesFor(leaderboard.shownLevel).Count);
            Assert.AreEqual(runs + 1, window.example.Q<MultiColumnListView>().itemsSource.Count, "on the board");

            Show("tab-view");
            var tabs = window.example.Q<TabView>();
            var tab = tabs.selectedTabIndex;
            simulate.Click(TryButton("Next tab"));
            Assert.AreNotEqual(tab, tabs.selectedTabIndex);

            Show("transitions");
            simulate.Click(TryButton("Wipe to Settings"));
            Assert.AreEqual(PlayManagerState.MainMenuPane.Settings, window.state.mainMenuPane);
        }

        [Test]
        public void TheThemeStripRestylesTheExample()
        {
            Show("uss-variables");
            var themes = AssetDatabase.LoadAssetAtPath<BagelThemeCollection>(BagelAllUIWindow.themeCollectionPath);
            simulate.Click(rootVisualElement.Q("theme-edgy"));
            simulate.FrameUpdate();
            Assert.AreEqual(1, window.state.settings.themeIndex);
            Assert.AreEqual(themes.collection[1].theme, window.currentTheme);
            Assert.IsTrue(window.screens[0].styleSheets.Contains(themes.collection[1].theme));
            Assert.IsFalse(window.screens[0].styleSheets.Contains(themes.collection[0].theme));

            // The next example comes in the same theme.
            Show("gradients");
            Assert.IsTrue(window.screens[0].styleSheets.Contains(themes.collection[1].theme));
        }

        [Test]
        public void ArabicTurnsTheExampleRightToLeft()
        {
            try
            {
                Show("right-to-left");
                simulate.Click(TryButton("Arabic"));
                simulate.FrameUpdate();
                var pane = window.parts[0];
                Assert.AreEqual(LanguageDirection.RTL, pane.languageDirection);
                Assert.IsTrue(pane.ClassListContains(FollowsLanguage.rtlUssClassName));
            }
            finally
            {
                window.SetLanguage("en");
            }
        }

        [Test]
        public void TheLevelEditorsPicturesAreLive()
        {
            Show("render-texture-images");
            Assert.IsNotNull(window.studio, "the library's things, turning");
            var picture = window.example.Q<BagelLevelEditor>().items[0].Q<Image>();
            Assert.AreEqual(window.studio.texture, picture.image);

            Show("navigation");
            Assert.IsNull(window.studio, "gone with the level editor");
        }

        [Test]
        public void ReadItSaysWhatAScreenReaderIsTold()
        {
            Show("accessibility");
            simulate.Click(TryButton("Read it"));
            simulate.FrameUpdate();
            Assert.AreEqual(DisplayStyle.Flex, window.note.resolvedStyle.display);
            StringAssert.Contains(LevelSelectionScreenManager.LevelName(window.state.levels[0]), window.note.text);

            Show("navigation");
            Assert.IsEmpty(window.note.text, "said for that example only");
        }

        [Test]
        public void ResetStartsAgainOnAFreshState()
        {
            var before = window.state;
            Show("gradients");
            window.NextTheme();
            window.Search("tab");

            window.ResetWindow();
            simulate.FrameUpdate();
            Assert.AreNotSame(before, window.state);
            Assert.AreEqual(features.First(), window.feature);
            Assert.AreEqual(0, window.state.settings.themeIndex);
            Assert.AreEqual(features.Count(), window.shownFeatures.Count);
        }
    }
}
