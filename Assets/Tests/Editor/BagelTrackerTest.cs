using NUnit.Framework;
using UnityEditor.PackageManager.UI;
using UnityEditor.UIElements.TestFramework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    public class BagelTrackerTest : EditorWindowUITestFixture<BagelTestEditorWindow>
    {
        BagelTrackerConstants m_BagelTrackerConstants;
        BagelTrackerData m_BagelTrackerData;
        VisualElement m_Root;

        [SetUp]
        public void SetUp()
        {
            debugMode = true;

            m_BagelTrackerConstants = ScriptableObject.CreateInstance<BagelTrackerConstants>();
            m_BagelTrackerConstants.hideFlags = HideFlags.HideAndDontSave;

            m_BagelTrackerData = ScriptableObject.CreateInstance<BagelTrackerData>();
            m_BagelTrackerData.hideFlags = HideFlags.HideAndDontSave;
            m_BagelTrackerData.constants = m_BagelTrackerConstants;
            m_BagelTrackerData.toppingsCount = 42;

            m_Root = window.rootVisualElement.Q("root");
            m_Root.dataSource = m_BagelTrackerData;
            simulate.FrameUpdate();
        }

        [TearDown]
        public void TearDown()
        {
            m_Root.dataSource = null;
            ScriptableObject.DestroyImmediate(m_BagelTrackerData);
        }

        [Test]
        public void BagelTrackerDataBindings()
        {
            var toppingsCountBar = m_Root.Q<ProgressBar>("toppings-count-bar");
            Assert.IsNotNull(toppingsCountBar);
            Assert.AreEqual(m_BagelTrackerData.toppingsCount, toppingsCountBar.value);

            m_BagelTrackerData.toppingsCount = 5;
            simulate.FrameUpdate();
            Assert.AreEqual(5f, toppingsCountBar.value);
        }
    }
}
