using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B04_Settings
{
    public class FieldOfViewTests : UITestFixture
    {
        [TearDown]
        public void TearDown()
        {
            rootVisualElement.Clear();
        }

        [Test]
        public void TheSliderAndTheSettingFollowEachOther()
        {
            var assets = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
            assets.settingsPaneForGameUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();
            var manager = rootVisualElement.Q<SettingsPaneManagerForGame>();
            var settings = manager.state.settings;

            // The range comes from the settings, and the value isn't clamped while it arrives.
            Assert.AreEqual(settings.fieldOfViewMin, manager.fieldOfViewSlider.lowValue);
            Assert.AreEqual(settings.fieldOfViewMax, manager.fieldOfViewSlider.highValue);
            Assert.AreEqual(settings.fieldOfView, manager.fieldOfViewSlider.value);

            manager.fieldOfViewSlider.value = 70f;
            simulate.FrameUpdate();
            Assert.AreEqual(70f, settings.fieldOfView);

            settings.fieldOfView = 55f;
            simulate.FrameUpdate();
            Assert.AreEqual(55f, manager.fieldOfViewSlider.value);
        }
    }
}
