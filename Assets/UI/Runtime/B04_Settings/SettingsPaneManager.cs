using System;
using UnityEngine.UIElements;

namespace Bagel
{
    // Settings: a window of tabs (Game, Look, Graphics, Controls, About), the same one on the blackboard and over the
    // paused game. Every tab fits in the window: nothing scrolls. On the blackboard (in a b-board), changing tab wipes
    // the old one off and writes the new one in (ChalkWipe), quickly; elsewhere the tab just changes.
    [UxmlElement(libraryPath = "Bagel/Managers")]
    public partial class SettingsPaneManager : BagelPane
    {
        // The blackboard's surface (BagelCommonStyles).
        public static readonly string boardUssClassName = "b-board";

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
            if (m_Bound)
                return;
            m_Bound = true;

            // The title isn't from a template here, but it has the same name: its binding is added here.
            BagelLocalization.Bind(this.Q<Label>("page-title"), "text", "settings.title");

            if (tabView != null)
                tabView.activeTabChanged += TabView_OnActiveTabChanged;
            MainMenuPaneManager.ShowFocusOnItem(backButton);
        }

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
    }
}
