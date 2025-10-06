using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B04_Settings
{
    public class SettingsTests : UITestFixture
    {
        BagelTestAssetList m_BagelTestAssetList;
        PopupMenuSimulator m_PopupMenuSimulator;

        public SettingsTests()
        {
            m_PopupMenuSimulator = AddTestComponent<PopupMenuSimulator>();
        }

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

        [Test]
        public void SettingsThemeDropdown()
        {
            // Load the UXML.
            m_BagelTestAssetList.settingsPaneForUIUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();
            simulate.FrameUpdate(); // Required for the way SettingsPaneManagerForUI does its init.

            // Get the elements.
            var manager = rootVisualElement.Q<SettingsPaneManagerForUI>();
            var themeDropdown = manager.themeDropdown;

            // Prepare for testing.
            manager.dataSource = null; // We don't need a real data source for this test.

            // Check initial state.
            Assert.AreEqual(2, themeDropdown.choices.Count);
            Assert.AreEqual("Classic", themeDropdown.value);

            // Get popup field input.
            var themePopupFieldInput = themeDropdown.Q(className: DropdownField.inputUssClassName);

            // Simulate changing the theme.
            simulate.Click(themePopupFieldInput);
            simulate.FrameUpdate();
            Assert.IsTrue(m_PopupMenuSimulator.menuIsDisplayed);
            Assert.IsTrue(m_PopupMenuSimulator.SimulateMenuSelection("Edgy"));
            simulate.FrameUpdate();
            Assert.AreEqual("Edgy", themeDropdown.value);
        }
    }
}
