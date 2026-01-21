using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B03_MainMenu
{
    public class MainMenuWorldspaceTests : RuntimeUITestFixture
    {
        PlayManager m_PlayManager;
        MainMenuScreenDriver m_Driver;
        MainMenuPaneManager m_PaneManager;
        PanelRenderer m_PanelRenderer;

        [UnityOneTimeSetUp]
        public IEnumerator UnityOneTimeSetUp()
        {
            var bagelTestAssetList = Resources.Load<BagelTestAssetList>("BagelTestAssetList");

            var op = SceneManager.LoadSceneAsync(bagelTestAssetList.mainSceneName, LoadSceneMode.Single);
            Assert.IsNotNull(op, $"Scene '{bagelTestAssetList.mainSceneName}' not found or not in Build Settings.");
            yield return op;
            yield return null; // Give Awake/OnEnable/Start a frame to run.

            m_PlayManager = Object.FindFirstObjectByType<PlayManager>(FindObjectsInactive.Include);
            m_Driver = Object.FindFirstObjectByType<MainMenuScreenDriver>(FindObjectsInactive.Include);

            m_PanelRenderer = m_Driver.GetComponent<PanelRenderer>();
            Assert.IsNotNull(m_PanelRenderer);
            SetPanelRenderer(m_PanelRenderer);

            m_PaneManager = rootVisualElement.Q<MainMenuPaneManager>();
        }

        [SetUp]
        public void SetUp()
        {
            simulate.FrameUpdate();
        }

        [UnityTest]
        public IEnumerator PlayInMainMenuTriggersGameStateChange()
        {
            yield return null;
            simulate.FrameUpdate();

            Debug.Log($"Panel: {simulate.panelName}");
            Debug.Log($"RootVisualElement: {rootVisualElement}");
            Debug.Log($"element worldbound: {m_PaneManager.playButton.worldBound.x}, {m_PaneManager.playButton.worldBound.y}, width + height : {m_PaneManager.playButton.worldBound.width}, {m_PaneManager.playButton.worldBound.height}");

            var currentState = m_PlayManager.state.currentState;

            Assert.NotZero(m_PaneManager.playButton.resolvedStyle.width);

            m_PlayManager.state.onStateChange += (sender, newState) =>
            {
                Assert.AreNotEqual(currentState, newState);
                currentState = newState;
            };

            bool bb = false;
            m_PaneManager.playButton.clicked += () =>
            {
                bb = true;
                Debug.Log($"Play button was clicked");
            };

            // DEBUG LOOP FOR TESTING PURPOSES
            bool debug = false;
            while (debug)
            {
                simulate.FrameUpdate();
                yield return null;
            }

            Assert.IsNotNull(m_PaneManager.playButton);

            var ve = m_PaneManager.playButton;
            var localCenter = ve.worldBound.center;

            var clickPos = localCenter;

            Debug.Log("Clicking with panelRenderer");
            bb = false; // FOR DEBUGGING
            simulate.ClickWorldSpace(m_PanelRenderer, ve);

            simulate.FrameUpdate();
            Assert.IsTrue(bb, "Play was not clicked");

            Assert.AreEqual(PlayManagerState.State.BagelSelection, currentState);
        }
    }
}