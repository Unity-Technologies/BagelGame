using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bagel
{
    // The UI's design rules, checked on a laid-out screen: every control can be focused and says
    // what it is, buttons don't overlap, rounded corners fit their element, and text isn't cut off.
    public static class BagelLayoutAudit
    {
        const float k_Tolerance = 1f;

        public static List<string> Check(VisualElement screen)
        {
            var problems = new List<string>();
            var buttons = new List<VisualElement>();

            screen.Query().ForEach(element =>
            {
                if (!IsShown(element))
                    return;

                CheckRadii(element, problems);

                // A control's own parts (a ScrollView's scrollers) aren't the screen's controls.
                if (!element.enabledInHierarchy || element.GetFirstAncestorOfType<Scroller>() != null)
                    return;

                switch (element)
                {
                    case Button button:
                        CheckControl(button, button.text, problems);
                        CheckTextFits(button, problems);
                        buttons.Add(button);
                        break;
                    case LongPressButton longPressButton:
                        CheckControl(longPressButton, longPressButton.text, problems);
                        buttons.Add(longPressButton);
                        break;
                    case Toggle toggle:
                        CheckControl(toggle, toggle.label, problems);
                        break;
                    case Slider slider:
                        CheckControl(slider, slider.label, problems);
                        break;
                    case DropdownField dropdown:
                        CheckControl(dropdown, dropdown.label, problems);
                        break;
                    case Label label when label.ClassListContains("b-title"):
                        CheckTextFits(label, problems);
                        break;
                }
            });

            for (int i = 0; i < buttons.Count; ++i)
            {
                for (int j = i + 1; j < buttons.Count; ++j)
                {
                    var a = buttons[i].worldBound;
                    var b = buttons[j].worldBound;
                    var overlap = Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin), Mathf.Min(a.xMax, b.xMax), Mathf.Min(a.yMax, b.yMax));
                    if (overlap.width > k_Tolerance && overlap.height > k_Tolerance)
                        problems.Add($"{Describe(buttons[i])} overlaps {Describe(buttons[j])}");
                }
            }

            return problems;
        }

        static bool IsShown(VisualElement element)
        {
            for (var e = element; e != null; e = e.hierarchy.parent)
            {
                var style = e.resolvedStyle;
                if (style.display == DisplayStyle.None || style.visibility == Visibility.Hidden || style.opacity <= 0f)
                    return false;
            }
            return true;
        }

        static void CheckControl(VisualElement control, string text, List<string> problems)
        {
            if (string.IsNullOrWhiteSpace(text))
                problems.Add($"{Describe(control)} has no text or label");
            if (!control.focusable)
                problems.Add($"{Describe(control)} can't be focused with a gamepad or keyboard");
        }

        static void CheckRadii(VisualElement element, List<string> problems)
        {
            var style = element.resolvedStyle;
            var size = element.layout.size;
            if (size.x <= 0f || size.y <= 0f || float.IsNaN(size.x))
                return;

            var limit = Mathf.Min(size.x, size.y) * 0.5f + k_Tolerance;
            var radius = Mathf.Max(Mathf.Max(style.borderTopLeftRadius, style.borderTopRightRadius), Mathf.Max(style.borderBottomLeftRadius, style.borderBottomRightRadius));
            if (radius > limit)
                problems.Add($"{Describe(element)}'s corner radius {radius} is more than half its {size}");
        }

        static void CheckTextFits(TextElement element, List<string> problems)
        {
            if (string.IsNullOrEmpty(element.text) || element.resolvedStyle.whiteSpace != WhiteSpace.NoWrap)
                return;

            var available = element.contentRect.width;
            var needed = element.MeasureTextSize(element.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x;
            if (needed > available + k_Tolerance)
                problems.Add($"{Describe(element)}'s text \"{element.text}\" needs {needed:F0}px but has {available:F0}px");
        }

        static string Describe(VisualElement element) =>
            string.IsNullOrEmpty(element.name) ? element.GetType().Name : $"{element.GetType().Name} #{element.name}";
    }
}
