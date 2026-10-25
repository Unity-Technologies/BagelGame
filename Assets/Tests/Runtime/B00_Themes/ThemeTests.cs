using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B00_Themes
{
    public class ThemeTests : UITestFixture
    {
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
            BagelThemes.RestorePanelSettings();
        }

        VisualElement ShowWithTheme(VisualTreeAsset uxml, ThemeStyleSheet theme)
        {
            rootVisualElement.Clear();
            var screen = uxml.Instantiate();
            screen.style.flexGrow = 1;
            screen.styleSheets.Add(theme);
            rootVisualElement.Add(screen);
            simulate.FrameUpdate();
            return screen;
        }

        [Test]
        public void EveryThemeStylesTheScreens()
        {
            foreach (var entry in m_BagelTestAssetList.themes.collection)
            {
                var screen = ShowWithTheme(m_BagelTestAssetList.mainMenuUxml, entry.theme);
                var playButton = screen.Q<Button>("play-button");
                var style = playButton.resolvedStyle;

                // A font asset: its weights and fallbacks (Arabic, Chinese) come with it.
                Assert.AreEqual("DynaPuff-Medium SDF", style.unityFontDefinition.fontAsset?.name, $"{entry.themeName}: the font comes from --b-font");
                Assert.AreNotEqual(style.backgroundColor, style.color, $"{entry.themeName}: text and background differ");

                // The main menu's words are chalked on the board, no boxes; buttons elsewhere have a border.
                screen = ShowWithTheme(m_BagelTestAssetList.pausePaneUxml, entry.theme);
                Assert.Greater(screen.Q<Button>("resume-button").resolvedStyle.borderTopWidth, 0f, $"{entry.themeName}: buttons have a border");
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void FocusIsVisibleInEveryTheme()
        {
            foreach (var entry in m_BagelTestAssetList.themes.collection)
            {
                var screen = ShowWithTheme(m_BagelTestAssetList.pausePaneUxml, entry.theme);
                var resumeButton = screen.Q<Button>("resume-button");
                var settingsButton = screen.Q<Button>("settings-button");

                // Buttons ease into their focused look.
                resumeButton.Focus();
                simulate.FrameUpdate();
                simulate.FrameUpdate(0.5f);

                Assert.AreEqual(resumeButton, resumeButton.focusController.focusedElement, entry.themeName);

                Assert.AreNotEqual(settingsButton.resolvedStyle.borderTopColor, resumeButton.resolvedStyle.borderTopColor,
                    $"{entry.themeName}: the focused button stands out");
            }
        }

        [Test]
        public void SwitchingThemesPutsThePanelSettingsBack()
        {
            var panelSettings = m_BagelTestAssetList.panelSettings.collection;
            var originals = new List<ThemeStyleSheet>();
            foreach (var settings in panelSettings)
                originals.Add(settings.themeStyleSheet);

            var themes = m_BagelTestAssetList.themes.collection;
            BagelThemes.ApplyToPanelSettings(m_BagelTestAssetList.panelSettings, themes[2].theme);
            BagelThemes.ApplyToPanelSettings(m_BagelTestAssetList.panelSettings, themes[3].theme);
            foreach (var settings in panelSettings)
                Assert.AreEqual(themes[3].theme, settings.themeStyleSheet);

            BagelThemes.RestorePanelSettings();
            for (int i = 0; i < panelSettings.Count; ++i)
                Assert.AreEqual(originals[i], panelSettings[i].themeStyleSheet);
        }
    }
}
