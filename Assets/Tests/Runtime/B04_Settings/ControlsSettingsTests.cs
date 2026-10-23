using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B04_Settings
{
    // Needs PlayInputBindings to run its Awake, so it's a PlayMode test only.
    public class ControlsSettingsTests : RuntimeUITestFixture
    {
        BagelTestAssetList m_BagelTestAssetList;
        GameObject m_BindingsObject;
        PlayInputBindings m_PlayInputBindings;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_BagelTestAssetList = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
        }

        [SetUp]
        public void SetUp()
        {
            m_BindingsObject = new GameObject("Test Play Input Bindings");
            m_PlayInputBindings = m_BindingsObject.AddComponent<PlayInputBindings>();
        }

        [TearDown]
        public void TearDown()
        {
            rootVisualElement.Clear();
            Object.Destroy(m_BindingsObject);
        }

        [UnityTest]
        public IEnumerator ShowsTheCurrentKeys()
        {
            m_BagelTestAssetList.settingsPaneForControlsUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();
            var controls = rootVisualElement.Q<SettingsPaneManagerForControls>();

            // On its own, the pane can't rebind anything.
            Assert.IsFalse(controls.GetButton(PlayInputBindings.Binding.Move_Up).enabledSelf);

            controls.playInputBindings = m_PlayInputBindings;
            simulate.FrameUpdate();
            yield return null;

            Assert.IsTrue(controls.GetButton(PlayInputBindings.Binding.Move_Up).enabledSelf);
            Assert.AreEqual(m_PlayInputBindings.GetBindingText(PlayInputBindings.Binding.Move_Up), controls.GetButton(PlayInputBindings.Binding.Move_Up).text);
            Assert.AreEqual(m_PlayInputBindings.GetBindingText(PlayInputBindings.Binding.Pause), controls.GetButton(PlayInputBindings.Binding.Pause).text);
        }
    }
}
