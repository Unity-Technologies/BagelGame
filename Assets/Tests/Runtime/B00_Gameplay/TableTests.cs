using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bagel.B00_Gameplay
{
    // The table at game over and after, in the real level: the bagel goes in the toaster, sliced, and a new game
    // shoves everything off the table and sets it again.
    public class TableTests
    {
        PlayManager m_PlayManager;

        PlayManagerState state => m_PlayManager.state;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var assets = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
            yield return SceneManager.LoadSceneAsync(assets.mainSceneName, LoadSceneMode.Single);
            yield return null;
            m_PlayManager = Object.FindAnyObjectByType<PlayManager>();
            state.GoToBagelSelection();
            state.GoToPlay();
            yield return null;
        }

        // Every game starts with the table shoved off and set again: wait for it.
        IEnumerator TableSet()
        {
            var clearing = Object.FindAnyObjectByType<TableClearing>();
            while (clearing.clearing)
                yield return null;
        }

        static IEnumerator Seconds(float seconds)
        {
            var end = Time.time + seconds;
            while (Time.time < end)
                yield return null;
        }

        [UnityTest]
        public IEnumerator AtGameOverTheBagelGoesInTheToasterSliced()
        {
            var toaster = Object.FindAnyObjectByType<Toaster>();
            var bagel = Object.FindAnyObjectByType<BagelController>();
            state.FinishRun();
            Assert.IsTrue(toaster.toasting);
            yield return Seconds(3f);

            Assert.IsFalse(toaster.toasting);
            Assert.IsTrue(toaster.hasToast);
            foreach (var renderer in bagel.GetComponentsInChildren<Renderer>())
                Assert.IsFalse(renderer.enabled, "the bagel that rolled is in the toaster now");

            // Each half stands on its edge in its slot, its cut face to the middle.
            var radius = state.selectedBagelType.topHalfMesh.bounds.extents.x;
            foreach (var top in new[] { true, false })
            {
                var half = top ? toaster.topHalf : toaster.bottomHalf;
                var inSlot = toaster.transform.TransformPoint(toaster.SlotPosition(top) + Vector3.up * radius);
                Assert.Less(Vector3.Distance(half.position, inSlot), 0.01f, top ? "the top half" : "the bottom half");
                var cutFace = half.TransformDirection(top ? Vector3.down : Vector3.up);
                Assert.Greater(Vector3.Dot(cutFace, toaster.transform.position - half.position), 0f, "its cut face is to the middle");
            }

            // A new game: the toaster is empty, and a bagel rolls again.
            state.GoToPlay();
            yield return null;
            Assert.IsFalse(toaster.hasToast);
            foreach (var renderer in bagel.GetComponentsInChildren<Renderer>())
                Assert.IsTrue(renderer.enabled);
        }

        // Every new game shoves everything off, moved or not, out to the right, toward the alley: copies of it fly, and
        // go once they're down on the floor. The things themselves come back up out of the table at once, as the
        // first game had them once its loose things settled (one sugar cube tumbles off the others, the same way every
        // time).
        [UnityTest]
        public IEnumerator ANewGameShovesEverythingOffAndSetsItAgain()
        {
            yield return TableSet();
            yield return Seconds(1.5f);
            var settled = new System.Collections.Generic.Dictionary<TableItem, Vector3>();
            foreach (var item in TableItem.items)
                settled[item] = item.transform.position;
            Assert.Greater(settled.Count, 12, "a table full of things");

            // The bagel rolled into the jam and pushed it about.
            var jam = GameObject.Find("JamJar").GetComponent<TableItem>();
            Assert.IsTrue(jam.loose, "the jam can be knocked about");
            jam.transform.position += new Vector3(-1f, 0f, -1f);

            state.FinishRun();
            yield return Seconds(1f);
            var clearing = Object.FindAnyObjectByType<TableClearing>();
            var level = clearing.transform;
            state.GoToPlay();
            yield return null;

            // Everything is coming back up already, from under where it belongs.
            foreach (var item in TableItem.items)
            {
                var home = level.InverseTransformPoint(item.homePosition);
                var now = level.InverseTransformPoint(item.transform.position);
                Assert.Less(now.y, home.y, $"{item.name} comes up out of the table");
                Assert.Less(new Vector2(now.x - home.x, now.z - home.z).magnitude, 0.01f, $"{item.name} comes up where it belongs");
            }

            // A copy of each thing as it was flies off to the right, the moved jam from where it was moved to.
            var leftovers = new System.Collections.Generic.List<TableLeftover>(clearing.leftovers);
            Assert.AreEqual(settled.Count, leftovers.Count, "everything is shoved off");
            var starts = new System.Collections.Generic.List<float>();
            foreach (var leftover in leftovers)
            {
                Assert.IsNull(leftover.GetComponent<TableItem>(), "a copy isn't one of the table's things");
                starts.Add(level.InverseTransformPoint(leftover.transform.position).x);
            }
            Assert.AreEqual(settled.Count, TableItem.items.Count, "the copies aren't counted");
            yield return Seconds(0.3f);
            for (int i = 0; i < leftovers.Count; i++)
            {
                Assert.IsTrue(leftovers[i] != null, $"{leftovers[i].name} is still flying");
                var now = level.InverseTransformPoint(leftovers[i].transform.position).x;
                Assert.Greater(now - starts[i], 0.5f, $"{leftovers[i].name} flies off to the right");
            }

            // Down on the floor, and gone, all of it, soon.
            var end = Time.time + 9f;
            while (Time.time < end && leftovers.Exists(leftover => leftover != null))
                yield return null;
            foreach (var leftover in leftovers)
                Assert.IsTrue(leftover == null, leftover != null ? $"{leftover.name} is gone, not at {leftover.transform.position}" : "");

            yield return TableSet();
            yield return Seconds(1.5f);
            // Loose things settle again where physics puts them, within a hair of the last time.
            foreach (var item in TableItem.items)
                Assert.Less(Vector3.Distance(item.transform.position, settled[item]), 0.15f, $"{item.name} is back");
        }
    }
}
