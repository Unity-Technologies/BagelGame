using NUnit.Framework;
using UnityEngine.UIElements.TestFramework;

namespace Bagel.B02_Common
{
    public class LongPressButtonTests : UITestFixture
    {
        [Test]
        public void LongPressButtonClick()
        {
            bool actioned = false;

            // Create the button.
            var button = new LongPressButton();
            button.text = "Test Button";
            button.clicked += () => actioned = true;
            rootVisualElement.Add(button);
            simulate.FrameUpdate();

            // Simple click should not trigger action.
            simulate.Click(button);
            simulate.FrameUpdate();
            Assert.IsFalse(actioned);

            // Long press should trigger action.
            simulate.MouseDown(button);
            simulate.FrameUpdate(button.holdTime);
            Assert.IsTrue(actioned);
        }
    }
}
