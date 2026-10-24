using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bagel.B00_Meshes
{
    // The procedural meshes as the level shows them: a mesh's size comes from its inputs, so a placed copy
    // scaled for some other mesh shows up here.
    public class SceneMeshTests
    {
        BagelController m_Bagel;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var assets = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
            yield return SceneManager.LoadSceneAsync(assets.mainSceneName, LoadSceneMode.Single);
            yield return null;
            m_Bagel = Object.FindAnyObjectByType<BagelController>(FindObjectsInactive.Include);
        }

        static bool IsBagel(MeshFilter filter) => filter.sharedMesh != null && filter.sharedMesh.name.StartsWith("Bagel") && filter.sharedMesh.name != "BagelHull";

        // The menu's bagels are a little bigger than the one that rolls; a copy scaled for some other mesh
        // would be many times bigger.
        [UnityTest]
        public IEnumerator EveryBagelIsAboutTheSizeOfTheOneThatRolls()
        {
            yield return null;
            var rolling = m_Bagel.GetComponentInChildren<MeshFilter>().GetComponent<Renderer>().bounds.size.magnitude;

            var bagels = 0;
            foreach (var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include))
            {
                if (!IsBagel(filter))
                    continue;
                ++bagels;
                var size = filter.GetComponent<Renderer>().bounds.size.magnitude;
                Assert.That(size, Is.InRange(rolling * 0.25f, rolling * 2f), $"{filter.name} under {filter.transform.parent?.name}");
            }
            Assert.Greater(bagels, 3, "the rolling bagel, the podiums' and the menu's");
        }

        // The bagel that's seen is the size of the collider that rolls.
        [UnityTest]
        public IEnumerator TheRollingBagelFitsItsCollider()
        {
            yield return null;
            var seen = m_Bagel.GetComponentInChildren<MeshFilter>().GetComponent<Renderer>().bounds;
            var collider = m_Bagel.GetComponent<Collider>().bounds;

            Assert.AreEqual(collider.size.y, seen.size.y, collider.size.y * 0.1f);
            Assert.AreEqual(collider.size.z, seen.size.z, collider.size.z * 0.1f);
            Assert.Less(Vector3.Distance(collider.center, seen.center), 0.1f);
        }
    }
}
