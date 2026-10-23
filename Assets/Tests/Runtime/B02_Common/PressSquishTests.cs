using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B02_Common
{
    public class PressSquishTests : UITestFixture
    {
        [TearDown]
        public void TearDown()
        {
            rootVisualElement.Clear();
        }

        [Test]
        public void PressingSquishesAndReleasingRestores()
        {
            var button = new Button { text = "Squish" };
            button.AddComponent(new PressSquish { pressedScale = 0.8f });
            rootVisualElement.Add(button);
            simulate.FrameUpdate();

            simulate.MouseDown(button);
            simulate.FrameUpdate();
            Assert.AreEqual(0.8f, button.resolvedStyle.scale.value.y, 1e-4f);

            simulate.MouseUp(button);
            simulate.FrameUpdate();
            Assert.AreEqual(1f, button.resolvedStyle.scale.value.y, 1e-4f);
        }

        [Test]
        public void TheMenuButtonsSquish()
        {
            var assets = Resources.Load<BagelTestAssetList>("BagelTestAssetList");
            assets.mainMenuUxml.CloneTree(rootVisualElement);
            simulate.FrameUpdate();

            foreach (var button in rootVisualElement.Query<Button>().ToList())
                Assert.IsTrue(button.HasComponent<PressSquish>(), button.name);
        }
    }
}
