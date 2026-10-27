using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Bagel
{
    // Settings: a window of tabs (Game, Look, Graphics, Controls, About), the same one on the blackboard and over the
    // paused game. Every tab fits in the window: nothing scrolls. On the blackboard (in a b-board), changing tab wipes
    // the old one off and writes the new one in (ChalkWipe), quickly; elsewhere the tab just changes.
    //
    // With the keyboard or a gamepad, the tabs are a column to go up and down, each shown as soon as it has the focus,
    // with Back under them. Across from the tabs (right, or left in a right-to-left language) is the chosen tab's page,
    // where the arrows go from setting to setting and never out of the window, and back across to the tab; left and
    // right move a slider. Cancel goes from a setting back to its tab, and from the tabs out of Settings. The shoulder
    // buttons, or Q and E, change tab from anywhere in the window. Tab and Shift+Tab go through all of it in reading
    // order: each tab's name and then its settings, Back last, and round again.
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class SettingsPaneManager : BagelPane
    {
        // The blackboard's surface (BagelCommonStyles).
        public static readonly string boardUssClassName = "b-board";

        // The UI actions that change tab (UIInputActions).
        public static readonly string previousTabActionName = "UI/PreviousTab";
        public static readonly string nextTabActionName = "UI/NextTab";

        // Of a slider's range, how far an arrow moves it.
        const float k_SliderStep = 1f / 20f;

        SettingsPaneManagerForGame m_SettingsPaneManagerForGame;
        SettingsPaneManagerForUI m_SettingsPaneManagerForUI;
        SettingsPaneManagerForGraphics m_SettingsPaneManagerForGraphics;
        SettingsPaneManagerForControls m_SettingsPaneManagerForControls;
        TabView m_TabView;
        Button m_BackButton;

        public SettingsPaneManagerForGame forGame => m_SettingsPaneManagerForGame ??= this.Q<SettingsPaneManagerForGame>();
        public SettingsPaneManagerForUI forUI => m_SettingsPaneManagerForUI ??= this.Q<SettingsPaneManagerForUI>();
        public SettingsPaneManagerForGraphics forGraphics => m_SettingsPaneManagerForGraphics ??= this.Q<SettingsPaneManagerForGraphics>();
        public SettingsPaneManagerForControls forControls => m_SettingsPaneManagerForControls ??= this.Q<SettingsPaneManagerForControls>();
        public TabView tabView => m_TabView ??= this.Q<TabView>("settings-tab-view");
        public Button backButton => m_BackButton ??= this.Q<Button>("back-button");

        readonly List<Tab> m_Tabs = new();
        readonly Dictionary<Tab, VisualElement> m_LastFocused = new();
        InputAction m_PreviousTabAction;
        InputAction m_NextTabAction;

        public IReadOnlyList<Tab> tabs => m_Tabs;

        public struct Callbacks
        {
            public Action onSettingsBack;
        }

        public void BindUI(Callbacks callbacks)
        {
            if (backButton != null && callbacks.onSettingsBack != null)
                backButton.clicked += callbacks.onSettingsBack;
        }

        bool m_Bound;

        protected override void OnAttached()
        {
            ListenForTabActions(true);

            if (m_Bound)
                return;
            m_Bound = true;

            // The title isn't from a template here, but it has the same name: its binding is added here.
            BagelLocalization.Bind(this.Q<Label>("page-title"), "text", "settings.title");

            if (tabView != null)
            {
                tabView.Query<Tab>().ToList(m_Tabs);
                tabView.activeTabChanged += TabView_OnActiveTabChanged;
                foreach (var tab in m_Tabs)
                {
                    var header = tab.tabHeader;
                    header.focusable = true;
                    header.tabIndex = 0;
                    header.RegisterCallback<FocusInEvent>(_ => tabView.activeTab = tab);
                }
            }
            MainMenuPaneManager.ShowFocusOnItem(backButton);

            RegisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);
            RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);
            RegisterCallback<FocusInEvent>(OnFocusIn);
        }

        protected override void OnDetached() => ListenForTabActions(false);

        void TabView_OnActiveTabChanged(Tab previous, Tab next)
        {
            var instant = !onBoard || state == null || state.settings.reduceMotion || !state.settings.visualEffects;
            ChalkWipe.Erase(previous, instant);
            ChalkWipe.Write(next, instant);
        }

        bool onBoard
        {
            get
            {
                for (var element = parent; element != null; element = element.parent)
                {
                    if (element.ClassListContains(boardUssClassName))
                        return true;
                }
                return false;
            }
        }

        // ------------------------------------------------------------------ Keyboard and gamepad

        // Where the focus goes when Settings open: the chosen tab.
        public void FocusTabs() => (tabView?.activeTab?.tabHeader ?? backButton)?.Focus();

        public void ShowPreviousTab() => ShowTab(-1);

        public void ShowNextTab() => ShowTab(1);

        // The tab before or after the chosen one, and the focus with it: on its name if the focus was on the tabs, on
        // its first setting if it was in the page.
        void ShowTab(int step)
        {
            var index = m_Tabs.IndexOf(tabView?.activeTab);
            if (index < 0)
                return;
            var next = m_Tabs[(index + step + m_Tabs.Count) % m_Tabs.Count];
            var inPage = focusedPage != null;
            tabView.activeTab = next;
            next.tabHeader.Focus();
            // Its page is only laid out once it's shown, on the next update.
            if (inPage)
                schedule.Execute(() =>
                {
                    if (tabView.activeTab == next && focused == next.tabHeader)
                        FocusPage(next);
                }).StartingIn(1);
        }

        VisualElement focused => focusController?.focusedElement as VisualElement;

        // The tab whose page has the focus, if one has.
        Tab focusedPage
        {
            get
            {
                var element = focused;
                foreach (var tab in m_Tabs)
                {
                    if (element != null && element != tab.tabHeader && tab.contentContainer.Contains(element))
                        return tab;
                }
                return null;
            }
        }

        // Into a tab's page: where the focus last was in it, or its first setting.
        bool FocusPage(Tab tab)
        {
            if (m_LastFocused.TryGetValue(tab, out var last) && IsNavigable(last) && tab.contentContainer.Contains(last))
            {
                last.Focus();
                return true;
            }
            var settings = Settings(tab);
            if (settings.Count == 0)
                return false;
            settings[0].Focus();
            return true;
        }

        // A tab's settings, in reading order: a row of choices one by one.
        List<VisualElement> Settings(Tab tab)
        {
            var settings = Navigables(tab.contentContainer);
            settings.Sort((a, b) => ReadingOrder(Bounds(a), Bounds(b)));
            return settings;
        }

        void OnFocusIn(FocusInEvent evt)
        {
            // The element itself, not the field it's in: in a row of choices, the choice.
            var element = evt.target as VisualElement;
            var page = focusedPage;
            if (page != null && IsNavigable(element) && page.contentContainer.Contains(element))
                m_LastFocused[page] = element;
        }

        void OnNavigationMove(NavigationMoveEvent evt)
        {
            // The element with the focus itself, not the field it's in (the event goes to it).
            var from = evt.target as VisualElement ?? focused;
            if (from == null || !Contains(from) || tabView == null)
                return;

            var rightToLeft = BagelLocalization.isRightToLeft;
            var inward = rightToLeft ? NavigationMoveEvent.Direction.Left : NavigationMoveEvent.Direction.Right;
            var outward = rightToLeft ? NavigationMoveEvent.Direction.Right : NavigationMoveEvent.Direction.Left;
            var direction = evt.direction;
            var headerIndex = m_Tabs.FindIndex(tab => tab.tabHeader == from);
            VisualElement to = null;

            if (direction == NavigationMoveEvent.Direction.Next || direction == NavigationMoveEvent.Direction.Previous)
            {
                Step(from, headerIndex, direction == NavigationMoveEvent.Direction.Next);
            }
            else if (headerIndex >= 0)
            {
                if (direction == NavigationMoveEvent.Direction.Up && headerIndex > 0)
                    to = m_Tabs[headerIndex - 1].tabHeader;
                else if (direction == NavigationMoveEvent.Direction.Down)
                    to = headerIndex < m_Tabs.Count - 1 ? m_Tabs[headerIndex + 1].tabHeader : backButton;
                else if (direction == inward)
                    FocusPage(m_Tabs[headerIndex]);
            }
            else if (from == backButton)
            {
                if (direction == NavigationMoveEvent.Direction.Up && m_Tabs.Count > 0)
                    to = m_Tabs[^1].tabHeader;
            }
            else if (focusedPage is { } page)
            {
                var slider = from as Slider ?? from.GetFirstAncestorOfType<Slider>();
                if (slider != null && (direction == NavigationMoveEvent.Direction.Left || direction == NavigationMoveEvent.Direction.Right))
                {
                    // Right is more, whichever way the page reads: the slider's track doesn't turn around.
                    var sign = direction == NavigationMoveEvent.Direction.Right ? 1f : -1f;
                    var range = slider.highValue - slider.lowValue;
                    slider.value = Mathf.Clamp(slider.value + sign * range * k_SliderStep, Mathf.Min(slider.lowValue, slider.highValue), Mathf.Max(slider.lowValue, slider.highValue));
                }
                else
                {
                    to = Nearest(Navigables(page.contentContainer), from, direction, rightToLeft);
                    if (to == null && direction == outward)
                        to = page.tabHeader;
                }
            }
            else
            {
                return;
            }

            to?.Focus();
            // Inside the window, this is all the navigation there is: the focus never wanders off to the rest of the
            // screen, or to another tab's name across the page.
            focusController.IgnoreEvent(evt);
            evt.StopPropagation();
        }

        // Tab and Shift+Tab: through the whole window in reading order, a tab's name and then its page's settings, tab
        // after tab, Back last, and round again. A tab shows as its name takes the focus, so going back into a tab's
        // page from the next tab's name waits for it to be laid out.
        void Step(VisualElement from, int headerIndex, bool forward)
        {
            if (m_Tabs.Count == 0)
                return;

            if (from == backButton)
            {
                if (forward)
                    m_Tabs[0].tabHeader.Focus();
                else
                    FocusLastSetting(m_Tabs.Count - 1);
            }
            else if (headerIndex >= 0)
            {
                var settings = Settings(m_Tabs[headerIndex]);
                if (forward && settings.Count > 0)
                    settings[0].Focus();
                else if (forward)
                    FocusTabAfter(headerIndex);
                else if (headerIndex > 0)
                    FocusLastSetting(headerIndex - 1);
                else
                    backButton?.Focus();
            }
            else if (focusedPage is { } page)
            {
                var settings = Settings(page);
                var index = settings.FindIndex(setting => setting == from || setting.Contains(from) || from.Contains(setting));
                var next = index + (forward ? 1 : -1);
                if (index >= 0 && next >= 0 && next < settings.Count)
                    settings[next].Focus();
                else if (forward)
                    FocusTabAfter(m_Tabs.IndexOf(page));
                else
                    page.tabHeader.Focus();
            }
        }

        void FocusTabAfter(int index)
        {
            if (index + 1 < m_Tabs.Count)
                m_Tabs[index + 1].tabHeader.Focus();
            else
                backButton?.Focus();
        }

        // A tab's name, and its last setting once its page is laid out.
        void FocusLastSetting(int index)
        {
            var tab = m_Tabs[index];
            tab.tabHeader.Focus();
            schedule.Execute(() =>
            {
                if (tabView.activeTab != tab || focused != tab.tabHeader)
                    return;
                var settings = Settings(tab);
                if (settings.Count > 0)
                    settings[^1].Focus();
            }).StartingIn(1);
        }

        // From a setting, back to its tab; from the tabs, on out of Settings (whoever opened them).
        void OnNavigationCancel(NavigationCancelEvent evt)
        {
            var page = focusedPage;
            if (page == null)
                return;
            page.tabHeader.Focus();
            evt.StopPropagation();
        }

        void ListenForTabActions(bool listen)
        {
            if (listen)
            {
                var actions = InputSystem.actions;
                m_PreviousTabAction = actions?.FindAction(previousTabActionName);
                m_NextTabAction = actions?.FindAction(nextTabActionName);
            }
            if (m_PreviousTabAction != null)
            {
                m_PreviousTabAction.performed -= OnPreviousTabAction;
                if (listen)
                    m_PreviousTabAction.performed += OnPreviousTabAction;
            }
            if (m_NextTabAction != null)
            {
                m_NextTabAction.performed -= OnNextTabAction;
                if (listen)
                    m_NextTabAction.performed += OnNextTabAction;
            }
        }

        // Only the window that has the focus changes tab.
        bool hasFocus => focused != null && Contains(focused) && enabledInHierarchy;

        void OnPreviousTabAction(InputAction.CallbackContext _)
        {
            if (hasFocus)
                ShowPreviousTab();
        }

        void OnNextTabAction(InputAction.CallbackContext _)
        {
            if (hasFocus)
                ShowNextTab();
        }

        // ------------------------------------------------------------------ Finding the next setting

        // What the focus can stop at, as the panel's own navigation sees it: shown, enabled, and not handing the focus on.
        static bool IsNavigable(VisualElement element)
        {
            return element != null && element.canGrabFocus && element.tabIndex >= 0 && !element.delegatesFocus &&
                element.visible && element.resolvedStyle.display != DisplayStyle.None;
        }

        static List<VisualElement> Navigables(VisualElement root)
        {
            var list = new List<VisualElement>();
            Collect(root, list);
            return list;

            static void Collect(VisualElement element, List<VisualElement> list)
            {
                if (element.resolvedStyle.display == DisplayStyle.None || !element.enabledInHierarchy)
                    return;
                if (IsNavigable(element))
                {
                    list.Add(element);
                    return;
                }
                foreach (var child in element.Children())
                    Collect(child, list);
            }
        }

        // Where an element is in the window (its layout's space, y down, wherever the window is shown).
        Rect Bounds(VisualElement element) => element.ChangeCoordinatesTo(this, new Rect(Vector2.zero, element.layout.size));

        // Top to bottom, then from the side the page reads from.
        static int ReadingOrder(Rect a, Rect b)
        {
            if (Mathf.Abs(a.yMin - b.yMin) > 1f)
                return a.yMin.CompareTo(b.yMin);
            return BagelLocalization.isRightToLeft ? b.xMax.CompareTo(a.xMax) : a.xMin.CompareTo(b.xMin);
        }

        // The nearest of the candidates in that direction: first those straight across (in the beam of the element the
        // focus is on), the closest of them, and in a row of them the one the row reads from; then any further along.
        VisualElement Nearest(List<VisualElement> candidates, VisualElement from, NavigationMoveEvent.Direction direction, bool rightToLeft)
        {
            var origin = Bounds(from);
            var vertical = direction == NavigationMoveEvent.Direction.Up || direction == NavigationMoveEvent.Direction.Down;
            var forward = direction == NavigationMoveEvent.Direction.Down || direction == NavigationMoveEvent.Direction.Right ? 1f : -1f;

            VisualElement best = null;
            var bestScore = float.MaxValue;
            foreach (var candidate in candidates)
            {
                if (candidate == from || candidate.Contains(from) || from.Contains(candidate))
                    continue;
                var rect = Bounds(candidate);

                // How far ahead, from the near edges, and how far off to the side.
                var ahead = vertical
                    ? (forward > 0f ? rect.yMin - origin.yMax : origin.yMin - rect.yMax)
                    : (forward > 0f ? rect.xMin - origin.xMax : origin.xMin - rect.xMax);
                var centerAhead = vertical ? (rect.center.y - origin.center.y) * forward : (rect.center.x - origin.center.x) * forward;
                if (centerAhead <= 1f || ahead < -Mathf.Min(vertical ? rect.height : rect.width, vertical ? origin.height : origin.width) * 0.5f)
                    continue;
                var aside = vertical
                    ? Mathf.Max(0f, Mathf.Max(rect.xMin - origin.xMax, origin.xMin - rect.xMax))
                    : Mathf.Max(0f, Mathf.Max(rect.yMin - origin.yMax, origin.yMin - rect.yMax));

                // In a row straight below or above, the one the row starts from.
                var start = vertical ? (rightToLeft ? origin.xMax - rect.xMax : rect.xMin - origin.xMin) : 0f;
                var score = Mathf.Max(0f, ahead) + (aside > 0f ? 10000f + aside * 2f : 0f) + Mathf.Abs(start) * 0.01f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }
            return best;
        }
    }
}
