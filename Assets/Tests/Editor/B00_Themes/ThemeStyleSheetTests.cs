using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Bagel.B00_Themes
{
    // Design rules for the theme files themselves.
    public class ThemeStyleSheetTests
    {
        BagelTestAssetList m_BagelTestAssetList;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_BagelTestAssetList = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
        }

        [Test]
        public void ThemesOnlySetVariables()
        {
            foreach (var entry in m_BagelTestAssetList.themes.collection)
            {
                var folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(entry.theme));
                var styles = Directory.GetFiles(folder, "*ThemeStyles.uss");
                Assert.AreEqual(1, styles.Length, $"{entry.themeName} has one variables file");

                var uss = Regex.Replace(File.ReadAllText(styles[0]), @"/\*.*?\*/", "", RegexOptions.Singleline);
                foreach (Match rule in Regex.Matches(uss, @"([^{}]+)\{([^}]*)\}"))
                {
                    var selector = rule.Groups[1].Value.Trim();
                    var body = rule.Groups[2].Value;
                    if (selector == ":root")
                    {
                        foreach (var declaration in body.Split(';'))
                            Assert.IsTrue(declaration.Trim().Length == 0 || declaration.Trim().StartsWith("--b-"),
                                $"{entry.themeName}: only --b- variables in :root, not '{declaration.Trim()}'");
                        continue;
                    }

                    // The one exception: -unity-material can't be set from a variable.
                    var declarations = body.Split(';').Select(declaration => declaration.Trim()).Where(declaration => declaration.Length > 0).ToArray();
                    Assert.AreEqual(1, declarations.Length, $"{entry.themeName}: '{selector}' does more than pick the buttons' shader");
                    StringAssert.StartsWith("-unity-material:", declarations[0], $"{entry.themeName}: '{selector}' does more than pick the buttons' shader");
                }
            }
        }

        [Test]
        public void ThemesImportTheCommonStyles()
        {
            foreach (var entry in m_BagelTestAssetList.themes.collection)
            {
                var tss = File.ReadAllText(AssetDatabase.GetAssetPath(entry.theme));
                StringAssert.Contains("T0_Common/UnityDefaultRuntimeTheme.tss", tss, entry.themeName);
                StringAssert.Contains("T0_Common/BagelCommonStyles.uss", tss, entry.themeName);
            }
        }
    }
}
