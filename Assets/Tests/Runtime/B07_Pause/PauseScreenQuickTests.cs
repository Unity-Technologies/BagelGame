using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B07_Pause
{
    public class PauseScreenQuickTests : UITestFixture
    {
        BagelTestAssetList m_BagelTestAssetList;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            m_BagelTestAssetList =
                Resources.Load<BagelTestAssetList>(
                    "BagelTestAssetList");
        }

        [Test]
        public void PauseScreen() {
            var root = rootVisualElement;
            m_BagelTestAssetList.pauseScreenUxml.CloneTree(root);
            simulate.FrameUpdate();

            var resumeButton = root.Q<Button>("resume-button");

            bool resumeClicked = false;
            resumeButton.clicked += () => resumeClicked = true;

            simulate.Click(resumeButton);
            simulate.FrameUpdate();
            Assert.IsTrue(resumeClicked);
        }
    }
}
