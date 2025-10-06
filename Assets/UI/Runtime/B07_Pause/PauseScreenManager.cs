using System;
using UnityEngine.UIElements;

namespace Bagel
{
    [UxmlElement]
    public partial class PauseScreenManager : VisualElement
    {
        public static readonly string inactivePaneClassName = "b-inactive-pane";

        VisualElement m_PausePane;
        VisualElement m_SettingsPane;
        PausePaneManager m_PausePaneManager;
        SettingsPaneManager m_SettingsPaneManager;

        public VisualElement pausePane => m_PausePane ??= this.Q<VisualElement>("pause-pane");
        public VisualElement settingsPane => m_SettingsPane ??= this.Q<VisualElement>("settings-pane");
        public PausePaneManager pausePaneManager => m_PausePaneManager ??= this.Q<PausePaneManager>();
        public SettingsPaneManager settingsPaneManager => m_SettingsPaneManager ??= this.Q<SettingsPaneManager>();

        public PauseScreenManager()
        {
            RegisterCallbackOnce<GeometryChangedEvent>(FirstInit);
        }

        void FirstInit(GeometryChangedEvent evt)
        {
            pausePaneManager.BindUI(new PausePaneManager.Callbacks
            {
                onSettings = GoToSettingsPane
            });
            settingsPaneManager.BindUI(new SettingsPaneManager.Callbacks
            {
                onSettingsBack = GoToPausePane
            });
        }

        public void GoToPausePane()
        {
            if (pausePane == null)
                return;

            pausePane.RemoveFromClassList(inactivePaneClassName);
            settingsPane.AddToClassList(inactivePaneClassName);
        }

        public void GoToSettingsPane()
        {
            pausePane.AddToClassList(inactivePaneClassName);
            settingsPane.RemoveFromClassList(inactivePaneClassName);
        }
    }
}
