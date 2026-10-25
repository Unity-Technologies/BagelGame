using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B04_Settings
{
    public class SettingsTests : UITestFixture
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
        }

        // A card for each theme, in its own colors: picking one picks the theme.
        [Test]
        public void SettingsThemeCards()
        {
            // The whole window: the cards' looks are in its styles.
            panelSize = new Vector2(800f, 500f);
            m_BagelTestAssetList.settingsPaneUxml.CloneTree(rootVisualElement);
            rootVisualElement.Q<TabView>().activeTab = rootVisualElement.Q<Tab>("ui-settings-tab");
            simulate.FrameUpdate();
            simulate.FrameUpdate();

            var manager = rootVisualElement.Q<SettingsPaneManagerForUI>();
            var themes = manager.bagelThemeCollection.collection;
            Assert.AreEqual(themes.Count, manager.themeChoice.Query<Button>(className: SettingsPaneManagerForUI.themeCardUssClassName).ToList().Count);
            Assert.AreEqual(0, Choice.Selected(manager.themeChoice.value));
            Assert.AreEqual("Classic", manager.GetThemeCard(0).text);

            // Each card's swatch has its own theme on it, so it shows that theme's colors whichever one is on.
            var classicDot = manager.GetThemeCard(0).Q(className: SettingsPaneManagerForUI.themeCardDotUssClassName + "--board");
            var edgyDot = manager.GetThemeCard(1).Q(className: SettingsPaneManagerForUI.themeCardDotUssClassName + "--board");
            Assert.AreNotEqual(classicDot.resolvedStyle.backgroundColor, edgyDot.resolvedStyle.backgroundColor);

            simulate.Click(manager.GetThemeCard(1));
            simulate.FrameUpdate();
            Assert.AreEqual(1, Choice.Selected(manager.themeChoice.value));

            // The pane's own copy of the game state follows the cards.
            Assert.AreEqual(1, manager.state.settings.themeIndex);
        }
    }
}
