using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel
{
    public class MainMenuRuntimeTest : UITestFixture
    {
        BagelTestAssets m_BagelTestAssets;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_BagelTestAssets = ScriptableObject.CreateInstance<BagelTestAssets>();
        }

        [Test]
        public void MainMenuNotNull()
        {
            Assert.IsNotNull(m_BagelTestAssets);
            Assert.IsNotNull(m_BagelTestAssets.mainMenuUxml);
        }
    }
}
