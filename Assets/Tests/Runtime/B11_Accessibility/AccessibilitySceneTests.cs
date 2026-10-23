using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bagel.B11_Accessibility
{
    // The world-space screens, in the real scene: their nodes are where the player sees them.
    public class AccessibilitySceneTests
    {
        PlayManager m_PlayManager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var assets = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
            yield return SceneManager.LoadSceneAsync(assets.mainSceneName, LoadSceneMode.Single);
            yield return null;
            m_PlayManager = Object.FindAnyObjectByType<PlayManager>();
        }

        static AccessibilityNode Find(AccessibilityHierarchy hierarchy, string label)
        {
            AccessibilityNode Search(System.Collections.Generic.IReadOnlyList<AccessibilityNode> nodes)
            {
                foreach (var node in nodes)
                {
                    if (node.label == label)
                        return node;
                    var found = Search(node.children);
                    if (found != null)
                        return found;
                }
                return null;
            }
            return Search(hierarchy.rootNodes);
        }

        [UnityTest]
        public IEnumerator MainMenuNodesAreOnScreen()
        {
            m_PlayManager.state.GoToMainMenu();
            yield return null;
            yield return null;

            var hierarchy = BagelAccessibility.hierarchy;
            Assert.IsNotNull(hierarchy);
            Assert.AreEqual(hierarchy, AssistiveSupport.activeHierarchy, "the Editor always shows it, for the Hierarchy Viewer");

            var play = Find(hierarchy, "Play");
            var exit = Find(hierarchy, "Exit");
            Assert.IsNotNull(play);
            Assert.IsNotNull(exit);

            var screen = new Rect(0, 0, Screen.width, Screen.height);
            Assert.IsTrue(screen.Contains(play.frame.center), $"{play.frame} on {screen}");
            Assert.Greater(play.frame.width, 10f);
            Assert.Less(play.frame.yMax, exit.frame.yMax, "Exit is below Play (top-left origin)");
        }

        [UnityTest]
        public IEnumerator PausingReadsThePauseScreen()
        {
            m_PlayManager.state.GoToBagelSelection();
            yield return null;
            m_PlayManager.state.GoToPlay();
            yield return null;
            Assert.AreEqual(0, BagelAccessibility.hierarchy.rootNodes.Count, "nothing to read while rolling");

            m_PlayManager.state.Pause();
            yield return null;
            yield return null;
            Assert.IsNotNull(Find(BagelAccessibility.hierarchy, "Resume"));

            m_PlayManager.state.Resume();
            m_PlayManager.state.GoToMainMenu();
            yield return null;
        }
    }
}
