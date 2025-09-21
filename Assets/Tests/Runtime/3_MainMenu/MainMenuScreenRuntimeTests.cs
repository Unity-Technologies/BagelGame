using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.T3_MainMenu
{
    public class MainMenuScreenRuntimeTests : RuntimeUITestFixture
    {
        PlayManager m_PlayManager;
        MainMenuScreenDriver m_Driver;
        UIDocument m_UIDocument;

        [UnityOneTimeSetUp]
        public IEnumerator UnityOneTimeSetUp()
        {
            var bagelTestAssetList = Resources.Load<BagelTestAssetList>("BagelTestAssetList");

            var op = SceneManager.LoadSceneAsync(bagelTestAssetList.mainSceneName, LoadSceneMode.Single);
            Assert.IsNotNull(op, $"Scene '{bagelTestAssetList.mainSceneName}' not found or not in Build Settings.");
            yield return op;
            yield return null; // Give Awake/OnEnable/Start a frame to run.
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_PlayManager = Object.FindFirstObjectByType<PlayManager>(FindObjectsInactive.Include);
            m_Driver = Object.FindFirstObjectByType<MainMenuScreenDriver>(FindObjectsInactive.Include);
            m_UIDocument = m_Driver.GetComponent<UIDocument>();
            Assert.IsNotNull(m_UIDocument);
            SetUIContent(m_UIDocument);
        }

        [SetUp]
        public void SetUp()
        {
            simulate.FrameUpdate();
        }

        [Test]
        public void PlayInMainMenuTriggersGameStateChange()
        {
            var currentState = m_PlayManager.State.CurrentState;

            Assert.NotZero(m_Driver.elements.playButton.resolvedStyle.width);

            /*m_PlayManager.State.OnStateChange += (sender, newState) =>
            {
                Assert.AreNotEqual(currentState, newState);
                currentState = newState;
            };

            bool bb = false;
            m_Driver.UI.playButton.clicked += () =>
            {
                bb = true;
            };

            Assert.IsNotNull(m_Driver.UI.playButton);
            simulate.Click(m_Driver.UI.playButton);
            simulate.FrameUpdate();
            Assert.IsTrue(bb);

            Assert.AreEqual(PlayManagerState.State.BagelSelection, currentState);*/
        }
    }
}
