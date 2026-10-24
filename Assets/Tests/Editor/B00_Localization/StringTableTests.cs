using System.Collections.Generic;
using NUnit.Framework;
using Unity.Localization;
using Unity.Localization.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;

namespace Bagel.B00_Localization
{
    // Rules for the string table itself.
    public class StringTableTests
    {
        const string k_CollectionPath = "Assets/Localization/BagelStrings/BagelStrings.asset";
        const string k_FontsPath = "Assets/UI/Runtime/B00_Themes/T0_Common/Fonts/";

        ResourceTableCollection m_Collection;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_Collection = AssetDatabase.LoadAssetAtPath<ResourceTableCollection>(k_CollectionPath);
        }

        [Test]
        public void EveryKeyIsInEveryLanguage()
        {
            Assert.AreEqual(4, m_Collection.Tables.Count, "English, French, Chinese and Arabic");

            foreach (var key in m_Collection.SharedData.Entries)
            {
                foreach (var table in m_Collection.Tables)
                {
                    var entry = table.GetEntry<StringEntry>(key.Id);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(entry?.Value), $"{key.Key} has no {table.LocaleIdentifier.Code} text");
                }
            }
        }

        // Every letter of every language is in the game's font, DynaPuff, or in a font it falls back to: Noto Kufi
        // Arabic and Noto Sans SC, in the regular and the bold weight. Read from the font files, so the font assets'
        // atlases don't change.
        [TestCase("DynaPuff-Medium SDF.asset")]
        [TestCase("DynaPuff-Bold SDF.asset")]
        public void EveryLanguageHasGlyphs(string fontAsset)
        {
            var font = AssetDatabase.LoadAssetAtPath<FontAsset>(k_FontsPath + fontAsset);
            var fonts = new List<Font> { font.sourceFontFile };
            foreach (var fallback in font.fallbackFontAssetTable)
                fonts.Add(fallback.sourceFontFile);

            foreach (var table in m_Collection.Tables)
            {
                var missing = new HashSet<char>();
                foreach (var entry in table.Entries)
                {
                    foreach (var c in (entry as StringEntry)?.Value ?? "")
                    {
                        if (!char.IsWhiteSpace(c))
                            missing.Add(c);
                    }
                }

                foreach (var file in fonts)
                {
                    Assert.AreEqual(FontEngineError.Success, FontEngine.LoadFontFace(file), file.name);
                    missing.RemoveWhere(c => FontEngine.TryGetGlyphIndex(c, out var index) && index != 0);
                }
                Assert.IsEmpty(missing, $"{fontAsset} can't draw these {table.LocaleIdentifier.Code} characters: {string.Join(" ", missing)}");
            }
        }

        // Every font draws all the game's text from one atlas page, with room to spare: a font that needs a second
        // page has the text generator fail to draw text (and a language change then shows nothing new). A glyph
        // takes about its point size plus the padding each way; an Arabic letter has up to four joined forms.
        [TestCase("DynaPuff-Medium SDF.asset")]
        [TestCase("NotoKufiArabic-Medium SDF.asset")]
        [TestCase("NotoSansSC-Regular SDF.asset")]
        public void EveryFontFitsOnOnePage(string fontAsset)
        {
            var font = AssetDatabase.LoadAssetAtPath<FontAsset>(k_FontsPath + fontAsset);
            Assert.AreEqual(FontEngineError.Success, FontEngine.LoadFontFace(font.sourceFontFile));

            var drawn = new HashSet<char>();
            foreach (var table in m_Collection.Tables)
            {
                foreach (var entry in table.Entries)
                {
                    foreach (var c in (entry as StringEntry)?.Value ?? "")
                    {
                        if (!char.IsWhiteSpace(c) && FontEngine.TryGetGlyphIndex(c, out var index) && index != 0)
                            drawn.Add(c);
                    }
                }
            }

            var glyphSize = font.faceInfo.pointSize * 1.2f + 2 * font.atlasPadding;
            var perPage = Mathf.FloorToInt(font.atlasWidth / glyphSize) * Mathf.FloorToInt(font.atlasHeight / glyphSize);
            var forms = fontAsset.StartsWith("NotoKufiArabic") ? 4 : 1;
            Assert.LessOrEqual(drawn.Count * forms * 2, perPage, $"{fontAsset}: {drawn.Count} characters, {perPage} glyphs a page");
        }

        // Bold text gets bold faces rather than thickened ones, in Arabic too.
        [Test]
        public void BoldTextHasBoldFaces()
        {
            var game = AssetDatabase.LoadAssetAtPath<FontAsset>(k_FontsPath + "DynaPuff-Medium SDF.asset");
            var arabic = AssetDatabase.LoadAssetAtPath<FontAsset>(k_FontsPath + "NotoKufiArabic-Medium SDF.asset");
            Assert.AreEqual("DynaPuff-Bold SDF", game.fontWeightTable[7].regularTypeface?.name);
            Assert.AreEqual("NotoKufiArabic-Bold SDF", arabic.fontWeightTable[7].regularTypeface?.name);
            CollectionAssert.Contains(game.fallbackFontAssetTable, arabic);
        }

        // Smart strings are formatted with the system's culture, not the selected language's: a plural names its
        // language's rule ({toppings:plural(ar):...}), or Arabic would be counted with the system's two forms
        // instead of its six.
        [Test]
        public void PluralsNameTheirLanguage()
        {
            foreach (var table in m_Collection.Tables)
            {
                var code = table.LocaleIdentifier.Code;
                var language = code.Split('-')[0];
                foreach (var entry in table.Entries)
                {
                    var text = (entry as StringEntry)?.Value ?? "";
                    var plurals = text.Split(":plural").Length - 1;
                    var named = text.Split($":plural({language}):").Length - 1;
                    Assert.AreEqual(plurals, named, $"{code}: {text}");
                }
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
