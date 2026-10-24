using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B04_Settings
{
    public class SettingsQuickTests : UITestFixture
    {
        BagelTestAssetList m_BagelTestAssetList;
        PopupMenuSimulator m_PopupMenuSimulator;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_BagelTestAssetList =
                Resources.Load<BagelTestAssetList>("BagelTestAssetList");

            m_PopupMenuSimulator = AddTestComponent<PopupMenuSimulator>();
        }

        [Test]
        public void SettingsThemeDropdown()
        {
            var root = rootVisualElement;
            m_BagelTestAssetList.settingsPaneForUIUxml.CloneTree(root);
            simulate.FrameUpdate();
            simulate.FrameUpdate(); // Required for the way SettingsPaneManagerForUI does its init.
            var dropdown = root.Q<DropdownField>("theme-dropdown");

            var manager = rootVisualElement.Q<SettingsPaneManagerForUI>();

            Assert.AreEqual(4, dropdown.choices.Count);
            Assert.AreEqual("Classic", dropdown.value);

            var themePopupFieldInput =
                dropdown.Q(className: DropdownField.inputUssClassName);

            simulate.Click(themePopupFieldInput);
            simulate.FrameUpdate();
            Assert.IsTrue(m_PopupMenuSimulator.menuIsDisplayed);
            Assert.IsTrue(m_PopupMenuSimulator.SimulateMenuSelection("Edgy"));
            simulate.FrameUpdate();
            Assert.AreEqual("Edgy", dropdown.value);

            // The pane's own copy of the game state follows the dropdown.
            Assert.AreEqual(1, manager.state.settings.themeIndex);
        }
    }
}
