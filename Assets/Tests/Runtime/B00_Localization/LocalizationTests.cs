using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B00_Localization
{
    public class LocalizationTests : UITestFixture
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
            BagelLocalization.SetLocale("en");
        }

        [TestCase("fr", "Jouer", "Réglages")]
        [TestCase("zh-Hans", "开始", "设置")]
        public void TheMainMenuFollowsTheLanguage(string localeCode, string play, string settings)
        {
            m_BagelTestAssetList.mainMenuUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();
            var mainMenuPaneManager = rootVisualElement.Q<MainMenuPaneManager>();
            Assert.AreEqual("Play", mainMenuPaneManager.playButton.text);

            BagelLocalization.SetLocale(localeCode);
            simulate.FrameUpdate();

            Assert.AreEqual(play, mainMenuPaneManager.playButton.text);
            Assert.AreEqual(settings, mainMenuPaneManager.settingsButton.text);
        }

        [TestCase(1, "Your Sesame bagel kept 1 topping!")]
        [TestCase(345, "Your Sesame bagel kept 345 toppings!")]
        [TestCase(0, "Your Sesame bagel lost every topping.")]
        public void TheGameOverSummaryIsASmartString(int toppings, string summary)
        {
            m_BagelTestAssetList.gameOverPaneUxml.CloneTree(rootVisualElement);
            var gameOverPaneManager = rootVisualElement.Q<GameOverPaneManager>();
            var state = gameOverPaneManager.state;
            state.SelectBagel(0);
            state.GoToPlay();
            state.tracker.toppingsCount = toppings;
            state.FinishRun();
            simulate.FrameUpdate();

            Assert.AreEqual(summary, gameOverPaneManager.summary.text);
        }

        [Test]
        public void TheSummaryFollowsTheLanguage()
        {
            m_BagelTestAssetList.gameOverPaneUxml.CloneTree(rootVisualElement);
            var gameOverPaneManager = rootVisualElement.Q<GameOverPaneManager>();
            var state = gameOverPaneManager.state;
            state.SelectBagel(0);
            state.GoToPlay();
            state.tracker.toppingsCount = 345;
            state.FinishRun();

            BagelLocalization.SetLocale("fr");
            simulate.FrameUpdate();

            Assert.AreEqual("Gagné !", gameOverPaneManager.title.text);
            Assert.AreEqual("Votre bagel Sésame a gardé 345 garnitures !", gameOverPaneManager.summary.text);
        }

        [Test]
        public void ThemeNamesFollowTheLanguage()
        {
            m_BagelTestAssetList.settingsPaneForUIUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();
            var themeDropdown = rootVisualElement.Q<SettingsPaneManagerForUI>().themeDropdown;
            Assert.AreEqual("Classic", themeDropdown.choices[0]);

            BagelLocalization.SetLocale("fr");
            simulate.FrameUpdate();

            Assert.AreEqual("Classique", themeDropdown.choices[0]);
            Assert.AreEqual("Classique", themeDropdown.value);
        }
    }
}
