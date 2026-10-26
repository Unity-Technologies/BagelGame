using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Bagel.B14_LevelSelection
{
    // The payment tablet in the real scene: the register's Order brings the camera to it, its screen takes the focus
    // and can be clicked, the level editor brings the camera close and the library's live pictures, and Pay & Play
    // plays the chosen level.
    public class LevelSelectionTests
    {
        PlayManager m_PlayManager;
        LevelSelectionScreenDriver m_Driver;

        PlayManagerState state => m_PlayManager.state;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var assets = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
            yield return SceneManager.LoadSceneAsync(assets.mainSceneName, LoadSceneMode.Single);
            yield return null;
            m_PlayManager = Object.FindAnyObjectByType<PlayManager>();
            m_Driver = Object.FindAnyObjectByType<LevelSelectionScreenDriver>();
            state.GoToBagelSelection();
            yield return null;
        }

        static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
                yield return null;
        }

        // A Cinemachine camera, on when it's the one looking.
        static Behaviour Camera(string name) => (Behaviour)GameObject.Find(name).GetComponent("CinemachineCamera");

        [UnityTest]
        public IEnumerator TheOrderGoesToTheTabletWhichTakesTheFocus()
        {
            var register = Object.FindAnyObjectByType<BagelSelectionControlsPaneDriver>();
            Assert.IsNotNull(register);
            state.GoToLevelSelection();
            yield return Frames(3);

            Assert.IsTrue(Camera("LevelSelectionCinemaCamera").enabled, "the camera's over the tablet");
            Assert.IsFalse(Camera("BagelSelectionCinemaCamera").enabled);
            var screen = m_Driver.screen;
            Assert.IsNotNull(screen);
            Assert.AreEqual(screen.rows[state.selectedLevelIndex], screen.focusController.focusedElement, "the chosen level's row has the focus");
            var bagel = Object.FindAnyObjectByType<BagelController>();
            Assert.IsTrue(bagel.GetComponentInChildren<Renderer>().enabled, "the new bagel waits at the start");
        }

        // A click on the tablet's screen reaches it: nothing of the tablet is in front of it.
        [UnityTest]
        public IEnumerator TheTabletsScreenCanBeClicked()
        {
            state.GoToLevelSelection();
            yield return Frames(3);
            var screen = m_Driver.transform;
            var camera = Camera("LevelSelectionCinemaCamera").transform;
            var hits = Physics.RaycastAll(camera.position, screen.position - camera.position, Vector3.Distance(camera.position, screen.position) + 1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            Assert.Greater(hits.Length, 0);
            Assert.AreEqual(screen.gameObject, hits[0].collider.gameObject, $"the screen is hit first, not {hits[0].collider.name}");
        }

        [UnityTest]
        public IEnumerator TheEditorBringsTheCameraCloseAndTheLivePictures()
        {
            state.GoToLevelSelection();
            yield return null;
            m_Driver.screen.NewLevel();
            yield return Frames(3);

            Assert.IsTrue(Camera("LevelEditorCinemaCamera").enabled, "close up, square to the screen");
            Assert.IsFalse(Camera("LevelSelectionCinemaCamera").enabled);
            var studio = m_Driver.studio;
            Assert.IsNotNull(studio);
            Assert.IsNotNull(studio.texture);
            Assert.AreEqual(BagelLevelStudio.runtimeLayer, studio.root.layer, "on a layer of its own");
            Assert.AreEqual(0, UnityEngine.Camera.main.cullingMask & (1 << studio.root.layer), "the bakery's camera never sees it");
            var editor = m_Driver.screen.editor;
            var picture = editor.items[0].Q<Image>();
            Assert.AreEqual(studio.texture, picture.image);
            Assert.AreEqual(studio.UV((string)editor.items[0].userData), picture.uv);

            // Spun, it turns faster, then slows back down.
            var spinner = studio.root.transform.Find("Ramp/Spinner");
            var before = spinner.localEulerAngles.y;
            studio.Spin("Ramp", 90f);
            Assert.AreEqual(Mathf.Repeat(before + 90f, 360f), spinner.localEulerAngles.y, 0.5f);

            state.EditLevel(false);
            yield return null;
            Assert.IsNull(m_Driver.studio, "gone with the editor");
            Assert.IsTrue(Camera("LevelSelectionCinemaCamera").enabled);
        }

        [UnityTest]
        public IEnumerator PayAndPlayPlaysTheChosenLevel()
        {
            state.GoToLevelSelection();
            yield return null;
            m_Driver.screen.rows[1].Focus();
            yield return null;
            Assert.AreEqual(1, state.selectedLevelIndex);
            m_Driver.screen.Play();
            yield return null;
            Assert.IsTrue(state.isPlaying);
            Assert.AreEqual("brunch", Object.FindAnyObjectByType<BagelLevelTable>().level.id);
        }
    }
}
