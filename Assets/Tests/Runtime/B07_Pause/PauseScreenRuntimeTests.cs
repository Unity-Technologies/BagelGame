using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B07_Pause
{
    public class PauseScreenRuntimeTests : RuntimeUITestFixture
    {
        PlayManager m_PlayManager;

        [UnityOneTimeSetUp]
        public IEnumerator UnityOneTimeSetUp()
        {
            var op = SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return op;
            yield return null; // Give Awake/OnEnable/Start a frame to run.
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_PlayManager = Object.FindFirstObjectByType<PlayManager>(FindObjectsInactive.Include);
            var driver = Object.FindFirstObjectByType<PauseScreenDriver>(FindObjectsInactive.Include);
            var doc = driver.GetComponent<UIDocument>();
            SetUIContent(doc);
        }

        [UnityTest]
        public IEnumerator GoToSettingsInPauseMenu()
        {
            simulate.FrameUpdate();

            var root = rootVisualElement;
            var settingsPane = root.Q<VisualElement>("settings-pane");
            var settingsButton = root.Q<Button>("settings-button");

            m_PlayManager.State.GoToPlay();
            yield return null;

            m_PlayManager.State.Pause();
            yield return null;

            Assert.AreEqual(DisplayStyle.None, settingsPane.resolvedStyle.display);
            simulate.Click(settingsButton);
            simulate.FrameUpdate();
            Assert.AreEqual(DisplayStyle.Flex, settingsPane.resolvedStyle.display);

            yield return null;
        }
    }
}
