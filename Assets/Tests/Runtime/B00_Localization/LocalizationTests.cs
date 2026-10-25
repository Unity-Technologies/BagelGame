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
        [TestCase("ar", "العب", "الإعدادات")]
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

        // Arabic counts in six forms (zero, one, two, few, many, other) and writes Arabic-Indic digits.
        [TestCase(1, "احتفظ بيغل سمسم الخاص بك بإضافة واحدة!")]
        [TestCase(2, "احتفظ بيغل سمسم الخاص بك بإضافتين!")]
        [TestCase(5, "احتفظ بيغل سمسم الخاص بك بـ٥ إضافات!")]
        [TestCase(1234, "احتفظ بيغل سمسم الخاص بك بـ١٬٢٣٤ إضافةً!")]
        [TestCase(0, "فقد بيغل سمسم الخاص بك كل إضافاته.")]
        public void TheArabicSummaryCountsInArabic(int toppings, string summary)
        {
            m_BagelTestAssetList.gameOverPaneUxml.CloneTree(rootVisualElement);
            var gameOverPaneManager = rootVisualElement.Q<GameOverPaneManager>();
            var state = gameOverPaneManager.state;
            state.SelectBagel(0);
            state.GoToPlay();
            state.tracker.toppingsCount = toppings;
            state.FinishRun();

            BagelLocalization.SetLocale("ar");
            simulate.FrameUpdate();

            Assert.AreEqual(summary, gameOverPaneManager.summary.text);
        }

        [TestCase("en", "1,234", "57%")]
        [TestCase("zh-Hans", "1,234", "57%")]
        [TestCase("ar", "١٬٢٣٤", "٥٧٪")]
        public void NumbersAreWrittenInTheLanguagesDigits(string localeCode, string number, string percent)
        {
            BagelLocalization.SetLocale(localeCode);
            Assert.AreEqual(number, BagelConverters.FormatNumber(1234));

            var fraction = 0.57f;
            Assert.IsTrue(ConverterGroups.TryGetConverterGroup(BagelConverters.percent, out var percentGroup));
            Assert.IsTrue(percentGroup.TryConvert(ref fraction, out string text));
            Assert.AreEqual(percent, text);
        }

        // The table's rows are bound once: they're bound again in the new language's digits.
        [Test]
        public void LeaderboardCountsFollowTheLanguage()
        {
            panelSize = new Vector2(600f, 600f);
            m_BagelTestAssetList.leaderboardPaneUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();
            var table = rootVisualElement.Q<MultiColumnListView>();
            Label Count() => table.GetRootElementForIndex(0).Query<Label>().AtIndex(1);
            Label Time() => table.GetRootElementForIndex(0).Query<Label>().Last();
            StringAssert.IsMatch("^[0-9,]+$", Count().text);
            StringAssert.IsMatch("^[0-9]+:[0-9]{2}[.][0-9]$", Time().text);

            BagelLocalization.SetLocale("ar");
            simulate.FrameUpdate();
            StringAssert.IsMatch("^[٠-٩٬]+$", Count().text);
            StringAssert.IsMatch("^[٠-٩]+:[٠-٩]{2}.[٠-٩]$", Time().text);
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
