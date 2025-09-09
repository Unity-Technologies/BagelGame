using NUnit.Framework;
using UnityEditor.UIElements.TestFramework;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel
{
    public class MainMenuTest : DebugUITestFixture
    {
        bool m_DoneThing;

        void DoThing()
        {
            m_DoneThing = true;
        }

        [Test]
        public void QuickTest()
        {
            var button = new Button(DoThing) { text = "TestButton" };
            rootVisualElement.Add(button);
            simulate.FrameUpdate();

            button = rootVisualElement.Q<Button>();
            Assert.IsNotNull(button);
            Assert.AreEqual(button.text, "TestButton");
            Assert.Greater(button.layout.width, 0);

            m_DoneThing = false;
            button.SimulateClick();
            simulate.FrameUpdate();
            Assert.IsTrue(m_DoneThing);
        }
    }
}
