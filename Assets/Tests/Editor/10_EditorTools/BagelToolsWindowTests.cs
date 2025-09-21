using NUnit.Framework;
using UnityEditor.UIElements.TestFramework;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.T10_EditorTools
{
    public class BagelToolsWindowTests : EditorWindowUITestFixture<BagelToolsWindow>
    {
        [SetUp]
        public void SetUp()
        {
            simulate.FrameUpdate();
        }

        [Test]
        public void LabelExists()
        {
            var label = rootVisualElement.Q<Label>();
            Assert.IsNotNull(label);
        }
    }
}
