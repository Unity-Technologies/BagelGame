using NUnit.Framework;
using UnityEditor.UIElements.TestFramework;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Bagel
{
    public class MainMenuTest : EditorWindowUITestFixture<BagelTestEditorWindow>
    {
        Button m_Button;
        bool m_DoneThing;

        [SetUp]
        public void SetUp()
        {
            // m_Button = new Button() { text = "TestButton" };
            // m_Button.RegisterCallback<ClickEvent>(e => DoThing());
            // rootVisualElement.Add(m_Button);
            // simulate.FrameUpdate();
        }

        void DoThing()
        {
            m_DoneThing = true;
        }

        [Test]
        public void MainMenu()
        {
            /*var elements = MainMenuScreenDriver.BindUI(window.mainMenuRoot, new MainMenuScreenDriver.Callbacks
            {
                onPlay = () => m_DoneThing = true,
                onExit = () => m_DoneThing = true
            });*/
        }


        [Test]
        public void QuickTest()
        {
            // TODO:
            /*
            - typing text
            - long press button
            - bindings
            */

            m_Button = new Button(DoThing) { text = "TestButton" };
            rootVisualElement.Add(m_Button);
            simulate.FrameUpdate();

            var button = rootVisualElement.Q<Button>();
            Assert.IsNotNull(button);
            Assert.AreEqual(button.text, "TestButton");
            Assert.Greater(button.resolvedStyle.height, 0.0f);

            m_DoneThing = false;
            simulate.Click(button);
            simulate.FrameUpdate();
            Assert.IsTrue(m_DoneThing);
        }
    }
}
