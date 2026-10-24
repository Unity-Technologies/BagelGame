using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;

namespace Bagel.B10_EditorTools
{
    // The demo's resets come from files in the repository: they have to be there and say what the demo
    // needs. The resets themselves rebuild the Editor's windows or stash the project, so they're tried by
    // hand, not here (BagelAllUIWindowTests covers the window's own reset).
    public class DemoResetTests
    {
        [Test]
        public void TheDemoMenuHasItsFourResets()
        {
            var paths = typeof(DemoReset).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .SelectMany(method => method.GetCustomAttributes<MenuItem>())
                .ToList();

            CollectionAssert.AreEquivalent(new[] { DemoReset.resetAllMenuPath, DemoReset.discardMenuPath, DemoReset.resetLayoutMenuPath, DemoReset.resetStatesMenuPath },
                paths.Select(item => item.menuItem));
            // The menus stay while a UXML is open on its own in the UI Viewport.
            foreach (var item in paths)
                CollectionAssert.Contains(item.editorModes, BagelMenu.uiToolkitAuthoringMode, item.menuItem);
        }

        [Test]
        public void TheToolbarPresetShowsTheBagelStripsOnTheRight()
        {
            var preset = File.ReadAllText(DemoReset.toolbarPresetPath);
            StringAssert.Contains("m_RawWindowType: UnityEditor.MainToolbarWindow", preset);
            foreach (var path in BagelToolbar.paths)
            {
                var entry = Regex.Match(preset, @"- dockPosition: (\d+)\s+containerId: \S+\s+displayed: (\d)\s+id: " + Regex.Escape(path) + @"\s");
                Assert.IsTrue(entry.Success, path);
                Assert.AreEqual("1", entry.Groups[1].Value, $"{path} docks on the right");
                Assert.AreEqual("1", entry.Groups[2].Value, $"{path} is shown");
            }
        }

        [Test]
        public void TheLayoutOpensTheBagelWindowNextToTheGame()
        {
            var layout = File.ReadAllText(DemoReset.layoutPath);
            StringAssert.Contains("m_Text: Game", layout);
            StringAssert.Contains("m_Text: Bagel All UI", layout);
            StringAssert.Contains("UIViewportWindow", layout);
            // Saved from someone's Editor: nothing of their machine in it.
            StringAssert.Contains("m_LastProjectPath: \n", layout.Replace("\r", ""));
        }
    }
}
