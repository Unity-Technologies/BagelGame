using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel
{
    public class PlayStatesTest : RuntimeUITestFixture
    {
        const string k_MainSceneName = "Main";

        BagelTestAssetList m_BagelTestAssets;

        [UnityOneTimeSetUp]
        public IEnumerator LoadScene()
        {
            var op = SceneManager.LoadSceneAsync(k_MainSceneName, LoadSceneMode.Single);
            Assert.IsNotNull(op, $"Scene '{k_MainSceneName}' not found or not in Build Settings.");
            yield return op;

            // Give Awake/OnEnable/Start a frame to run.
            yield return null;

            m_BagelTestAssets = ScriptableObject.CreateInstance<BagelTestAssetList>();

            var driver = Object.FindFirstObjectByType<MainMenuScreenDriver>(FindObjectsInactive.Include);
            var uiDocument = driver.GetComponent<UIDocument>();
            Assert.IsNotNull(uiDocument);
            SetUIContent(uiDocument);
        }

        [Test]
        public void MainMenuButtonCallbacks()
        {
            var playButton = rootVisualElement.Q<Button>("play-button");
            Assert.IsNotNull(playButton);

            Assert.IsNotNull(m_BagelTestAssets.mainMenuUxml);
        }
    }
}
