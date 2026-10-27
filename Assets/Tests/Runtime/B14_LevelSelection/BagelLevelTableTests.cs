using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bagel.B14_LevelSelection
{
    // The counter set for the chosen level, in the real scene: the tutorial as it always was, a longer level
    // stretching the room with what hangs on the far wall going with it, only what changes in a level coming and
    // going, another level shoving everything off the table, and a new game starting with the whole level there.
    public class BagelLevelTableTests
    {
        PlayManager m_PlayManager;
        BagelLevelTable m_Table;
        Bakery m_Bakery;

        PlayManagerState state => m_PlayManager.state;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var assets = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
            yield return SceneManager.LoadSceneAsync(assets.mainSceneName, LoadSceneMode.Single);
            yield return null;
            m_PlayManager = Object.FindAnyObjectByType<PlayManager>();
            m_Table = Object.FindAnyObjectByType<BagelLevelTable>();
            m_Bakery = Object.FindAnyObjectByType<Bakery>();
        }

        IEnumerator Changed(float timeout = 5f)
        {
            yield return null;
            for (var end = Time.time + timeout; m_Table.changing && Time.time < end;)
                yield return null;
            Assert.IsFalse(m_Table.changing, "done changing");
            // What sank is gone a moment later.
            yield return new WaitForSeconds(0.6f);
        }

        // Every piece the level has is on the table where the level puts it, and nothing else.
        void AssertSetFor(BagelLevel level)
        {
            var placed = new List<GameObject>(m_Table.pieces);
            Assert.AreEqual(level.pieceCount, placed.Count, $"{level.id}'s pieces");
            foreach (var piece in level.pieces)
            {
                var at = m_Table.transform.TransformPoint(piece.x, 0f, piece.z);
                var found = placed.Find(instance => instance.name == piece.kind && Vector3.Distance(instance.transform.position, at) < 0.01f);
                Assert.IsNotNull(found, $"{piece} is there");
                Assert.AreEqual(piece.angle, found.transform.eulerAngles.y, 0.1f, $"{piece} is turned");
                placed.Remove(found);
            }
            var built = 0;
            foreach (Transform child in m_Table.transform)
            {
                if (child.name != "Cloth" && child != m_Table.toaster)
                    built++;
            }
            Assert.AreEqual(level.pieceCount, built, "nothing else is on the table");
            Assert.AreEqual(level.length, m_Table.toaster.localPosition.z, 1e-3f, "the toaster's at the end");
        }

        static BakeryAnchor Anchor(string name)
        {
            foreach (var anchor in Object.FindObjectsByType<BakeryAnchor>())
            {
                if (anchor.name == name)
                    return anchor;
            }
            return null;
        }

        [UnityTest]
        public IEnumerator TheTutorialIsSetAsItAlwaysWas()
        {
            yield return null;
            Assert.AreEqual("breakfast", m_Table.level.id);
            AssertSetFor(m_Table.level);
            Assert.AreEqual(27.5f, m_Table.toaster.position.z, 0.01f);
            Assert.AreEqual(41f, m_Bakery.northSide, 1e-3f, "the room as it always was");
            Assert.AreEqual(49.8f, m_Bakery.counterRect.height, 1e-3f);
            Assert.IsTrue(Anchor("Beam4").shown);
            Assert.IsFalse(Anchor("Beam5").shown, "past the far wall");
        }

        [UnityTest]
        public IEnumerator ALongerLevelStretchesTheRoomAndWhatsOnTheFarWallGoesWithIt()
        {
            state.GoToBagelSelection();
            state.GoToLevelSelection();
            yield return null;
            // The game over screen, and what hangs on or by the far wall.
            var onTheWall = new List<Transform> { GameObject.Find("GameOverScreenUI").transform };
            foreach (var name in new[] { "Television", "Clock", "BagelFrame", "NorthCrown" })
                onTheWall.Add(Anchor(name).transform);
            var fromTheWall = onTheWall.ConvertAll(thing => thing.position.z - m_Bakery.northSide);
            var banquet = state.levels[2];

            state.SelectLevel(2);
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(m_Table.changing);
            Assert.Greater(m_Table.length, 27.5f, "on its way");
            Assert.Less(m_Table.length, banquet.length);
            Assert.AreEqual(m_Table.length + 13.5f, m_Bakery.northSide, 1e-3f, "the room stretches with it");

            yield return Changed();
            AssertSetFor(banquet);
            Assert.AreEqual(banquet.length + 13.5f, m_Bakery.northSide, 1e-3f);
            for (int i = 0; i < onTheWall.Count; i++)
                Assert.AreEqual(fromTheWall[i], onTheWall[i].position.z - m_Bakery.northSide, 1e-3f, $"{onTheWall[i].name} goes with the wall");
            Assert.IsTrue(Anchor("Beam5").shown, "a beam that was past the wall is in the room");
            Assert.IsFalse(Anchor("Beam8").shown);

            state.SelectLevel(0);
            yield return Changed();
            AssertSetFor(state.levels[0]);
            Assert.AreEqual(41f, m_Bakery.northSide, 1e-3f, "and back");
            Assert.IsFalse(Anchor("Beam5").shown);
        }

        [UnityTest]
        public IEnumerator OnlyWhatChangesComesAndGoes()
        {
            state.GoToBagelSelection();
            state.GoToLevelSelection();
            var level = state.AddLevel("Mine");
            level.Add(new BagelLevelPiece("Platform", 2f, 10f));
            level.Add(new BagelLevelPiece("Orange", -2f, 12f));
            yield return Changed();
            AssertSetFor(level);
            var before = new List<GameObject>(m_Table.pieces);

            level.Add(new BagelLevelPiece("JamJar", 3f, 20f, 45f));
            yield return null;
            var jam = new List<GameObject>(m_Table.pieces).Find(instance => instance.name == "JamJar");
            Assert.IsNotNull(jam, "the jam's there at once");
            Assert.Less(jam.GetComponentInChildren<TableItem>().transform.position.y, m_Table.transform.position.y, "coming up out of the table");
            foreach (var instance in before)
                Assert.IsTrue(instance != null && new List<GameObject>(m_Table.pieces).Contains(instance), $"{instance.name} stays as it was");

            var platform = before.Find(instance => instance.name == "Platform");
            level.RemoveAt(0);
            yield return null;
            Assert.IsTrue(platform != null, "still sinking");
            Assert.Less(platform.transform.localPosition.y, 0.01f);
            yield return Changed();
            Assert.IsTrue(platform == null, "gone");
            AssertSetFor(level);
        }

        [UnityTest]
        public IEnumerator AnotherLevelShovesEverythingOffTheTable()
        {
            state.GoToBagelSelection();
            state.GoToLevelSelection();
            yield return null;
            var clearing = m_Table.GetComponent<TableClearing>();
            var before = new List<GameObject>(m_Table.pieces);
            var things = 0;
            foreach (var piece in before)
                things += Mathf.Max(1, piece.GetComponentsInChildren<TableItem>().Length);

            state.SelectLevel(1);
            yield return null;
            Assert.IsTrue(before.TrueForAll(piece => piece == null), "the old level's gone at once");
            var leftovers = new List<TableLeftover>(clearing.leftovers);
            Assert.AreEqual(things, leftovers.Count, "everything's shoved off, the course too");
            var starts = new List<float>();
            foreach (var leftover in leftovers)
            {
                Assert.AreNotEqual(m_Table.transform, leftover.transform.parent, "a copy isn't on the table");
                starts.Add(m_Table.transform.InverseTransformPoint(leftover.transform.position).x);
            }
            yield return new WaitForSeconds(0.3f);
            for (int i = 0; i < leftovers.Count; i++)
            {
                Assert.IsTrue(leftovers[i] != null, "still flying");
                var now = m_Table.transform.InverseTransformPoint(leftovers[i].transform.position).x;
                Assert.Greater(now - starts[i], 0.5f, $"{leftovers[i].name} flies off to the right");
            }

            yield return Changed();
            AssertSetFor(state.levels[1]);
        }

        [UnityTest]
        public IEnumerator ANewGameStartsWithTheWholeLevelThere()
        {
            state.GoToBagelSelection();
            state.GoToLevelSelection();
            var bagel = Object.FindAnyObjectByType<BagelController>();
            Assert.IsTrue(bagel.GetComponent<Rigidbody>().isKinematic, "the bagel waits at the start");
            Assert.Less(Vector3.Angle(bagel.transform.right, Vector3.right), 1f, "standing up");

            state.SelectLevel(2);
            yield return null;
            state.GoToPlay();
            Assert.IsFalse(m_Table.changing, "done at once");
            AssertSetFor(state.levels[2]);
            Assert.IsTrue(Object.FindAnyObjectByType<TableClearing>().clearing, "then the table's shoved off and set again");
            Assert.IsFalse(bagel.GetComponent<Rigidbody>().isKinematic, "and the bagel rolls");
            yield return null;
        }
    }
}
