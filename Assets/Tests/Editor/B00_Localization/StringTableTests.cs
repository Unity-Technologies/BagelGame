using System.Collections.Generic;
using NUnit.Framework;
using Unity.Localization;
using Unity.Localization.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Bagel.B00_Localization
{
    // Rules for the string table itself.
    public class StringTableTests
    {
        const string k_CollectionPath = "Assets/Localization/BagelStrings/BagelStrings.asset";
        const string k_ChineseFontPath = "Assets/UI/Runtime/B00_Themes/T0_Common/Fonts/NotoSansSC-Regular SDF.asset";

        ResourceTableCollection m_Collection;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_Collection = AssetDatabase.LoadAssetAtPath<ResourceTableCollection>(k_CollectionPath);
        }

        [Test]
        public void EveryKeyIsInEveryLanguage()
        {
            Assert.AreEqual(3, m_Collection.Tables.Count, "English, French and Chinese");

            foreach (var key in m_Collection.SharedData.Entries)
            {
                foreach (var table in m_Collection.Tables)
                {
                    var entry = table.GetEntry<StringEntry>(key.Id);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(entry?.Value), $"{key.Key} has no {table.LocaleIdentifier.Code} text");
                }
            }
        }

        [Test]
        public void ChineseTextHasGlyphs()
        {
            var font = AssetDatabase.LoadAssetAtPath<FontAsset>(k_ChineseFontPath);
            var chinese = m_Collection.GetTable(new LocaleIdentifier("zh-Hans"));

            foreach (var entry in chinese.Entries)
            {
                var text = (entry as StringEntry)?.Value;
                if (string.IsNullOrEmpty(text))
                    continue;

                // The font asset is dynamic: this adds the glyphs it can find in the font file.
                Assert.IsTrue(font.TryAddCharacters(text, out string missing), $"Missing from Noto Sans SC: {missing}");
            }
        }

        [Test]
        public void NamesInTheGameDataHaveKeys()
        {
            var keys = new HashSet<string>();
            foreach (var key in m_Collection.SharedData.Entries)
                keys.Add(key.Key);

            var assets = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
            foreach (var bagelType in assets.startState.bagelTypes.collection)
                Assert.IsTrue(keys.Contains(bagelType.nameKey), $"{bagelType.name}: '{bagelType.nameKey}'");
            foreach (var theme in assets.themes.collection)
                Assert.IsTrue(keys.Contains(theme.nameKey), $"{theme.themeName}: '{theme.nameKey}'");
        }
    }
}
