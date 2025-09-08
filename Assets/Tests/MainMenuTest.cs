using NUnit.Framework;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel
{
    public class MainMenuTest : UITestFixture
    {
        VisualElement m_Root;
        Button m_Button;
        bool m_DoneThing;

        [SetUp]
        public void SetUp()
        {
            m_Root = panel.visualTree;
            m_Button = new Button(DoThing) { text = "TestButton" };
            m_Root.Add(m_Button);
        }

        void DoThing()
        {
            m_DoneThing = true;
        }

        [Test]
        public void QuickTest()
        {
            var button = m_Root.Q<Button>();
            Assert.IsNotNull(button);
            Assert.AreEqual(button.text, "TestButton");

            m_DoneThing = false;
            button.SimulateClick();
            simulate.FrameUpdate();
            Assert.IsTrue(m_DoneThing);
        }
    }
}
