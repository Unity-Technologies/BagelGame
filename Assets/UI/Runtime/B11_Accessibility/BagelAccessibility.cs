using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace Bagel
{
    // Screen reader support for the screen that's in front of the player. Unity generates
    // accessibility hierarchies for screen-space panels only, and most of this game's UI is in
    // world space, so this builds the hierarchy itself: each control becomes a node with a role,
    // a label and a value, and its frame is the element projected through the camera.
    [NoAutoStaticsCleanup] // ResetStatics starts every Play session with no screen
    public static class BagelAccessibility
    {
        static readonly Dictionary<AccessibilityNode, VisualElement> s_Elements = new();
        static readonly Dictionary<AccessibilityNode, Actions> s_Actions = new();

        // What a node does when the screen reader activates or adjusts it.
        struct Actions
        {
            public Func<bool> invoke;
            public Action increment;
            public Action decrement;
        }

        static VisualElement s_Screen;
        static PanelRenderer s_PanelRenderer;
        static AccessibilityHierarchy s_Hierarchy;
        static bool s_Subscribed;

        public static AccessibilityHierarchy hierarchy => s_Hierarchy;
        public static VisualElement screen => s_Screen;

        // Raised after each rebuild, for tests and tools.
        public static event Action<AccessibilityHierarchy> hierarchyChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Screen = null;
            s_PanelRenderer = null;
            s_Hierarchy = null;
            s_Elements.Clear();
            s_Actions.Clear();
        }

        // Called by the Drivers when their screen comes to the front, or changes pane. Null while
        // nothing on screen can be used (the bagel is rolling).
        public static void ShowScreen(VisualElement screenRoot)
        {
            s_Screen = screenRoot;
            s_PanelRenderer = screenRoot != null ? PanelRenderer.FindPanelRenderer(screenRoot) : null;
            Subscribe();

            // Wait for the panel to apply the change (pane classes, bindings, layout) before reading it.
            if (screenRoot != null)
                screenRoot.schedule.Execute(Rebuild);
            else
                Rebuild();
        }

        // Says something without moving the screen reader's cursor, like the bagel that was just picked.
        public static void Announce(string text)
        {
            if (Application.isPlaying && AssistiveSupport.isScreenReaderEnabled && !string.IsNullOrEmpty(text))
                AssistiveSupport.notificationDispatcher.SendAnnouncement(text);
        }

        public static void Rebuild()
        {
            if (s_Screen != null && s_Screen.panel == null)
                return;

            s_Hierarchy = s_Screen != null ? Build(s_Screen, s_PanelRenderer) : new AccessibilityHierarchy();

            if (Application.isPlaying && (AssistiveSupport.isScreenReaderEnabled || Application.isEditor))
            {
                AssistiveSupport.activeHierarchy = s_Hierarchy;
                AssistiveSupport.notificationDispatcher.SendScreenChanged();
            }

            hierarchyChanged?.Invoke(s_Hierarchy);
        }

        public static AccessibilityHierarchy Build(VisualElement screenRoot, PanelRenderer panelRenderer)
        {
            s_Elements.Clear();
            s_Actions.Clear();
            var builder = new Builder(new AccessibilityHierarchy(), panelRenderer);
            builder.Walk(screenRoot, null);
            return builder.hierarchy;
        }

        public static VisualElement GetElement(AccessibilityNode node) => s_Elements.TryGetValue(node, out var element) ? element : null;

        // The same as a screen reader activating or adjusting the node.
        public static bool Invoke(AccessibilityNode node) => s_Actions.TryGetValue(node, out var actions) && actions.invoke != null && actions.invoke();
        public static void Increment(AccessibilityNode node) { if (s_Actions.TryGetValue(node, out var actions)) actions.increment?.Invoke(); }
        public static void Decrement(AccessibilityNode node) { if (s_Actions.TryGetValue(node, out var actions)) actions.decrement?.Invoke(); }

        static void SetActions(AccessibilityNode node, Func<bool> invoke, Action increment = null, Action decrement = null)
        {
            s_Actions[node] = new Actions { invoke = invoke, increment = increment, decrement = decrement };
            if (invoke != null)
                node.invoked += () => Invoke(node);
            if (increment != null)
                node.incremented += () => Increment(node);
            if (decrement != null)
                node.decremented += () => Decrement(node);
        }

        static void Subscribe()
        {
            if (s_Subscribed)
                return;
            s_Subscribed = true;

            BagelLocalization.localeChanged += () => s_Screen?.schedule.Execute(Rebuild);
            AssistiveSupport.screenReaderStatusChanged += _ => Rebuild();
        }

        // Where an element is on screen, in pixels from the top-left corner, as screen readers expect.
        public static Rect GetScreenFrame(VisualElement element, PanelRenderer panelRenderer)
        {
            var bounds = element.worldBound;
            if (element.panel == null || float.IsNaN(bounds.width) || float.IsNaN(bounds.height))
                return Rect.zero;

            var camera = Camera.main;
            if (panelRenderer == null || camera == null || panelRenderer.panelSettings == null)
                return bounds;

            if (panelRenderer.panelSettings.renderMode != PanelRenderMode.WorldSpace)
            {
                // Screen space: the panel's pixels are the screen's, scaled.
                var screenSize = new Vector2(Screen.width, Screen.height);
                var panelSize = RuntimePanelUtils.ScreenToPanel(element.panel, screenSize) - RuntimePanelUtils.ScreenToPanel(element.panel, Vector2.zero);
                var scale = new Vector2(screenSize.x / Mathf.Abs(panelSize.x), screenSize.y / Mathf.Abs(panelSize.y));
                return new Rect(bounds.position * scale, bounds.size * scale);
            }

            // World space: the element's bounds are in its PanelRenderer's local space.
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 4; ++i)
            {
                var corner = new Vector3(i % 2 == 0 ? bounds.xMin : bounds.xMax, i < 2 ? bounds.yMin : bounds.yMax, 0f);
                var point = camera.WorldToScreenPoint(panelRenderer.transform.TransformPoint(corner));
                if (point.z < 0f)
                    return Rect.zero; // Behind the camera.

                var screenPoint = new Vector2(point.x, Screen.height - point.y);
                min = Vector2.Min(min, screenPoint);
                max = Vector2.Max(max, screenPoint);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        static bool IsShown(VisualElement element)
        {
            var style = element.resolvedStyle;
            return style.display != DisplayStyle.None && style.visibility == Visibility.Visible && style.opacity > 0f;
        }

        struct Builder
        {
            public readonly AccessibilityHierarchy hierarchy;
            readonly PanelRenderer m_PanelRenderer;

            public Builder(AccessibilityHierarchy hierarchy, PanelRenderer panelRenderer)
            {
                this.hierarchy = hierarchy;
                m_PanelRenderer = panelRenderer;
            }

            public void Walk(VisualElement element, AccessibilityNode parent)
            {
                if (!IsShown(element))
                    return;

                switch (element)
                {
                    case Button button:
                        AddButton(button, button.text, parent, () => Submit(button));
                        return;
                    case LongPressButton longPressButton:
                        // A screen reader double-tap can't be held: it completes the press.
                        AddButton(longPressButton, longPressButton.text, parent, longPressButton.Activate);
                        return;
                    case Toggle toggle:
                        AddToggle(toggle, parent);
                        return;
                    case Slider slider:
                        AddSlider(slider, parent);
                        return;
                    case DropdownField dropdown:
                        AddDropdown(dropdown, dropdown.label, dropdown.value, parent);
                        return;
                    case TextField textField:
                        AddTextField(textField, parent);
                        return;
                    case ProgressBar progressBar:
                        AddText(progressBar, progressBar.title, parent, $"{Mathf.RoundToInt(Mathf.InverseLerp(progressBar.lowValue, progressBar.highValue, progressBar.value) * 100f)}%");
                        return;
                    case IntegerField integerField:
                        AddText(integerField, integerField.label, parent, BagelConverters.FormatNumber(integerField.value));
                        return;
                    case TabView tabView:
                        AddTabView(tabView, parent);
                        return;
                    case MultiColumnListView listView:
                        AddList(listView, parent);
                        return;
                    case Label label:
                        if (!string.IsNullOrWhiteSpace(label.text))
                            AddText(label, label.text, parent, role: label.ClassListContains("b-title") ? AccessibilityRole.Header : AccessibilityRole.StaticText);
                        return;
                }

                // Panes shown in the background are disabled (the main menu's other pages): skip them.
                if (!element.enabledSelf)
                    return;

                // A popup field that isn't a DropdownField (the language dropdown).
                if (element is BaseField<Unity.Localization.Locale> popup)
                {
                    AddDropdown(popup, popup.label, popup.value?.LocaleName, parent);
                    return;
                }

                foreach (var child in element.hierarchy.Children())
                    Walk(child, parent);
            }

            AccessibilityNode AddNode(VisualElement element, string label, AccessibilityRole role, AccessibilityNode parent)
            {
                var node = hierarchy.AddNode(label, parent);
                node.role = role;
                node.isActive = true;
                var panelRenderer = m_PanelRenderer;
                node.frameGetter = () => GetScreenFrame(element, panelRenderer);
                if (!element.enabledInHierarchy)
                    node.state |= AccessibilityState.Disabled;
                s_Elements[node] = element;
                return node;
            }

            void AddButton(VisualElement element, string label, AccessibilityNode parent, Action invoke)
            {
                var node = AddNode(element, label, AccessibilityRole.Button, parent);
                SetActions(node, () =>
                {
                    if (!element.enabledInHierarchy)
                        return false;
                    invoke();
                    return true;
                });
                node.focusChanged += (_, focused) => { if (focused) element.Focus(); };
            }

            void AddToggle(Toggle toggle, AccessibilityNode parent)
            {
                var node = AddNode(toggle, toggle.label, AccessibilityRole.Toggle, parent);
                if (toggle.value)
                    node.state |= AccessibilityState.Selected;
                SetActions(node, () =>
                {
                    toggle.value = !toggle.value;
                    node.state = toggle.value ? node.state | AccessibilityState.Selected : node.state & ~AccessibilityState.Selected;
                    return true;
                });
                node.focusChanged += (_, focused) => { if (focused) toggle.Focus(); };
            }

            void AddSlider(Slider slider, AccessibilityNode parent)
            {
                var node = AddNode(slider, slider.label, AccessibilityRole.Slider, parent);
                node.value = Mathf.RoundToInt(slider.value).ToString(BagelLocalization.culture);
                var step = (slider.highValue - slider.lowValue) / 10f;
                void Change(float delta)
                {
                    slider.value = Mathf.Clamp(slider.value + delta, slider.lowValue, slider.highValue);
                    node.value = Mathf.RoundToInt(slider.value).ToString(BagelLocalization.culture);
                }
                SetActions(node, null, () => Change(step), () => Change(-step));
                node.focusChanged += (_, focused) => { if (focused) slider.Focus(); };
            }

            void AddDropdown(VisualElement field, string label, string value, AccessibilityNode parent)
            {
                var node = AddNode(field, label, AccessibilityRole.Dropdown, parent);
                node.value = value;

                // Adjusting cycles through the choices, without opening the menu.
                void Cycle(int direction)
                {
                    switch (field)
                    {
                        case DropdownField dropdown when dropdown.choices.Count > 0:
                            dropdown.index = (dropdown.index + direction + dropdown.choices.Count) % dropdown.choices.Count;
                            node.value = dropdown.value;
                            break;
                        case PopupField<Unity.Localization.Locale> popup when popup.choices.Count > 0:
                            var index = (popup.choices.IndexOf(popup.value) + direction + popup.choices.Count) % popup.choices.Count;
                            popup.value = popup.choices[index];
                            node.value = popup.value?.LocaleName;
                            break;
                    }
                }
                SetActions(node, () => { Cycle(1); return true; }, () => Cycle(1), () => Cycle(-1));
                node.focusChanged += (_, focused) => { if (focused) field.Focus(); };
            }

            void AddTextField(TextField textField, AccessibilityNode parent)
            {
                var node = AddNode(textField, textField.label, AccessibilityRole.TextField, parent);
                node.value = textField.value;
                SetActions(node, () => { textField.Focus(); return true; });
                node.focusChanged += (_, focused) => { if (focused) textField.Focus(); };
            }

            void AddText(VisualElement element, string label, AccessibilityNode parent, string value = null, AccessibilityRole role = AccessibilityRole.StaticText)
            {
                var node = AddNode(element, label, role, parent);
                node.value = value;
            }

            void AddTabView(TabView tabView, AccessibilityNode parent)
            {
                var tabBar = AddNode(tabView, null, AccessibilityRole.TabBar, parent);
                foreach (var child in tabView.Children())
                {
                    if (child is not Tab tab)
                        continue;

                    var node = AddNode(tab.tabHeader, tab.label, AccessibilityRole.TabButton, tabBar);
                    if (tabView.activeTab == tab)
                        node.state |= AccessibilityState.Selected;
                    SetActions(node, () =>
                    {
                        tabView.activeTab = tab;
                        tabView.schedule.Execute(Rebuild);
                        return true;
                    });
                }

                if (tabView.activeTab != null)
                {
                    foreach (var child in tabView.activeTab.Children())
                        Walk(child, parent);
                }
            }

            void AddList(MultiColumnListView listView, AccessibilityNode parent)
            {
                var list = AddNode(listView, null, AccessibilityRole.ScrollView, parent);
                foreach (var row in listView.Query(className: BaseListView.itemUssClassName).ToList())
                {
                    if (!IsShown(row))
                        continue;

                    var cells = new List<string>();
                    foreach (var cell in row.Query<Label>().ToList())
                    {
                        if (!string.IsNullOrWhiteSpace(cell.text))
                            cells.Add(cell.text);
                    }
                    if (cells.Count > 0)
                        AddText(row, string.Join(", ", cells), list);
                }
            }

            static void Submit(Button button)
            {
                using var evt = NavigationSubmitEvent.GetPooled();
                evt.target = button;
                button.SendEvent(evt);
            }
        }
    }
}
