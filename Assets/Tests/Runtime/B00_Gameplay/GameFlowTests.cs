using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B00_Gameplay
{
    // A whole game through the world-space screens of the real scene, clicked like a player would.
    public class GameFlowTests : RuntimeUITestFixture
    {
        PlayManager m_PlayManager;

        PlayManagerState state => m_PlayManager.state;

        // The scene loads once: loading it again would take away the panels the fixture uses.
        [UnityOneTimeSetUp]
        public IEnumerator LoadTheGame()
        {
            var assets = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
            yield return SceneManager.LoadSceneAsync(assets.mainSceneName, LoadSceneMode.Single);
            yield return null;
            m_PlayManager = Object.FindAnyObjectByType<PlayManager>();
        }

        [TearDown]
        public void TearDown()
        {
            state.GoToMainMenu();
            Time.timeScale = 1f;
        }

        static PanelRenderer Renderer<TDriver>() where TDriver : Component =>
            Object.FindAnyObjectByType<TDriver>(FindObjectsInactive.Include).GetComponent<PanelRenderer>();

        // The camera glides from screen to screen: wait for it before clicking through it.
        static IEnumerator WaitForTheCamera()
        {
            var end = Time.time + 2.5f;
            while (Time.time < end)
                yield return null;
        }

        [UnityTest]
        public IEnumerator AFreshGameShowsOnlyTheMainMenu()
        {
            yield return null;
            Assert.IsTrue(state.isMainMenu);

            SetPanelRenderer(Renderer<GameOverScreenDriver>());
            yield return null;
            Assert.AreEqual(DisplayStyle.None, rootVisualElement.resolvedStyle.display);

            SetPanelRenderer(Renderer<PauseScreenDriver>());
            yield return null;
            Assert.AreEqual(DisplayStyle.None, rootVisualElement.resolvedStyle.display);
        }

        [UnityTest]
        public IEnumerator PlayingThroughEveryScreen()
        {
            // Main menu > Play.
            SetPanelRenderer(Renderer<MainMenuScreenDriver>());
            yield return WaitForTheCamera();
            simulate.Click(rootVisualElement.Q<Button>("play-button"));
            yield return null;
            Assert.IsTrue(state.isBagelSelection);

            // Bagel selection: next bagel, then Select.
            SetPanelRenderer(Renderer<BagelSelectionControlsPaneDriver>());
            yield return WaitForTheCamera();
            simulate.Click(rootVisualElement.Q<Button>("right-button"));
            yield return null;
            Assert.AreEqual(1, state.selectedBagelIndex);
            simulate.Click(rootVisualElement.Q<Button>("select-button"));
            yield return null;
            Assert.IsTrue(state.isPlaying);

            // The run ends (the roll itself is BagelRollTests' job).
            state.FinishRun();
            yield return null;
            Assert.IsTrue(state.isGameOver);

            // Game over: the result shows, then back to the main menu.
            SetPanelRenderer(Renderer<GameOverScreenDriver>());
            yield return WaitForTheCamera();
            Assert.AreEqual(DisplayStyle.Flex, rootVisualElement.resolvedStyle.display);
            Assert.AreEqual("You Win!", rootVisualElement.Q<Label>("page-title").text);
            simulate.Click(rootVisualElement.Q<Button>("main-menu-button"));
            yield return null;
            Assert.IsTrue(state.isMainMenu);
        }

        // Over the coffee, the steam's panel draws with the vertex channels its material reads: the shader moves it.
        [UnityTest]
        public IEnumerator WhilePlayingTheSteamRisesOnTheGpu()
        {
            state.GoToBagelSelection();
            state.GoToPlay();
            SetPanelRenderer(Renderer<FaceTheCamera>());
            yield return null;
            yield return null;

            var steam = rootVisualElement.Q<SteamPuffs>();
            Assert.IsTrue(steam.movesOnTheGpu);
            Assert.AreEqual("Steam", steam.resolvedStyle.unityMaterial.material.name);
        }

        [UnityTest]
        public IEnumerator TheLeaderboardOpensFromTheMainMenu()
        {
            SetPanelRenderer(Renderer<MainMenuScreenDriver>());
            yield return WaitForTheCamera();

            simulate.Click(rootVisualElement.Q<Button>("leaderboard-button"));
            yield return null;
            Assert.AreEqual(PlayManagerState.MainMenuPane.Leaderboard, state.mainMenuPane);

            yield return WaitForTheCamera();
            simulate.Click(rootVisualElement.Q("leaderboard-pane").Q<Button>("back-button"));
            yield return null;
            Assert.AreEqual(PlayManagerState.MainMenuPane.Main, state.mainMenuPane);
        }
    }
}
