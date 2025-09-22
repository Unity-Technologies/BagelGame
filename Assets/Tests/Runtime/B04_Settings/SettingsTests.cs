using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.T4_Settings
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
            m_BagelTestAssetList.settingsPaneUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();

            // Get the elements.
            var elements = SettingsPaneManager.BindUI(rootVisualElement);

            // Check initial state.
            Assert.AreEqual(2, elements.themeDropdown.choices.Count);
            Assert.AreEqual("Classic", elements.themeDropdown.value);

            // Get popup field input.
            var themePopupFieldInput = elements.themeDropdown.Q(className: "unity-popup-field__input");

            // Simulate changing the theme.
            simulate.Click(themePopupFieldInput);
            simulate.FrameUpdate();
            Assert.IsTrue(m_PopupMenuSimulator.menuIsDisplayed);
            Assert.IsTrue(m_PopupMenuSimulator.SimulateMenuSelection("Dark"));
            simulate.FrameUpdate();
            Assert.AreEqual("Dark", elements.themeDropdown.value);
        }
    }
}
