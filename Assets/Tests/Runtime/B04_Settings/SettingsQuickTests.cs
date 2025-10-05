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
            var dropdown = root.Q<DropdownField>("theme-dropdown");

            Assert.AreEqual(2, dropdown.choices.Count);
            Assert.AreEqual("Classic", dropdown.value);

            var themePopupFieldInput =
                dropdown.Q(className: DropdownField.inputUssClassName);

            simulate.Click(themePopupFieldInput);
            simulate.FrameUpdate();
            Assert.IsTrue(m_PopupMenuSimulator.menuIsDisplayed);
            Assert.IsTrue(m_PopupMenuSimulator.SimulateMenuSelection("Dark"));
            simulate.FrameUpdate();
            Assert.AreEqual("Dark", dropdown.value);
        }
    }
}
