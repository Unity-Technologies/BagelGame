using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B10_EditorTools
{
    public class BagelToolbarTests : UITestFixture
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
            BagelToolbar.SelectTheme(0);
            BagelToolbar.SelectLanguage("en");
            rootVisualElement.Clear();
        }

        [Test]
        public void ThemePreviewsRestyleOpenScreensButNoAsset()
        {
            var panelSettings = m_BagelTestAssetList.panelSettings.collection;
            var before = new List<string>();
            foreach (var settings in panelSettings)
                before.Add(EditorJsonUtility.ToJson(settings));

            m_BagelTestAssetList.pausePaneUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();
            var pane = rootVisualElement.Q<PausePaneManager>();
            var highContrast = m_BagelTestAssetList.themes.collection[3].theme;

            BagelToolbar.SelectTheme(3);
            Assert.AreEqual(highContrast, BagelThemes.previewTheme);
            Assert.IsTrue(pane.styleSheets.Contains(highContrast));

            BagelToolbar.SelectTheme(0);
            Assert.IsNull(BagelThemes.previewTheme);
            Assert.IsFalse(pane.styleSheets.Contains(highContrast));

            for (int i = 0; i < panelSettings.Count; ++i)
                Assert.AreEqual(before[i], EditorJsonUtility.ToJson(panelSettings[i]), panelSettings[i].name);
        }

        [Test]
        public void LanguageChangesOpenScreens()
        {
            m_BagelTestAssetList.pausePaneUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();
            var pane = rootVisualElement.Q<PausePaneManager>();

            BagelToolbar.SelectLanguage("fr");
            simulate.FrameUpdate();

            Assert.AreEqual("Reprendre", pane.resumeButton.text);
        }
    }
}
