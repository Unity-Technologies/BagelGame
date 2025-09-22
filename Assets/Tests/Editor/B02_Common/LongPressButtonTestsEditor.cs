using NUnit.Framework;
using UnityEngine;
using UnityEditor.UIElements.TestFramework;
using UnityEngine.UIElements;

namespace Bagel.B02_Common
{
    public class LongPressButtonTestsEditor : LongPressButtonTests
    {
        StylesApplicator m_StylesApplicator;

        public LongPressButtonTestsEditor()
        {
            m_StylesApplicator = AddTestComponent<StylesApplicator>();
        }

        [Test]
        public void LongPressButtonChangeTimerProgressColor()
        {
            // Create the button.
            var button = new LongPressButton();
            button.text = "Test Button";
            var timerProgress = button.Q(className: LongPressButton.progressUssClassName);
            rootVisualElement.Add(button);
            simulate.FrameUpdate();

            // Change the timer progress color.
            m_StylesApplicator.AddStylesToElement(button, $@"
                .{LongPressButton.progressUssClassName} {{
                    background-color: #ff0000;
                }}");
            simulate.FrameUpdate();
            Assert.AreEqual(new Color(1f, 0f, 0f, 1f), timerProgress.resolvedStyle.backgroundColor);
        }
    }
}
