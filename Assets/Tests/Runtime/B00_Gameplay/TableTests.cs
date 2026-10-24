using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bagel.B00_Gameplay
{
    // The table at game over and after, in the real level: the menu falls open onto it, knocking what's in
    // its way aside, and a new game sweeps the table clear and sets it again.
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
        public IEnumerator AtGameOverTheMenuFallsOpenOnTheTable()
        {
            Assert.IsFalse(TableItem.disturbed, "the table starts set");
            state.FinishRun();
            var driver = Object.FindAnyObjectByType<GameOverScreenDriver>();
            var menu = driver.menu;
            Assert.IsTrue(menu.gameObject.activeSelf);
            Assert.IsTrue(menu.falling);
            yield return Seconds(2.5f);

            Assert.IsFalse(menu.falling);
            Assert.Less(Quaternion.Angle(menu.transform.rotation, Quaternion.identity), 0.1f, "it lies flat");
            Assert.Less(Vector3.Distance(driver.transform.position, menu.pages.position), 1e-3f, "the screen is on its pages");
            Assert.IsTrue(TableItem.disturbed);

            // Nothing that was in its way ended up on its pages.
            var half = menu.size * 0.5f - Vector2.one * 0.3f;
            foreach (var item in TableItem.items)
            {
                if (item == menu.GetComponent<TableItem>())
                    continue;
                var local = menu.transform.InverseTransformPoint(item.transform.position);
                if (Mathf.Abs(local.x) < half.x && Mathf.Abs(local.z) < half.y)
                    Assert.Less(local.y, 0f, $"{item.name} is on the menu's pages");
            }
        }

        // The table set again is the table as the first game had it, once its loose things settled (one sugar
        // cube tumbles off the others, the same way every time).
        [UnityTest]
        public IEnumerator ANewGameClearsTheTableAndSetsItAgain()
        {
            yield return Seconds(1.5f);
            var settled = new System.Collections.Generic.Dictionary<TableItem, Vector3>();
            foreach (var item in TableItem.items)
                settled[item] = item.transform.position;

            state.FinishRun();
            yield return Seconds(2f);
            var menu = Object.FindAnyObjectByType<GameOverScreenDriver>().menu;

            state.GoToPlay();
            yield return Seconds(0.3f);
            foreach (var item in TableItem.items)
                Assert.IsTrue(item.loose, $"{item.name} is swept off");

            yield return Seconds(4.5f);
            Assert.IsFalse(TableItem.disturbed, "the table is set again");
            Assert.IsFalse(menu.gameObject.activeSelf, "the menu is gone");
            foreach (var item in TableItem.items)
            {
                if (item.comesBack)
                    Assert.Less(Vector3.Distance(item.transform.position, settled[item]), 0.05f, $"{item.name} is back");
            }
        }
    }
}
