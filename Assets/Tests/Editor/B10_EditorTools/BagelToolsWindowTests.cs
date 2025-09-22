using NUnit.Framework;
using UnityEditor.UIElements.TestFramework;
using UnityEngine.UIElements;

namespace Bagel.B10_EditorTools
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
