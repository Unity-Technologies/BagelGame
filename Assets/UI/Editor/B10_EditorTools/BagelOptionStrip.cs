using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Bagel
{
    // A strip of icon buttons, like the main toolbar's own button groups: options to pick from (the
    // chosen one lit, the others dimmed), actions (Reset) and toggles (the Inspector). The theme and
    // language pickers of the main toolbar (BagelToolbar) and of the Bagel All UI window are strips, and
    // so are the window's game states. The main toolbar draws the group around its options itself, so
    // there they're made one by one with CreateOption.
    [UxmlElement(libraryPath = "Bagel/Editor")]
    public partial class BagelOptionStrip : VisualElement
    {
        public const string styleSheetPath = "Assets/UI/Editor/B10_EditorTools/BagelOptionStrip.uss";

        public static readonly string ussClassName = "b-strip";
        public static readonly UniqueStyleString lightSkinUssClassName = new(ussClassName + "--light-skin");
        public static readonly string optionUssClassName = ussClassName + "__option";
        public static readonly UniqueStyleString selectedOptionUssClassName = new(optionUssClassName + "--selected");
        public static readonly string actionOptionUssClassName = optionUssClassName + "--action";
        public static readonly string textOptionUssClassName = optionUssClassName + "--text";
        public static readonly string labelUssClassName = ussClassName + "__label";
        public static readonly string iconUssClassName = ussClassName + "__icon";

        int m_SelectedIndex = -1;

        // Raised with an option's index when it's clicked, unless it's the one already chosen.
        public event Action<int> chosen;

        public int selectedIndex => m_SelectedIndex;

        public BagelOptionStrip()
        {
            AddToClassList(ussClassName);
            EnableInClassList(lightSkinUssClassName, !EditorGUIUtility.isProSkin);
            AddStyleSheet(this);
        }

        // An option at the end of the strip. Its tooltip is its name, then the strip's own tooltip.
        public VisualElement AddOption(VisualElement icon, string optionName, string elementName = null)
        {
            var index = childCount;
            var option = CreateOption(icon, Tooltip(optionName), index == m_SelectedIndex, () =>
            {
                if (index == m_SelectedIndex)
                    return;
                SetSelectedWithoutNotify(index);
                chosen?.Invoke(index);
            });
            option.name = elementName;
            Add(option);
            return option;
        }

        // A button that does something (Reset) rather than pick: never the chosen one.
        public VisualElement AddAction(VisualElement icon, string actionName, Action click, string elementName = null)
        {
            var option = CreateOption(icon, Tooltip(actionName), false, click);
            option.AddToClassList(actionOptionUssClassName);
            option.name = elementName;
            Add(option);
            return option;
        }

        // A button with words that does something (Setups), an icon before them if given.
        public VisualElement AddTextAction(string text, string actionTooltip, Action click, VisualElement icon = null, string elementName = null)
        {
            var option = new VisualElement { name = elementName, tooltip = actionTooltip };
            option.AddToClassList(optionUssClassName);
            option.AddToClassList(actionOptionUssClassName);
            option.AddToClassList(textOptionUssClassName);
            if (icon != null)
            {
                icon.pickingMode = PickingMode.Ignore;
                icon.AddToClassList(iconUssClassName);
                option.Add(icon);
            }
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(labelUssClassName);
            option.Add(label);
            option.AddManipulator(new Clickable(click));
            Add(option);
            return option;
        }

        // A button that shows or hides something (the Inspector), or starts and stops it (Simulate Roll): lit while on.
        public VisualElement AddToggle(VisualElement icon, string toggleName, bool on, Action<bool> toggled, string elementName = null)
        {
            VisualElement option = null;
            option = CreateOption(icon, Tooltip(toggleName), on, () =>
            {
                var value = !IsOn(option);
                SetOnWithoutNotify(option, value);
                toggled?.Invoke(value);
            });
            option.name = elementName;
            Add(option);
            return option;
        }

        public static bool IsOn(VisualElement toggle) => toggle.ClassListContains(selectedOptionUssClassName);

        public static void SetOnWithoutNotify(VisualElement toggle, bool on) => toggle?.EnableInClassList(selectedOptionUssClassName, on);

        public void SetSelectedWithoutNotify(int index)
        {
            m_SelectedIndex = index;
            for (int i = 0; i < childCount; i++)
                this[i].EnableInClassList(selectedOptionUssClassName, i == index);
        }

        string Tooltip(string optionName) => string.IsNullOrEmpty(tooltip) ? optionName : $"{optionName}\n{tooltip}";

        // One option: an icon in a button, lit when it's the chosen one.
        public static VisualElement CreateOption(VisualElement icon, string optionTooltip, bool selected, Action click)
        {
            var option = new VisualElement { tooltip = optionTooltip };
            option.AddToClassList(optionUssClassName);
            option.EnableInClassList(selectedOptionUssClassName, selected);
            icon.pickingMode = PickingMode.Ignore;
            icon.AddToClassList(iconUssClassName);
            option.Add(icon);
            option.AddManipulator(new Clickable(click));
            return option;
        }

        // The strip's styles, for an option outside a strip (the main toolbar's).
        public static void AddStyleSheet(VisualElement element)
        {
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(styleSheetPath);
            if (sheet != null && !element.styleSheets.Contains(sheet))
                element.styleSheets.Add(sheet);
        }
    }
}
