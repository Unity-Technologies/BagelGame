using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B00_Layout
{
    // Every screen, in every theme and every language, against the design rules (BagelLayoutAudit).
    public class LayoutAuditTests : UITestFixture
    {
        static readonly string[] s_Locales = { "en", "fr", "zh-Hans", "ar" };

        BagelTestAssetList m_BagelTestAssetList;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_BagelTestAssetList = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
        }

        [TearDown]
        public void TearDown()
        {
            rootVisualElement.Clear();
            BagelLocalization.SetLocale("en");
        }

        // The screens as the game shows them, so every pane has its real width.
        public static readonly string[] screens =
        {
            "Main Menu", "Main Menu: Settings: Game", "Main Menu: Settings: Look", "Main Menu: Settings: Graphics",
            "Main Menu: Settings: Controls", "Main Menu: Settings: About", "Bagel Selection", "Bagel Card",
            "Level Selection", "Level Selection: Editor", "Tracker",
            "Pause", "Pause: Settings: Game", "Pause: Settings: Look",
            "Television", "Television: Leaderboard", "Television: Game Over"
        };

        VisualElement Show(string screenName, ThemeStyleSheet theme)
        {
            var assets = m_BagelTestAssetList;
            var uxml = screenName.Split(':')[0] switch
            {
                "Main Menu" => assets.mainMenuScreenUxml,
                "Bagel Selection" => assets.bagelSelectionControlsUxml,
                "Bagel Card" => assets.bagelSelectionTypeInfoUxml,
                "Level Selection" => assets.levelSelectionScreenUxml,
                "Tracker" => assets.bagelTrackerRightDisplayUxml,
                "Pause" => assets.pauseScreenUxml,
                _ => assets.gameOverScreenUxml,
            };

            rootVisualElement.Clear();
            var screen = uxml.Instantiate();
            screen.styleSheets.Add(theme);
            rootVisualElement.Add(screen);
            var state = screen.Q<BagelPane>()?.state;

            // Put each screen in the state it's audited in: a settings tab, a pause pane, the leaderboard opened from
            // the main menu, a won game.
            if (screenName.StartsWith("Main Menu: Settings"))
                state.SetMainMenuPane(PlayManagerState.MainMenuPane.Settings);
            else if (screenName == "Television: Leaderboard")
                state.SetMainMenuPane(PlayManagerState.MainMenuPane.Leaderboard);
            else if (screenName.StartsWith("Pause"))
            {
                state.GoToPlay();
                state.Pause();
                if (screenName.Contains("Settings"))
                    state.pausePane = PlayManagerState.PausePane.Settings;
            }
            else if (screenName.StartsWith("Level Selection"))
            {
                state.GoToLevelSelection();
                if (screenName.EndsWith("Editor"))
                {
                    var level = state.AddLevel("My Table");
                    foreach (var piece in state.levels[1].pieces)
                        level.Add(piece);
                    state.EditLevel(true);
                    screen.Q<BagelLevelEditor>().ChooseBrush("Orange");
                }
            }
            else if (screenName == "Television: Game Over")
            {
                state.GoToPlay();
                state.tracker.toppingsCount = 345;
                state.FinishRun();
            }

            var tabView = screen.Q<TabView>();
            if (tabView != null)
            {
                var tabName = screenName.Substring(screenName.LastIndexOf(':') + 1).Trim() switch
                {
                    "Look" => "ui-settings-tab",
                    "Graphics" => "graphics-settings-tab",
                    "Controls" => "controls-settings-tab",
                    "About" => "about-settings-tab",
                    _ => "game-settings-tab"
                };
                tabView.activeTab = screen.Q<Tab>(tabName);
            }

            simulate.FrameUpdate();
            simulate.FrameUpdate();
            return screen;
        }

        [Test]
        public void EveryScreenFollowsTheDesignRules([ValueSource(nameof(screens))] string screenName)
        {
            panelSize = new Vector2(1500f, 900f);
            var problems = new List<string>();

            foreach (var locale in s_Locales)
            {
                BagelLocalization.SetLocale(locale);
                foreach (var theme in m_BagelTestAssetList.themes.collection)
                {
                    var screen = Show(screenName, theme.theme);
                    foreach (var problem in BagelLayoutAudit.Check(screen))
                        problems.Add($"[{theme.themeName}, {locale}] {problem}");
                }
            }

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
