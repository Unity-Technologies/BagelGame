using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bagel.B00_Gameplay
{
    // The table at game over and after, in the real level: the bagel goes in the toaster, sliced, and a new game
    // sweeps off what the last run pushed out of place and sets it again.
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

        // The table set again is the table as the first game had it, once its loose things settled (one sugar
        // cube tumbles off the others, the same way every time). What's held still and wasn't moved stays put.
        [UnityTest]
        public IEnumerator ANewGameSetsAgainWhatWasPushedOutOfPlace()
        {
            yield return Seconds(1.5f);
            var settled = new System.Collections.Generic.Dictionary<TableItem, Vector3>();
            foreach (var item in TableItem.items)
                settled[item] = item.transform.position;

            // The bagel rolled into the sugar cubes and pushed them about.
            TableItem pushed = null;
            foreach (var item in TableItem.items)
            {
                if (item.loose && pushed == null)
                    pushed = item;
            }
            Assert.IsNotNull(pushed, "something on the table is loose");
            pushed.transform.position += new Vector3(1.5f, 0f, -1f);
            pushed.transform.rotation *= Quaternion.Euler(0f, 40f, 0f);

            state.FinishRun();
            yield return Seconds(1f);
            state.GoToPlay();
            yield return Seconds(0.3f);
            Assert.IsTrue(pushed.outOfPlace && pushed.loose, $"{pushed.name} is swept off");
            foreach (var item in TableItem.items)
            {
                if (!item.loose)
                    Assert.Less(Vector3.Distance(item.transform.position, settled[item]), 0.05f, $"{item.name} stays put");
            }

            yield return Seconds(4f);
            Assert.IsFalse(TableItem.disturbed, "the table is set again");
            foreach (var item in TableItem.items)
                Assert.Less(Vector3.Distance(item.transform.position, settled[item]), 0.05f, $"{item.name} is back");
        }
    }
}
