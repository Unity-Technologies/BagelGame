using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B04_Settings
{
    public class SettingsQuickTests : UITestFixture
    {
        BagelTestAssetList m_BagelTestAssetList;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_BagelTestAssetList =
                Resources.Load<BagelTestAssetList>("BagelTestAssetList");
        }

        [Test]
        public void SettingsThemeCards()
        {
            var root = rootVisualElement;
            m_BagelTestAssetList.settingsPaneForUIUxml.CloneTree(root);
            simulate.FrameUpdate();
            simulate.FrameUpdate();
            var manager = rootVisualElement.Q<SettingsPaneManagerForUI>();

            Assert.AreEqual(4, manager.themeChoice.Query<Button>().ToList().Count);
            Assert.AreEqual("Classic", manager.GetThemeCard(0).text);

            simulate.Click(manager.GetThemeCard(1));
            simulate.FrameUpdate();

            // The pane's own copy of the game state follows the cards.
            Assert.AreEqual(1, manager.state.settings.themeIndex);
        }
    }
}
