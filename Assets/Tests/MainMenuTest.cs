using NUnit.Framework;
using UnityEditor.UIElements.TestFramework;
using UnityEngine.UIElements;

namespace Bagel
{
    public class MainMenuTest : EditorWindowUITestFixture<BagelTestEditorWindow>
    {
        // TODO:
        /*
        - typing text
        - long press button
        - bindings
        */

        [SetUp]
        public void SetUp()
        {
            debugMode = true;
            simulate.FrameUpdate();
        }

        [Test]
        public void MainMenuButtonCallbacks()
        {
            bool playClicked = false;
            bool exitClicked = false;

            // Get the elements.
            var elements = MainMenuScreenDriver.BindUI(window.mainMenuRoot, new MainMenuScreenDriver.Callbacks
            {
                onPlay = () => playClicked = true,
                onExit = () => exitClicked = true
            });

            // Click play button.
            simulate.Click(elements.playButton);
            simulate.FrameUpdate();
            Assert.IsTrue(playClicked);

            // Click exit button (should not trigger yet).
            simulate.Click(elements.exitButton);
            simulate.FrameUpdate();
            Assert.IsFalse(exitClicked);

            // Long press exit button (should trigger).
            simulate.MouseDown(elements.exitButton);
            simulate.FrameUpdate(elements.exitButton.holdTime);
            Assert.IsTrue(exitClicked);
        }
    }
}
